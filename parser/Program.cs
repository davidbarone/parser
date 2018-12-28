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
                new ProductionRule("LITERAL_STRING", "['][^']*[']"),
                new ProductionRule("LITERAL_NUMBER", @"-?(([1-9]\d*)|0)(.0*[1-9](0*[1-9])*)"),
                new ProductionRule("AND_LOG_OP", "AND"),
                new ProductionRule("OR_LOG_OP", "AND"),
                new ProductionRule("EQ_OP", "EQ"),
                new ProductionRule("NE_OP", "NE"),
                new ProductionRule("LT_OP", "LT"),
                new ProductionRule("LE_OP", "LE"),
                new ProductionRule("GE_OP", "GE"),
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
                new ProductionRule("boolean expression", "=comparison predicate"),
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

            var expr1 = "FIELD_1 EQ '123'";
            var parser = new Parser(grammar);
            var tokens = parser.Tokenise(expr1);
            var ast = parser.Parse(tokens, "where filter");
            var result = parser.Execute(ast, visitor, (state) => new {
                Sql = state.Sql,
                Parameters = state.Parameters
            });
        }
    }
}