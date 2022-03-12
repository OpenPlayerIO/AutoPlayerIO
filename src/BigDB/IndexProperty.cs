namespace AutoPlayerIO
{
    public class IndexProperty
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The type of the property.
        /// </summary>
        public IndexPropertyType Type { get; internal set; }

        /// <summary>
        /// The order of the property.
        /// </summary>
        public IndexPropertyOrder Order { get; internal set; }
    }
}
