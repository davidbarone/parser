using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            List<ProductionRule> grammar = new List<ProductionRule>()
            {
                new ProductionRule("AND_LOG_OP", "AND"),
                new ProductionRule("OR_LOG_OP", "AND"),
                new ProductionRule("EQ_OP", "EQ"),
                new ProductionRule("NE_OP", "NE"),
                new ProductionRule("LT_OP", "LT"),
                new ProductionRule("LE_OP", "LE"),
                new ProductionRule("GT_OP", "GT"),
                new ProductionRule("GE_OP", "GE"),
                new ProductionRule("LEFT_PAREN", "[(]"),
                new ProductionRule("RIGHT_PAREN", "[)]"),
                new ProductionRule("COMMA", ","),
                new ProductionRule("IN", "(IN)"),

                new ProductionRule("LITERAL_STRING", "['][^']*[']"),
                new ProductionRule("LITERAL_NUMBER", @"[+-]?((\d+(\.\d*)?)|(\.\d+))"),
                new ProductionRule("IDENTIFIER", "[A-Z_][A-Z_0-9]+"),
                new ProductionRule("WHITESPACE", @"\s+"),

                new ProductionRule("comparison operator", "=EQ_OP"),
                new ProductionRule("comparison operator", "=NE_OP"),
                new ProductionRule("comparison operator", "=LT_OP"),
                new ProductionRule("comparison operator", "=LE_OP"),
                new ProductionRule("comparison operator", "=GT_OP"),
                new ProductionRule("comparison operator", "=GE_OP"),

                new ProductionRule("comparison operand", "=LITERAL_STRING"),
                new ProductionRule("comparison operand", "=LITERAL_NUMBER"),
                new ProductionRule("comparison operand", "=IDENTIFIER"),

                new ProductionRule("comparison predicate", "LHV=comparison operand", "OPERATOR=comparison operator", "RHV=comparison operand"),
                new ProductionRule("in factor", "COMMA!", "=comparison operand"),
                new ProductionRule("in predicate", "LHV=comparison operand", "IN!", "LEFT_PAREN!", "RHV=comparison operand", "RHV=in factor*", "RIGHT_PAREN!"),
                new ProductionRule("boolean expression", "=comparison predicate"),
                new ProductionRule("boolean expression", "=in predicate"),
                new ProductionRule("and factor", "AND_LOG_OP!", "=boolean expression"),
                new ProductionRule("and logical expression", "=boolean expression", "=and factor*"),
                new ProductionRule("where filter", "PREDICATES=and logical expression")
            };

            var visitor = new Visitor();
            visitor.AddVisitor(
                "where filter",
                (v, n) =>
                {
                    // Set up state
                    v.State.Parameters = new List<SqlParameter>();
                    v.State.Predicates = new List<string>();

                    foreach (var item in (IEnumerable<Object>)n.Properties["PREDICATES"])
                    {
                        var node = item as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");
                        node.Accept(v);
                    }

                    v.State.Sql = string.Join(" AND ", visitor.State.Predicates);
                }
            );

            visitor.AddVisitor(
                "comparison predicate",
                (v, n) =>
                {
                    Dictionary<string, string> operators = new Dictionary<string, string>()
                            {
                                {"EQ_OP", "="},
                                {"NE_OP", "<>"},
                                {"LT_OP", "<"},
                                {"LE_OP", "<="},
                                {"GT_OP", ">"},
                                {"GE_OP", ">="},
                            };

                    var i = v.State.Parameters.Count;
                    var sql = string.Format(
                        "{0} {1} @{2}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        operators[(string)((Token)n.Properties["OPERATOR"]).TokenName],
                        "P" + i
                    );
                    v.State.Predicates.Add(sql);
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i,
                        Value = ((Token)n.Properties["RHV"]).TokenValue
                    });
                }
            );

            visitor.AddVisitor(
                "in predicate",
                (v, n) =>
                {
                    var i = v.State.Parameters.Count;
                    var sql = "";
                    sql = string.Format(
                        "{0} IN @{1}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        "P" + i
                    );

                    // Add the SQL + update args object.
                    v.State.Predicates.Add(sql);
                    object value = ((List<object>)n.Properties["RHV"]).Select(t => ((Token)t).TokenValue.Replace("'", ""));
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i,
                        Value = value
                    });
                }
            );

            // Success
            TestSuccess(grammar, "MY_LIST IN ('abc')", "where filter", visitor);
            TestSuccess(grammar, null, "where filter");
            TestSuccess(grammar, "", "where filter");
            TestSuccess(grammar, "FIELD_1 EQ '123'", "where filter");
            TestSuccess(grammar, "FIELD_1 EQ 123", "where filter");
            TestSuccess(grammar, "FIELD_1 EQ '123' AND FIELD_2 GT 123", "where filter");
            TestSuccess(grammar, "FIELD_1 EQ '123' AND FIELD_2 GT 123 AND FIELD_3 EQ 'XYZ'", "where filter");
            TestSuccess(grammar, "FISCAL_YEAR EQ 2018 AND FISCAL_PERIOD EQ 12 AND FISCAL_WEEK EQ 4 AND FORECAST_PERIOD EQ 201812", "where filter");
            TestSuccess(grammar, "MY_LIST IN ('abc','mno','xyz')", "where filter", visitor);

            // Failure
            TestFailure(grammar, "FIELD", "comparison predicate");
            TestFailure(grammar, "FIELD GT 123 AND", "comparison predicate");
            TestFailure(grammar, "FIELD", "where filter");
            TestFailure(grammar, "FIELD GT 123 AND", "where filter");

            Console.WriteLine("Press a key to continue.");
            Console.ReadKey();
            // Testing execution of AST.
            /*
            var result = parser.Execute(ast, visitor, (state) => new {
                Sql = state.Sql,
                Parameters = state.Parameters
            });
            */
        }

        public static void TestSuccess(List<ProductionRule> grammar, string input, string productionRule, Visitor visitors = null)
        {
            Console.WriteLine(string.Format("TEST SUCCESS: Production rules: {0}, Input: [{1}], Start [{2}]", grammar.Count(), input, productionRule));
            try
            {
                var parser = new Parser(grammar);
                var ast = parser.Parse(input, productionRule);
                if (visitors != null)
                {
                    var result = parser.Execute(ast, visitors);
                    var a = result;
                }

                if (ast == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Parsing successful, but tree is empty.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Failure: {0}", ex.Message));
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public static void TestFailure(List<ProductionRule> grammar, string input, string productionRule)
        {
            Console.WriteLine(string.Format("TEST FAILURE: Production rules: {0}, Input: [{1}], Start [{2}]", grammar.Count(), input, productionRule));
            try
            {
                var parser = new Parser(grammar);
                var ast = parser.Parse(input, productionRule);

                if (ast != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Parser returns a tree where null was expected.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Expecting exception but none thrown.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch
            {
                // Expect to get here.
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}