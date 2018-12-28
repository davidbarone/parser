using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser
{
    public static class Extensions
    {
        /// <summary>
        /// Unions 2 objects together into a enumerable. Individual
        /// objects can be enumerables or plain objects. The objects
        /// can also be a node of type SYMBOL_MANY. In this case, we
        /// take the first / only property (which should be an
        /// IEnumerable), and union this instead.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<object> Union(this object a, object obj)
        {
            List<object> results = new List<object>();
            var enumerableA = a as System.Collections.IEnumerable;
            var enumerableObj = obj as System.Collections.IEnumerable;

            var nodeA = a as Node;
            var nodeObj = obj as Node;

            if (enumerableA != null)
            {
                foreach (var item in enumerableA)
                    results.Add(item);
            }
            else if (nodeA != null && nodeA.Name == "SYMBOL_MANY")
            {
                var list = nodeA.Properties.First().Value as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var item in list)
                        results.Add(item);
                }
            }
            else if (a != null)
                results.Add(a);

            if (enumerableObj != null)
            {
                foreach (var item in enumerableObj)
                    results.Add(item);
            }
            else if (nodeObj != null && nodeObj.Name == "SYMBOL_MANY")
            {
                var list = nodeObj.Properties.First().Value as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var item in list)
                        results.Add(item);
                }
            }
            else if (obj != null)
                results.Add(obj);

            return results;
        }

        /// <summary>
        /// Clones the tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public static IList<Token> Clone(this IList<Token> tokens)
        {
            List<Token> temp = new List<Token>();
            foreach (var token in tokens)
                temp.Add(token);

            return temp;
        }
    }

    /// <summary>
    /// A terminal token within the input.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Describes the token.
        /// </summary>
        public string TokenName { get; set; }
        /// <summary>TOke
        /// The actual value of the token.
        /// </summary>
        public string TokenValue { get; set; }
    }

    /// <summary>
    /// Result from the Match method. Used to match tokens.
    /// </summary>
    public class MatchResult
    {
        public bool Success { get; set; }
        public string Matched { get; set; }
        public string Remainder { get; set; }
    }

    public enum RuleType
    {
        // Terminal symbol
        LexerRule,

        // Non Terminal Symbol
        ParserRule
    }

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
                } else if (!context.TokenEOF)
                    throw new Exception("Unexpected input at EOF.");
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
            foreach (var symbol in this.Symbols)
            {
                context.PushResult(null);
                var ok = symbol.Parse(context);
                var result = context.PopResult();

                if (ok)
                {
                    context.UpdateResult(result, symbol.Alias, this.Name);
                }
                else
                    return false;
            }
            return true;
        }
    }

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

    /// <summary>
    /// Represents a node of the AST.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Name of the node is the production rule or token name it matches
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Properties (children) of the production rule.
        /// </summary>
        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public void Accept(Visitor v)
        {
            v.Visit(this);
        }
    }

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
            // save token position
            int temp = context.CurrentTokenIndex;
            bool ok = false;
            var once = false;
            while (true && !context.TokenEOF)
            {
                ok = this.ParseHandler(context);
                // wind back the token index if the symbol did not match tokens.
                if (ok)
                    once = true;
                if (!Many)
                    break;

                if (!ok && !Many)
                    context.CurrentTokenIndex = temp;
            }
            // return true if match, or optional and didn't match.
            return ok || once;
        }

        /// <summary>
        /// Parses a set of symbols.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        private bool ParseSymbols(ParserContext context, List<Symbol> symbols)
        {
            // Rule is non terminal
            foreach (var symbol in symbols)
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
                        var ok = ParseSymbols(context, rule.Symbols);
                        if (ok)
                            return true;
                    }
                    return false || Optional;
                }
            }
        }
    }

    /// <summary>
    /// Object that can traverse an abstract syntax tree, using a visitor pattern.
    /// </summary>
    public class Visitor
    {
        public dynamic State = new ExpandoObject();

        Dictionary<string, Action<Visitor, Node>> Visitors { get; set; }

        public Visitor()
        {
            Visitors = new Dictionary<string, Action<Visitor, Node>>();
        }

        public void AddVisitor(string key, Action<Visitor, Node> visitor)
        {
            this.Visitors.Add(key, visitor);
        }

        public void Visit(Node node)
        {
            var name = node.Name;
            var visitor = this.Visitors.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (visitor == null)
                throw new Exception(string.Format("Visitor not found for '{0}'!", name));

            Visitors[visitor](this, node);
        }
    }
}