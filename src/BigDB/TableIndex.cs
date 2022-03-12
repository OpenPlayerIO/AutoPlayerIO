using System.Collections.Generic;

namespace AutoPlayerIO
{
    public class TableIndex
    {
        /// <summary>
        /// The name of the index.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The properties of the index.
        /// </summary>
        public List<IndexProperty> Properties { get; internal set; }

        internal TableIndex()
        {
        }
    }
}
