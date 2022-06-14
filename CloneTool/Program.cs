﻿using System;
using Serilog;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using AutoPlayerIO;
using System.Security;
using System.Threading.Tasks;
using System.Linq;
using AngleSharp.Html.Dom;
using System.IO;
using System.IO.Compression;

namespace CloneTool
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            // ASCII art header
            Console.WriteLine(Encoding.UTF8.GetString
                (Convert.FromBase64String("ICAgICAgICAgICBfX19fICBfXyAgICAgICAgICAgICAgICAgICAgIF9fX19fX19fICAgICANCiAgIC" +
                "AgICAgICAvIF9fIFwvIC9fX18gX19fICBfX19fXyAgX19fXy8gIF8vIF9fIFwgICAgDQogICAgICAgICAvIC9fLyAvIC8gX18gYC8gLy" +
                "AvIC8gXyBcLyBfX18vIC8vIC8gLyAvICAgIA0KICAgICAgICAvIF9fX18vIC8gL18vIC8gL18vIC8gIF9fLyAvIF8vIC8vIC9fLyAvIC" +
                "AgICANCiAgIF9fX18vXy9fICAvXy9cX18sXy9cX18sIC9cX19fL18oXylfX18vXF9fX18vICAgIF9fDQogIC8gX19fXy8gL19fXyAgX1" +
                "9fXyAvX19fXy8gICAgIC9fICBfXy9fX18gIF9fX18gIC8gLw0KIC8gLyAgIC8gLyBfXyBcLyBfXyBcLyBfIFwgICAgICAgLyAvIC8gX1" +
                "8gXC8gX18gXC8gLyANCi8gL19fXy8gLyAvXy8gLyAvIC8gLyAgX18vICAgICAgLyAvIC8gL18vIC8gL18vIC8gLyAgDQpcX19fXy9fL1" +
                "xfX19fL18vIC9fL1xfX18vICAgICAgL18vICBcX19fXy9cX19fXy9fLyAgIA0K")));

            Log.Information("Version: {version}", Assembly.GetExecutingAssembly().GetName().Version);
            Log.Information("Repository: {repository}", "https://github.com/OpenPlayerIO/AutoPlayerIO");
            Console.WriteLine();

            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = ConsoleExtension.ReadPassword();
            Console.WriteLine();

            var client = await PlayerIO.LoginAsync(username, password);

            Log.Information("Logged in as {username}.", client.Username);
            Console.WriteLine();

            Console.WriteLine($"Select a game to clone. (1-{client.Games.Count()})");

            for (int i = 0; i < client.Games.Count; i++)
            {
                DeveloperGame game = client.Games[i];
                Console.WriteLine("{0} - {1}", i + 1, game.Name);
            }

            Console.WriteLine();
            Console.Write(">");

            var e_game = client.Games[int.Parse(Console.ReadLine()) - 1];
            var e_bigDB = await e_game.LoadBigDBAsync();
            var e_connections = await e_game.LoadConnectionsAsync();

            Log.Information("Selected export game: {name}", e_game.Name);
            Log.Information("BigDB tables count: {count}", e_bigDB.Tables.Count);
            Log.Information("QuickConnect connections count: {count}", e_connections.Count);

            Console.WriteLine();
            Console.WriteLine("Select a game to clone data to.");

            for (int i = 0; i < client.Games.Count; i++)
            {
                DeveloperGame game = client.Games[i];
                Console.WriteLine("{0} - {1}", i + 1, game.Name);
            }

            Console.WriteLine();
            Console.Write(">");

            var i_game = client.Games[int.Parse(Console.ReadLine()) - 1];

            Log.Information("You have chosen to copy data from {export} to {import}. Is this correct?", e_game.Name, i_game.Name);

            Console.WriteLine();
            Console.Write("(Y/N): ");
            if (Console.ReadLine().ToLower() != "y")
                return;

            Console.WriteLine();
            Log.Information("Operation 1. The tables from {export} are being copied into {import}.", e_game.Name, i_game.Name);

            var i_bigDB = await i_game.LoadBigDBAsync();

            foreach (var table in e_bigDB.Tables)
            {
                if (i_bigDB.Tables.Any(t => t.Name == table.Name))
                    continue;

                await i_bigDB.CreateTableAsync(table.Name, table.Description);
                Log.Information("Table created: {name}.", table.Name);
            }

            Console.WriteLine();
            Log.Information("Creating indexes for tables.");
            i_bigDB = await i_game.LoadBigDBAsync(); // reload for new tables

            foreach (var table in e_bigDB.Tables)
            {
                foreach (var index in table.Indexes)
                {
                    var i_table = i_bigDB.Tables.First(t => t.Name == table.Name);

                    // skip creating/modifying any existing indexes
                    if (i_table.Indexes.Any(t => t.Name == index.Name))
                        continue;
                    
                    await i_bigDB.CreateOrModifyIndex(i_table, index.Name, index.Properties.ToList());
                    Log.Information("Index created: {name} in table '{tableName}'", index.Name, table.Name);
                }
            }

            Console.WriteLine();
            Log.Information("Operation 2. The connections from {export} are being copied into {import}.", e_game.Name, i_game.Name);

            var i_connections = await i_game.LoadConnectionsAsync();

            foreach (var connection in e_connections)
            {
                // skip creating any existing connections
                if (i_connections.Any(t => t.Name == connection.Name))
                    continue;

                await i_game.CreateConnectionAsync(
                    connection.Name,
                    connection.Description,
                    connection.AuthenticationMethod,
                    connection.GameDB,
                    connection.TableAccessRights.Select(r => (i_bigDB.Tables.First(t => t.Name == r.Name), r.LoadByKeys, r.Create, r.LoadByIndexes, r.Delete, r.FullCreatorRights, r.Save)).ToList(),
                    connection.Rights,
                    connection.SharedSecret);

                Log.Information("Connection created: {name} with authentication type: {auth}", connection.Name, connection.AuthenticationMethod);
            }

            Console.WriteLine();
            Log.Information("Operation 3. The database objects from {export} are being copied into {import} - this may take some time.");

            var bigdb_archive_path = Path.Combine(Environment.CurrentDirectory, $"bigdb-{e_game.GameId}.zip");

            foreach (var table in e_bigDB.Tables)
            {
                uint current_page = 0u;
                var web_export_utility = e_bigDB.GetWebExport(table); // TODO: support exporting from different game dbs beyond default

                using (var fs = new FileStream(bigdb_archive_path, FileMode.OpenOrCreate))
                {
                    using (var archive = new ZipArchive(fs, ZipArchiveMode.Update, true))
                    {
                        // create directory for table if doesn't exist
                        var directory = archive.Entries.Any(t => t.Name == table.Name)
                            ? new ZipArchiveDirectory(archive, table.Name)
                            : archive.CreateDirectory(table.Name);
                    }
                }

                var hit_end_of_pages = false;
                while (!hit_end_of_pages)
                {
                    current_page++;

                    var download_page = await web_export_utility.DownloadPage(WebExportOutputFormat.RawJSON, current_page, 100);

                    Log.Information("Downloading database objects from table '{name}' ... (page: {page})", table.Name, current_page);

                    // [] empty array
                    if (download_page.Length == 2)
                        hit_end_of_pages = true;

                    if (!hit_end_of_pages)
                    {
                        using (var fs = new FileStream(bigdb_archive_path, FileMode.OpenOrCreate))
                        {
                            using (var archive = new ZipArchive(fs, ZipArchiveMode.Update, true))
                            {
                                var directory = new ZipArchiveDirectory(archive, table.Name);

                                var entry = directory.CreateEntry($"{current_page}.page", CompressionLevel.Fastest);
                                var contents = Encoding.UTF8.GetBytes(download_page);

                                using (var zipStream = entry.Open())
                                    zipStream.Write(contents, 0, contents.Length);
                            }
                        }
                    }
                }
            }

            Log.Information("The clone operation has successfully completed.");
            await Task.Delay(-1);
        }
    }
}