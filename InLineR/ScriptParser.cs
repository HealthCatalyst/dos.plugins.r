using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Catalyst.Platform.CommonExtensions;

namespace Plugins.InLineR
{
    public class ScriptParser
    {
        private static readonly Regex RCodeRegex = new Regex(@"<r>([\s\S]*?)<\/r>");

        public bool HasRCode(string input)
        {
            var match = RCodeRegex.Match(input);

            if (match.Groups.Count > 1)
            {
                return true;
            }

            return false;
        }

        public string GetRCode(string input)
        {
            var match = RCodeRegex.Match(input);

            if (match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        public IList<string> GetRCodeLines(string input)
        {
            var text = GetRCode(input);

            IList<string> lines = text.Split('\n').Where(l => l.HasData()).ToList();

            return lines;
        }
    }
}
