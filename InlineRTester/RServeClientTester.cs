using System;
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
        public void TestRunSqlQuery()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            var script = $@"
                library(RODBC)

                servername = ""hc2427.hqcatalyst.local""

                connectionstring <- paste(""driver = ODBC Driver 17 for SQL Server; server = "",servername, ""; Database = master; Trusted_Connection = yes"", sep = """")

                print(""Hello world"")

                sql < -c(""select name from sys.databases"")
                tryCatch({{
                    ch < -odbcDriverConnect(connectionstring)

                    res < -sqlQuery(ch, sql)
                    print(""success"")
                }},error = function(e) {{
                    print(e)
                    print(odbcGetErrMsg(ch))
                    print(""error"")
                }})
  
                head(res, n = 20L)

                odbcClose(ch)";

            rServeClientTester.Run(script);
        }
    }
}
