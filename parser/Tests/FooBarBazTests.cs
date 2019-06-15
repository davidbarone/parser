using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.Tests
{
    public class FooBarBazTests : Tests
    {
        public override void DoTests()
        {
            TestVisitor(FooBarBazGrammar, "FOO", "fbb", FooBarBazVisitor, (d) => d.items.Count, 1);
            TestVisitor(FooBarBazGrammar, "FOOBAR", "fbb", FooBarBazVisitor, (d) => d.items.Count, 2);
            TestVisitor(FooBarBazGrammar, "FOOBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 3);
            TestVisitor(FooBarBazGrammar, "FOOBARBAZBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 4);
            TestVisitor(FooBarBazGrammar, "FOOBARBAZBAZBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 5);
            TestVisitor(FooBarBazGrammar, "FOOBARBAR", "fbb", FooBarBazVisitor, (d) => d.items.Count, 3);
            TestVisitor(FooBarBazGrammar, "FOOBARBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 4);
            TestVisitor(FooBarBazGrammar, "FOOBARBARBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 5);
            TestVisitor(FooBarBazGrammar, "FOOBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 3);
        }

        public string FooBarBazGrammar => @"
FOO     = ""FOO"";
BAR     = ""BAR"";
BAZ     = ""BAZ"";
fb      = :FOO,:BAR*;
fbb     = ITEMS:fb,ITEMS:BAZ*;
";

        private Visitor FooBarBazVisitor
        {
            get
            {
                // Initial state
                dynamic state = new ExpandoObject();
                state.items = new List<Token>();

                var visitor = new Visitor(state);

                visitor.AddVisitor(
                    "fbb",
                    (v, n) =>
                    {
                        v.State.items = n.Properties["ITEMS"];
                    }
                );

                return visitor;
            }
        }
    }
}
