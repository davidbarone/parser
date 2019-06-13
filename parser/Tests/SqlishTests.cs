﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class SqlishTests
    {
        /// <summary>
        /// Defines the grammar of Sqlish - our 'pseudo SQL' language.
        /// </summary>
        public static string SqlishGrammar => @"

(* Lexer Rules *)

AND             = ""\bAND\b"";
OR              = ""\bOR\b"";
EQ_OP           = ""\bEQ\b"";
NE_OP           = ""\bNE\b"";
LT_OP           = ""\bLT\b"";
LE_OP           = ""\bLE\b"";
GT_OP           = ""\bGT\b"";
GE_OP           = ""\bGE\b"";
LEFT_PAREN      = ""[(]"";
RIGHT_PAREN     = ""[)]"";
COMMA           = "","";
IN              = ""\b(IN)\b"";
CONTAINS        = ""\bCONTAINS\b"";
BETWEEN         = ""\bBETWEEN\b"";
ISBLANK         = ""\bISBLANK\b"";
NOT             = ""\bNOT\b"";
LITERAL_STRING  = ""['][^']*[']"";
LITERAL_NUMBER  = ""[+-]?((\d+(\.\d*)?)|(\.\d+))"";
IDENTIFIER      = ""[A-Z_][A-Z_0-9]*"";
WHITESPACE      = ""\s+"";

(*Parser Rules *)

comparison_operator =   :EQ_OP | :NE_OP | :LT_OP | :LE_OP | :GT_OP | :GE_OP;
comparison_operand  =   :LITERAL_STRING | :LITERAL_NUMBER | :IDENTIFIER;
comparison_predicate=   LHV:comparison_operand, OPERATOR:comparison_operator, RHV:comparison_operand;
in_factor           =   COMMA!, :comparison_operand;
in_predicate        =   LHV:comparison_operand, NOT:NOT?, IN!, LEFT_PAREN!, RHV:comparison_operand, RHV:in_factor*, RIGHT_PAREN!;
between_predicate   =   LHV:comparison_operand, NOT:NOT?, BETWEEN!, OP1:comparison_operand, AND!, OP2:comparison_operand;
contains_predicate  =   LHV:comparison_operand, NOT:NOT?, CONTAINS!, RHV:comparison_operand;
blank_predicate     =   LHV:comparison_operand, NOT:NOT?, ISBLANK;
predicate           =   :comparison_predicate | :in_predicate | :between_predicate | :contains_predicate | :blank_predicate;
boolean_primary     =   :predicate;
boolean_primary     =   LEFT_PAREN!, CONDITION:search_condition, RIGHT_PAREN!;
boolean_factor      =   AND!, :boolean_primary;
boolean_term        =   AND:boolean_primary, AND:boolean_factor*;
search_factor       =   OR!, :boolean_term;
search_condition    =   OR:boolean_term, OR:search_factor*;";

        /// <summary>
        /// Returns a visitor object suitable for parsing Sqlish grammar.
        /// </summary>
        /// <returns></returns>
        private static Visitor SqlishVisitor()
        {
            // Initial state
            dynamic state = new ExpandoObject();
            state.Parameters = new List<SqlParameter>();
            state.Predicates = new Stack<string>();
            state.Sql = string.Empty;

            var visitor = new Visitor(state);

            visitor.AddVisitor(
                "search condition",
                (v, n) =>
                {
                    dynamic searchCondition = n.Properties["OR"];
                    foreach (var item in (IEnumerable<Object>)searchCondition)
                    {
                        var node = item as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");
                        node.Accept(v);
                    }

                    List<string> items = new List<string>();
                    foreach (var item in (IEnumerable<Object>)n.Properties["OR"])
                    {
                        items.Add(v.State.Predicates.Pop());
                    }
                    var sql = string.Format("{0}", string.Join(" OR ", items.ToArray()));
                    v.State.Predicates.Push(sql);
                    v.State.Sql = sql;
                }
            );

            visitor.AddVisitor(
                "boolean term",
                (v, n) =>
                {
                    foreach (var item in (IEnumerable<Object>)n.Properties["AND"])
                    {
                        var node = item as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");
                        node.Accept(v);
                    }

                    List<string> items = new List<string>();
                    foreach (var item in (IEnumerable<Object>)n.Properties["AND"])
                    {
                        items.Add(v.State.Predicates.Pop());
                    }
                    var sql = string.Format("{0}", string.Join(" AND ", items.ToArray()));
                    v.State.Predicates.Push(sql);
                }
            );

            visitor.AddVisitor(
                "boolean primary",
                (v, n) =>
                {
                    // If CONDITION property present, then need to wrap () around condition.
                    if (n.Properties.ContainsKey("CONDITION"))
                    {
                        var node = n.Properties["CONDITION"] as Node;
                        if (node == null)
                            throw new Exception("Array element type not Node.");

                        node.Accept(v);

                        var predicates = ((Stack<string>)v.State.Predicates).Pop();
                        var sql = string.Format("({0})", predicates);
                        v.State.Predicates.Push(sql);
                    }
                }
            );

            visitor.AddVisitor(
                "comparison predicate",
                (v, n) =>
                {
                    Dictionary<string, string> operators = new Dictionary<string, string>()
                            {
                                {"EQ_OP", "="},
                                {"NE_OP", "<>"},
                                {"LT_OP", "<"},
                                {"LE_OP", "<="},
                                {"GT_OP", ">"},
                                {"GE_OP", ">="},
                            };

                    var i = v.State.Parameters.Count;
                    var sql = string.Format(
                        "{0} {1} @{2}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        operators[(string)((Token)n.Properties["OPERATOR"]).TokenName],
                        "P" + i
                    );
                    v.State.Predicates.Push(sql);
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i,
                        Value = ((Token)n.Properties["RHV"]).TokenValue
                    });
                }
            );

            visitor.AddVisitor(
                "in predicate",
                (v, n) =>
                {
                    var i = v.State.Parameters.Count;
                    var sql = "";
                    sql = string.Format(
                        "{0} {1} @{2}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        n.Properties.ContainsKey("NOT") ? "NOT IN" : "IN",
                        "P" + i
                    );

                    // Add the SQL + update args object.
                    v.State.Predicates.Push(sql);
                    object value = ((List<object>)n.Properties["RHV"]).Select(t => ((Token)t).TokenValue.Replace("'", ""));
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i,
                        Value = value
                    });
                }
            );

            visitor.AddVisitor(
                "between predicate",
                (v, n) =>
                {
                    var i = v.State.Parameters.Count;
                    var sql = string.Format(
                        "{0} {1} @{2} AND @{3}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        n.Properties.ContainsKey("NOT") ? "NOT BETWEEN" : "BETWEEN",
                        "P" + i,
                        "P" + (i + 1)
                    );
                    v.State.Predicates.Push(sql);
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i,
                        Value = ((Token)n.Properties["OP1"]).TokenValue
                    });
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i++,
                        Value = ((Token)n.Properties["OP1"]).TokenValue
                    });
                }
            );

            visitor.AddVisitor(
                "contains predicate",
                (v, n) =>
                {
                    var i = v.State.Parameters.Count;
                    var sql = string.Format(
                        "{0} {1} @{2}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        n.Properties.ContainsKey("NOT") ? "NOT LIKE" : "LIKE",
                        "P" + i
                    );
                    v.State.Predicates.Push(sql);
                    v.State.Parameters.Add(new SqlParameter()
                    {
                        ParameterName = "P" + i,
                        Value = ((Token)n.Properties["RHV"]).TokenValue
                    });
                }
            );

            visitor.AddVisitor(
                "blank predicate",
                (v, n) =>
                {
                    var i = v.State.Parameters.Count;
                    var sql = string.Format(
                        "{0} {1}",
                        ((Token)n.Properties["LHV"]).TokenValue,
                        n.Properties.ContainsKey("NOT") ? "IS NOT NULL" : "IS NULL"
                    );
                    v.State.Predicates.Push(sql);
                }
            );

            return visitor;
        }



    }
}