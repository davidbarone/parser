using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Provides context during the parsing process.
    /// </summary>
    public class ParserContext
    {
        /// <summary>
        /// Creates a new ParserContext object.
        /// </summary>
        /// <param name="productionRules">The list of production rules.</param>
        /// <param name="tokens">The tokenised input to parse.</param>
        public ParserContext(IList<ProductionRule> productionRules, IList<Token> tokens)
        {
            this.ProductionRules = productionRules;
            this.Tokens = tokens.Clone();
            this.CurrentTokenIndex = 0;
            this.Results = new Stack<object>();
            this.CurrentProductionRule = new Stack<ProductionRule>();
        }

        public IList<ProductionRule> ProductionRules { get; private set; }
        public Stack<ProductionRule> CurrentProductionRule { get; set; }
        private IList<Token> Tokens { get; set; }
        public Stack<object> Results { get; private set; }

        public Token PeekToken()
        {
            return Tokens[CurrentTokenIndex];
        }

        public void PushResult(object value)
        {
            Results.Push(value);
        }

        public object PopResult()
        {
            return Results.Pop();
        }

        /// <summary>
        /// Returns true if past the end of the token list.
        /// </summary>
        /// <returns></returns>
        public bool TokenEOF
        {
            get
            {
                return CurrentTokenIndex == Tokens.Count();

            }
        }

        /// <summary>
        /// Pointer to current token position.
        /// </summary>
        public int CurrentTokenIndex { get; set; }

        /// <summary>
        /// Attempts to get the next token. If the next TokenName matches
        /// the tokenName parameter, the token is returned and the position
        /// is advanced by 1. Otherwise, returns null. Exception throw if
        /// EOF reached.
        /// </summary>
        /// <returns></returns>
        public Token TryToken(string tokenName)
        {
            if (CurrentTokenIndex >= Tokens.Count)
                throw new Exception("End of file.");
            if (tokenName.Equals(Tokens[CurrentTokenIndex].TokenName, StringComparison.OrdinalIgnoreCase))
            {
                var token = Tokens[CurrentTokenIndex];
                CurrentTokenIndex++;
                return token;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Helper method to construct the tree. Updates the result object(s) in the context.
        /// </summary>
        public void UpdateResult(string name, object value)
        {
            // only update if value is set. Possible that a symbol returns true, but
            // no match (for example if the symbol is set to optional)
            if (value != null)
            {
                var result = Results.Peek();
                var resultAsNode = result as Node;

                var productionRule = this.CurrentProductionRule.Peek();
                var isEnumerated = productionRule.IsEnumeratedSymbol(name);

                if (!string.IsNullOrEmpty(name))
                {
                    if (isEnumerated)
                        ((IList<object>)resultAsNode.Properties[name]).Add(value);
                    else
                        resultAsNode.Properties[name] = value;
                }
                else
                {
                    if (isEnumerated)
                        ((IList<object>)result).Add(value);
                    else
                    {
                        Results.Pop();
                        Results.Push(value);
                    }
                }
            }
        }
    }
}
