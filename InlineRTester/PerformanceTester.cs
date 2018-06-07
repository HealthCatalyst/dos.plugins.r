using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plugins.InLineR;
using RServeClient;

namespace InlineRTester
{
    [TestClass]
    public class PerformanceTester
    {
        private const string CompletedSuccessfullyText = @"Completed Successfully";

        private string urlTorServe = "kubernetes.hqcatalyst.local";
        private string _rscriptExe = @"C:\Program Files\R\R-3.5.0\bin\Rscript.exe";


        [TestMethod]
        public void TestInlineR()
        {
            var script = GetEmbeddedScript();

            var initial = @".libPaths(c( .libPaths(), ""C:/himss/R/lib""))";
            var initial2 = @"setwd(""C:/himss/R"")";

            script = initial + "\n" + initial2 + "\n" + script;

            string printStatement = $@"print(""{CompletedSuccessfullyText}"")";
            script += $"\n{printStatement}\n";

            var stopwatch = Stopwatch.StartNew();

            var shellTaskResult = new MyRExecutionService().ExecuteScriptAsync(_rscriptExe,script, true,CompletedSuccessfullyText ).Result;

            stopwatch.Stop();
            
            Assert.AreEqual(true, shellTaskResult.Succeeded, shellTaskResult.Output + "\n" + shellTaskResult.Error);

            Console.WriteLine(stopwatch.Elapsed);
        }

        [TestMethod]
        public void TestRunningScriptViaRserve()
        {
            var script = GetEmbeddedScript();

            string printStatement = $@"print(""{CompletedSuccessfullyText}"")";
            script += $"\n{printStatement}\n";

            var stopwatch = Stopwatch.StartNew();

            var shellTaskResult = new MyRServeExecutionService().ExecuteScriptAsync(urlTorServe, script, true, CompletedSuccessfullyText).Result;

            stopwatch.Stop();

            Assert.AreEqual(true, shellTaskResult.Succeeded, shellTaskResult.Output + "\n" + shellTaskResult.Error);

            Console.WriteLine(stopwatch.Elapsed);
        }


        private static string GetEmbeddedScript()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var manifestResourceNames = assembly.GetManifestResourceNames();
            var resourceName = "InlineRTester.TestBinding.R";

            string script = string.Empty;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                script = reader.ReadToEnd();
            }

            return script;
        }
    }
}
