using System.Collections.Generic;

namespace Plugins.InLineR
{
    public class RScriptParameters
    {
        public string Script { get; set; }
        public string PathToRModelFolder { get; set; }
        public List<RScriptSourceEntityInfo> SourceEntities { get; set; }
        public string SourceDataSystemTypeCode { get; set; }
        public string SourceServer { get; set; }
        public string SourceConnectionDatabase { get; set; }
        public string DestinationSystemTypeCode { get; set; }
        public string DestinationServer { get; set; }
        public string DestinationDatabaseName { get; set; }
        public string BindingScript { get; set; }
        public string CompletedSuccessfullyText { get; set; }
    }

    public class RScriptSourceEntityInfo
    {
        public string DatabaseName { get; set; }
        public string SchemaName { get; set; }
        public string EntityName { get; set; }
    }


}