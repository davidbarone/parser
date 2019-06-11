using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Represents a node of the AST.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Name of the node is the production rule or token name it matches
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Properties (children) of the production rule.
        /// </summary>
        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public void Accept(Visitor v)
        {
            v.Visit(this);
        }
    }
}
