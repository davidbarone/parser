using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class ProductionRuleTests : Tests
    {
        public override void DoTests()
        {
            // Null / invalid grammars
            DoTest("null test", null, null, null, null, null, null, true);
            DoTest("empty string", "", null, null, null, null, null, true);
            DoTest("space", " ", null, null, null, null, null, true);
            DoTest("comment only", "(* JUST A COMMENT *)", null, null, null, null, null, true);
            DoTest("invalid production rule", "this is fail", null, null, null, null, null, true);
            DoTest("incomplete rule", @"TEST=""ABC", null, null, null, null, null, true);

            // Valid grammars
            DoTest("Single Rule", @"SIMPLE=""X"";", null, null, null, null, null, false);
            DoTest("Two Rules", @"SIMPLE=""X"";ANOTHER=""Y"";", null, null, null, null, null, false);
            DoTest("Multi Line", @"

(* This is a test *)

SIMPLE  =   ""X"";
ANOTHER=""Y"";", null, null, null, null, null, false);
            DoTest("Comments", @"
SIMPLE=""X""; (* This is a comment *)
(* Another comment *)
ANOTHER=""Y"";", null, null, null, null, null, false);
            DoTest("Lexer and Parser Rule 1", @"
SIMPLE=""X"";
ANOTHER=""Y"";
rule=SIMPLE;
", null, null, null, null, null, false);
            DoTest("Parser Rule 1", @"myrule=SIMPLE,ANOTHER;", null, null, null, null, null, false);
            DoTest("Parser Rule with alias and modifier", @"myrule   =   TEST:SIMPLE*;", null, null, null, null, null, false);
            DoTest("Parser with alternates", @"myrule    =   SIMPLE, ANOTHER | SIMPLE;", null, null, null, null, null, false);
        }
    }
}
