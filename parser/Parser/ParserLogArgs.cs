using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public enum ParserLogType
    {
        BEGIN,
        END,
        INFORMATION,
        SUCCESS,
        FAILURE
    }

    public class ParserLogArgs
    {
        public int NestingLevel { get; set; }
        public string Message { get; set; }
        public ParserLogType ParserLogType { get; set; }
    }
}
