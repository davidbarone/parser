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
