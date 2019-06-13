# parser
A simple C# lexer and parser

This is a top-down brute-force parser with backtracking which will parse context free grammars. It is a very simplistic parser and intended for simple DSLs. The parser takes an input and can provide the following services:
- Lexically analyse the input into a series of tokens (tokenising)
- Parse the tokens into an abstract syntax tree (parsing), or
- Navigation through the abstract syntax tree, using a visitor class.

The parser requires a grammar to be specified. This grammar can be specified using a 'BNF-ish' syntax, or through a special `ProductionRule` class. A `Visitor` class is provided to allow developers to implement logic to navigate or 'walk' the abstract syntax trees.

## Grammar
A grammar is used to define the language for the parser. This grammar can be created in 2 ways:
- Creating an enumeration of `ProductionRule` objects
- Creating a grammar using 'BNF-ish' syntax

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
(The above is actually the grammar for specifying the 'BNF-ish' grammar used by this tool)

Each line specifies an 'expansion' rule. Rules can be either:
- Lexer rules
- Production rules

Rules are named using alpha-numeric characters, or the underscore character. Rule names must start with an alpha character. Lexer rules are defined as requiring an uppercase first character, and parser rules must start with a lower case character.

Lexer rules define terminal symbols in the grammar. Every possible terminal symbol must be defined explicitly as a lexer rule (Parser rules cannot use literal symbols). Each lexer rule maps to a single literal expansion only. The expansion is written using a regex.

Parser rules define non-terminal symbols. Parser rules general map to a set of lexer rules or other parser rules. The general format of a parser symbol is:

`(alias(:))symbol(modifier)`

The alias and modifier parts are options. In a simple case, the symbol is the name of another rule (either lexer or parser rule). symbols in a parser rule can be other parser rules which in turn expand into other parser rules, and hence in this fashion a complex grammar can be specified.

### Specifying a grammar using BNF-ish syntax
A grammar can also be specified in a format similar to BNF/EBNF for example:
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
The above grammar specifies an 'SQL-ish' grammar for constructing a 'filter' expression. Again, the same rules apply for lexer rules and parser rules. Multiple alternate expansions of a rule can be separated by '|', and a series of symbols are separated by a comma. The full set of symbols allows in the grammar is shown below:

|Symbol    |Description                              |
|:--------:|:----------------------------------------|
|=         |Assignment of production rule            |
|:         |Alias/rewrite modifier                   |
|\|        |Alternate expansion                      |
|,         |Sequence / contanenation                 |
|;         |Termination of rule                      |
|(\*...\*) |Comment                                  |
|?         |Symbol modifier - 0 or 1 times (optional)|
|+         |Symbol modifier - 1 or more times        |
|*         |Symbol modifier - 0 or more times        |
|!         |Symbol modifier - ignore result from ast |

## Tokeniser
The tokeniser uses regex expressions as rules. Any valid C# regex can be used. Note that every string token in your input must be defined as a lexer rule. There is no support for literal tokens defined in parser rules. All parser rules must reference either other parser rules, or lexer rules.

## Tree Generation and Rule Modifiers
The result of the `Parser.Parse()` method is an abstract syntax tree. The structure of the tree is designed to be close to the grammar. for example, given a grammar:
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
In general, a non-terminal node is represented using the `Node` class, and terminal / leaf nodes are represented using the `Token` class. The `Name` property provides the name of non-terminal nodes, and the `TokenName` property provides the name of tokens (the `TokenValue` provides the actual value of a token). This is the default behaviour without specifying any aliases or modification rules.

In the above example, FOO, PLUS, BAR, and BAZ are all represented by `Token` objects. Each `Node` object contains a `Properties` dictionary which provides access to the child / leaf nodes. By default, the key of each property is the same as the child name. Therefore, to access the input value "FOO" in the tree, you would use:

`fbb.Properties["fb"].Properties["FOO"].TokenValue`

### Ignoring Items
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
### Aliases / Rewrites of Symbols
Another way of manipulating the tree is to rename properties. This is done via the `alias:symbol` syntax. for example, if we change the grammar to be:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = FB1:FOO,PLUS!,FB2:BAR;
fbb     = fb,PLUS!,BAZ;
```
The tree is changed as follows:
```
             fbb
              |
        -------------
        |           |
        fb         BAZ
        |
   -----------
   |         |
  FB1       FB2
```
Note how the properties of the 'fb' node have been changed. The contents of these are still the same (i.e. FOO and BAR), but the *names* have changed. All this does is change the keys used in the Properties dictionary. If a rule uses the same alias for more than one symbol, then the results are automatically grouped into a collection. For example, using a grammar:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = FB:FOO,PLUS!,FB:BAR;
fbb     = fb,PLUS!,BAZ;
```
(Note both symbols in the 'fb' rule are now aliased to 'FB'), the following tree is formed:
```
             fbb
              |
        -------------
        |           |
        fb         BAZ
        |
        FB
```
In this case, the object referenced at `fbb.Properties["fb"].Properties["FB"]` is of type `IEnumerable<Token>` and contains both FOO and BAR.

A special renaming case is to use a blank name, for example:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = :FOO,PLUS!,:BAR;
fbb     = fb,PLUS!,BAZ;
```
Here, for the 'fb' rule, we've removed the aliases, but kept the colon (read as: 'rename to empty'). By *not* providing a property name, instead of creating a node under fb, the objects get collapsed up the tree, so the new tree looks like this:
```
             fbb
              |
        -------------
        |           |
        fb         BAZ
```
In this example, the IEnumerable<Token> object has been collapsed up the tree, and is now referenced at `fbb.Properties["fb"].`.

*Note that a constraint exists that a rule must not contain a mixture of blank/non blank aliases. If a blank alias is specified, then ALL symbols in the rule must also have a blank alias.*

Making a further modification to the grammar:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = :FOO,PLUS!,:BAR;
fbb     = ITEMS:fb,PLUS!,ITEMS:BAZ;
```
Results in the tree being flattened further:
```
             fbb
              |
            ITEMS 
```
In this case, the ITEMS node (referenced by `fbb.Properties["ITEMS"] contains a collection of the 3 items, ['FOO', 'BAR', 'BAZ']).

### Optional Modifier
Another modifier is the 'optional' modifier (? or *). These allow input to be optional. For example, changing the original grammar to:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = FOO,PLUS?,BAR;
fbb     = fb,PLUS?,BAZ;
```
Then the any of the following inputs will be parsed correctly:
```
FOO+BAR+BAZ
FOO+BARBAZ
FOOBAR+BAZ
FOOBARBAZ
```
When an optional symbol is not matched, then no child is inserted into the tree.

### Many Modifier
The 'many' modifiers (+ and *) allow a symbol to be repeated more than once. For example:
```
FOO     = "FOO";
BAR     = "BAR";
BAZ     = "BAZ";     
PLUS    = "[+]";
fb      = FOO+,PLUS,BAR*;
fbb     = fb,PLUS,BAZ;
```
will match:
```
FOO+BAR+BAZ
FOOFOO+BAR+BAZ
FOOFOOFOO+BAR+BAZ
FOO++BAZ
FOO+BARBAR+BAZ
FOO+BARBARBAR+BAZ
```
When a symbol is modified with the 'many' modifiers, they still occupy a single node or child property in the tree. However, the contents of the node becomes an `IEnumerable` which can be iterated over.

## Ordering of rules
the order of rules is important. As the parser adopts a brute force approach, it will continue looking for matching rules until the first match. Subsequent rules will be ignored. If a failure occurs at a nested position, the parser will backtrack from the point of failure, and continue looking for matching rules. If all paths are attempted without a match, parsing of the input fails.

## Left Recursion
This parser does not support left recursion automatically. However, it does support repeated rules.
Therefore a left-recursive rule can easily be written:

`a : a B | C`

Can be rewritten as:

`a : C B*`

Additionally, the alias modification rules mean that C & B can be 'aliased' in the tree for improved semantics.

## Processing a Tree Using the Visitor Class
A `Visitor` class is included which allows for an abstract syntax tree to be processed. A new visitor is created using:

`var visitor = new Visitor(initialState)`

The `initialState` parameter is a `dynamic` object, providing any initial state to the tree processing function. Visitor handlers are then added. A visitor typically processes a portion of the tree:

`visitor.AddVisitor(ruleName, (visitor, node)=> {...});`

The first parameter is the name of the rule which is processed by the handler. The second parameter is a function which takes 2 parameters:
- The `Visitor` class
- The current node being evaluated
The body of this function is then free to process the node in any way.

State can be persisted across handlers by using the `Visitor.State` property.

--- end ---