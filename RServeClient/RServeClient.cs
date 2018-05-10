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
                rconnection.WriteFileAsync(myscriptR, memoryStream).Wait();

                var code = $@"source(""{myscriptR}"")";

                var runRCode = RunRCode(code, rconnection);
                var result = runRCode.AsList;
                var sexpArrayString = (SexpArrayString) result[0];
                var json = sexpArrayString[0].ToString();

                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public void CopyFilesToRserveAsync(string[] files, string serverfolder)
        {
            using (var rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 })))
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var serverFileName = serverfolder + fileName;
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        rconnection.WriteFileAsync(serverFileName, stream).Wait();
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
                    RunRCodeNoOutputAsync(line, rconnection);
                }
            }
        }


        private static void RunRCodeNoOutputAsync(string script, RConnection rconnection)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                rconnection.VoidEvalAsync(script).Wait();
            }
            catch (Exception myException)
            {
                var error = rconnection.EvalAsync("geterrmessage()").Result;
                var message = myException.Message;
                throw;
            }
        }

        public string[] RunRCodeOneLine(string line)
        {
            using (var rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 })))
            {
                var result = RunRCode(line, rconnection);
                return result.AsStrings;
            }
        }
        private static Sexp RunRCode(string script, RConnection rconnection)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                var result = rconnection.EvalAsync(script).Result;
                return result;
            }
            catch (Exception myException)
            {
                var error = rconnection.EvalAsync("geterrmessage()").Result;
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
