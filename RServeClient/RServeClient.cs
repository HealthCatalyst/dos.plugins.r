using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RserveCLI2;

namespace RServeClient
{
    public class RServeClient
    {
        public void Run(string script)
        {
            using (var rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 })))
            {
                var lines = GetLines(script, true);

                foreach (var line in lines)
                {
                    RunRCode(line, rconnection);
                }
            }
        }

        private static void RunRCode(string script, RConnection rconnection)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                // var result = rconnection.Eval(script);
                rconnection.VoidEval(script);
            }
            catch (Exception myException)
            {
                var error = rconnection.Eval("geterrmessage()");
                var message = myException.Message;
                throw;
            }
        }

        private static IEnumerable<string> GetLines(string str, bool removeEmptyLines = false)
        {
            return str.Split(new[] { "\r\n", "\r", "\n" },
                removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }
    }
}
