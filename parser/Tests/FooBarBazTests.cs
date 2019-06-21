using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public class FooBarBazTests : AbstractTests
    {
        public override void DoTests()
        {
            DoTest("FOOBARBAZ1", FooBarBazGrammar, "FOO", "fbb", FooBarBazVisitor, (d) => d.items.Count, 1, false);
            DoTest("FOOBARBAZ2", FooBarBazGrammar, "FOOBAR", "fbb", FooBarBazVisitor, (d) => d.items.Count, 2, false);
            DoTest("FOOBARBAZ3", FooBarBazGrammar, "FOOBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 3, false);
            DoTest("FOOBARBAZ4", FooBarBazGrammar, "FOOBARBAZBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 4, false);
            DoTest("FOOBARBAZ5", FooBarBazGrammar, "FOOBARBAZBAZBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 5, false);
            DoTest("FOOBARBAZ6", FooBarBazGrammar, "FOOBARBAR", "fbb", FooBarBazVisitor, (d) => d.items.Count, 3, false);
            DoTest("FOOBARBAZ7", FooBarBazGrammar, "FOOBARBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 4, false);
            DoTest("FOOBARBAZ8", FooBarBazGrammar, "FOOBARBARBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 5, false);
            DoTest("FOOBARBAZ9", FooBarBazGrammar, "FOOBARBAZ", "fbb", FooBarBazVisitor, (d) => d.items.Count, 3, false);
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
