using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests1 = new ProductionRuleTests();
            tests1.DoTests();

            var tests2 = new SqlishTests();
            tests2.DoTests();

            var tests3 = new FooBarBazTests();
            tests3.DoTests();

            var tests4 = new ExpressionTests();
            tests4.DoTests();

            var tests5 = new LeftRecursionTests();
            tests5.DoTests();

            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine("--------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{AbstractTests.Passed} tests passed.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{AbstractTests.Failed} tests failed.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press a key to continue.");
            Console.ReadKey();
        }
    }
}