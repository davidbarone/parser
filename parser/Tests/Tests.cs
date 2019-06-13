using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public abstract class Tests
    {
        public static int TestNumber = 0;
        public static int Passed = 0;
        public static int Failed = 0;

        public abstract void DoTests();

        protected void TestGrammar(string name, string grammar, bool expectFail = false)
        {
            TestNumber++;
            Console.WriteLine(string.Format("[{0}] BNF Grammar Parse test: {1}", TestNumber, name));

            try
            {
                var parser = new Parser(grammar);
                parser.Debug = false;
                var rules = parser.ProductionRules;
                if (expectFail)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("Failure: This grammar should have failed parsing."));
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Failed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format($"Success: This grammar passed parsing. {rules.Count()} rules parsed."));
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Passed++;
                }
            }
            catch (Exception ex)
            {
                if (!expectFail)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format($"Failure: This grammar has failed parsing. [{ex.Message}]"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Failed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format($"Success: This grammar successfully failed parsing: [{ex.Message}]"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Passed++;
                }
            }
        }

        protected void TestParser(string grammar, string input, string productionRule, Visitor visitors = null, bool expectFailure = false)
        {
            TestNumber++;
            try
            {
                var parser = new Parser(grammar);
                Console.WriteLine(string.Format(@"[{3}]
Production rules: {0}
Input: [{1}]
Start Production Rule
[{2}]
Expect failure: {4}", parser.ProductionRules.Count(), input, productionRule, TestNumber, expectFailure));
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

                if (expectFailure)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Test Failed: Expected fail, got pass.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Failed++;
                } else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Test successful! (no failure)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Passed++;
                }
            }
            catch (Exception ex)
            {
                if (expectFailure)
                {
                    // Expect to get here.
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Test successful. Error [{ex.Message}] thrown.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Passed++;
                } else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Test Failed: Error {ex.Message} thrown.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Failed++;
                }
            }
        }
    }
}