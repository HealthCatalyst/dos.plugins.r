using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InlineRTester
{
    [TestClass]
    public class RServeClientTester
    {
        const string serverfolder = "/opt/healthcatalyst/models/";

        [TestMethod]
        public void TestRunSimplePrintStatement()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            var script = $@"print(""imran"")";

            rServeClientTester.RunLineByLine(script);
        }

        [TestMethod]
        public void TestRunSimple()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            var script = $@"setwd(""{ serverfolder}"");getwd()";

            rServeClientTester.RunLineByLine(script);
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
setwd(""{ serverfolder}"");
.libPaths()

installed.packages()[,c(1,3:4)]

library(RODBC)
library(jsonlite)

servername = ""{fullMachineName}""

connectionstring <- paste(""driver=ODBC Driver 17 for SQL Server;server="",servername, "";Database=master;Trusted_Connection=yes"", sep = """")

print(connectionstring)
sql <- c(""select name from sys.databases"")
ch <- odbcDriverConnect(connectionstring)
res <- sqlQuery(ch, sql)
  
head(res, n = 20L)

odbcClose(ch)

cat(""Finished script"")
print(res)
jsonlite::toJSON(res)";

            var dataTable = rServeClientTester.Run<DataTable>(script);
            var rowsCount = dataTable.Rows.Count;
            var row = dataTable.Rows[0];
            var column  = row[0];

            dataTable.Print();
        }


        [TestMethod]
        public void TestCopyModelFiles()
        {
            var rServeClientTester = new RServeClient.RServeClient();


            var strings = new [] { @"C:\himss\R\rmodel_info_SepsisLassoModel_lasso.rda", @"C:\himss\R\rmodel_probability_SepsisLassoModel_lasso.rda" };
            rServeClientTester.CopyFilesToRserve(strings, serverfolder);

            var script = $@"
library(jsonlite)
res <- list.files(""{serverfolder}"")
jsonlite::toJSON(res)";

            var list = rServeClientTester.Run<IList>(script);

            Console.WriteLine($"Files in {serverfolder}");
            foreach (var item in list)
            {
                Console.WriteLine(item);
            }
        }
    }
}
