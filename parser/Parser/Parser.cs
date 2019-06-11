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
    /// Class which encapsulates a number of parsing functions to parse context-free grammars.
    /// </summary>
    public class Parser
    {
        public bool Debug { get; set; }
        /// <summary>
        /// Can describe the production rules as EBNF grammar
        /// </summary>
        private string Grammar { get; set; }

        /// <summary>
        /// Or can describe the rules directly.
        /// </summary>
        private IList<ProductionRule> productionRules { get; set; }

        /// <summary>
        /// Gets the production rules either directly, or by evaluating the EBNF grammar.
        /// </summary>
        public IList<ProductionRule> ProductionRules
        {
            get
            {
                if (string.IsNullOrEmpty(this.Grammar) && this.productionRules == null)
                {
                    throw new Exception("grammar specification is empty.");
                }

                else if (!string.IsNullOrEmpty(this.Grammar))
                {
                    Parser parser = new Parser(this.BNFGrammar, "COMMENT", "NEWLINE");
                    parser.Debug = this.Debug;
                    var tokens = parser.Tokenise(this.Grammar);
                    var ast = parser.Parse(this.Grammar, "grammar");
                    return (IList<ProductionRule>)parser.Execute(ast, BNFVisitor, (d)=>d.ProductionRules);
                }
                else
                {
                    List<ProductionRule> rules = new List<ProductionRule>();
                    foreach (var pr in this.productionRules)
                    {
                        pr.Debug = this.Debug;
                        rules.Add(pr);
                    }
                    return rules;
                }
            }
        }

        private List<string> IgnoreTokens { get; set; }

        private List<ProductionRule> BNFGrammar => new List<ProductionRule>
        {
            // Lexer Rules
            new ProductionRule("COMMENT", @"\(\*.*\*\)"), // (*...*)
            new ProductionRule("EQ", "="),                  // definition
            new ProductionRule("COMMA", "[,]"),               // concatenation
            new ProductionRule("SEMICOLON", ";"),           // termination
            new ProductionRule("OR", @"[|]"),                 // alternation
            new ProductionRule("QUOTEDLITERAL", @"""(?:[^""\\]|\\.)*"""),
            new ProductionRule("IDENTIFIER", "[a-zA-Z][a-zA-Z0-9_]+"),
            new ProductionRule("NEWLINE", "\n"),

            // Parser Rules
            new ProductionRule("parserSymbolTerm", ":IDENTIFIER"),
            new ProductionRule("parserSymbolFactor", "COMMA!", ":IDENTIFIER"),
            new ProductionRule("parserSymbolExpr", ":parserSymbolTerm", ":parserSymbolFactor*"),
            new ProductionRule("parserSymbolsFactor", "OR!", ":parserSymbolExpr"),
            new ProductionRule("parserSymbolsExpr", ":parserSymbolExpr", ":parserSymbolsFactor*"),

            new ProductionRule("rule", "RULE:IDENTIFIER", "EQ!", "EXPANSION:QUOTEDLITERAL", "SEMICOLON!"),      // Lexer rule
            new ProductionRule("rule", "RULE:IDENTIFIER", "EQ!", "EXPANSION:parserSymbolsExpr", "SEMICOLON!"),  // Parser rule
            new ProductionRule("grammar", "RULES:rule+")
        };

        private Visitor BNFVisitor
        {
            get
            {
                // Initial state
                dynamic state = new ExpandoObject();
                state.ProductionRules = new List<ProductionRule>();

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
                        if (n.Properties.ContainsKey("QUOTEDLITERAL"))
                        {
                            // Terminal (Lexer) rule
                            ProductionRule rule = new ProductionRule(
                                ((Token)n.Properties["IDENTIFIER"]).TokenValue,
                                ((Token)n.Properties["QUOTEDLITERAL"]).TokenValue
                            );
                            v.State.ProductionRules.Add(rule);
                        }
                        else
                        {
                            // Non-terminal rule
                            ProductionRule rule = new ProductionRule(
                                ((Token)n.Properties["IDENTIFIER"]).TokenValue,
                                ((Token)n.Properties["QUOTEDLITERAL"]).TokenValue
                            );
                        }
                    });

                return visitor;
            }
        }

        public Parser(IList<ProductionRule> grammar, params string[] ignoreTokens)
        {
            this.productionRules = grammar;
            this.IgnoreTokens = new List<string>();
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }
        }

        public Parser(string grammar, params string[] ignoreTokens)
        {
            this.Grammar = grammar;
            this.IgnoreTokens = new List<string>();
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }
        }


        /// <summary>
        /// Takes a string as input, and outputs a set of tokens according to the specified grammar.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IList<Token> Tokenise(string input)
        {
            // Base case
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
            throw new Exception(string.Format("Syntax error near {0}...", input));
        }

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
        /// Parses an abstract syntax tree using a set of visitors.
        /// </summary>GRAMMAR
        /// <typeparam name="TState"></typeparam>
        /// <param name="node"></param>
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
}
