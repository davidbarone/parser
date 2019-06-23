using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public static class Extensions
    {
        /// <summary>
        /// PrettyPrints a node tree structure.
        /// </summary>
        /// <param name="node">The node. Can be type Node (non leaf) or Token (leaf).</param>
        /// <param name="indent">The indent level.</param>
        /// <param name="isLastChild">True if last child.</param>
        /// <returns></returns>
        public static string PrettyPrint(this object node, string indent, bool isLastChild)
        {
            var nodeAsNode = node as Node;
            var nodeAsToken = node as Token;
            string output = "";

            if (nodeAsNode == null && nodeAsToken == null)
            {
                throw new Exception("Node must be a Node or Token object.");
            }
            else if (nodeAsNode != null)
            {
                output = indent + "+- " + nodeAsNode.Name + System.Environment.NewLine;
                indent += isLastChild ? "   " : "|  ";

                // print children too
                List<string> keys = nodeAsNode.Properties.Keys.ToList();

                for (int i = 0; i < keys.Count(); i++)
                {
                    var key = keys[i];
                    var childAsIEnumerable = nodeAsNode.Properties[key] as IEnumerable<object>;
                    if (childAsIEnumerable != null)
                    {
                        foreach (var item in childAsIEnumerable)
                        {
                            output += item.PrettyPrint(indent, i == keys.Count() - 1 && item==childAsIEnumerable.Last());
                        }
                    } else
                    {
                        output += nodeAsNode.Properties[key].PrettyPrint(indent, i == keys.Count() - 1);
                    }
                }
            }
            else
            {
                output += indent + "+- " + $"{nodeAsToken.TokenName} [{nodeAsToken.TokenValue}]" + System.Environment.NewLine;
            }
            return output;
        }

        /// <summary>
        /// Unions 2 objects together into a enumerable. Individual
        /// objects can be enumerables or plain objects.
        /// </summary>
        /// <param name="a">The source object.</param>
        /// <param name="obj">The object to be unioned.</param>
        /// <returns></returns>
        public static IEnumerable<object> Union(this object a, object obj)
        {
            List<object> results = new List<object>();
            var enumerableA = a as System.Collections.IEnumerable;
            var enumerableObj = obj as System.Collections.IEnumerable;

            if (enumerableA != null)
            {
                foreach (var item in enumerableA)
                    results.Add(item);
            }
            else if (a != null)
                results.Add(a);
            else
                throw new Exception("error!");

            if (enumerableObj != null)
            {
                foreach (var item in enumerableObj)
                    results.Add(item);
            }
            else if (obj != null)
                results.Add(obj);
            else
                throw new Exception("error!");

            return results;
        }

        /// <summary>
        /// Clones the tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public static IList<Token> Clone(this IList<Token> tokens)
        {
            List<Token> temp = new List<Token>();
            foreach (var token in tokens)
                temp.Add(token);

            return temp;
        }
    }
}
