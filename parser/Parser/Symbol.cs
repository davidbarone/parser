using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Defines a symbol in a production rule. A symbol can be a terminal (lexer / terminal symbol)
    /// or another production rule (non terminal)
    /// </summary>
    public class Symbol
    {
        // The name of the symbol
        public string Name { get; set; }

        /// <summary>
        /// The alias name of the symbol when parsed into the AST.
        /// 1. If null then the value is ignored in the AST.
        /// 2. If = '' then value becomes a node
        /// 3. If <> '' then value becomes a property of a node
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Set to true if the symbol is optional in the syntax.
        /// </summary>
        public bool Optional { get; set; }

        public bool Many { get; set; }

        public bool Ignore { get; set; }

        public Symbol(string value, RuleType ruleType)
        {
            string name = value;
            string alias = null;
            string modifier = null;

            if (ruleType == RuleType.ParserRule)
            {
                var equalsPos = value.IndexOf('=');
                if (equalsPos >= 0)
                {
                    alias = value.Substring(0, equalsPos).Trim();
                    name = value.Substring(equalsPos + 1, value.Count() - equalsPos - 1).Trim();
                }

                var modifiers = new char[] { '+', '*', '?', '!' };

                if (modifiers.Contains(value.Last()))
                {
                    modifier = name.Substring(name.Length - 1, 1).First().ToString();
                    name = name.Substring(0, name.Length - 1);
                }
            }

            this.Alias = alias;
            this.Name = name;
            this.Optional = modifier == "?" || modifier == "*";
            this.Many = modifier == "+" || modifier == "*";
            this.Ignore = modifier == "!";
        }

        public MatchResult Match(string input)
        {
            var pat = string.Format(@"^\s*(?<match>({0}))(?<remainder>(.*))\s*$", Name);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var match = re.Match(input);
            return new MatchResult
            {
                Success = match.Success,
                Matched = match.Groups["match"].Value,
                Remainder = match.Groups["remainder"].Value
            };
        }

        public bool IsMatch(string input)
        {
            var pat = string.Format(@"^\s*(?<match>({0}))(?<remainder>(.*))\s*$", Name);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return re.IsMatch(input);
        }

        /// <summary>
        /// Checks whether the current symbol matches, and can read the input.
        /// If successful, the successful input is returned in the context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Parse(ParserContext context)
        {
            /*
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Symbol={0}, Alias={1} Token={2},{3},{4}, Results={5}", this.Name, this.Alias, context.CurrentTokenIndex, context.PeekToken().TokenName, context.PeekToken().TokenValue, context.Results.Count());
            Console.ForegroundColor = ConsoleColor.Gray;
            */

            // save token position
            int temp = context.CurrentTokenIndex;
            bool ok = false;
            var once = false;
            while (true && !context.TokenEOF)
            {
                ok = this.ParseHandler(context);
                // wind back the token index if the symbol did not match tokens.
                //Console.WriteLine(string.Format("OK = {0}", ok));
                if (ok)
                    once = true;
                else
                {
                    if (!Many)
                        context.CurrentTokenIndex = temp;

                    break;
                }
                if (!Many)
                    break;
            }

            // return true if match (at least once).
            return ok || once;
        }

        /// <summary>
        /// Take the token list and attempts to parse.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="productionRules"></param>
        /// <returns>Either returns a node object (which is a child), or the actual value which can be added to the parent.</returns>
        public bool ParseHandler(ParserContext context)
        {
            // Is the symbol a simple token?
            var token = context.TryToken(this.Name);

            if (token != null)
            {
                context.UpdateResult(token, this.Alias, Name);
                return true;
            }
            // check to see if the symbol a pointer to another production rule?
            else
            {
                var rules = context.ProductionRules.Where(r => r.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
                if (!rules.Any())
                    return false;
                else
                {
                    foreach (var rule in rules)
                    {
                        var ok = rule.Parse(context);
                        if (ok)
                            return true;
                    }
                    return false;
                }
            }
        }
    }
}
