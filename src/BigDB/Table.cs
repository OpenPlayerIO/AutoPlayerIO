using System.Collections.Generic;

namespace AutoPlayerIO
{
    /// <summary>
    /// A table in BigDB.
    /// </summary>
    public class Table
    {
        /// <summary>
        /// The name of the table.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The description of the table.
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// This contains the amount of objects and bytes.
        /// </summary>
        public string ExtraDetails { get; internal set; }

        /// <summary>
        /// The indexes belonging to the table.
        /// </summary>
        public List<TableIndex> Indexes { get; internal set; }

        internal Table()
        {
        }
    }
}
