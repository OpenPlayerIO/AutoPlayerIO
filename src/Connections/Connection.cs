namespace AutoPlayerIO
{
    public class Connection
    {
        public string Name { get; }
        public string Description { get; }

        internal Connection(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }
    }
}
