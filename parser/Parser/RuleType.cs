using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Defines the type of production rule.
    /// </summary>
    public enum RuleType
    {
        // Terminal symbol
        LexerRule,

        // Non Terminal Symbol
        ParserRule
    }
}
