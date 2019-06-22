using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Parser
{
    public enum LogType
    {
        BEGIN,
        END,
        INFORMATION,
        SUCCESS,
        FAILURE
    }

    public class LogArgs
    {
        public int NestingLevel { get; set; }
        public string Message { get; set; }
        public LogType LogType { get; set; }
    }
}
