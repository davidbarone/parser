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
            DoTest("LEFT_RECURSION_1", LeftRecursionGrammar, "1+2*3", "expression", LeftRecursionVisitor, (d)=> (int)d.Stack.Pop(), 7, false);
        }

        public string LeftRecursionGrammar => @"
NUMBER_LITERAL  = ""\d+"";
PLUS_OP         = ""\+"";
MINUS_OP        = ""\-"";
MUL_OP          = ""\*"";
DIV_OP          = ""\/"";
LPAREN         = ""\("";
RPAREN         = ""\)"";

expression      = expression, OP:PLUS_OP, term | expression, OP:MINUS_OP, term | term;
term            = factor | term, OP:MUL_OP, factor | term, OP:DIV_OP, factor;
factor          = primary | OP:PLUS_OP, primary | OP:MINUS_OP, primary;
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
                    "expression",
                    (v, n) =>
                    {
                        int? expression = null;
                        Token op = null;

                        var node = (Node)n.Properties["term"];
                        node.Accept(v);
                        var term = v.State.Stack.Pop();

                        if (n.Properties.ContainsKey("expression"))
                        {
                            var t = (Node)n.Properties["expression"];
                            t.Accept(v);
                            expression = v.State.Stack.Pop();
                            op = (Token)n.Properties["OP"];
                        }

                        if (!expression.HasValue)
                            v.State.Stack.Push(term);
                        else
                        {
                            if (op!=null && op.TokenName == "PLUS_OP")
                                v.State.Stack.Push(expression + term);
                            else
                                v.State.Stack.Push(expression - term);
                        }
                    }
                );

                visitor.AddVisitor(
                    "term",
                    (v, n) =>
                    {
                        Token op = null;
                        var node = (Node)n.Properties["factor"];
                        node.Accept(v);
                        var factor = v.State.Stack.Pop();
                        int? term = null;

                        if (n.Properties.ContainsKey("term"))
                        {
                            var t = (Node)n.Properties["term"];
                            t.Accept(v);
                            term = v.State.Stack.Pop();

                            op = (Token)n.Properties["OP"];
                        }


                        if (!term.HasValue)
                            v.State.Stack.Push(factor);
                        else
                        {
                            if (op.TokenName=="MUL_OP")
                                v.State.Stack.Push(term * factor);
                            else
                                v.State.Stack.Push(term / factor);
                        }
                    }
                );

                visitor.AddVisitor(
                    "factor",
                    (v, n) =>
                    {
                        var node = (Node)n.Properties["primary"];
                        node.Accept(v);
                        var primary = v.State.Stack.Pop();

                        var factor = 1;
                        if (n.Properties.ContainsKey("OP"))
                        {
                            var token = (Token)n.Properties["OP"];
                            factor = token.TokenName == "PLUS_OP" ? 1 : -1;
                        }
                        int number = primary * factor;
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
