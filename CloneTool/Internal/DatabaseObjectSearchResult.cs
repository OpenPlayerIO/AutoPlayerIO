using System.Collections.Generic;

namespace CloneTool
{
    public class DatabaseObjectSearchResult
    {
        public string title { get; set; }
        public string creator { get; set; }
        public string deleteUrl { get; set; }
        public string changeUrl { get; set; }
        public List<DatabaseObjectProperty> properties { get; set; }
    }

}