﻿using AngleSharp.Dom;
using AngleSharp.Html.Dom;

using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerIO
{
    public partial class DeveloperGame
    {
        internal static async Task<DeveloperGame> LoadAsync(
            FlurlClient client,
            string path,
            CancellationToken cancellationToken = default
        )
        {
            var gameDetails = await client.Request(path).LoadDocumentAsync(cancellationToken).ConfigureAwait(false);

            var name = gameDetails.QuerySelector(".headerprefix").Children.First().Text();

            var gameId = gameDetails.QuerySelectorAll(".gamecreatedinfo").Any()
                ? gameDetails.QuerySelector(".yourgameid").GetElementsByTagName("td").First().Text() // when a game is first created
                : gameDetails.GetElementsByTagName("h3").First(t => t.TextContent == "Game ID:").ParentElement.NextElementSibling.TextContent; // when a game has already been created

            var navigationMenu = gameDetails.QuerySelector(".leftrail").QuerySelector("nav").QuerySelector("ul").GetElementsByTagName("li").Children("a");

            var navigationId = (navigationMenu.First() as IHtmlAnchorElement).PathName.Split('/')[4];
            var xsrfToken = (navigationMenu.First() as IHtmlAnchorElement).PathName.Split('/').Last();

            return new DeveloperGame(client, xsrfToken, name, navigationId, gameId);
        }

        private string XSRFToken { get; }

        /// <summary>
        /// The name of the game.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The unique ID used in the navigation panel.
        /// </summary>
        public string NavigationId { get; }

        /// <summary>
        /// The unique ID of the game.
        /// </summary>
        public string GameId { get; }

        /// <summary>
        /// A class used for managing BigDB for this game.
        /// </summary>
        [Obsolete("Please use " + nameof(LoadBigDBAsync) + ".")]
        public BigDB BigDB => LoadBigDBAsync().GetAwaiter().GetResult();

        /// <summary>
        /// A list of connections for this game.
        /// </summary>
        [Obsolete("Please use " + nameof(LoadConnectionsAsync) + ".")]
        public List<Connection> Connections => LoadConnectionsAsync().GetAwaiter().GetResult();

        // internal is used to workaround the other parts of this library requiring FlurlClient
        internal readonly FlurlClient _client;

        private DeveloperGame(FlurlClient client, string xsrfToken, string name, string navigationId, string gameId)
        {
            _client = client;

            XSRFToken = xsrfToken;
            Name = name;
            NavigationId = navigationId;
            GameId = gameId;
        }

        /// <summary>
        /// Delete the specified connection.
        /// </summary>
        /// <param name="connectionId"> The name of the connection to delete. </param>
        public async Task<bool> DeleteConnection(Connection connection)
        {
            if (string.IsNullOrEmpty(connection.Name))
                throw new ArgumentException("Unable to delete connection. Parameter 'connectionId' cannot be null or empty.");

            var response = await _client.Request($"/my/connections/delete/{this.NavigationId}/{connection.Name}/{this.XSRFToken}").PostUrlEncodedAsync(new
            {
                Confirm = "delete connection"
            }).ConfigureAwait(false);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        /// <summary>
        /// Create a note in the Changelog section.
        /// </summary>
        /// <param name="content"> The text in the note. </param>
        public async Task<bool> CreateNote(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Unable to create note. Parameter 'content' cannot be null or empty.");

            var response = await _client.Request($"/my/changelog/addnote/{this.NavigationId}/{this.XSRFToken}").PostUrlEncodedAsync(new
            {
                Note = content
            }).ConfigureAwait(false);

            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        /// <summary>
        /// Creates a connection in the 'Settings' panel.
        /// </summary>
        /// <param name="connectionId"> The name of the connnection. </param>
        /// <param name="description"> The description of the connection. </param>
        /// <param name="authenticationMethod"> The method the connection will use for authentication. </param>
        /// <param name="gameDB"> The database the connection has access to. By default it is set to 'Default'. If set to null, 'Default' will be used. </param>
        /// <param name="table_privileges"> The BigDB privileges to give this connection </param>
        /// <param name="sharedSecret"> If the authentication method is Basic, the sharedSecret specified here will be used. </param>
        public async Task CreateConnection(string connectionId, string description, AuthenticationMethod authenticationMethod, string gameDB, List<(Table table, bool can_load_by_keys, bool can_create, bool can_load_by_indexes, bool can_delete, bool creator_has_full_rights, bool can_save)> table_privileges, string sharedSecret = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentException("Unable to create connection - connectionId cannot be null or empty.");

            if (connectionId.Any(char.IsUpper) || connectionId.Any(char.IsDigit))
                throw new ArgumentException("Unable to create connection - connectionId must be lowercase and only consist of alphabetical characters.");

            // TODO: cache Connections?
            var connections = await LoadConnectionsAsync(cancellationToken).ConfigureAwait(false);
            if (connections.Any(t => t.Name == connectionId))
                throw new Exception($"Unable to create connection '{connectionId}' - a connection already exists with that name.");

            if (authenticationMethod == AuthenticationMethod.BasicRequiresAuthentication && string.IsNullOrEmpty(sharedSecret))
                throw new Exception($"Unabel to create connection '{connectionId}' - when using BasicRequiresAuthentication as your authentication method, you must provide a non-empty sharedSecret.");

            if (string.IsNullOrEmpty(gameDB))
                gameDB = "Default";

            var creation_details = await _client.Request($"/my/connections/create/{this.NavigationId}/{this.XSRFToken}")
                .LoadDocumentAsync(cancellationToken)
                .ConfigureAwait(false);

            var access_rights = creation_details.QuerySelector("#bigdbaccessrights");
            var table_names = access_rights.QuerySelectorAll("b");
            var connection_privileges = new List<(string id, string name, bool can_load_by_keys, bool can_create, bool can_load_by_indexes, bool can_delete, bool creator_has_full_rights, bool can_save)>();

            foreach (var label in table_names)
            {
                var connection_name = label.Text();

                // If the table wasn't specified in the method parameters, skip.
                if (!table_privileges.Any(t => t.table.Name == connection_name))
                    continue;

                var table_privilege = table_privileges.Find(t => t.table.Name == connection_name);

                var inputs = label.NextElementSibling.QuerySelectorAll("input");
                var id = inputs.First().Id.Split('-')[0];
                var can_load_by_keys = inputs.Skip(0).Take(1).First();
                var can_create = inputs.Skip(1).Take(1).First();
                var can_load_by_indexes = inputs.Skip(2).Take(1).First();
                var can_delete = inputs.Skip(3).Take(1).First();
                var creator_has_full_rights = inputs.Skip(4).Take(1).First();
                var can_save = inputs.Skip(5).Take(1).First();

                connection_privileges.Add((id, connection_name, table_privilege.can_load_by_keys, table_privilege.can_create, table_privilege.can_load_by_indexes, table_privilege.can_delete, table_privilege.creator_has_full_rights, table_privilege.can_save));
            }

            dynamic arguments = new ExpandoObject();

            arguments.Identifier = connectionId;
            arguments.Description = description;
            arguments.GameDB = gameDB;
            arguments.GameDBName = "";

            switch (authenticationMethod)
            {
                case AuthenticationMethod.Basic:
                    arguments.AuthProvider = "basic256";
                    break;

                case AuthenticationMethod.BasicRequiresAuthentication:
                    arguments.AuthProvider = "basic256";
                    arguments.Basic256RequiresAuth = "on";
                    arguments.Basic256AuthSharedSecret = sharedSecret;
                    break;
            }

            foreach (var privilege in connection_privileges)
            {
                if (privilege.can_load_by_keys)
                    ((IDictionary<string, object>)arguments).Add(privilege.id + "-canloadbykeys", "on");

                if (privilege.can_create)
                    ((IDictionary<string, object>)arguments).Add(privilege.id + "-cancreate", "on");

                if (privilege.can_load_by_keys)
                    ((IDictionary<string, object>)arguments).Add(privilege.id + "-canloadbyindexes", "on");

                if (privilege.can_delete)
                    ((IDictionary<string, object>)arguments).Add(privilege.id + "-candelete", "on");

                if (privilege.creator_has_full_rights)
                    ((IDictionary<string, object>)arguments).Add(privilege.id + "-creatorhasfullrights", "on");

                if (privilege.can_save)
                    ((IDictionary<string, object>)arguments).Add(privilege.id + "-cansave", "on");
            }

            var create_connection_response = _client.Request($"/my/connections/create/{this.NavigationId}/{this.XSRFToken}").PostUrlEncodedAsync((object)arguments).Result;
            var edit_connection_response = _client.Request($"/my/connections/edit/{this.NavigationId}/{arguments.Identifier}/{this.XSRFToken}").PostUrlEncodedAsync((object)arguments).Result;
        }

        public Task<BigDB> LoadBigDBAsync(CancellationToken cancellationToken = default)
            => global::PlayerIO.BigDB.LoadAsync(_client, XSRFToken, this, cancellationToken);

        public async Task<List<Connection>> LoadConnectionsAsync(CancellationToken cancellationToken = default)
        {
            var connections = new List<Connection>();

            var settingsDetails = await _client.Request($"/my/games/settings/{this.NavigationId}/{this.XSRFToken}")
                .LoadDocumentAsync(cancellationToken)
                .ConfigureAwait(false);

            var section = settingsDetails.QuerySelectorAll("section")
                .First(x => x.GetElementsByTagName("h3").Any(t => t.TextContent == "Connections"));

            var rows = section.QuerySelectorAll("tr.colrow");
            var contents = rows.Select(row => new
            {
                Name = row.QuerySelectorAll("a").First()?.TextContent,
                Description = row.QuerySelectorAll("div").First()?.TextContent,
            });

            var connectionTasks = new List<Connection>();

            foreach (var table in contents)
            {
                connectionTasks.Add(new Connection(table.Name, table.Description));
            }

            return connections;
        }
    }
}