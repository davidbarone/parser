using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public class SubRuleTests : AbstractTests
    {
        public override void DoTests()
        {
            DoTest("SUBRULE_1", SubRuleGrammar, "4", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 4, false);
            DoTest("SUBRULE_2", SubRuleGrammar, "-4", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), -4, false);
            DoTest("SUBRULE_3", SubRuleGrammar, "9+9", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 18, false);
            DoTest("SUBRULE_4", SubRuleGrammar, "1+2+3+4", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 10, false);
            DoTest("SUBRULE_5", SubRuleGrammar, "2*3", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 6, false);
            DoTest("SUBRULE_6", SubRuleGrammar, "1+2*3", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 7, false);
            DoTest("SUBRULE_7", SubRuleGrammar, "(1+2)*3", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 9, false);
            DoTest("SUBRULE_8", SubRuleGrammar, "2*-3", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), -6, false);
            DoTest("SUBRULE_9", SubRuleGrammar, "-2*-3", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 6, false);
            DoTest("SUBRULE_10", SubRuleGrammar, "3*4+5*6", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 42, false);
            DoTest("SUBRULE_11", SubRuleGrammar, "7-4", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 3, false);
            DoTest("SUBRULE_12", SubRuleGrammar, "10-3+2", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 9, false);
            DoTest("SUBRULE_13", SubRuleGrammar, "10-2*3+4*5", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 24, false);
            DoTest("SUBRULE_14", SubRuleGrammar, "10--2*3+4*5", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 36, false);
            DoTest("SUBRULE_15", SubRuleGrammar, "10+8/2-2*5", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 4, false);
            DoTest("SUBRULE_16", SubRuleGrammar, "((((1+7)/(3-1))/2)*(5+2)+(-7+15)-(-2*-4))", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 14, false);
            DoTest("SUBRULE_17", SubRuleGrammar, "6*2/3", "expression", SubRuleVisitor, (d) => (int)d.Stack.Pop(), 4, false);
        }

        public string SubRuleGrammar => @"
NUMBER_LITERAL  = ""\d+"";
PLUS_OP         = ""\+"";
MINUS_OP        = ""\-"";
MUL_OP          = ""\*"";
DIV_OP          = ""\/"";
LPAREN         = ""\("";
RPAREN         = ""\)"";

expression      = TERMS:(:term, :(OP:MINUS_OP, term | OP:PLUS_OP, term)*);
term            = FACTORS:(:factor, :(OP:DIV_OP, factor | OP:MUL_OP, factor)*);
factor          = primary | PLUS_OP, primary | MINUS_OP, primary;
primary         = NUMBER_LITERAL | LPAREN, expression, RPAREN;";

        public Visitor SubRuleVisitor
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
                    "term",
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
