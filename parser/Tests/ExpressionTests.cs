using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class ExpressionTests : Tests
    {
        /*
         stack overflow 
        http://codeability.blogspot.com/2012/04/chapter-6-syntax.html
            */

        public override void DoTests()
        {
            DoTest("EXPR_1", ExpressionGrammar, "9+9", "expression", ExpressionVisitor, (d)=>(int)d.Stack.Pop(), 18, false);
            DoTest("EXPR_2", ExpressionGrammar, "1+2+3+4", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 10, false);
            DoTest("EXPR_3", ExpressionGrammar, "2*3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 6, false);
            DoTest("EXPR_4", ExpressionGrammar, "1+2*3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 7, false);
            DoTest("EXPR_5", ExpressionGrammar, "2*-3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), -6, false);
            DoTest("EXPR_6", ExpressionGrammar, "-2*-3", "expression", ExpressionVisitor, (d) => (int)d.Stack.Pop(), 6, false);
        }

        public string ExpressionGrammar => @"
NUMBER_LITERAL  = ""\b\d+\b"";
PLUS_OP         = ""[\+]"";
MINUS_OP        = ""[\-]"";
MUL_OP          = ""\*"";
DIV_OP          = ""[\/]"";
LPARENS         = ""[\(]"";
RPARENS         = ""[\)]"";

expression      = term | plus_expr | minus_expr;
minus_expr      = TERMS:term, TERMS:minus_expr_*;
minus_expr_     = MINUS_OP!, :term;
plus_expr       = TERMS:term, TERMS:plus_expr_*;
plus_expr_      = PLUS_OP!, :term;
term            = mul_term | div_term | factor;
mul_term        = FACTORS:factor, FACTORS:mul_term_*;
mul_term_       = MUL_OP!, :factor;
div_term        = FACTORS:factor, FACTORS:div_term_*;
div_term_       = DIV_OP!, :factor;
factor          = primary | PLUS_OP, primary | MINUS_OP, primary;
primary         = NUMBER_LITERAL;";

        public Visitor ExpressionVisitor
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
                    "plus_expr",
                    (v, n) =>
                    {
                        int sum = 0;
                        var nodes = (IEnumerable<Object>)n.Properties["TERMS"];
                        foreach (var node in nodes)
                        {
                            ((Node)node).Accept(v);
                            sum = sum + (int)v.State.Stack.Pop();
                        }
                        v.State.Stack.Push(sum);
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
                        var number = int.Parse(((Token)n.Properties["NUMBER_LITERAL"]).TokenValue);
                        v.State.Stack.Push(number);
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
