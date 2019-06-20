using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Parser class which encapsulates a number of parsing functions to parse context-free grammars.
    /// </summary>
    public class Parser
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
            new ProductionRule("IDENTIFIER", "([a-zA-Z][a-zA-Z0-9_']+|ε)"),
            new ProductionRule("NEWLINE", "\n"),

            // Parser Rules
            new ProductionRule("alias", ":IDENTIFIER?", ":COLON"),
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
                            var identifier = ((Token)node.Properties["IDENTIFIER"]).TokenValue;
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
        }

        #endregion

        #region Public Properties / Methods

        /// <summary>
        /// WHen set to true, provides additional debugging information.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Removes direct left recursion.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        private IList<ProductionRule> RemoveDirectLeftRecursion(IList<ProductionRule> rules)
        {
            IList<ProductionRule> addedRules = new List<ProductionRule>();
            IList<ProductionRule> temp = new List<ProductionRule>();
            foreach (var item in rules)
            {
                temp.Add(item);
            }

            while (true)
            {
                bool again = false;

                foreach (var rule in temp.Where(r=>r.RuleType==RuleType.ParserRule))
                {
                    if (rule.Symbols[0].Name == rule.Name)
                    {
                        again = true;

                        var tailNonTerminal = $"{rule.Name}'";

                        // left recursive
                        // a) Get all the rules for the non-terminal
                        foreach (var item in rules.Where(r => r.Name == rule.Name))
                        {
                            // We need to create 2 sets of productions
                            if (item.Symbols[0].Name != item.Name)
                            {
                                // one for the rule
                                var s = item.Symbols.ToList();
                                s.Add(new Symbol(tailNonTerminal, RuleType.ParserRule));
                                addedRules.Add(new ProductionRule(rule.Name, s.ToArray()));
                            }
                            else
                            {
                                // and one for A' (tail)
                                var s = item.Symbols.Where(i => item.Symbols.IndexOf(i) != 0).ToList();
                                s.Add(new Symbol(tailNonTerminal, RuleType.ParserRule));
                                addedRules.Add(new ProductionRule(tailNonTerminal, s.ToArray()));
                            }
                        }

                        addedRules.Add(new ProductionRule(tailNonTerminal, "ε"));
                        temp = temp.Where(r => r.Name != rule.Name).ToList();
                        break;
                    }
                }
                if (!again)
                    break;
            }

            foreach (var item in addedRules)
            {
                temp.Add(item);
            }
            return temp;
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
                rule.Debug = this.Debug;
                ParserContext context = new ParserContext(productionRules, tokens);
                object obj = null;
                var ok = rule.Parse(context, out obj);
                if (ok && context.TokenEOF)
                {
                    return (Node)obj;
                }
            }

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
