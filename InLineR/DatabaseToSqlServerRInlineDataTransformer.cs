using RScriptParser;

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

        private readonly ILoggingRepository loggingRepository;
        private readonly IMetadataServiceClient metadataServiceClient;
        private readonly IProcessingContextWrapperFactory processingContextWrapperFactory;

        public DatabaseToSqlServerRInlineDataTransformer(
            ILoggingRepository loggingRepository,
            IMetadataServiceClient metadataServiceClient,
            ISqlExecutor sqlExecutor,
            IProcessingContextWrapperFactory processingContextWrapperFactory)
            : base(sqlExecutor)
        {
            loggingRepository.CheckWhetherArgumentIsNull(nameof(loggingRepository));
            metadataServiceClient.CheckWhetherArgumentIsNull(nameof(metadataServiceClient));
            processingContextWrapperFactory.CheckWhetherArgumentIsNull(nameof(processingContextWrapperFactory));

            this.loggingRepository = loggingRepository;
            this.metadataServiceClient = metadataServiceClient;
            this.processingContextWrapperFactory = processingContextWrapperFactory;
        }

        public override bool CanHandle(BindingExecution bindingExecution, Binding binding, Entity destinationEntity)
        {
            binding.CheckWhetherArgumentIsNull(nameof(binding));
            destinationEntity.CheckWhetherArgumentIsNull(nameof(destinationEntity));

            return DataSystemTypeCode.AcceptableDatabaseTypes.Contains(binding.SourceConnection.DataSystemTypeCode)
                   && destinationEntity.Connection.DataSystemTypeCode == DataSystemTypeCode.SqlServer
                   && binding.UserDefinedSql != null
                   && new ScriptParser().HasRCode(binding.UserDefinedSql);
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
                string pathToRExe = processingContextWrapper
                    .GetSystemAttribute(AttributeName.PathToRExecutable)?.AttributeValueText;

                string pathToRModelFolder = processingContextWrapper
                    .GetSystemAttribute(AttributeName.PathToRModelFolder)?.AttributeValueText;

                string sourceConnectionDatabase = binding.SourceConnection.Database;

                var rScriptSourceEntityInfos = new List<RScriptSourceEntityInfo>();
                foreach (int sourceEntityId in binding.SourcedByEntities.Select(e => e.SourceEntityId))
                {
                    Entity sourceEntity = await this.metadataServiceClient.GetEntityAsync(sourceEntityId);

                    rScriptSourceEntityInfos.Add(new RScriptSourceEntityInfo
                    {
                        DatabaseName = sourceEntity.DatabaseName,
                        EntityName = sourceEntity.EntityName,
                        SchemaName = sourceEntity.SchemaName,
                    });

                    // HACK: sourceConnectionDatabase is always set to master which doesn't work when R is trying to update the table
                    sourceConnectionDatabase = sourceEntity.DatabaseName;
                }

                var rScriptParameters = new RScriptParameters
                {
                    BindingScript = binding.Script,
                    Script = binding.UserDefinedSql,
                    CompletedSuccessfullyText = CompletedSuccessfullyText,
                    DestinationDatabaseName = entity.Connection.Database,
                    DestinationServer = entity.Connection.Server,
                    DestinationSystemTypeCode = entity.Connection.DataSystemTypeCode,
                    PathToRModelFolder = pathToRModelFolder,
                    SourceConnectionDatabase = sourceConnectionDatabase,
                    SourceDataSystemTypeCode = binding.SourceConnection.DataSystemTypeCode,
                    SourceServer = binding.SourceConnection.Server,
                    SourceEntities = rScriptSourceEntityInfos
                };

                var augmentedRScript = new ScriptParser().GetAugmentedRScript(rScriptParameters);

                return await this.StartScriptR(pathToRExe, bindingExecution, augmentedRScript);
            }
        }

        private async Task<long> StartScriptR(string pathToRExe, BindingExecution bindingExecution, string script)
        {
            script.CheckWhetherArgumentIsNullOrWhiteSpace(nameof(script));

            this.loggingRepository.LogExecutionStatement(
                this.GetType().Name,
                nameof(this.StartScriptR),
                bindingExecution.EntityExecution.BatchExecution,
                script,
                bindingExecution.EntityExecution,
                bindingExecution);

            var result = await new MyRExecutionService().ExecuteScriptAsync(pathToRExe, script, true, CompletedSuccessfullyText);

            return this.FinishScriptR(bindingExecution, result, script);
        }

        private long FinishScriptR(BindingExecution bindingExecution, MyShellTaskResult result, string script)
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

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(Resources.ScriptExecutionErrorOccurred.FormatCurrentCulture(script, result.Error));
            }

            return result.RecordCount;
        }
    }
}
