using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp;
using AngleSharp.Dom;
using Flurl.Http;

namespace AutoPlayerIO
{
    public class BigDB
    {
        public static async Task<BigDB> LoadAsync(CookieSession client, string xsrfToken, DeveloperGame game, CancellationToken cancellationToken = default)
        {
            var tables = new List<Table>();

            var bigdbDetails = await client.Request($"/my/bigdb/tables/{game.NavigationId}/{xsrfToken}")
                .LoadDocumentAsync(cancellationToken)
                .ConfigureAwait(false);

            var rows = bigdbDetails.QuerySelector(".innermainrail").QuerySelector(".box").QuerySelector("tbody").QuerySelectorAll("tr.colrow");
            var contents = rows.Select(row => new
            {
                Id = row.QuerySelectorAll("a.big").First()?.GetAttribute("href").Split('/').Reverse().Skip(1).Take(1).First(),
                Name = row.QuerySelectorAll("a.big").First()?.TextContent,
                Description = row.QuerySelectorAll("p").First()?.TextContent,
                ExtraDetails = row.QuerySelectorAll("td").Skip(1).Take(1).First()?.Text().Replace("\n", "").Replace("\r", "").Replace("\t", "")
            });

            foreach (var content in contents)
            {
                var table = new Table()
                {
                    Name = content.Name,
                    Description = content.Description,
                    ExtraDetails = content.ExtraDetails,
                    Indexes = new List<TableIndex>()
                };

                var tableIndexDetails = await client.Request($"/my/bigdb/edittable/{game.NavigationId}/{content.Id}/{xsrfToken}")
                    .LoadDocumentAsync(cancellationToken)
                    .ConfigureAwait(false);

                var indexRows = tableIndexDetails.QuerySelectorAll(".bigdbindex");

                foreach (var row in indexRows)
                {
                    var indexName = row.QuerySelector(".indexheader").QuerySelector("b").TextContent;
                    var props = row.QuerySelector(".props").QuerySelectorAll("li");

                    var tableIndex = new TableIndex
                    {
                        Name = indexName,
                        Properties = new List<IndexProperty>()
                    };

                    foreach (var prop in props)
                    {
                        var propertyName = prop.QuerySelector("span").TextContent;
                        var propertyContent = prop.TextContent.Replace("(", "").Replace(")", "").Replace(",", "").Split(' ');

                        var propertyType = (IndexPropertyType)Enum.Parse(typeof(IndexPropertyType), propertyContent[1]);
                        var propertyOrder = (IndexPropertyOrder)Enum.Parse(typeof(IndexPropertyOrder), propertyContent[2]);

                        tableIndex.Properties.Add(new IndexProperty()
                        {
                            Name = propertyName,
                            Type = propertyType,
                            Order = propertyOrder,
                        });
                    }

                    table.Indexes.Add(tableIndex);
                }

                tables.Add(table);
            }

            return new BigDB(client, xsrfToken, game, tables);
        }

        public List<Table> Tables { get; }
        public DeveloperGame Game { get; }

        private readonly CookieSession _client;
        private readonly string _xsrfToken;

        private BigDB(CookieSession client, string xsrfToken, DeveloperGame game, List<Table> tables)
        {
            _client = client;
            _xsrfToken = xsrfToken;

            this.Game = game;
            this.Tables = tables;
        }

        /// <summary>
        /// Create a table in BigDB.
        /// </summary>
        /// <param name="name"> The name of the table. </param>
        /// <param name="description"> A description for the table </param>
        public async Task CreateTableAsync(string name, string description)
        {
            if (this.Tables.Any(table => string.Equals(table.Name, name, StringComparison.CurrentCultureIgnoreCase)))
                throw new InvalidOperationException($"Unable to create table. A table already exists with the name '{name}'");

            await _client.Request($"/my/bigdb/createtable/{this.Game.Name.ToLower()}/{_xsrfToken}").PostUrlEncodedAsync(new
            {
                Name = name,
                Description = description ?? ""
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Export tables to the specified email address.
        /// </summary>
        /// <param name="tables"> A list of tables to export. </param>
        /// <param name="emailAddress"> The email address to send the exported JSON files to. </param>
        public async Task ExportAsync(IEnumerable<Table> tables, string emailAddress)
        {
            if (tables?.Any() != true)
                throw new ArgumentException("You must specify at least one table to export.");

            if (string.IsNullOrEmpty(emailAddress) || !emailAddress.Contains('@'))
                throw new ArgumentException("You must specify a valid email address to export to.");

            dynamic arguments = new ExpandoObject();
            arguments.Email = emailAddress;

            foreach (var table in tables)
                ((IDictionary<string, object>)arguments).Add(table.Name, "on");

            await _client.Request($"/my/bigdb/exporttables/{this.Game.Name.ToLower()}/{_xsrfToken}")
                .PostUrlEncodedAsync((object)arguments)
                .ConfigureAwait(false);
        }
    }
}
