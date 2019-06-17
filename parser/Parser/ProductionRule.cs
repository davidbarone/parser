using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Specifies a single rule in the grammar..
    /// </summary>
    public class ProductionRule
    {
        public ProductionRule(string name, params string[] symbols)
        {
            this.Name = name;
            this.Symbols = new List<Symbol>();
            symbols.ToList().ForEach(s => {
                var symbol = new Symbol(s, this.RuleType);
                this.Symbols.Add(symbol);
            });
        }

        public bool Debug { get; set; }

        /// <summary>
        /// Name of the rule. Used to name nodes of the abstract syntax tree.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Rule Type. If the first character of the rule is upper case, it is defined
        /// as a lexer rule. Otherwise, it is a parser rule.
        /// </summary>
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
        /// Returns true if a symbol exists more than once, or the symbol is of type 'many'.
        /// Such symbols use an IEnumerable in the tree to represent the members.
        /// </summary>
        /// <param name="alias">The alias used in the production rule.</param>
        /// <returns></returns>
        public bool IsEnumeratedSymbol(string alias)
        {
            var isList = false;
            var found = false;

            var symbols = Symbols.Where(s => s.Alias == alias);

            if (symbols.Count() >= 1)
            {
                found = true;
                if (symbols.Count() > 1)
                    isList = true;
                else
                {
                    var symbol = symbols.First();
                    if (symbol.Many)
                        isList = true;
                }
            }

            if (!found)
            {
                throw new Exception($"Symbol {alias} does not exist in production rule {this.Name}.");
            }

            return isList;
        }

        /// <summary>
        /// Parses a set of tokens into an abstract syntax tree.
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <returns>The return object contains a portion of the tree (object / node) parsed by this production rule alone.</returns>
        public bool Parse(ParserContext context, out object obj)
        {
            foreach (var symbol in this.Symbols)
            {
                symbol.Debug = this.Debug;
            }

            context.CurrentProductionRule.Push(this);
            context.PushResult(GetResultObject());
            var temp = context.CurrentTokenIndex;

            bool success = true;

            if (Debug)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"-----------------------------------------------------------");
                Console.WriteLine($"[ProductionRule.Parse()] {this.Name} - Pushing new result to stack.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // Rule is non terminal
            foreach (var symbol in this.Symbols)
            {
                symbol.Debug = this.Debug;
                if (symbol.Optional && context.TokenEOF)
                    break;
                else if (context.TokenEOF)
                    throw new Exception("Unexpected EOF");

                var ok = symbol.Parse(context);

                if (symbol.Optional || ok) { }
                else
                {
                    // General case if ok = false
                    success = false;
                    break;
                }
            }

            obj = context.PopResult();
            context.CurrentProductionRule.Pop();

            if (success)
            {
                if (Debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[ProductionRule.Parse()] Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}: success");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                return true;
            }
            else
            {
                if (Debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[ProductionRule.Parse()] Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}: failure");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                context.CurrentTokenIndex = temp;
                obj = null;
                return false;
            }
        }

        /// <summary>
        /// Goes through all symbols in production rule, creating the appropriate
        /// result object:
        /// 1. if all symbols have aliases, create a node object
        /// 1.1 If any symbols are in a list, create IEnumerable property
        /// 2. if all symbols have no aliases, and single, create Object
        /// 2.1 if all symbols have no aliass, and multiple, create IEnumerable of Object.
        /// </summary>
        /// <param name="context"></param>
        private object GetResultObject()
        {
            bool hasBlankAlias = false;
            bool hasNonBlankAlias = false;
            object ret = null;

            // Get all the aliases
            foreach (var alias in this.Symbols.Select(s => s.Alias).Distinct())
            {
                if (!string.IsNullOrEmpty(alias))
                {
                    hasNonBlankAlias = true;

                    if (ret == null)
                        ret = new Node(this.Name);

                    if (IsEnumeratedSymbol(alias))
                    {
                        Node retAsNode = ret as Node;
                        retAsNode.Properties[alias] = new List<object>();
                    }
                }
                else
                {
                    hasNonBlankAlias = false;
                    if (IsEnumeratedSymbol(alias))
                    {
                        ret = new List<object>();
                    }
                }
            }
            if (hasNonBlankAlias && hasBlankAlias)
                throw new Exception("Cannot mix blank and non-blank aliases.");

            return ret;
        }
    }
}
