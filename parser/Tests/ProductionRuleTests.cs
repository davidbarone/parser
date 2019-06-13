using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class ProductionRuleTests
    {
        static void DoBNFTests()
        {
            DoBNFTest("null test", null, true);
            DoBNFTest("empty string", "", true);
            DoBNFTest("space", " ", true);
            DoBNFTest("comment only", "(* JUST A COMMENT *)", true);
            DoBNFTest("invalid production rule", "this is fail", true);
            DoBNFTest("incomplete rule", @"TEST=""ABC", true);

            // Valid grammars
            DoBNFTest("Single Rule", @"SIMPLE=""X"";", false);
            DoBNFTest("Two Rules", @"SIMPLE=""X"";ANOTHER=""Y"";", false);
            DoBNFTest("Multi Line", @"

(* This is a test *)

SIMPLE  =   ""X"";
ANOTHER=""Y"";", false);
            DoBNFTest("Comments", @"
SIMPLE=""X""; (* This is a comment *)
(* Another comment *)
ANOTHER=""Y"";", false);
            DoBNFTest("Lexer and Parser Rule 1", @"
SIMPLE=""X"";
ANOTHER=""Y"";
rule=SIMPLE;
");
            DoBNFTest("Parser Rule 1", @"myrule=SIMPLE,ANOTHER;");
            DoBNFTest("Parser Rule with alias and modifier", @"myrule   =   TEST:SIMPLE*;");
            DoBNFTest("Parser with alternates", @"myrule    =   SIMPLE, ANOTHER | SIMPLE;");

            DoBNFTest("Sqlish Grammar", GrammarText);
        }

        static void DoBNFTest(string name, string grammar, bool expectFail = false, int? expectedRules = null)
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
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format($"Success: This grammar passed parsing. {rules.Count()} rules parsed."));
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            catch (Exception ex)
            {
                if (!expectFail)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format($"Failure: This grammar has failed parsing. [{ex.Message}]"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format($"Success: This grammar successfully failed parsing: [{ex.Message}]"));
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }
    }
}
