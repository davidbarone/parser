using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    /// <summary>
    /// Parser class which encapsulates a number of parsing functions to parse context-free grammars.
    /// </summary>
    public class Parser : ILoggable
    {
        /// <summary>
        /// External specification of the grammar.
        /// </summary>
        private string Grammar { get; set; }

        /// <summary>
        /// Starting non-terminal rule for grammar.
        /// </summary>
        private string RootProductionRule { get; set; }
        
        /// <summary>
        /// Internal representation of the grammar.
        /// </summary>
        private IList<ProductionRule> productionRules { get; set; }

        public IList<ProductionRule> ProductionRules => productionRules;

        /// <summary>
        /// List of tokens to be ignored by tokeniser. Typically comment tokens.
        /// </summary>
        private List<string> IgnoreTokens { get; set; }

        #region BNF-ish Grammar + Visitor

        /// <summary>
        /// Production rules to describe the BNFish syntax.
        /// </summary>
        /// <remarks>
        /// This list of production rules is used to convert BNFish grammar into a set of production rule objects.
        /// </remarks>
        private List<ProductionRule> BNFGrammar => new List<ProductionRule>
        {
            // Lexer Rules
            new ProductionRule("COMMENT", @"\(\*.*\*\)"), // (*...*)
            new ProductionRule("EQ", "="),                  // definition
            new ProductionRule("COMMA", "[,]"),               // concatenation
            new ProductionRule("COLON", "[:]"),               // rewrite / aliasing
            new ProductionRule("SEMICOLON", ";"),           // termination
            new ProductionRule("MODIFIER", "[?!+*]"),      // modifies the symbol
            new ProductionRule("OR", @"[|]"),                 // alternation
            new ProductionRule("QUOTEDLITERAL", @"""(?:[^""\\]|\\.)*"""),
            new ProductionRule("IDENTIFIER", "[a-zA-Z][a-zA-Z0-9_']+"),
            new ProductionRule("NEWLINE", "\n"),
            new ProductionRule("LPAREN", @"\("),
            new ProductionRule("RPAREN", @"\)"),

            // Parser Rules
            new ProductionRule("alias", ":IDENTIFIER?", ":COLON"),
            new ProductionRule("subrule", "LPAREN!", ":parserSymbolsExpr", "RPAREN!"),
            new ProductionRule("symbol", "ALIAS:alias?", "SUBRULE:subrule", "MODIFIER:MODIFIER?"),
            new ProductionRule("symbol", "ALIAS:alias?", "IDENTIFIER:IDENTIFIER", "MODIFIER:MODIFIER?"),
            new ProductionRule("parserSymbolTerm", ":symbol"),
            new ProductionRule("parserSymbolFactor", "COMMA!", ":symbol"),
            new ProductionRule("parserSymbolExpr", "SYMBOL:parserSymbolTerm", "SYMBOL:parserSymbolFactor*"),
            new ProductionRule("parserSymbolsFactor", "OR!", ":parserSymbolExpr"),
            new ProductionRule("parserSymbolsExpr", "ALTERNATE:parserSymbolExpr", "ALTERNATE:parserSymbolsFactor*"),

            new ProductionRule("rule", "RULE:IDENTIFIER", "EQ!", "EXPANSION:QUOTEDLITERAL", "SEMICOLON!"),      // Lexer rule
            new ProductionRule("rule", "RULE:IDENTIFIER", "EQ!", "EXPANSION:parserSymbolsExpr", "SEMICOLON!"),  // Parser rule
            new ProductionRule("grammar", "RULES:rule+")
        };

        /// <summary>
        /// Visitor to process the BNFish tree, converting BNFish into a list of ProductionRule objects.
        /// </summary>
        private Visitor BNFVisitor
        {
            get
            {
                // Initial state
                dynamic state = new ExpandoObject();
                state.ProductionRules = new List<ProductionRule>();
                state.CurrentRule = "";
                state.SubRules = 0;
                var visitor = new Visitor(state);

                visitor.AddVisitor(
                    "grammar",
                    (v, n) =>
                    {
                        foreach (var node in ((IEnumerable<object>)n.Properties["RULES"]))
                        {
                            ((Node)node).Accept(v);
                        }
                    });

                visitor.AddVisitor(
                    "rule",
                    (v, n) =>
                    {
                        var rule = ((Token)n.Properties["RULE"]).TokenValue;
                        var expansion = ((object)n.Properties["EXPANSION"]);
                        var expansionAsToken = expansion as Token;

                        // for lexer rules (terminal nodes), the expansion is a single token
                        // for lexer rules (non terminal nodes), the expansion is a set of identifiers
                        if (expansionAsToken != null)
                        {
                            // Lexer Rule
                            var expansionValue = expansionAsToken.TokenValue;
                            if (expansionValue[0] == '"' && expansionValue[expansionValue.Length - 1] == '"')
                            {
                                // remove start / ending "
                                expansionValue = expansionValue.Substring(1, expansionValue.Length - 2);
                            }

                            ProductionRule pr = new ProductionRule(
                                rule,
                                expansionValue
                            );
                            v.State.ProductionRules.Add(pr);
                        }
                        else
                        {
                            v.State.CurrentRule = rule;
                            var expansionNode = expansion as Node;
                            expansionNode.Accept(v);
                        }
                    });

                visitor.AddVisitor(
                    "parserSymbolsExpr",
                    (v, n) =>
                    {
                        // each alternate contains a separate list of tokens.
                        foreach (var node in ((IEnumerable<Object>)n.Properties["ALTERNATE"]))
                        {
                            ((Node)node).Accept(v);
                        }
                    });

                visitor.AddVisitor(
                    "parserSymbolExpr",
                    (v, n) =>
                    {
                        List<string> tokens = new List<string>();
                        foreach (var symbol in ((IEnumerable<object>)n.Properties["SYMBOL"]))
                        {
                            var node = symbol as Node;
                            // Unpack components
                            var aliasList = node.Properties.ContainsKey("ALIAS") ? node.Properties["ALIAS"] as IEnumerable<object> : null;

                            // A symbol can be either an identifier or a subrule
                            string identifier = "";
                            if (node.Properties.ContainsKey("IDENTIFIER"))
                            {
                                // Identifier
                                identifier = ((Token)node.Properties["IDENTIFIER"]).TokenValue;
                            } else if (node.Properties.ContainsKey("SUBRULE"))
                            {
                                // for subrules, the subrule is parsed and added as a
                                // new production, and the subrule is replaced with the
                                // autogenerated name of the subrule.
                                identifier = $"anonymous_{v.State.SubRules++}";
                                var temp = v.State.CurrentRule;
                                v.State.CurrentRule = identifier;
                                var subrule = (Node)node.Properties["SUBRULE"];
                                subrule.Accept(v);
                                v.State.CurrentRule = temp;
                            }
                            var modifierToken = node.Properties.ContainsKey("MODIFIER") ? node.Properties["MODIFIER"] as Token : null;
                            var alias = "";
                            if (aliasList != null)
                            {
                                alias = string.Join("", aliasList.Select(a => ((Token)a).TokenValue));
                            }
                            var modifier = (modifierToken != null) ? modifierToken.TokenValue : "";
                            tokens.Add($"{alias}{identifier}{modifier}");
                        }

                        ProductionRule pr = new ProductionRule(
                            v.State.CurrentRule,
                            tokens.ToArray()
                        );
                        v.State.ProductionRules.Add(pr);
                    });

                return visitor;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Parser object using a list of production rules.
        /// </summary>
        /// <param name="grammar">The list of production rules defining the grammar.</param>
        /// <param name="rootProductionRule">The root production rule to start parsing.</param>
        /// <param name="ignoreTokens">An optional list of token names to exclude from the tokeniser and parser.</param>
        private Parser(IList<ProductionRule> grammar, string rootProductionRule, params string[] ignoreTokens)
        {
            this.productionRules = grammar;
            this.IgnoreTokens = new List<string>();
            this.RootProductionRule = rootProductionRule;
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }

            this.productionRules = RemoveDirectLeftRecursion(this.productionRules);
            this.productionRules = EliminateEmptyProduction(this.ProductionRules);
        }

        /// <summary>
        /// Creates a new Parser object using BNF-ish grammar.
        /// </summary>
        /// <param name="grammar">The BNF-ish grammar.</param>
        /// <param name="rootProductionRule">The root production rule to start parsing.</param>
        /// <param name="ignoreTokens">An optional list of token names to exclude from the tokeniser and parser.</param>
        public Parser(string grammar, string rootProductionRule, params string[] ignoreTokens)
        {
            this.IgnoreTokens = new List<string>();
            this.RootProductionRule = rootProductionRule;
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }

            Parser parser = new Parser(this.BNFGrammar, "grammar", "COMMENT", "NEWLINE");
            var tokens = parser.Tokenise(grammar);
            var ast = parser.Parse(grammar);
            productionRules = (IList<ProductionRule>)parser.Execute(ast, BNFVisitor, (d) => d.ProductionRules);
            productionRules = RemoveDirectLeftRecursion(productionRules);
            this.productionRules = EliminateEmptyProduction(this.ProductionRules);
        }

        #endregion

        #region Public Properties / Methods

        /// <summary>
        /// Optional logger to get Parser information.
        /// </summary>
        public Action<object, LogArgs> LogHandler { get; set; }

        /// <summary>
        /// Removes direct left recursion.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        private IList<ProductionRule> RemoveDirectLeftRecursion(IList<ProductionRule> rules)
        {
            List<ProductionRule> output = new List<ProductionRule>();
            var ruleGroups = rules.GroupBy(r => r.Name);
            
            foreach (var ruleGroup in ruleGroups)
            {
                if (ruleGroup.Count()==1 && ruleGroup.First().RuleType == RuleType.LexerRule)
                    output.Add(ruleGroup.First());
                else if (!ruleGroup.Any(r=>r.Symbols[0].Name == r.Name))
                {
                    foreach (var rule in ruleGroup)
                        output.Add(rule);
                } else
                {
                    // left recursive
                    var tailNonTerminal = $"{ruleGroup.Key}'";

                    // Get all the rules for the non-terminal
                    // and create 2 sets of new productions to
                    // eliminate left recursion.
                    foreach (var rule in ruleGroup)
                    {
                        if (rule.Symbols[0].Name != rule.Name)
                        {
                            var s = rule.Symbols.ToList();
                            s.Add(new Symbol(tailNonTerminal, RuleType.ParserRule));
                            output.Add(new ProductionRule(rule.Name, s.ToArray()));
                        }
                        else
                        {
                            var s = rule.Symbols.Where(i => rule.Symbols.IndexOf(i) > 0).ToList();
                            s.Add(new Symbol(tailNonTerminal, RuleType.ParserRule));
                            output.Add(new ProductionRule(tailNonTerminal, s.ToArray()));
                            output.Add(new ProductionRule(tailNonTerminal, "ε"));
                        }
                    }
                }
            }
            return output;
        }

        private List<ProductionRule> EliminateEmptyProduction(IList<ProductionRule> rules)
        {
            List<ProductionRule> additionalRules = new List<ProductionRule>();
            List<ProductionRule> output = new List<ProductionRule>();

            var rulesWithEmpty = rules
                .GroupBy(r => r.Name)
                .Where(rg => rg.Any(pr => pr.Symbols.Count() == 1 && pr.Symbols.First().Name == "ε"))
                .Select(rg => rg.Key);

            // for each production rule that has empty / nullable option,
            // search through all production rules and where you find
            // that production rule as a symbol in another rule, create
            // a copy of that production rule without the symbol

            foreach (var rule in rules)
            {
                if (rule.Symbols.Any(s=> rulesWithEmpty.Contains(s.Name)))
                {
                    var nonNullableSymbols = rule.Symbols.Where(s => !rulesWithEmpty.Contains(s.Name));
                    additionalRules.Add(new ProductionRule(rule.Name, nonNullableSymbols.ToArray()));
                }
            }

            foreach (var rule in rules.Where(r=>!(r.Symbols.Count()==1 && r.Symbols[0].Name== "ε")))
                output.Add(rule);
            foreach (var rule in additionalRules)
                output.Add(rule);
            return output;
        }

        /// <summary>
        /// Takes a string input, and outputs a set of tokens according to the specified grammar.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IList<Token> Tokenise(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new List<Token>() { };

            // Start at the beginning of the string and
            // recursively identify tokens. First token to match wins
            foreach (var rule in productionRules.Where(p => p.RuleType == RuleType.LexerRule))
            {
                var symbols = rule.Symbols;
                if (symbols.Count() > 1)
                    throw new Exception("Lexer rule can only have 1 symbol");

                var symbol = symbols[0];

                if (symbol.IsMatch((input)))
                {
                    var match = symbol.Match(input);
                    var token = new Token()
                    {
                        TokenName = rule.Name,
                        TokenValue = match.Matched
                    };
                    var list = new List<Token>();
                    if (!this.IgnoreTokens.Contains(rule.Name))
                    {
                        list.Add(token);
                    }
                    list.AddRange(Tokenise(match.Remainder));
                    return list;
                }
            }
            throw new Exception(string.Format("Syntax error near {0}...", input.Substring(0, 20)));
        }

        /// <summary>
        /// Parses a string input into an abstract syntax tree.
        /// </summary>
        /// <param name="input">The input to parse.</param>
        /// <param name="rootProductionRule">The starting / root production rule which defines the grammar.</param>
        /// <param name="throwOnFailure">When set to true, the method throws an error on failure. Otherwise, the method simply returns a null result.</param>
        /// <returns></returns>
        public Node Parse(string input, bool throwOnFailure = true)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var tokens = this.Tokenise(input);

            if (tokens == null || tokens.Count() == 0)
                throw new Exception("input yields no tokens!");

            // find any matching production rules.
            var rules = productionRules.Where(p => this.RootProductionRule == null || p.Name.Equals(this.RootProductionRule, StringComparison.OrdinalIgnoreCase));
            if (!rules.Any())
                throw new Exception(string.Format("Production rule: {0} not found.", this.RootProductionRule));

            // try each rule. Use the first rule which succeeds.
            foreach (var rule in rules)
            {
                rule.LogHandler = this.LogHandler;
                ParserContext context = new ParserContext(productionRules, tokens);
                object obj = null;
                var ok = rule.Parse(context, out obj);
                if (ok && context.TokenEOF)
                {
                    return (Node)obj;
                }
            }

            // should not get here...
            if (throwOnFailure)
                throw new Exception("Input cannot be parsed.");
            else
                return null;
        }

        /// <summary>
        /// Navigates an abstract syntax tree using a set of visitors.
        /// </summary>
        /// <param name="node">The (root) node to at the top of the tree to navigate.</param>
        /// <param name="visitors">The Visitor object to use to navigate the tree.</param>
        /// <param name="resultMapping">An optional function to map the final state of the visitor into the desired result. If not set, then returns the state.</param>
        /// <returns></returns>
        public object Execute(Node node, Visitor visitors, Func<dynamic, object> resultMapping = null)
        {
            if (node == null)
                return null;

            node.Accept(visitors);
            var state = visitors.State;
            if (resultMapping == null)
                return state;
            else
                return resultMapping(state);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, productionRules);
        }
    }

    #endregion

}
