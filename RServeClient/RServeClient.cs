using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RserveCLI2;

namespace RServeClient
{
    public class RServeClient
    {
        public T Run<T>(string script)
        {
            using (var rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 })))
            {
                // copy file to rserve
                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(script));
                var myscriptR = "myscript.R";
                rconnection.WriteFile(myscriptR, memoryStream);

                var code = $@"source(""{myscriptR}"")";

                var runRCode = RunRCode(code, rconnection);
                var result = runRCode.AsList;
                var sexpArrayString = (SexpArrayString) result[0];
                var json = sexpArrayString[0].ToString();

                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public void CopyFilesToRserve(string[] files, string serverfolder)
        {
            using (var rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 })))
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var serverFileName = serverfolder + fileName;
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        rconnection.WriteFile(serverFileName, stream);
                    }
                }
            }
        }

        public void RunLineByLine(string script)
        {
            using (var rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 })))
            {
                var lines = GetLines(script, true);

                foreach (var line in lines)
                {
                    RunRCodeNoOutput(line, rconnection);
                }
            }
        }

        private static void RunRCodeNoOutput(string script, RConnection rconnection)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                rconnection.VoidEval(script);
            }
            catch (Exception myException)
            {
                var error = rconnection.Eval("geterrmessage()");
                var message = myException.Message;
                throw;
            }
        }

        private static Sexp RunRCode(string script, RConnection rconnection)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                var result = rconnection.Eval(script);
                return result;
            }
            catch (Exception myException)
            {
                var error = rconnection.Eval("geterrmessage()");
                var message = myException.Message;
                throw new Exception(error.AsString, myException);
            }
        }

        private static IEnumerable<string> GetLines(string str, bool removeEmptyLines = false)
        {
            return str.Split(new[] { "\r\n", "\r", "\n" },
                removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }
    }
}
