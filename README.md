# parser
Simple C# lexer and parser

This is a top-down brute-force parse with backtracking which will parse context free grammars.

##Lexer vs Parser rules
Lexer rules and parser rules are included in the same grammar specification. The convention
is that lexer rules must be capitalised and parser rules must start with a lower case.

##Left Recursion
This parser does not support left recursion automatically. However, it does support repeated rules.
Therefore a left-recursive rule can easily be written:

a : a B | C

Can be rewritten as:

a : C B*

##Lexer rules
Lexer rules are defined using simple regex strings. Every string token in your grammar must be defined as 
a lexer rule. 

##Parser rules
Every parser rules must consist of symbols that are either:
a) Parser symbols
b) Lexer symbols

##Matching rules
Rules are checked in the order they are specified. Therefore, when there is possibility
of matching multiple rules, order is important.

