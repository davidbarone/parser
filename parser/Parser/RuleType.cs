using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Specifies the type of rule. Rules either represent terminal nodes (lexer rules)
    /// or non-terminal nodes (parser rules).
    /// </summary>
    public enum RuleType
    {
        // Terminal symbol
        LexerRule,

        // Non Terminal Symbol
        ParserRule
    }
}
