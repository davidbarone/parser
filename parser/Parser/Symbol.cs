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
    /// or another production rule (non terminal).
    /// </summary>
    public class Symbol
    {
        public Symbol(string value, RuleType ruleType)
        {
            string name = value;
            string modifier = null;
            string[] parts = null;

            if (ruleType == RuleType.ParserRule)
            {
                // Check for rewrite rule (only parser rules)
                parts = name.Split(new string[] { ":" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    this.Alias = parts[0];
                    name = parts[1];
                }

                // modifiers
                var modifiers = new char[] { '+', '*', '?', '!' };

                if (modifiers.Contains(name.Last()))
                {
                    modifier = name.Substring(name.Length - 1, 1).First().ToString();
                    name = name.Substring(0, name.Length - 1);
                }
            }

            this.Name = name;
            if (parts == null || parts.Length == 1)
                this.Alias = this.Name;

            this.Optional = modifier == "?" || modifier == "*";
            this.Many = modifier == "+" || modifier == "*";
            this.Ignore = modifier == "!";
        }

        /// <summary>
        /// Optional alias. If not set, then equivalent to the Name property. Used to name child properties in the abstract syntax tree.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Set to true to provide additional debug information.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Name of the symbol.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Set to true if the symbol is optional in the syntax.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Set to true if symbol allows multiple values (list).
        /// </summary>
        public bool Many { get; set; }

        /// <summary>
        /// Set to true if symbol to be ignored in the abstract syntax tree.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Matches symbol to input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public MatchResult Match(string input)
        {
            var pat = string.Format(@"\A[\s]*(?<match>({0}))(?<remainder>([\s\S]*))[\s]*\Z", Name);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = re.Match(input);
            return new MatchResult
            {
                Success = match.Success,
                Matched = match.Groups["match"].Value,
                Remainder = match.Groups["remainder"].Value
            };
        }

        /// <summary>
        /// Checks whether the input matches this symbol.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool IsMatch(string input)
        {
            var pat = string.Format(@"\A[\s]*(?<match>({0}))(?<remainder>([\s\S]*))[\s]*\Z", Name);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return re.IsMatch(input);
        }

        /// <summary>
        /// Checks whether the current symbol matches, and can read the input.
        /// If successful, the successful input is returned in the context.
        /// </summary>
        /// <param name="context">The parser context.</param>
        /// <returns>True if successful. The abstract syntax tree is constructed using the context.Results object.</returns>
        public bool Parse(ParserContext context)
        {
            if (Debug)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($@"[Symbol.Parse()] Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}, Symbol={this.Name}, Next Token=[{context.PeekToken().TokenName} - ""{context.PeekToken().TokenValue}""]");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // save token position
            int temp = context.CurrentTokenIndex;
            bool ok = false;
            var once = false;
            while (true && !context.TokenEOF)
            {
                var token = context.TryToken(this.Name);

                if (token != null)
                {
                    // terminal
                    ok = true;
                    if (!this.Ignore)
                        context.UpdateResult(this.Alias, token);
                }
                // check to see if the symbol a pointer to another production rule?
                // if so, add new item onto stack.
                else
                {
                    // non terminal
                    var rules = context
                        .ProductionRules
                        .Where(r=>r.RuleType==RuleType.ParserRule)
                        .Where(r => r.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));

                    if (!rules.Any())
                        break;

                    foreach (var rule in rules)
                    {
                        object obj = null;
                        ok = rule.Parse(context, out obj);
                        if (ok)
                        {
                            if (!this.Ignore)
                                context.UpdateResult(this.Alias, obj);
                            break;
                        }
                    }
                }

                // wind back the token index if the symbol did not match tokens.
                //Console.WriteLine(string.Format("OK = {0}", ok));
                if (ok)
                {
                    once = true;
                    if (!Many)
                        break;
                }
                else
                {
                    if (!once)
                        context.CurrentTokenIndex = temp;
                    break;
                }
            }

            // return true if match (at least once).
            var success = ok || once || Optional;
            if (Debug)
            {
                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (once)
                        Console.WriteLine($"[Symbol.Parse()] Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}: success (once)");
                    else
                        Console.WriteLine($"[Symbol.Parse()] Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}: success");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Symbol.Parse()] Token Index: {context.CurrentTokenIndex}, Results: {context.Results.Count()}: failure");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }

            return success;
        }
    }
}
