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
        private string Grammar { get; set; }
        private IList<ProductionRule> productionRules { get; set; }
        private List<string> IgnoreTokens { get; set; }

        #region BNF-ish Grammar + Visitor

        /// <summary>
        /// Production rules to describe the BNF-ish syntax.
        /// </summary>
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
            new ProductionRule("IDENTIFIER", "[a-zA-Z][a-zA-Z0-9_]+"),
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
        /// Visitor to process the BNF-ish tree.
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
        /// <param name="ignoreTokens">An optional list of token names to exclude from the tokeniser and parser.</param>
        public Parser(IList<ProductionRule> grammar, params string[] ignoreTokens)
        {
            this.productionRules = grammar;
            this.IgnoreTokens = new List<string>();
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }
        }

        /// <summary>
        /// Creates a new Parser object using BNF-ish grammar.
        /// </summary>
        /// <param name="grammar">The BNF-ish grammar.</param>
        /// <param name="ignoreTokens">An optional list of token names to exclude from the tokeniser and parser.</param>
        public Parser(string grammar, params string[] ignoreTokens)
        {
            this.Grammar = grammar;
            this.IgnoreTokens = new List<string>();
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }
        }

        #endregion

        #region Public Properties / Methods

        /// <summary>
        /// WHen set to true, provides additional debugging information.
        /// </summary>
        public bool Debug { get; set; }

        private IList<ProductionRule> _productionRules = null;

        /// <summary>
        /// Returns the production rules list.
        /// </summary>
        public IList<ProductionRule> ProductionRules
        {
            get
            {
                if (string.IsNullOrEmpty(this.Grammar) && this.productionRules == null)
                {
                    throw new Exception("grammar specification is empty.");
                }

                if (_productionRules != null)
                    return _productionRules;
                else if (!string.IsNullOrEmpty(this.Grammar))
                {
                    Parser parser = new Parser(this.BNFGrammar, "COMMENT", "NEWLINE");
                    var tokens = parser.Tokenise(this.Grammar);
                    var ast = parser.Parse(this.Grammar, "grammar");
                    _productionRules = (IList<ProductionRule>)parser.Execute(ast, BNFVisitor, (d)=>d.ProductionRules);
                    return _productionRules;
                }
                else
                {
                    _productionRules = new List<ProductionRule>();
                    foreach (var pr in this.productionRules)
                    {
                        pr.Debug = this.Debug;
                        _productionRules.Add(pr);
                    }
                    return _productionRules;
                }
            }
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
            foreach (var rule in ProductionRules.Where(p => p.RuleType == RuleType.LexerRule))
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
        public Node Parse(string input, string rootProductionRule, bool throwOnFailure = true)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var tokens = this.Tokenise(input);

            if (tokens == null || tokens.Count() == 0)
                throw new Exception("input yields no tokens!");

            // find any matching production rules.
            var rules = ProductionRules.Where(p => rootProductionRule == null || p.Name.Equals(rootProductionRule, StringComparison.OrdinalIgnoreCase));
            if (!rules.Any())
                throw new Exception(string.Format("Production rule: {0} not found.", rootProductionRule));

            // try each rule. Use the first rule which succeeds.
            foreach (var rule in rules)
            {
                rule.Debug = this.Debug;
                ParserContext context = new ParserContext(ProductionRules, tokens);
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
    }

    #endregion

}
