using System.Threading.Tasks;

namespace RScriptParser
{
    public interface IMyRExecutionService
    {
        Task<MyShellTaskResult> ExecuteScriptAsync(string pathToRExe, string script,
            bool treatErrorsAsWarnings, string completedSuccessfullyText);
    }
}