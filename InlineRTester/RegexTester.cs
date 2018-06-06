using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plugins.InLineR;

namespace InlineRTester
{
    [TestClass]
    public class RegexTester
    {
        [TestMethod]
        public void TestSimpleString()
        {
            var text = @"
<r>
this is fun
</r>
";
            var regex = new Regex(@"<r>([\s\S]*?)<\/r>");

            var match = regex.Match(text);

            Assert.AreEqual(2, match.Groups.Count);

            Assert.AreEqual("\r\nthis is fun\r\n", match.Groups[1].Value);
        }

        [TestMethod]
        public void TestHasRCodeSuccess()
        {
            var text = @"
SELECT * FROM
SAM.Sepsis.Summary
<r>
this is fun
</r>
";
            var rCode = new ScriptParser().HasRCode(text);
            Assert.AreEqual(true, rCode);
        }

        [TestMethod]
        public void TestHasRCodeFail()
        {
            var text = @"
SELECT * FROM
SAM.Sepsis.Summary
";
            var rCode = new ScriptParser().HasRCode(text);
            Assert.AreEqual(false, rCode);
        }

        [TestMethod]
        public void TestGetRCodeSimpleWithSpaces()
        {
            var text = @"
/* <r>
this is fun
</r> */
";
            var rCode = new ScriptParser().GetRCode(text);
            Assert.AreEqual("\r\nthis is fun\r\n", rCode);
        }

        [TestMethod]
        public void TestGetRCodeSimple()
        {
            var text = @"
<r>
this is fun
</r>
";
            var rCode = new ScriptParser().GetRCode(text);
            Assert.AreEqual("\r\nthis is fun\r\n", rCode);
        }

        [TestMethod]
        public void TestGetRCodeLinesSimple()
        {
            var text = @"
<r>
this is fun
</r>
";
            var rCode = new ScriptParser().GetRCodeLines(text);
            Assert.AreEqual(1, rCode.Count);
            Assert.AreEqual("this is fun\r", rCode[0]);
        }
    }
}
