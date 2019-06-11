using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// The parser rules in the grammar.
    /// </summary>
    public class ProductionRule
    {
        public ProductionRule(string name, params string[] symbols)
        {
            this.Name = name;
            this.Symbols = new List<Symbol>();
            foreach (var symbol in symbols)
            {
                this.Symbols.Add(new Symbol(symbol, this.RuleType));
            }
        }

        /// <summary>
        /// Name of the rule. Used to name nodes of the abstract syntax tree.
        /// </summary>
        public string Name { get; set; }

        public RuleType RuleType
        {
            get
            {
                if (char.IsUpper(this.Name[0]))
                    return RuleType.LexerRule;
                else
                    return RuleType.ParserRule;
            }
        }

        /// <summary>
        /// The symbols that make up this rule.
        /// </summary>
        public List<Symbol> Symbols { get; set; }

        /// <summary>
        /// Parses a set of tokens into an abstract syntax tree.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Parse(ParserContext context)
        {
            // Rule is non terminal
            foreach (var symbol in this.Symbols)
            {
                context.ConvertResultToIEnumerable(symbol.Alias);
                var once = false;
                while (true)
                {
                    if ((symbol.Optional || once) && context.TokenEOF)
                        break;
                    else if (context.TokenEOF)
                        throw new Exception("Unexpected EOF");

                    context.PushResult(null);
                    var ok = symbol.Parse(context);
                    var result = context.PopResult();
                    if (ok)
                    {
                        context.UpdateResult(result, symbol.Alias, this.Name);
                        once = true;
                    }
                    else if (once && symbol.Many)
                    {
                        // have had at least 1 success, with 'many' symbol. Still quit with success
                        break;
                    }
                    else if (symbol.Optional)
                        break;
                    else
                    {
                        // General case if ok = false
                        return false;
                    }
                    if (!symbol.Many)
                        break;
                }
            }
            // if got here, then success
            return true;
        }
    }
}
