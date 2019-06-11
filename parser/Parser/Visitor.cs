using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    /// <summary>
    /// Object that can traverse an abstract syntax tree, using a visitor pattern.
    /// </summary>
    public class Visitor
    {
        public dynamic State = null;

        Dictionary<string, Action<Visitor, Node>> Visitors { get; set; }

        public Visitor(dynamic initialState = null)
        {
            Visitors = new Dictionary<string, Action<Visitor, Node>>();
            if (initialState != null)
                State = initialState;
            else
                State = new ExpandoObject();
        }

        public void AddVisitor(string key, Action<Visitor, Node> visitor)
        {
            this.Visitors.Add(key, visitor);
        }

        public void Visit(Node node)
        {
            var name = node.Name;
            var visitor = this.Visitors.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (visitor == null)
                throw new Exception(string.Format("Visitor not found for '{0}'!", name));

            Visitors[visitor](this, node);
        }
    }
}
