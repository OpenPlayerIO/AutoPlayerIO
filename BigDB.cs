using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using Flurl.Http;

namespace PlayerIO
{
    public class BigDB
    {
        public List<Table> Tables { get; }

        internal BigDB(DeveloperGame parent)
        {
            this.Game = parent;
            this.Tables = new List<Table>();

            var bigdb_details = BrowsingContext.New(Configuration.Default)
                .OpenAsync(req => req.Content(this.Game.Account.Client.Request($"/my/bigdb/tables/{this.Game.NavigationId}/{this.Game.XSRFToken}").GetStreamAsync().Result)).Result;

            var rows = bigdb_details.QuerySelector(".innermainrail").QuerySelector(".box").QuerySelector("tbody").QuerySelectorAll("tr.colrow");
            var contents = rows.Select(row => new
            {
                Name = row.QuerySelectorAll("a.big").First()?.TextContent,
                Description = row.QuerySelectorAll("p").First()?.TextContent,
                ExtraDetails = row.QuerySelectorAll("td").Skip(1).Take(1).First()?.Text().Replace("\n", "").Replace("\r", "").Replace("\t", "")
            });

            foreach (var table in contents)
            {
                this.Tables.Add(new Table()
                {
                    Name = table.Name,
                    Description = table.Description,
                    ExtraDetails = table.ExtraDetails
                });
            }
        }

        /// <summary>
        /// Create a table in BigDB.
        /// </summary>
        /// <param name="name"> The name of the table. </param>
        /// <param name="description"> A description for the table </param>
        public void CreateTable(string name, string description)
        {
            if (this.Tables.Any(table => table.Name.ToLower() == name.ToLower()))
                throw new InvalidOperationException($"Unable to create table. A table already exists with the name '{name}'");

            this.Game.Account.Client.Request($"/my/bigdb/createtable/{this.Game.Name.ToLower()}/{this.Game.XSRFToken}").PostUrlEncodedAsync(new
            {
                Name = name,
                Description = description ?? ""
            });
        }

        /// <summary>
        /// Export tables to the specified email address.
        /// </summary>
        /// <param name="tables"> A list of tables to export. </param>
        /// <param name="emailAddress"> The email address to send the exported JSON files to. </param>
        public void Export(IEnumerable<Table> tables, string emailAddress)
        {
            if (tables == null || tables.Count() == 0)
                throw new ArgumentException("You must specify at least one table to export.");

            if (string.IsNullOrEmpty(emailAddress) || !emailAddress.Contains('@'))
                throw new ArgumentException("You must specify a valid email address to export to.");

            dynamic arguments = new ExpandoObject();
            arguments.Email = emailAddress;

            foreach (var table in tables)
                ((IDictionary<string, object>)arguments).Add(table.Name, "on");

            this.Game.Account.Client.Request($"/my/bigdb/exporttables/{this.Game.Name.ToLower()}/{this.Game.XSRFToken}").PostUrlEncodedAsync((object)arguments);
        }

        internal DeveloperGame Game { get; set; }
    }
}
