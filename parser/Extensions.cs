using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    public static class Extensions
    {
        /// <summary>
        /// Unions 2 objects together into a enumerable. Individual
        /// objects can be enumerables or plain objects. The objects
        /// can also be a node of type SYMBOL_MANY. In this case, we
        /// take the first / only property (which should be an
        /// IEnumerable), and union this instead.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<object> Union(this object a, object obj)
        {
            List<object> results = new List<object>();
            var enumerableA = a as System.Collections.IEnumerable;
            var enumerableObj = obj as System.Collections.IEnumerable;

            var nodeA = a as Node;
            var nodeObj = obj as Node;

            if (enumerableA != null)
            {
                foreach (var item in enumerableA)
                    results.Add(item);
            }
            else if (nodeA != null && nodeA.Name == "SYMBOL_MANY")
            {
                var list = nodeA.Properties.First().Value as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var item in list)
                        results.Add(item);
                }
            }
            else if (a != null)
                results.Add(a);

            if (enumerableObj != null)
            {
                foreach (var item in enumerableObj)
                    results.Add(item);
            }
            else if (nodeObj != null && nodeObj.Name == "SYMBOL_MANY")
            {
                var list = nodeObj.Properties.First().Value as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var item in list)
                        results.Add(item);
                }
            }
            else if (obj != null)
                results.Add(obj);

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
