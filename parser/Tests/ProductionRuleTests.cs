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
            TestGrammar("null test", null, true);
            TestGrammar("empty string", "", true);
            TestGrammar("space", " ", true);
            TestGrammar("comment only", "(* JUST A COMMENT *)", true);
            TestGrammar("invalid production rule", "this is fail", true);
            TestGrammar("incomplete rule", @"TEST=""ABC", true);

            // Valid grammars
            TestGrammar("Single Rule", @"SIMPLE=""X"";", false);
            TestGrammar("Two Rules", @"SIMPLE=""X"";ANOTHER=""Y"";", false);
            TestGrammar("Multi Line", @"

(* This is a test *)

SIMPLE  =   ""X"";
ANOTHER=""Y"";", false);
            TestGrammar("Comments", @"
SIMPLE=""X""; (* This is a comment *)
(* Another comment *)
ANOTHER=""Y"";", false);
            TestGrammar("Lexer and Parser Rule 1", @"
SIMPLE=""X"";
ANOTHER=""Y"";
rule=SIMPLE;
");
            TestGrammar("Parser Rule 1", @"myrule=SIMPLE,ANOTHER;");
            TestGrammar("Parser Rule with alias and modifier", @"myrule   =   TEST:SIMPLE*;");
            TestGrammar("Parser with alternates", @"myrule    =   SIMPLE, ANOTHER | SIMPLE;");
        }
    }
}
