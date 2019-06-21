using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public interface ILoggable
    {
        Action<object, ParserLogArgs> ParserLogFunc { get; set; }
    }
}
