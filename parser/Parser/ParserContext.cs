using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Provides context / state whilst parsing the input.
    /// </summary>
    public class ParserContext
    {
        public ParserContext(IList<ProductionRule> productionRules, IList<Token> tokens)
        {
            this.ProductionRules = productionRules;
            this.Tokens = tokens.Clone();
            this.CurrentTokenIndex = 0;
            this.Results = new Stack<object>();
        }

        public IList<ProductionRule> ProductionRules { get; private set; }
        private IList<Token> Tokens { get; set; }

        public Token PeekToken()
        {
            return Tokens[CurrentTokenIndex];
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

        // Pointer to current token position.
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

        public Stack<object> Results { get; private set; }

        public void PushResult(object value)
        {
            Results.Push(value);
        }

        public object PopResult()
        {
            return Results.Pop();
        }

        /// <summary>
        /// Converts the property to IEnumerable if multiple symbols point to it.
        /// </summary>
        /// <param name="property"></param>
        public void ConvertResultToIEnumerable(string property)
        {
            if (property != null)
            {
                var result = Results.Peek();
                var resultAsNode = result as Node;

                // Convert top item on stack to IEnumerable
                if (result != null && property == "")
                {
                    var r = Results.Pop();
                    r = r.Union(null);
                    Results.Push(r);
                }
                else if (resultAsNode != null && resultAsNode.Properties.ContainsKey(property) && !string.IsNullOrEmpty(property))
                {
                    var r = (Node)Results.Pop();
                    r.Properties[property] = r.Properties[property].Union(null);
                    Results.Push(r);
                }
            }
        }

        /// <summary>
        /// Updates the result object. The result object can be either:
        /// 1. A Node object with properties
        /// 2. A simple value
        /// 
        /// Rules for writing the result are as follows:
        /// 1. If the symbol's property is null, no value is written (the result is ignored)
        /// 2. If the symbol's property is '' then the result is returned as-is
        /// 3. If the symbol's property !='' then the result is written to a property of a Node object.
        /// </summary>
        public void UpdateResult(object value, string property, string name)
        {
            // only update if value is set. Possible that a symbol returns true, but
            // no match (for example if the symbol is set to optional)
            if (value != null)
            {
                var result = Results.Peek();
                if (!string.IsNullOrEmpty(property))
                {
                    // Create a node object with multiple properties
                    if (result == null)
                    {
                        var n = new Node();
                        n.Name = name;
                        result = n;
                    }
                    else if (result.GetType() != typeof(Node))
                    {
                        throw new Exception("Unable to save result!");
                    }

                    var node = result as Node;
                    // convert property
                    if (node.Properties.ContainsKey(property))
                        node.Properties[property] = node.Properties[property].Union(value);
                    else
                        node.Properties[property] = value;
                }
                else if (property == "")
                {
                    if (result != null)
                    {
                        result = result.Union(value);
                    }
                    else
                    {
                        result = value;
                    }
                }
                var temp = Results.Pop();
                Results.Push(result);
            }
        }
    }
}
