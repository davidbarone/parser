using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class LeftRecursionTests : AbstractTests
    {
        public override void DoTests()
        {
            DoTest("LR1", LeftRecursionGrammar, "1+2-3", "expression", null, null, null, false);
        }

        public string LeftRecursionGrammar2 => @"
NUMBER_LITERAL = ""\d+"";
PLUS_OP = ""\+"";
MINUS_OP = ""\-"";
MUL_OP = ""\*"";
DIV_OP = ""\/"";
LPAREN = ""\("";
RPAREN = ""\)"";
factor = primary;
factor = PLUS_OP, primary;
factor = MINUS_OP, primary;
primary = NUMBER_LITERAL;
primary = LPAREN, expression, RPAREN;
expression' = PLUS_OP, term, expression';
expression' = MINUS_OP, term, expression';
expression = term, expression';
expression' = ε;
term = factor, term';
term' = MUL_OP, factor, term';
term' = DIV_OP, factor, term';
term' = ε;";
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
                    "expression",
                    (v, n) =>
                    {
                        var node = n.Properties.Values.First() as Node;
                        node.Accept(v);
                    }
                );

                visitor.AddVisitor(
                    "minus_plus_expr",
                    (v, n) =>
                    {
                        int sum = 0;
                        var nodes = (IEnumerable<Object>)n.Properties["TERMS"];
                        foreach (var item in nodes)
                        {
                            var node = ((Node)item);
                            node.Accept(v);

                            if (!node.Properties.ContainsKey("OP"))
                            {
                                sum = (int)v.State.Stack.Pop();
                            }
                            else
                            {
                                var sign = ((Token)node.Properties["OP"]).TokenValue;
                                if (sign == "+")
                                {
                                    sum = sum + (int)v.State.Stack.Pop();
                                }
                                else
                                {
                                    sum = sum - (int)v.State.Stack.Pop();
                                }
                            }
                        }
                        v.State.Stack.Push(sum);
                    }
                );

                visitor.AddVisitor(
                    "minus_plus_expr_",
                    (v, n) =>
                    {
                        var node = n.Properties["term"] as Node;
                        node.Accept(v);
                    }
                );

                visitor.AddVisitor(
                    "term",
                    (v, n) =>
                    {
                        var node = n.Properties.Values.First() as Node;
                        node.Accept(v);
                    }
                );

                visitor.AddVisitor(
                    "mul_div_term",
                    (v, n) =>
                    {
                        int sum = 0;
                        var nodes = (IEnumerable<Object>)n.Properties["FACTORS"];
                        foreach (var item in nodes)
                        {
                            var node = ((Node)item);
                            node.Accept(v);

                            if (!node.Properties.ContainsKey("OP"))
                            {
                                sum = (int)v.State.Stack.Pop();
                            }
                            else
                            {
                                var sign = ((Token)node.Properties["OP"]).TokenValue;
                                if (sign == "*")
                                {
                                    sum = sum * (int)v.State.Stack.Pop();
                                }
                                else
                                {
                                    sum = sum / (int)v.State.Stack.Pop();
                                }
                            }
                        }
                        v.State.Stack.Push(sum);
                    }
                );

                visitor.AddVisitor(
                    "mul_div_term_",
                    (v, n) =>
                    {
                        var node = n.Properties["factor"] as Node;
                        node.Accept(v);
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

                visitor.AddVisitor(
                    "mul_term",
                    (v, n) =>
                    {
                        bool one = false;
                        int sum = 0;
                        var nodes = (IEnumerable<Object>)n.Properties["FACTORS"];
                        foreach (var node in nodes)
                        {
                            ((Node)node).Accept(v);
                            if (!one)
                            {
                                sum = (int)v.State.Stack.Pop();
                                one = true;
                            }
                            else
                                sum = (int)v.State.Stack.Pop() * sum;
                        }
                        v.State.Stack.Push(sum);
                    }
                );

                visitor.AddVisitor(
                    "div_term",
                    (v, n) =>
                    {

                        var hasMinus = n.Properties.ContainsKey("MINUS_OP");
                        int number = v.State.Stack.Pop();
                        if (hasMinus)
                            number = number * -1;
                        v.State.Stack.Push(number);
                    }
                );

                return visitor;
            }
        }
    }
}
