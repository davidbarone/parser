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
        static int TestNumber;

        static void Main(string[] args)
        {
            DoBNFTests();
            //DoTests();

            Console.ReadKey();
        }



        private static List<ProductionRule> Grammar => new List<ProductionRule>()
        {
                new ProductionRule("AND", @"\bAND\b"),
                new ProductionRule("OR", @"\bOR\b"),
                new ProductionRule("EQ_OP", @"\bEQ\b"),
                new ProductionRule("NE_OP", @"\bNE\b"),
                new ProductionRule("LT_OP", @"\bLT\b"),
                new ProductionRule("LE_OP", @"\bLE\b"),
                new ProductionRule("GT_OP", @"\bGT\b"),
                new ProductionRule("GE_OP", @"\bGE\b"),
                new ProductionRule("LEFT_PAREN", "[(]"),
                new ProductionRule("RIGHT_PAREN", "[)]"),
                new ProductionRule("COMMA", ","),
                new ProductionRule("IN", @"\b(IN)\b"),
                new ProductionRule("CONTAINS", @"\bCONTAINS\b"),
                new ProductionRule("BETWEEN", @"\bBETWEEN\b"),
                new ProductionRule("ISBLANK", @"\bISBLANK\b"),
                new ProductionRule("NOT", @"\bNOT\b"),

                new ProductionRule("LITERAL_STRING", "['][^']*[']"),
                new ProductionRule("LITERAL_NUMBER", @"[+-]?((\d+(\.\d*)?)|(\.\d+))"),
                new ProductionRule("IDENTIFIER", "[A-Z_][A-Z_0-9]*"),
                //new ProductionRule("WHITESPACE", @"\s+"),

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
                new ProductionRule("in predicate", "LHV=comparison operand", "NOT=NOT?", "IN!", "LEFT_PAREN!", "RHV=comparison operand", "RHV=in factor*", "RIGHT_PAREN!"),
                new ProductionRule("between predicate", "LHV=comparison operand", "NOT=NOT?", "BETWEEN!", "OP1=comparison operand", "AND!", "OP2=comparison operand"),
                new ProductionRule("contains predicate", "LHV=comparison operand", "NOT=NOT?", "CONTAINS!", "RHV=comparison operand"),
                new ProductionRule("blank predicate", "LHV=comparison operand", "NOT=NOT?", "ISBLANK"),

                new ProductionRule("predicate", "=comparison predicate"),
                new ProductionRule("predicate", "=in predicate"),
                new ProductionRule("predicate", "=between predicate"),
                new ProductionRule("predicate", "=contains predicate"),
                new ProductionRule("predicate", "=blank predicate"),

                new ProductionRule("boolean primary", "=predicate"),
                new ProductionRule("boolean primary", "LEFT_PAREN!", "CONDITION=search condition", "RIGHT_PAREN!"),

                new ProductionRule("boolean factor", "AND!", "=boolean primary"),
                new ProductionRule("boolean term", "AND=boolean primary", "AND=boolean factor*"),

                new ProductionRule("search factor", "OR!", "=boolean term"),
                new ProductionRule("search condition", "OR=boolean term", "OR=search factor*"),
        };

        private static void DoTests()
        {
            // Success
            TestSuccess(Grammar, "LEVEL_1 LE '123' AND FISCAL_PERIOD EQ 12 AND FORECAST_PERIOD NE 201812 OR MY_FIELD EQ '123'", "search condition", Visitor);
            TestSuccess(Grammar, "MY_LIST IN ('abc')", "search condition", Visitor);
            TestSuccess(Grammar, null, "search condition");
            TestSuccess(Grammar, "", "search condition");
            TestSuccess(Grammar, "FIELD_1 EQ '123'", "search condition", Visitor);
            TestSuccess(Grammar, "FIELD_1 EQ 123", "search condition", Visitor);
            TestSuccess(Grammar, "FIELD_1 EQ '123' AND FIELD_2 GT 123", "search condition", Visitor);
            TestSuccess(Grammar, "FIELD_1 EQ '123' AND FIELD_2 GT 123 AND FIELD_3 EQ 'XYZ'", "search condition", Visitor);
            TestSuccess(Grammar, "FISCAL_YEAR EQ 2018 AND FISCAL_PERIOD EQ 12 AND FISCAL_WEEK EQ 4 AND FORECAST_PERIOD EQ 201812", "search condition", Visitor);
            TestSuccess(Grammar, "MY_LIST IN ('abc','mno','xyz')", "search condition", Visitor);

            // Using an identifier starting with same characters as another token ('LE')
            TestSuccess(Grammar, "LEVEL_1 LE '123'", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 LE '123' OR FISCAL_PERIOD EQ 12", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 LE '123' AND FISCAL_PERIOD EQ 12 AND FORECAST_PERIOD NE 201812 OR MY_FIELD EQ '123'", "search condition", Visitor);

            // BETWEEN / NOT  BETWEEN
            TestSuccess(Grammar, "LEVEL_1 BETWEEN '123' AND '456'", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 NOT BETWEEN '123' AND '456'", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 NOT BETWEEN '123' AND '456' AND LEVEL_2 GT 2", "search condition", Visitor);

            // CONTAINS / NOT CONTAINS
            TestSuccess(Grammar, "LEVEL_1 CONTAINS 'HELLO'", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 NOT CONTAINS 'HELLO'", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 NOT CONTAINS 'HELLO' AND LEVEL_2 GT 2", "search condition", Visitor);

            // ISBLANK / ISNOTBLANK
            TestSuccess(Grammar, "LEVEL_1 ISBLANK", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 NOT ISBLANK", "search condition", Visitor);
            TestSuccess(Grammar, "LEVEL_1 NOT ISBLANK AND LEVEL_2 GT 2", "search condition", Visitor);

            // Parens
            TestSuccess(Grammar, "(LEVEL_1 ISBLANK)", "search condition", Visitor);
            TestSuccess(Grammar, "(LEVEL_1 ISBLANK AND LEVEL_2 EQ '2')", "search condition", Visitor);
            TestSuccess(Grammar, "(LEVEL_2 EQ '2' AND LEVEL_3 NE 4) OR (LEVEL_4 EQ 'Z' AND LEVEL_5 NE 123)", "search condition", Visitor);
            TestSuccess(Grammar, "MY_FIELD EQ 'ZZZ' AND ((LEVEL_2 EQ '2' AND LEVEL_3 NE 4) OR (LEVEL_4 EQ 'Z' AND LEVEL_5 NE 123))", "search condition", Visitor);
            TestSuccess(Grammar, "MY_FIELD EQ 'ZZZ' AND ((LEVEL_2 EQ '2' AND LEVEL_3 ISBLANK) OR (LEVEL_4 NOT IN (1,2,3) AND LEVEL_5 CONTAINS 'TEST'))", "search condition", Visitor);

            // Failure
            TestFailure(Grammar, "FIELD", "comparison predicate");
            TestFailure(Grammar, "FIELD GT 123 AND", "comparison predicate");
            TestFailure(Grammar, "FIELD", "search condition");
            TestFailure(Grammar, "FIELD GT 123 AND", "search condition");

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

        private static void TestSuccess(List<ProductionRule> grammar, string input, string productionRule, Visitor visitors = null)
        {
            TestNumber++;
            Console.WriteLine(string.Format("[{3}] TEST SUCCESS: Production rules: {0}, Input: [{1}], Start [{2}]", grammar.Count(), input, productionRule, TestNumber));
            try
            {
                var parser = new Parser(grammar);
                var ast = parser.Parse(input, productionRule);
                if (visitors != null)
                {
                    dynamic result = parser.Execute(ast, visitors);
                    var a = result.Sql;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(string.Format("Output: {0}", a));
                    Console.ForegroundColor = ConsoleColor.Gray;
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

        private static void TestFailure(List<ProductionRule> grammar, string input, string productionRule)
        {
            TestNumber++;
            Console.WriteLine(string.Format("[{3}] TEST FAILURE: Production rules: {0}, Input: [{1}], Start [{2}]", grammar.Count(), input, productionRule, TestNumber));
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