using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public class LeftRecursionTests : AbstractTests
    {
        public override void DoTests()
        {
            DoTest("LR1", LeftRecursionGrammar, "1+2+3+4", "expression", null, null, null, false);
        }

        public string LeftRecursionGrammar => @"
NUMBER_LITERAL  = ""\d+"";
PLUS_OP         = ""\+"";
MINUS_OP        = ""\-"";
MUL_OP          = ""\*"";
DIV_OP          = ""\/"";
LPAREN         = ""\("";
RPAREN         = ""\)"";

expression      = expression, PLUS_OP, term | expression, MINUS_OP, term | term;
term            = factor | term, MUL_OP, factor | term, DIV_OP, factor;
factor          = primary | PLUS_OP, primary | MINUS_OP, primary;
primary         = NUMBER_LITERAL | LPAREN, expression, RPAREN;";

        public Visitor LeftRecursionVisitor
        {
            get
            {
                // Initial state
                dynamic state = new ExpandoObject();
                state.Stack = new Stack<int>();

                var visitor = new Visitor(state);

                visitor.AddVisitor(
                    "term",
                    (v, n) =>
                    {
                        var node = (Node)n.Properties["primary"];
                        node.Accept(v);
                        var hasMinus = n.Properties.ContainsKey("MINUS_OP");
                        int number = v.State.Stack.Pop();
                        if (hasMinus)
                            number = number * -1;
                        v.State.Stack.Push(number);
                    }
                );

                visitor.AddVisitor(
                    "factor",
                    (v, n) =>
                    {
                        var node = (Node)n.Properties["primary"];
                        node.Accept(v);
                        var hasMinus = n.Properties.ContainsKey("MINUS_OP");
                        int number = v.State.Stack.Pop();
                        if (hasMinus)
                            number = number * -1;
                        v.State.Stack.Push(number);
                    }
                );

                visitor.AddVisitor(
                    "primary",
                    (v, n) =>
                    {
                        if (n.Properties.ContainsKey("NUMBER_LITERAL"))
                        {
                            var number = int.Parse(((Token)n.Properties["NUMBER_LITERAL"]).TokenValue);
                            v.State.Stack.Push(number);
                        }
                        else
                        {
                            var expr = (Node)n.Properties["expression"];
                            expr.Accept(v);
                            int result = (int)v.State.Stack.Pop();
                            v.State.Stack.Push(result);
                        }
                    }
                );

                return visitor;
            }
        }
    }
}
