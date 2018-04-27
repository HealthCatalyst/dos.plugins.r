using System;
using System.Net.NetworkInformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InlineRTester
{
    [TestClass]
    public class RServeClientTester
    {
        [TestMethod]
        public void TestRunSimplePrintStatement()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            var script = $@"print(""imran"")";

            rServeClientTester.Run(script);
        }

        [TestMethod]
        public void TestRunSimple()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            var script = $@"getwd()";

            rServeClientTester.Run(script);
        }

        public static string GetLocalhostFqdn()
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            return string.Format("{0}.{1}", ipProperties.HostName, ipProperties.DomainName);
        }

        [TestMethod]
        public void TestRunSqlQuery()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            var machineName = Environment.MachineName;
            var fullMachineName = GetLocalhostFqdn();


            var script = $@"
print(""Hello world"")

.libPaths()

installed.packages()[,c(1,3:4)]

library(RODBC)

servername = ""{fullMachineName}""

connectionstring <- paste(""driver=ODBC Driver 17 for SQL Server;server="",servername, "";Database=master;Trusted_Connection=yes"", sep = """")

print(connectionstring)
sql <- c(""select name from sys.databases"")
ch <- odbcDriverConnect(connectionstring)
res <- sqlQuery(ch, sql)
  
head(res, n = 20L)

odbcClose(ch)";

            rServeClientTester.Run(script);
        }
    }
}
