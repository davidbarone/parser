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
var grammar = new List<ProductionRule>
{
    // Lexer Rules
    new ProductionRule("COMMENT", @"\(\*.*\*\)"),                   // (*...*)
    new ProductionRule("EQ", "="),                                  // definition
    new ProductionRule("COMMA", "[,]"),                             // concatenation
    new ProductionRule("COLON", "[:]"),                             // rewrite / aliasing
    new ProductionRule("SEMICOLON", ";"),                           // termination
    new ProductionRule("MODIFIER", "[?!+*]"),                       // modifies the symbol
    new ProductionRule("OR", @"[|]"),                               // alternation
    new ProductionRule("QUOTEDLITERAL", @"""(?:[^""\\]|\\.)*"""),   // quoted literal
    new ProductionRule("IDENTIFIER", "[a-zA-Z][a-zA-Z0-9_]+"),      // rule names
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
}
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

The above grammar specifies an 'SQL-like' grammar. Again, the same rules apply for lexer rules and parser rules. Multiple alternate expansions of a rule can be separated by '|', and a series of symbols are separated by a comma. The full set of symbols is shown below:

|Symbol    |Description                            |
|:--------:|:------------------------------------- |
|=         |Assignment of production rule          |
|:         |Alias/rewrite node property name       |
|\|        |Alternate expansion                    |
|,         |Sequence / contanenation               |
|;         |Termination of rule                    |
|(\*...\*) |Comment                                |
|?         |Rule modifier - 0 or 1 times (optional)|
|+         |Rule modifier - 1 or more times        |
|*         |Rule modifier - 0 or more times        |
|!         |Rule modifier - ignore result from ast |

## Tree Generation and Rule Modifiers
The result of the .Parse() method is an abstract syntax tree. The structure of the tree is generally designed to be close to the grammar. for example, given a grammar:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = FOO,PLUS,BAR;
fbb     = fb,PLUS,BAZ;
```
and and input of:
`FOO+BAR+BAZ`

Then the string gets parsed into a tree as follows:
```
             fbb
              |
        -------------
        |     |     |
        fb   PLUS  BAZ
        |
   -----------
   |    |    |
  FOO  PLUS BAR
```
In general, a non-terminal node is represented using the `Node` class, and terminal / leaf nodes are represented using the `Token` class. The `Name` property provides the name of non-terminal nodes, and the `TokenName` property provides the name of tokens (the `TokenValue` provides the actual value of a token). This is the default behaviour withough specifying any aliases or modification rules.

In the above example, FOO, PLUS, BAR, and BAZ are all represented by `Token` objects. Each `Node` object contains a `Properties` dictionary which provides access to the child nodes. By default, the key of each property is the same as the child name. Therefore, to access the input value "FOO" in the tree, you would use:

`fbb.Properties["fb"].Properties["FOO"].TokenValue`

Sometimes you need to manipulate the tree. The first way to manipulate the tree is by ignoring nodes. In the example above, the PLUS symbols don't really add much semantics to the tree. We can have the parser remove these completely, by changing the grammar to:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = FOO,PLUS!,BAR;
fbb     = fb,PLUS!,BAZ;
```
The resulting tree would be:
```
             fbb
              |
        -------------
        |           |
        fb         BAZ
        |
   -----------
   |         |
  FOO       BAR
```


### Ignoring nodes
for example, in the above case



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

