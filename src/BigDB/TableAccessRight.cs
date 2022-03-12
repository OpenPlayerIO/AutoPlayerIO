namespace AutoPlayerIO
{
    public class TableAccessRight
    {
        /// <summary>
        /// The name of the table.
        /// </summary>
        public string Name { get; internal set; }

        public bool LoadByKeys { get; internal set; }
        public bool LoadByIndexes { get; internal set; }
        public bool FullCreatorRights { get; internal set; }
        public bool Create { get; internal set; }
        public bool Delete { get; internal set; }
        public bool Save { get; internal set; }
    }
}
