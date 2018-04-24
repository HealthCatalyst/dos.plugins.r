namespace Plugins.InLineR
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Catalyst.DataProcessing.Engine.SqlServer.DataTransformer;
    using Catalyst.DataProcessing.Engine.SqlServer.Utilities;
    using Catalyst.DataProcessing.Shared.Models.DataProcessing;
    using Catalyst.DataProcessing.Shared.Models.Enums;
    using Catalyst.DataProcessing.Shared.Models.Metadata;
    using Catalyst.DataProcessing.Shared.Utilities.Client;
    using Catalyst.DataProcessing.Shared.Utilities.Context;
    using Catalyst.DataProcessing.Shared.Utilities.Logging;
    using Catalyst.DataProcessing.Shared.Utilities.Services;
    using Catalyst.Platform.CommonExtensions;

    using Plugins.InLineR.Properties;

    public class DatabaseToSqlServerRInlineDataTransformer : SqlServerOptimizedDataTransformer
    {
        private const string CompletedSuccessfullyText = @"Completed Successfully";

        private static readonly Regex CountRegex = new Regex(@"\[\d+\] (\d+)", RegexOptions.IgnoreCase);

        private readonly ILoggingRepository loggingRepository;
        private readonly IRExecutionService rExecutionService;
        private readonly IMetadataServiceClient metadataServiceClient;
        private readonly IProcessingContextWrapperFactory processingContextWrapperFactory;

        public DatabaseToSqlServerRInlineDataTransformer(
            ILoggingRepository loggingRepository,
            IRExecutionService rExecutionService,
            IMetadataServiceClient metadataServiceClient,
            ISqlExecutor sqlExecutor,
            IProcessingContextWrapperFactory processingContextWrapperFactory)
            : base(sqlExecutor)
        {
            loggingRepository.CheckWhetherArgumentIsNull(nameof(loggingRepository));
            rExecutionService.CheckWhetherArgumentIsNull(nameof(rExecutionService));
            metadataServiceClient.CheckWhetherArgumentIsNull(nameof(metadataServiceClient));
            processingContextWrapperFactory.CheckWhetherArgumentIsNull(nameof(processingContextWrapperFactory));

            this.loggingRepository = loggingRepository;
            this.rExecutionService = rExecutionService;
            this.metadataServiceClient = metadataServiceClient;
            this.processingContextWrapperFactory = processingContextWrapperFactory;
        }

        public override bool CanHandle(BindingExecution bindingExecution, Binding binding, Entity destinationEntity)
        {
            binding.CheckWhetherArgumentIsNull(nameof(binding));
            destinationEntity.CheckWhetherArgumentIsNull(nameof(destinationEntity));

            return DataSystemTypeCode.AcceptableDatabaseTypes.Contains(binding.SourceConnection.DataSystemTypeCode)
                   && destinationEntity.Connection.DataSystemTypeCode == DataSystemTypeCode.SqlServer
                   && (binding.UserDefinedSql?.Contains(@"/*<r>") == true);
        }

        protected override async Task<long> LoadStagingEntityAsync(
            BindingExecution bindingExecution,
            Binding binding,
            Entity entity,
            CancellationToken cancellationToken)
        {
            bindingExecution.CheckWhetherArgumentIsNull(nameof(bindingExecution));
            binding.CheckWhetherArgumentIsNull(nameof(bindingExecution));
            entity.CheckWhetherArgumentIsNull(nameof(entity));

            using (IProcessingContextWrapper processingContextWrapper =
                this.processingContextWrapperFactory.CreateProcessingContextWrapper())
            {
                IList<string> lines = binding.UserDefinedSql.Split('\n').Where(l => l.HasData()).ToList();

                var rScriptLines = new List<string>();
                bool startR = false;
                foreach (var line in lines)
                {
                    if (line.Contains("</r>"))
                    {
                        startR = false;
                    }

                    if (startR)
                    {
                        rScriptLines.Add(line);
                    }

                    if (line.Contains("<r>"))
                    {
                        startR = true;
                    }
                }

                string pathToRModelFolder = processingContextWrapper
                    .GetSystemAttribute(AttributeName.PathToRModelFolder)?.AttributeValueText;

                // since we run R using the service account that DPE is running under, we have to set a folder in libpath
                // so R can find the models and the installed packages
                var libPaths = $@".libPaths( c( .libPaths(), ""{pathToRModelFolder}/lib"") )";
                var workingDirectory = $@"setwd(""{pathToRModelFolder}"")";

                string sourceConnectionDatabase = binding.SourceConnection.Database;

                StringBuilder inputDfs = new StringBuilder();
                foreach (int sourceEntityId in binding.SourcedByEntities.Select(e => e.SourceEntityId))
                {
                    Entity sourceEntity = await this.metadataServiceClient.GetEntityAsync(sourceEntityId);
                    inputDfs.AppendLine(
                        $"{sourceEntity.DatabaseName}.{sourceEntity.SchemaName}.{sourceEntity.EntityName} <- sqlQuery(sourceConnection, \"SELECT * FROM {sourceEntity.DatabaseName}.{sourceEntity.SchemaName}.{sourceEntity.EntityName}\")");

                    // HACK: sourceConnectionDatabase is always set to master which doesn't work when R is trying to update the table
                    sourceConnectionDatabase = sourceEntity.DatabaseName;
                }

                var sourceConnectionString =
                    $@"dos.source.connectionstring <- ""driver={{{
                            binding.SourceConnection.DataSystemTypeCode
                        }}};server={binding.SourceConnection.Server};database={
                            sourceConnectionDatabase
                        };trusted_connection=yes;""";

                var destinationConnectionString =
                    $@"dos.destination.connectionstring <- ""driver={{{entity.Connection.DataSystemTypeCode}}};server={
                            entity.Connection.Server
                        };database={entity.DatabaseName};trusted_connection=yes;""";

                var sb = new StringBuilder();
                sb.Append("#------ below code was injected by the DOS AI Engine ------\n");
                sb.Append($"{libPaths}\n");
                sb.Append($"{workingDirectory}\n");
                sb.Append($"{sourceConnectionString}\n");
                sb.Append($"{destinationConnectionString}\n");
                sb.AppendLine("#------ end of code injected by the DOS AI Engine ------\n");

                if (rScriptLines.Any())
                {
                    var scriptLines = rScriptLines.Take(rScriptLines.Count);
                    foreach (var scriptLine in scriptLines)
                    {
                        sb.AppendFormat("{0}\n", scriptLine); //the newline is needed for R to work
                    }

                    sb.AppendLine();
                }
                else
                {
                    sb.AppendFormat("{0}\n", binding.Script); //the newline is needed for R to work
                    sb.AppendLine();
                }

                string printStatement = $@"print(""{CompletedSuccessfullyText}"")";
                sb.AppendFormat($"{printStatement}\n");

                return await this.StartScriptR(bindingExecution, sb.ToString());
            }
        }

        private async Task<long> StartScriptR(BindingExecution bindingExecution, string script)
        {
            script.CheckWhetherArgumentIsNullOrWhiteSpace(nameof(script));

            this.loggingRepository.LogExecutionStatement(
                this.GetType().Name,
                nameof(this.StartScriptR),
                bindingExecution.EntityExecution.BatchExecution,
                script,
                bindingExecution.EntityExecution,
                bindingExecution);

            var result = await this.rExecutionService.ExecuteScriptAsync(script, true);

            return this.FinishScriptR(bindingExecution, result, script);
        }

        private long FinishScriptR(BindingExecution bindingExecution, ShellTaskResult result, string script)
        {
            result.CheckWhetherArgumentIsNull(nameof(result));

            this.loggingRepository.LogExecutionStatement(
                this.GetType().Name,
                nameof(this.FinishScriptR),
                bindingExecution.EntityExecution.BatchExecution,
                (result.Output ?? string.Empty) + "\n" + (result.Error ?? string.Empty),
                bindingExecution.EntityExecution,
                bindingExecution);

            if (string.IsNullOrEmpty(result.Output))
            {
                return 0;
            }

            if (!result.Output.Contains($"{CompletedSuccessfullyText}"))
            {
                throw new InvalidOperationException(Resources.ScriptExecutionErrorOccurred.FormatCurrentCulture(script, result.Error));
            }

            var match = CountRegex.Match(result.Output);

            if (match.Success)
            {
                long recordCount;
                if (!long.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.CurrentCulture, out recordCount))
                {
                    recordCount = 0;
                }

                return recordCount;
            }

            return 0;
        }
    }
}
