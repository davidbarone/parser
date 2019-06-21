using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public abstract class AbstractTests
    {
        public static int TestNumber = 0;
        public static int Passed = 0;
        public static int Failed = 0;
        public bool debug = false;

        public void ParserLogFunc(object sender, ParserLogArgs args)
        {
            if (args.ParserLogType==ParserLogType.BEGIN || args.ParserLogType == ParserLogType.END)
                Console.ForegroundColor = ConsoleColor.Magenta;
            else if (args.ParserLogType == ParserLogType.FAILURE)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (args.ParserLogType == ParserLogType.INFORMATION)
                Console.ForegroundColor = ConsoleColor.White;
            else if (args.ParserLogType == ParserLogType.SUCCESS)
                Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine($"{new String(' ', args.NestingLevel)} {args.ParserLogType.ToString()} {args.Message}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public abstract void DoTests();

        /// <summary>
        /// Executes a single test.
        /// </summary>
        /// <remarks>
        /// If no input set, then only grammar is checked for correctness.
        /// If input + productionRule set, then parsing occurs.
        /// If visitor set, then ast is traversed, and a result is calculated.
        /// </remarks>
        /// <param name="name">Name of the test.</param>
        /// <param name="grammar">The grammar used for the test.</param>
        /// <param name="input">The input to parse.</param>
        /// <param name="productionRule">The root production rule to use.</param>
        /// <param name="visitor">Visitor to use to navigate ast.</param>
        /// <param name="resultMapping">Mapping of result.</param>
        /// <param name="expected">Expected result.</param>
        /// <param name="expectException">The expected result.</param>
        protected void DoTest(
            string name,
            string grammar,
            string input,
            string productionRule,
            Visitor visitor,
            Func<dynamic, object>resultMapping,
            object expected, bool expectException)
        {
            TestNumber++;

            Console.WriteLine($@"
[{TestNumber}]: {name}");
            try
            {
                var parser = new Parser(grammar, productionRule);
                if (debug)
                    parser.ParserLogFunc = ParserLogFunc;

                var rules = parser.ProductionRules;

                Console.WriteLine($@"Production rules: {rules.Count}
Input: [{input ?? ""}]");

                if (!string.IsNullOrEmpty(input))
                {
                    var ast = parser.Parse(input, true);
                    if (visitor != null)
                    {
                        var actual = parser.Execute(ast, visitor, resultMapping);

                        // display output
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"Output: [{actual}]");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        if (expected!=null && !string.IsNullOrEmpty(expected.ToString()))
                        {
                            if (actual.Equals(expected))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(string.Format($"Success: actual {actual}, expected {expected}."));
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                            else
                            {
                                throw new Exception($"Failure: actual {actual}, expected {expected}.");
                            }
                        }
                    }
                }

                if (expectException)
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
                if (!expectException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format($"Failure: Test expected no failure, but exception thrown: [{ex.Message}]"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Failed++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format($"Success: Test expected failure, and exception thrown: [{ex.Message}]"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Passed++;
                }
            }
        }
    }
}