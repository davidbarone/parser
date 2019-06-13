# parser
Simple C# lexer and parser

This is a top-down brute-force parse with backtracking which will parse context free grammars. It is a very simplistic parser and intended for simple DSLs. The parser takes a input and can provide the following services:
(a) lexically analyse the string into a series of tokens
(b) parse the tokens into an abstract syntax tree, or
(c) Process the abstract syntax tree, to execute the input in some way.

The parser requires a grammar to be specified. This grammar can be specified in a 'EBNF-Like' fashion, or through a special 'ProductionRule' class. A dynamic visitor class is provided to allow developers to implement logic to process the AST. There is no pre-processing step rq

## Grammar
A grammar is used to define the language for the parser. This grammar can be created in 2 ways:
(a) Creating an enumeration of ProductionRule objects
(b) Creating a grammar vaguely similar to BNF/EBNF

### Using Production Rules for Grammar
A grammar looks something like this:

```
            // Lexer Rules
            new ProductionRule("COMMENT", @"\(\*.*\*\)"), // (*...*)
            new ProductionRule("EQ", "="),                  // definition
            new ProductionRule("COMMA", "[,]"),               // concatenation
            new ProductionRule("COLON", "[:]"),               // rewrite / aliasing
            new ProductionRule("SEMICOLON", ";"),           // termination
            new ProductionRule("MODIFIER", "[?!+*]"),      // modifies the symbol
            new ProductionRule("OR", @"[|]"),                 // alternation
            new ProductionRule("QUOTEDLITERAL", @"""(?:[^""\\]|\\.)*"""),
            new ProductionRule("IDENTIFIER", "[a-zA-Z][a-zA-Z0-9_]+"),
            new ProductionRule("NEWLINE", "\n"),

            // Parser Rules
            new ProductionRule("alias", ":IDENTIFIER?", ":COLON"),
            new ProductionRule("symbol", "ALIAS:alias?", "IDENTIFIER:IDENTIFIER", "MODIFIER:MODIFIER?"),
            new ProductionRule("parserSymbolTerm", ":symbol"),
            new ProductionRule("parserSymbolFactor", "COMMA!", ":symbol"),
            new ProductionRule("parserSymbolExpr", "SYMBOL:parserSymbolTerm", "SYMBOL:parserSymbolFactor*"),
            new ProductionRule("parserSymbolsFactor", "OR!", ":parserSymbolExpr"),
            new ProductionRule("parserSymbolsExpr", "ALTERNATE:parserSymbolExpr", "ALTERNATE:parserSymbolsFactor*"),

            new ProductionRule("rule", "RULE:IDENTIFIER", "EQ!", "EXPANSION:QUOTEDLITERAL", "SEMICOLON!"),      // Lexer rule
            new ProductionRule("rule", "RULE:IDENTIFIER", "EQ!", "EXPANSION:parserSymbolsExpr", "SEMICOLON!"),  // Parser rule
            new ProductionRule("grammar", "RULES:rule+")
```

(The above is actually the grammar for specifying the 'BNF-like' grammar used by this tool)

Each line specifies an 'expansion' rule. Rules can be either:
(a) Lexer rules
(b) Production rules

Rules are named using alpha-numeric characters, or the underscore character. Rule names must start with an alpha character. Lexer rules are defined as requiring an uppercase first character, and parser rules must start with a lower case character.

Lexer rules define terminal symbols in the grammar. Every possible terminal symbol must be defined explicitly as a lexer rule (Parser rules cannot use literal symbols). Each lexer rule maps to a single literal expansion only. The expansion is written using a regex.

Parser rules define non-terminal symbols. Parser rules general map to a set of lexer rules or other parser rules. The general format of a parser symbol is:

`(alias(:))symbol(modifier)`

The alias and modifier parts are options. In a simple case, the symbol is the name of another rule (either lexer or parser rule).

### Specifying a grammar in BFN style

A grammar can also be specified in a format similar to BNFm for example:

```
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
```

The above grammar specifies an 'SQL-like' grammar. Again, the same rules apply for lexer rules and parser rules.

# Tokeniser
The tokeniser uses regex expressions as rules.
## Lexer vs Parser rules
Lexer rules and parser rules are included in the same grammar specification. The convention
is that lexer rules must be capitalised and parser rules must start with a lower case.

## Ordering of rules
the order of rules is important. As the parser adopts a brute force approach, it will continue looking for matching rules until the first match.

## Left Recursion
This parser does not support left recursion automatically. However, it does support repeated rules.
Therefore a left-recursive rule can easily be written:

`a : a B | C`

Can be rewritten as:

`a : C B*`

Additionally, the rewriting rules mean that C & B can be 'joined' in the tree for improved semantics.

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

