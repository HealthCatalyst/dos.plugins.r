using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RScriptParser;

namespace RServeClient
{
    public class MyRServeExecutionService : IMyRExecutionService
    {
        public async Task<MyShellTaskResult> ExecuteScriptAsync(string urlTorServe, string script, bool treatErrorsAsWarnings, string completedSuccessfullyText)
        {
            using (var rServeClientTester = new RServeClient(urlTorServe))
            {
                var result = await rServeClientTester.RunAsync<string>(script);

                return new MyShellTaskResult
                {
                    Succeeded = true,
                    Output = result,
                };
            }
            
        }
    }
}
