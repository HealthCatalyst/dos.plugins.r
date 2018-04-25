using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InlineRTester
{
    [TestClass]
    public class RServeClientTester
    {
        [TestMethod]
        public void TestRun()
        {
            var rServeClientTester = new RServeClient.RServeClient();
            rServeClientTester.Run();
        }
    }
}
