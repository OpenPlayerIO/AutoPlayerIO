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

    public class TableIndex
    {
        /// <summary>
        /// The name of the index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The properties of the index.
        /// </summary>
        public List<IndexProperty> Properties { get; set; }

        internal TableIndex()
        {
        }
    }

    public class IndexProperty
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the property.
        /// </summary>
        public IndexPropertyType Type { get; set; }

        /// <summary>
        /// The order of the property.
        /// </summary>
        public IndexPropertyOrder Order { get; set; }
    }

    public enum IndexPropertyOrder
    {
        Ascending,
        Descending
    }

    public enum IndexPropertyType
    {
        String,
        Integer,
        UnsignedInteger,
        Long,
        Boolean,
        Float,
        Double,
        Date,
        DateTime
    }
}
