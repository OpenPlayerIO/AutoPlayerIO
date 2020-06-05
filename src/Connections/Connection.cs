namespace AutoPlayerIO
{
    public class Connection
    {
        /// <summary>
        /// The name of the connection.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The description of the connection.
        /// </summary>
        public string Description { get; }

        internal Connection(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }
    }
}
