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
        private string urlTorServe = "kubernetes.hqcatalyst.local";

        [TestMethod]
        public void TestRunSimplePrintStatement()
        {
            using (var rServeClientTester = new RServeClient.RServeClient(urlTorServe))
            {
                var script = $@"print(""imran"")";

                rServeClientTester.RunLineByLine(script);

            }
        }

        [TestMethod]
        public void TestRunSimple()
        {
            using (var rServeClientTester = new RServeClient.RServeClient(urlTorServe))
            {
                var script = $@"setwd(""{serverfolder}"");getwd()";

                var results = rServeClientTester.RunRCodeOneLineAsStringArray(script);
                foreach (var result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }

        [TestMethod]
        public void TestShowInstalledPackages()
        {
            using (var rServeClientTester = new RServeClient.RServeClient(urlTorServe))
            {
                var script = $@"setwd(""{serverfolder}"");installed.packages()[,1]";

                var results = rServeClientTester.RunRCodeOneLineAsStringArray(script);
                foreach (var result in results)
                {
                    Console.WriteLine(result);
                }
            }
        }

        public static string GetLocalhostFqdn()
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            return string.Format("{0}.{1}", ipProperties.HostName, ipProperties.DomainName);
        }

        [TestMethod]
        public void TestRunSqlQuery()
        {
            using (var rServeClientTester = new RServeClient.RServeClient(urlTorServe))
            {
                var machineName = Environment.MachineName;
                var fullMachineName = GetLocalhostFqdn();


                var script = $@"
print(""Hello world"")
setwd(""{serverfolder}"");
.libPaths()

installed.packages()[,c(1,3:4)]

library(RODBC)
library(jsonlite)

servername = ""{fullMachineName}""

connectionstring <- paste(""driver=ODBC Driver 17 for SQL Server;server="",servername, "";Database=master;Trusted_Connection=yes"", sep = """")

print(connectionstring)
sql <- c(""select database_id, name from sys.databases"")
ch <- odbcDriverConnect(connectionstring)
res <- sqlQuery(ch, sql)
  
head(res, n = 20L)

odbcClose(ch)

cat(""Finished script"")
print(res)
jsonlite::toJSON(res)";

                var dataTable = rServeClientTester.RunAsync<DataTable>(script).Result;
                var rowsCount = dataTable.Rows.Count;
                var row = dataTable.Rows[0];
                var column = row[0];

                dataTable.Print();
            }
        }


        [TestMethod]
        public void TestCopyModelFiles()
        {
            using (var rServeClientTester = new RServeClient.RServeClient(urlTorServe))
            {

                var localfolder = @"C:\himss\R\";
                var files = new[]
                    {$@"rmodel_info_SepsisLassoModel_lasso.rda", @"rmodel_probability_SepsisLassoModel_lasso.rda"};

                var fileExists = rServeClientTester.DoAllFilesExist(serverfolder, files);

                if (fileExists != true)
                {
                    Console.WriteLine($"Sending files from {localfolder} to rserve folder {serverfolder}");
                    rServeClientTester.CopyFilesToRserve(localfolder, serverfolder, files);
                }
                else
                {
                    Console.WriteLine("File already exists so no need to send it");
                }

                var script = $@"
library(jsonlite)
res <- list.files(""{serverfolder}"")
jsonlite::toJSON(res)";

                var list = rServeClientTester.RunAsync<IList>(script).Result;

                Console.WriteLine($"Files in {serverfolder}");
                foreach (var item in list)
                {
                    Console.WriteLine(item);
                }
            }
        }


        [TestMethod]
        public void TestInstallPackages()
        {
            using (var rServeClientTester = new RServeClient.RServeClient(urlTorServe))
            {

                var script = $@"
setwd(""{serverfolder}"")
install.packages('randgeo', repos='https://cran.r-project.org')
library(jsonlite)
library(randgeo)
res <- wkt_point()
jsonlite::toJSON(res)";

                var list = rServeClientTester.RunAsync<IList>(script).Result;
                foreach (var item in list)
                {
                    Console.WriteLine(item);
                }

            }
        }
    }
}
