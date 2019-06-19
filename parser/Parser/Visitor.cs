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
        /// <summary>
        /// Creates a new empty visitor.
        /// </summary>
        /// <param name="initialState">Optional initialisation of state</param>
        public Visitor(dynamic initialState = null)
        {
            Visitors = new Dictionary<string, Action<Visitor, Node>>();
            if (initialState != null)
                State = initialState;
            else
                State = new ExpandoObject();
        }

        /// <summary>
        /// Provides state to the visitor.
        /// </summary>
        public dynamic State = null;

        /// <summary>
        /// Default visitor which simply cascades through all properties
        /// calling the Visit() method recursively.
        /// </summary>
        /// <param name="v">The visitor.</param>
        /// <param name="n">The current node.</param>
        private void DefaultVisitor(Visitor v, Node n)
        {
            foreach (var key in n.Properties.Keys)
            {
                var item = n.Properties[key];
                var itemAsNode = item as Node;
                var itemAsIEnumerable = item as IEnumerable<Object>;
                if (itemAsNode != null)
                    itemAsNode.Accept(v);
                else if (itemAsIEnumerable != null)
                {
                    foreach (var obj in itemAsIEnumerable)
                    {
                        var objAsNode = obj as Node;
                        if (objAsNode != null)
                            objAsNode.Accept(v);
                    }
                }
            }
        }

        /// <summary>
        /// A collection of routines that can process single node types within the abstract syntax tree.
        /// </summary>
        Dictionary<string, Action<Visitor, Node>> Visitors { get; set; }

        /// <summary>
        /// Adds a new visitor.
        /// </summary>
        /// <param name="key">The name of the production rule this visitor can traverse.</param>
        /// <param name="visitor">The navigation handler / logic.</param>
        public void AddVisitor(string key, Action<Visitor, Node> visitor)
        {
            this.Visitors.Add(key, visitor);
        }

        /// <summary>
        /// Called by the engine.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        public void Visit(Node node)
        {
            var name = node.Name;
            var visitor = this.Visitors.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (visitor == null) {
                //throw new Exception(string.Format("Visitor not found for '{0}'!", name));
                DefaultVisitor(this, node);
            }
            else
            {
                Visitors[visitor](this, node);
            }
        }
    }
}
