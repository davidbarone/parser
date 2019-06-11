using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    public class Parser
    {
        private IList<ProductionRule> ProductionRules { get; set; }
        private List<string> IgnoreTokens { get; set; }

        public Parser(IList<ProductionRule> grammar, params string[] ignoreTokens)
        {
            this.ProductionRules = grammar;
            this.IgnoreTokens = new List<string>();
            foreach (var token in ignoreTokens)
            {
                this.IgnoreTokens.Add(token);
            }
        }

        public IList<Token> Tokenise(string input)
        {
            // Base case
            if (string.IsNullOrEmpty(input))
                return new List<Token>() { };

            // Start at the beginning of the string and
            // recursively id
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
                throw new Exception("input yields not tokens!");

            // find any matching production rules.
            var rules = ProductionRules.Where(p => rootProductionRule == null || p.Name.Equals(rootProductionRule, StringComparison.OrdinalIgnoreCase));
            if (!rules.Any())
                throw new Exception(string.Format("Production rule: {0} not found.", rootProductionRule));

            // try each rule. Use the first rule which succeeds.
            foreach (var rule in rules)
            {
                ParserContext context = new ParserContext(ProductionRules, tokens);
                context.PushResult(null);
                var ok = rule.Parse(context);
                var result = context.PopResult();
                if (ok && context.TokenEOF)
                {
                    return (Node)result;
                }
            }

            if (throwOnFailure)
                throw new Exception("Input cannot be parsed.");
            else
                return null;
        }

        /// <summary>
        /// Parses an abstract syntax tree using a set of visitors.
        /// </summary>
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
