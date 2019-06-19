using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class ExpressionTests : AbstractTests
    {
        public override void DoTests()
        {
            DoTest("EXPR_1", ExpressionGrammar, "4", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 4, false);
            DoTest("EXPR_2", ExpressionGrammar, "-4", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), -4, false);
            DoTest("EXPR_3", ExpressionGrammar, "9+9", "expression", ExpressionVisitor, (d)=>(int)d.Stack.Pop(), 18, false);
            DoTest("EXPR_4", ExpressionGrammar, "1+2+3+4", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 10, false);
            DoTest("EXPR_5", ExpressionGrammar, "2*3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 6, false);
            DoTest("EXPR_6", ExpressionGrammar, "1+2*3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 7, false);
            DoTest("EXPR_7", ExpressionGrammar, "(1+2)*3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 9, false);
            DoTest("EXPR_8", ExpressionGrammar, "2*-3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), -6, false);
            DoTest("EXPR_9", ExpressionGrammar, "-2*-3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 6, false);
            DoTest("EXPR_10", ExpressionGrammar, "3*4+5*6", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 42, false);
            DoTest("EXPR_11", ExpressionGrammar, "7-4", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 3, false);
            DoTest("EXPR_12", ExpressionGrammar, "10-3+2", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 9, false);
            DoTest("EXPR_13", ExpressionGrammar, "10-2*3+4*5", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 24, false);
            DoTest("EXPR_14", ExpressionGrammar, "10--2*3+4*5", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 36, false);
            DoTest("EXPR_15", ExpressionGrammar, "10+8/2-2*5", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 4, false);
            DoTest("EXPR_16", ExpressionGrammar, "((((1+7)/(3-1))/2)*(5+2)+(-7+15)-(-2*-4))", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 14, false);
            DoTest("EXPR_17", ExpressionGrammar, "6*2/3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 4, false);
        }

        public string ExpressionGrammar => @"
NUMBER_LITERAL  = ""\d+"";
PLUS_OP         = ""\+"";
MINUS_OP        = ""\-"";
MUL_OP          = ""\*"";
DIV_OP          = ""\/"";
LPAREN         = ""\("";
RPAREN         = ""\)"";

expression      = minus_plus_expr | term;
minus_plus_expr = TERMS:term, TERMS:minus_plus_expr_*;
minus_plus_expr_
                = OP:MINUS_OP, term | OP:PLUS_OP, term;

term            = mul_div_term | factor;
mul_div_term    = FACTORS:factor, FACTORS:mul_div_term_*;
mul_div_term_   = OP:DIV_OP, factor | OP:MUL_OP, factor;

factor          = primary | PLUS_OP, primary | MINUS_OP, primary;
primary         = NUMBER_LITERAL | LPAREN, expression, RPAREN;";

        public Visitor ExpressionVisitor
        {
            get
            {
                // Initial state
                dynamic state = new ExpandoObject();
                state.Stack = new Stack<int>();

                var visitor = new Visitor(state);

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
                            } else
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
                        } else
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
