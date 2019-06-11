using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// A terminal token within the input.
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Describes the token.
        /// </summary>
        public string TokenName { get; set; }
        /// <summary>TOke
        /// The actual value of the token.
        /// </summary>
        public string TokenValue { get; set; }
    }
}
