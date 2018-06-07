using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Catalyst.Platform.CommonExtensions;
using RScriptParser;

namespace Plugins.InLineR
{
    public class MyRExecutionService : IMyRExecutionService
    {
        private static readonly Regex CountRegex = new Regex(@"\[\d+\] (\d+)", RegexOptions.IgnoreCase);


        public async Task<MyShellTaskResult> ExecuteScriptAsync(string pathToRExe, string script,
            bool treatErrorsAsWarnings, string completedSuccessfullyText)
        {
            script.CheckWhetherArgumentIsNullOrWhiteSpace(nameof(script));

            Uri interpreterPath;
            if (!Uri.TryCreate(pathToRExe, UriKind.Absolute, out interpreterPath))
            {
                throw new ArgumentException(pathToRExe.FormatCurrentCulture());
            }

            string scriptFile = Path.GetTempFileName();
            try
            {
                using (StreamWriter fileWriter = new StreamWriter(new FileStream(scriptFile, FileMode.Append)))
                {
                    await fileWriter.WriteAsync(script);
                    await fileWriter.FlushAsync();
                    fileWriter.Close();
                }

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = interpreterPath.LocalPath,
                    Arguments = scriptFile,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = info;
                    process.Start();

                    string result = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    // healthcare-ai writes out the upgrade notice to stderr
                    if (error.HasData() && !treatErrorsAsWarnings)
                    {
                        throw new InvalidOperationException(script.FormatCurrentCulture(error));
                    }

                    var match = CountRegex.Match(result);

                    long recordCount = 0;

                    if (match.Success)
                    {
                        if (!long.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.CurrentCulture, out recordCount))
                        {
                            recordCount = 0;
                        }
                    }


                    return new MyShellTaskResult
                    {
                        Output = result,
                        Error = error,
                        Succeeded = result.Contains($"{completedSuccessfullyText}"),
                        RecordCount = recordCount,
                    };
                }
            }
            finally
            {
                File.Delete(scriptFile);
            }
        }

    }

}
