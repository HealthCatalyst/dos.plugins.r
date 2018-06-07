using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RScriptParser
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

            IList<string> lines = text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            return lines;
        }

        [Pure]
        public string GetAugmentedRScript(RScriptParameters rScriptParameters)
        {
            var rScriptLines = new ScriptParser().GetRCodeLines(rScriptParameters.Script);

            // since we run R using the service account that DPE is running under, we have to set a folder in libpath
            // so R can find the models and the installed packages
            var libPaths = $@".libPaths( c( .libPaths(), ""{rScriptParameters.PathToRModelFolder}/lib"") )";
            var workingDirectory = $@"setwd(""{rScriptParameters.PathToRModelFolder}"")";

            StringBuilder inputDfs = new StringBuilder();
            foreach (var sourceEntity in rScriptParameters.SourceEntities)
            {
                inputDfs.AppendLine(
                    $"{sourceEntity.DatabaseName}.{sourceEntity.SchemaName}.{sourceEntity.EntityName} <- sqlQuery(sourceConnection, \"SELECT * FROM {sourceEntity.DatabaseName}.{sourceEntity.SchemaName}.{sourceEntity.EntityName}\")");
            }

            var sourceConnectionString =
                $@"dos.source.connectionstring <- ""driver={{{rScriptParameters.SourceDataSystemTypeCode}}};server={rScriptParameters.SourceServer};database={rScriptParameters.SourceConnectionDatabase};trusted_connection=yes;""";

            var destinationConnectionString =
                $@"dos.destination.connectionstring <- ""driver={{{rScriptParameters.DestinationSystemTypeCode}}};server={rScriptParameters.DestinationServer};database={rScriptParameters.DestinationDatabaseName};trusted_connection=yes;""";

            var sb = new StringBuilder();
            sb.Append("#------ below code was injected by the DOS AI Engine ------\n");
            sb.Append($"{libPaths}\n");
            sb.Append($"{workingDirectory}\n");
            sb.Append($"{sourceConnectionString}\n");
            sb.Append($"{destinationConnectionString}\n");
            sb.AppendLine("#------ end of code injected by the DOS AI Engine ------\n");

            if (Enumerable.Any<string>(rScriptLines))
            {
                var scriptLines = Enumerable.Take<string>(rScriptLines, rScriptLines.Count);
                foreach (var scriptLine in scriptLines)
                {
                    if (!scriptLine.EndsWith("\n"))
                    {
                        sb.AppendFormat("{0}\n", scriptLine); //the newline is needed for R to work
                    }
                }

                sb.AppendLine();
            }
            else
            {
                sb.AppendFormat((string)"{0}\n", (object)rScriptParameters.BindingScript); //the newline is needed for R to work
                sb.AppendLine();
            }

            sb.Append("#------ below code was injected by the DOS AI Engine ------\n");
            string printStatement = $@"print(""{rScriptParameters.CompletedSuccessfullyText}"")";
            sb.AppendFormat($"{printStatement}\n");
            sb.AppendLine("#------ end of code injected by the DOS AI Engine ------\n");

            return sb.ToString();
        }
    }
}
