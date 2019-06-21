using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    /// <summary>
    /// Represents a non-leaf node of the abstract syntax tree.
    /// </summary>
    public class Node
    {
        public Node(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Name of the node. Equivalent to the name of the symbol it matches, or its alias.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Properties (children) of the production rule.
        /// </summary>
        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        /// <summary>
        /// Accepts a visitor on this node.
        /// </summary>
        /// <param name="v"></param>
        public void Accept(Visitor v)
        {
            v.Visit(this);
        }
    }
}
