using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RserveCLI2;

namespace RServeClient
{
    public class RServeClient : IDisposable
    {
        private readonly RConnection _rconnection;

        public RServeClient()
        {
            _rconnection = RConnection.Connect(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 }));
        }
        public T Run<T>(string script)
        {
            // copy file to rserve
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(script));
            var myscriptR = "myscript.R";
            _rconnection.WriteFileAsync(myscriptR, memoryStream).Wait();

            var code = $@"source(""{myscriptR}"")";

            var runRCode = RunRCode(code);
            var result = runRCode.AsList;
            var sexpArrayString = (SexpArrayString)result[0];
            var json = sexpArrayString[0].ToString();

            return JsonConvert.DeserializeObject<T>(json);
        }

        public void CopyFilesToRserve(string localfolder, string serverfolder, string[] files)
        {
                foreach (var file in files)
                {
                    var serverFileName = serverfolder + file;
                    var localfilepath = Path.Combine(localfolder, file);
                    using (var stream = new FileStream(localfilepath, FileMode.Open, FileAccess.Read))
                    {
                        _rconnection.WriteFileAsync(serverFileName, stream).Wait();
                    }
                }
        }

        public void RunLineByLine(string script)
        {
                var lines = GetLines(script, true);

                foreach (var line in lines)
                {
                    RunRCodeNoOutputAsync(line);
                }
        }


        private void RunRCodeNoOutputAsync(string script)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                _rconnection.VoidEvalAsync(script).Wait();
            }
            catch (Exception myException)
            {
                var error = _rconnection.EvalAsync("geterrmessage()").Result;
                var message = myException.Message;
                throw;
            }
        }

        [Pure]
        public string[] RunRCodeOneLineAsStringArray(string line)
        {
                var result = RunRCode(line);
                return result.AsStrings;
        }

        [Pure]
        public bool? RunRCodeOneLineAsBoolean(string line)
        {
                var result = RunRCode(line);
                return result.AsBool;
        }

        private Sexp RunRCode(string script)
        {
            try
            {
                // rserve faq: https://www.rforge.net/Rserve/faq.html
                var result = _rconnection.EvalAsync(script).Result;
                return result;
            }
            catch (Exception myException)
            {
                var error = _rconnection.EvalAsync("geterrmessage()").Result;
                var message = myException.Message;
                throw new Exception(error.AsString, myException);
            }
        }

        private static IEnumerable<string> GetLines(string str, bool removeEmptyLines = false)
        {
            return str.Split(new[] { "\r\n", "\r", "\n" },
                removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }

        public bool? DoesFileExist(string pathToFile)
        {
            var result = RunRCodeOneLineAsBoolean($@"file.exists(""{pathToFile}"")");

            return result;
        }

        public bool DoAllFilesExist(string serverfolder, string[] files)
        {
            return files.All(f => DoesFileExist($"{serverfolder}/{f}") == true);
        }

        public void Dispose()
        {
            _rconnection.Dispose();
        }
    }
}
