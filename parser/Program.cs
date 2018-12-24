using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserDemo
{
    #region Extensions

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
            else
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
            else
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

    #endregion

    #region Lexer

    /// <summary>
    /// A token within the input
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
    /// Result from the Match method
    /// </summary>
    public class MatchResult
    {
        public bool Success { get; set; }
        public string Matched { get; set; }
        public string Remainder { get; set; }
    }

    /// <summary>
    /// A lexer rule in the language grammar
    /// </summary>
    public class LexerRule
    {
        public LexerRule(string tokenName, string pattern, bool ignore = false)
        {
            this.TokenName = tokenName;
            this.Pattern = pattern;
            this.Ignore = false;
        }

        public string TokenName { get; set; }
        public string Pattern { get; set; }
        public bool Ignore { get; set; }

        public MatchResult Match(string input)
        {
            var pat = string.Format(@"^\s*(?<match>({0}))(?<remainder>(.*))\s*$", Pattern);
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
            var pat = string.Format(@"^\s*(?<match>({0}))(?<remainder>(.*))\s*$", Pattern);
            Regex re = new Regex(pat, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return re.IsMatch(input);
        }
    }

    public class Lexer
    {
        public IList<LexerRule> Rules { get; set; }

        public Lexer(IList<LexerRule> rules)
        {
            this.Rules = rules;
        }

        public IList<Token> Tokenise(string input)
        {
            // Base case
            if (string.IsNullOrEmpty(input))
                return new List<Token>() { };

            // Start at the beginning of the string and
            // recursively id
            foreach (var rule in Rules)
            {
                if (rule.IsMatch(input))
                {
                    var match = rule.Match(input);
                    var token = new Token()
                    {
                        TokenName = rule.TokenName,
                        TokenValue = match.Matched
                    };
                    var list = new List<Token>();
                    if (!rule.Ignore)
                    {
                        list.Add(token);
                    }
                    list.AddRange(Tokenise(match.Remainder));
                    return list;
                }
            }
            throw new Exception(string.Format("Syntax error near {0}...", input));
        }
    }

    #endregion

    #region Parser

    public class Parser
    {
        private IList<ProductionRule> ProductionRules { get; set; }

        public Parser(IList<ProductionRule> productionRules)
        {
            this.ProductionRules = productionRules;
        }

        public Node Parse(IList<Token> tokens, string rootRroductionRule, bool throwOnFailure = true)
        {
            var rule = ProductionRules.FirstOrDefault(p => rootRroductionRule == null || p.Name.Equals(rootRroductionRule, StringComparison.OrdinalIgnoreCase));
            if (rule == null)
                throw new Exception("Production rule not found.");

            ParserContext context = new ParserContext(ProductionRules, tokens);

            context.PushResult(null);
            var ok = rule.Parse(context);
            var result = context.PopResult();

            // Valid production rule. Parsed OK, and no remaining tokens in queue.
            if (ok && context.TokenEOF)
            {
                return (Node)result;
            }
            else if (!context.TokenEOF)
            {
                throw new Exception("Unexpected input at EOF.");
            }
            else 
            {
                if (throwOnFailure)
                    throw new Exception("Input cannot be parsed.");
                else
                    return null;
            }
        }

        /// <summary>
        /// Parses the tokens into an abstract syntax tree (AST).
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public Node Parse(IList<Token> tokens)
        {
            foreach (var rule in ProductionRules)
            {
                var result = Parse(tokens, rule.Name, false);
                if (result != null)
                    return result;
            }
            return null;
        }
    }

    /// <summary>
    /// The parser rules in the grammar.
    /// </summary>
    public class ProductionRule
    {
        public ProductionRule(string name, params Symbol[] symbols)
        {
            this.Name = name;
            this.Symbols = symbols.ToList();
        }

        /// <summary>
        /// Name of the rule
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The symbols that make up this rule
        /// </summary>
        public List<Symbol> Symbols { get; set; }

        /// <summary>
        /// Parses a set of tokens into an abstract syntax tree.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool Parse(ParserContext context)
        {
            Node node = new Node();
            node.Name = this.Name;
            foreach (var symbol in this.Symbols)
            {
                context.PushResult(null);
                var ok = symbol.Parse(context);
                var result = context.PopResult();

                if (ok)
                {
                    context.UpdateResult(result, symbol.Property, this.Name);
                } else
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
    }

    #endregion

    #region Symbols

    public abstract class Symbol
    {
        /// <summary>
        /// The name of the property in the resulting node in the AST.
        /// 1. If null then the value is ignored in the AST.
        /// 2. If = '' then value becomes a node
        /// 3. If <> '' then value becomes a property of a node
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Set to true if the symbol is optional in the syntax.
        /// </summary>
        public bool Optional { get; set; }

        public Symbol(string property, bool optional = false)
        {
            this.Property = property;
            this.Optional = optional;
        }

        /// <summary>
        /// Factory Method
        /// </summary>
        public static Symbol One(string property, string name, bool optional = false)
        {
            return new SymbolOne(property, name, optional);
        }

        public static Symbol Choice(string property, params Symbol[] symbols)
        {
            return new SymbolChoice(property, false, symbols);
        }

        public static Symbol Choice(string property, bool optional, params Symbol[] symbols)
        {
            return new SymbolChoice(property, optional, symbols);
        }

        public static Symbol Complex(string property, params Symbol[] symbols)
        {
            return new SymbolComplex(property, false, symbols);
        }

        public static Symbol Complex(string property, bool optional, params Symbol[] symbols)
        {
            return new SymbolComplex(property, optional, symbols);
        }

        public static Symbol Many(string property, Symbol symbol, bool optional = false)
        {
            return new SymbolMany(property, symbol, optional);
        }

        public bool Parse(ParserContext context)
        {
            // save token position
            int temp = context.CurrentTokenIndex;
            var ok = this.ParseHandler(context);
            // wind back the token index if the symbol did not match tokens.
            if (!ok)
                context.CurrentTokenIndex = temp;

            // return true if match, or optional and didn't match.
            return ok || Optional;
        }

        /// <summary>
        /// Take the token list and attempts to parse.
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="productionRules"></param>
        /// <returns>Either returns a node object (which is a child), or the actual value which can be added to the parent.</returns>
        public abstract bool ParseHandler(ParserContext context);
    }

    /// <summary>
    /// A symbol used in a production rule. Can be a token, terminal production rule or non-terminal production rule
    /// </summary>
    public class SymbolOne : Symbol
    {
        public SymbolOne(string property, string name, bool optional) : base(property, optional)
        {
            this.Name = name;
        }

        /// <summary>
        /// The name of the symbol. Can be a token (terminal) or another production rule (non terminal).
        /// </summary>
        public string Name { get; set; }

        public override bool ParseHandler(ParserContext context)
        {
            // Is the symbol a simple token?
            var token = context.TryToken(this.Name);

            if (token!=null)
            {
                context.UpdateResult(token, this.Property, Name);
                return true;
            }
            // check to see if the symbol a pointer to another production rule?
            else 
            {
                var rule = context.ProductionRules.FirstOrDefault(r => r.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));

                if (rule == null)
                    return false;
                else
                {
                    // Rule is non terminal
                    foreach (var symbol in rule.Symbols)
                    {
                        context.PushResult(null);
                        var ok = symbol.Parse(context);
                        var result = context.PopResult();
                        if (ok)
                        {
                            context.UpdateResult(result, symbol.Property, this.Name);
                        }
                        else
                            return false;
                    }
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// To define a choice of symbols in a production rule.
    /// </summary>
    public class SymbolChoice : Symbol
    {
        public SymbolChoice(string property, bool optional, params Symbol[] symbols) : base(property, optional)
        {
            this.Symbols = symbols.ToList();
        }
        public IEnumerable<Symbol> Symbols { get; set; }

        public override bool ParseHandler(ParserContext context)
        {
            // returns the first item in the choice that matches.
            foreach (var symbol in Symbols)
            {
                context.PushResult(null);
                var ok = symbol.Parse(context);
                var result = context.PopResult();
                if (ok)
                {
                    context.UpdateResult(result, this.Property, "SYMBOL_CHOICE");
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// To define a set of symbols which must all be matched.
    /// </summary>
    public class SymbolComplex : Symbol
    {
        public SymbolComplex(string property, bool optional, params Symbol[] symbols) : base(property, optional)
        {
            this.Symbols = symbols;
        }
        public IEnumerable<Symbol> Symbols { get; set; }

        public override bool ParseHandler(ParserContext context)
        {
            foreach (var symbol in Symbols)
            {
                context.PushResult(null);
                var ok = symbol.Parse(context);
                var result = context.PopResult();
                if (ok)
                    context.UpdateResult(result, this.Property, "SYMBOL_COMPLEX");
                else
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// To define a symbol that can be repeated in a list.
    /// </summary>
    public class SymbolMany : Symbol
    {
        public SymbolMany(string property, Symbol symbol, bool optional) : base(property, optional)
        {
            this.Symbol = symbol;
        }
        public Symbol Symbol { get; set; }

        public override bool ParseHandler(ParserContext context)
        {
            while(true && !context.TokenEOF)
            {
                // TO DO - NEED TO WIND BACK TOKENS IF THIS FAILS!!!
                context.PushResult(null);
                var ok = Symbol.Parse(context);
                var result = context.PopResult();
                if (ok)
                {
                    context.UpdateResult(result, this.Property, "SYMBOL_MANY");
                }
                else
                {
                    break;
                }
            }
            return true;
        }
    }

    #endregion

    #region Visitors

    /// <summary>
    /// Visits the nodes of an abstract syntax tree. Handlers are provided for each type of node.
    /// </summary>
    public class NodeVisitor
    {
        public NodeVisitor(Dictionary<string, Func<Node, bool>> visitors)
        {
            this.Visitors = visitors;
        }

        public Dictionary<string, Func<Node, bool>> Visitors { get; set; }
    }

    #endregion

    class Program
    {
        static void Main(string[] args)
        {
            // NOTE THAT LEXER RULES AND PRODUCTION RULE
            // NAMES MUST BE UNIQUE BETWEEN THE TWO LISTS.

            List<LexerRule> rules = new List<LexerRule>()
            {
                new LexerRule("LITERAL_STRING", "['][^']*[']"),
                new LexerRule("LITERAL_NUMBER", @"-?(([1-9]\d*)|0)(.0*[1-9](0*[1-9])*)"),
                new LexerRule("AND_LOG_OP", "AND"),
                new LexerRule("OR_LOG_OP", "AND"),
                new LexerRule("EQ_OP", "EQ"),
                new LexerRule("NE_OP", "NE"),
                new LexerRule("LT_OP", "LT"),
                new LexerRule("LE_OP", "LE"),
                new LexerRule("GE_OP", "GE"),
                new LexerRule("IDENTIFIER", "[A-Z_][A-Z_0-9]+"),
                new LexerRule("WHITESPACE", @"\s+", true)
            };

            // Note that the order of these is important!
            List<ProductionRule> productionRules = new List<ProductionRule>()
            {
                new ProductionRule(
                    "WHERE_FILTER",
                    Symbol.One("PREDICATES", "COMPARISON_PREDICATE"),
                    Symbol.Many("PREDICATES",
                        Symbol.Complex("",
                            Symbol.One(null, "AND_LOG_OP"),
                            Symbol.One("", "COMPARISON_PREDICATE")
                        ), true
                    )
                ),
                // Left recursive?
                new ProductionRule(
                    "BOOLEAN_TERM",
                    Symbol.Choice(
                        "",
                        Symbol.One("", "COMPARISON_PREDICATE"),
                        Symbol.Complex(
                            "",
                            Symbol.One("", "COMPARISON_PREDICATE"),
                            Symbol.One("", "LOGICAL_OPERATOR"),
                            Symbol.One("", "BOOLEAN_TERM")
                        )
                    )
                ),
                new ProductionRule(
                    "LOGICAL_OPERATOR",
                    Symbol.Choice(
                        "",
                        Symbol.One("", "AND_LOG_OP"),
                        Symbol.One("", "OR_LOG_OP")
                    )
                ),
                new ProductionRule(
                    "COMPARISON_PREDICATE",
                    Symbol.One("LHV", "COMPARISON_OPERAND"),
                    Symbol.One("OPERATOR", "COMPARISON_OPERATOR"),
                    Symbol.One("RHV", "COMPARISON_OPERAND")
                ),
                new ProductionRule(
                    "COMPARISON_OPERAND",
                    Symbol.Choice(
                        "",
                        Symbol.One("", "LITERAL_STRING"),
                        Symbol.One("", "LITERAL_NUMBER"),
                        Symbol.One("", "IDENTIFIER")
                    )
                ),
                new ProductionRule(
                    "COMPARISON_OPERATOR",
                    Symbol.Choice(
                        "",
                        Symbol.One("", "EQ_OP"),
                        Symbol.One("", "NE_OP"),
                        Symbol.One("", "LT_OP"),
                        Symbol.One("", "LE_OP"),
                        Symbol.One("", "GT_OP"),
                        Symbol.One("", "GE_OP")
                    )
                )
            };

            NodeVisitor visitors = new NodeVisitor(new Dictionary<string, Func<Node, bool>>()
                {
                    {"test", (node) => { return true;  } }
                }
            );

            var numeric1 = "123";
            var string1 = "'HELLO WORLD'";
            var reference1 = "MY_FIELD ";
            var expr1 = "FIELD_1 EQ '123' AND FIELD_2 EQ 'ABC' AND FIELD_3 LT 'ABC' AND FIELD_4 GE 123.45";
            var tokens = new Lexer(rules).Tokenise(expr1);
            var parser = new Parser(productionRules);
            var ast = parser.Parse(tokens, "WHERE_FILTER");

            var a = tokens;
        }
    }
}