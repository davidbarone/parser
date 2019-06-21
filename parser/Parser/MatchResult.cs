using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    /// <summary>
    /// Result from the Match method. Used to match tokens.
    /// </summary>
    public class MatchResult
    {
        public bool Success { get; set; }
        public string Matched { get; set; }
        public string Remainder { get; set; }
    }
}
