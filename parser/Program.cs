using Parser.Tests;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests1 = new ProductionRuleTests();
            tests1.DoTests();

            var tests2 = new SqlishTests();
            tests2.DoTests();

            Console.WriteLine("Summary:");
            Console.WriteLine("--------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{SqlishTests.Passed} tests passed.");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{SqlishTests.Failed} tests failed.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press a key to continue.");
            Console.ReadKey();
        }
    }
}