using System.Collections.Generic;

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

        /// <summary>
        /// The method for authentication.
        /// </summary>
        public AuthenticationMethod AuthenticationMethod { get; }

        /// <summary>
        /// The shared secret used for basic connection types.
        /// </summary>
        public string SharedSecret { get; }

        /// <summary>
        /// Whether the connection requires email address.
        /// </summary>
        public bool RequireEmailAddress { get; }

        // TODO: Implement captcha.
        public bool CaptchaEnabled { get; }

        /// <summary>
        /// The BigDB game database ID the connection belongs to.
        /// </summary>
        public string GameDB { get; }

        /// <summary>
        /// The connection access rights for each table.
        /// </summary>
        public List<TableAccessRight> TableAccessRights { get; }

        /// <summary>
        /// The rights of the connection.
        /// </summary>
        public ConnectionRights Rights { get; }

        /// <summary>
        /// A dictionary containing properties for the connection.
        /// </summary>
        public Dictionary<string, object> Properties { get; }

        internal Connection(string name, string description, Dictionary<string, object> properties, ConnectionRights rights, List<TableAccessRight> tableAccessRights, AuthenticationMethod authenticationMethod)
        {
            this.Name = name;
            this.Description = description;
            this.Properties = properties;
            this.Rights = rights;
            this.TableAccessRights = tableAccessRights;
            this.AuthenticationMethod = authenticationMethod;
        }
    }
}
