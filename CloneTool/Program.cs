using System;
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
using CloneTool.Internal;
using ValueType = CloneTool.Internal.ValueType;
using System.Collections.Generic;
using PlayerIOClient;
using Konsole;
using PlayerIO = AutoPlayerIO.PlayerIO;
using BigDB = AutoPlayerIO.BigDB;
using System.Collections.Concurrent;
using System.Threading;
using System.Security.Cryptography;
using VenturePIO = PlayerIOClient.PlayerIO;

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

			Console.WriteLine("Operation Modes:\n");
            Console.WriteLine("1.) Game-to-Game Clone.\tThis method allows copying BigDB table and connection settings to another game.\n\t - With typical usage being in a newly created, empty game.\n");
            Console.WriteLine("2.) Game to Local Save.\tThis method creates a local copy of the game to disk.\n\t - May take a long time if there are many database objects. Use option 3 for a shallow save.\n");
            Console.WriteLine("3.) Game to Local Save (shallow). \n\t - Same as above, but without BigDB objects saved.\n");

			// TODO: Needs fixing. Getting a weird exception from Venture during create export connection.
            //Console.WriteLine("4 - Game to Local Save (Zippity Quick).\n\t - This method reads keys from Player.IO export archives with keys provided in a 'keys.txt' file.\n");

			Console.Write("Please select an operation mode (1-3):");
            var mode = Console.ReadLine();

            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = ConsoleExtension.ReadPassword();
            Console.WriteLine();

            var account = await PlayerIO.LoginAsync(username, password);

            Log.Information("Logged in as {username}.", account.Username);
            Console.WriteLine();

			switch (mode)
			{
				case "1":
					await CloneOperation(account);
					break;

				case "2":
					await LocalSaveOperation(account, shallow: false, zippityquick: false);
					break;

				case "3":
					await LocalSaveOperation(account, shallow: true, zippityquick: false);
					break;

				case "4":
					break;
					Console.WriteLine("Please enter the path containing the '.zip' export files from Player.IO: ");
					Console.Write(">");
					var archivesDirectory = Console.ReadLine();
					await LocalSaveOperation(account, shallow: false, zippityquick: true, archivesDirectory);
					break;
			}
        }

		public static async Task LocalSaveOperation(DeveloperAccount client, bool shallow, bool zippityquick = false, string archivesDirectory = "")
		{
			Console.WriteLine($"Select a game to clone locally. (1-{client.Games.Count()})");

			for (int i = 0; i < client.Games.Count; i++)
			{
				DeveloperGame g = client.Games[i];
				Console.WriteLine("{0} - {1}", i + 1, g.Name);
			}

			Console.WriteLine();
			Console.Write(">");

			// 'e_' is a prefix for 'export', and 'i_' for 'import'
			var game = client.Games[int.Parse(Console.ReadLine()) - 1];
			var bigDB = await game.LoadBigDBAsync();
			var connections = await game.LoadConnectionsAsync();
			var payvault = await game.LoadPayVaultAsync();

			Log.Information("Selected export game: {name}", game.Name);
			Log.Information("BigDB tables count: {count}", bigDB.Tables.Count);
			Log.Information("QuickConnect connections count: {count}", connections.Count);

			var saveDirectory = Path.Combine(Environment.CurrentDirectory, "saves", game.GameId);
			Directory.CreateDirectory(saveDirectory);

			Log.Information("Saving connections.");
			File.WriteAllText(Path.Combine(saveDirectory, "connections.json"), JsonConvert.SerializeObject(connections));
			Log.Information("Connections saved.");

			Log.Information("Saving payvault.");
			File.WriteAllText(Path.Combine(saveDirectory, "payvault.json"), JsonConvert.SerializeObject(payvault));
			Log.Information("PayVault saved.");

			Log.Information("Saving tables.");
			File.WriteAllText(Path.Combine(saveDirectory, "tables.json"), JsonConvert.SerializeObject(bigDB.Tables));
			Log.Information("Tables saved.");

			if (!shallow)
			{
				Log.Information("Downloading database objects. This may take a long time depending on how many objects there are.");

				if (!zippityquick)
				{
					var bigdb_archive_path = Path.Combine(saveDirectory, $"objects.zip");
					await DownloadTables(bigdb_archive_path, bigDB);
					Log.Information("Saved database objects.");
				}
				else
				{
					if (!Directory.Exists(archivesDirectory))
					{
						Log.Error("The directory specified does not exist.");
						return;
					}

					var archive_files = Directory.GetFiles(archivesDirectory, "*.zip", SearchOption.TopDirectoryOnly).ToList();

					if (archive_files.Count == 0)
					{
						Log.Error("The directory specified does not contain any .zip files.");
						return;
					}

					var shared_secret = Guid.NewGuid().ToString() + RandomNumberGenerator.GetInt32(int.MaxValue).ToString();
					var tables = (await game.LoadBigDBAsync()).Tables;
					var connection_rights = new ConnectionRights() { };


					// delete export connection if already exists
					while (true)
					{
						var bc = await game.LoadConnectionsAsync();

						if (!bc.Any(c => c.Name == "export"))
							break;

						Log.Information("An existing export connection was found - attempting to recreate it. This process should only take a few seconds.");

						var conn = bc.FirstOrDefault(t => t.Name == "export");

						if (conn != null)
							await game.DeleteConnectionAsync(conn);

						//we don't want to spam
						await Task.Delay(1000);
					}

					// ensure the export connection exists before continuing
					while (true)
					{
						await game.CreateConnectionAsync("export", "A connection with read access to all BigDB tables - used for exporting games.", AuthenticationMethod.BasicRequiresAuthentication, "Default",
							tables.Select(t => (t, true, false, false, false, false, false)).ToList(), connection_rights, shared_secret);

						var bc = await game.LoadConnectionsAsync();

						if (bc.Any(c => c.Name == "export"))
							break;

						Log.Information("waiting for export connection to be created...");
						Thread.Sleep(1000); // we don't want to spam.
					}

					var ventureClient = VenturePIO.Authenticate(game.GameId, "export", new Dictionary<string, string>() { { "userId", "user" }, { "auth", VenturePIO.CalcAuth256("user", shared_secret) } } );

					var export_tasks = new List<Task<List<DatabaseObject>>>();
					var progress_bars = new ConcurrentBag<ProgressBar>();

					foreach (var archive_file in archive_files)
					{
						var split = new FileInfo(archive_file).Name.Split('_');
						var game_name = split[0];
						var table = split[1];
						var game_db = split[2];

						// create output directory
						var output_directory = Path.Combine("exports", game_name, table, game_db);

						// ensure output directory exists.
						Directory.CreateDirectory(output_directory);

						// find all keys in table export as fujson.
						var archive_keys = GetDatabaseObjectKeysFromArchive(archive_file);
						var already_exported = Directory.GetDirectories(output_directory, "*", SearchOption.TopDirectoryOnly).Select(x => new DirectoryInfo(x).Name).ToList();

						// add progress bar to the console
						var progress_bar = new ProgressBar(PbStyle.DoubleLine, archive_keys.Count);
						progress_bars.Add(progress_bar);
						progress_bar.Refresh(0, table);
						export_tasks.Add(ProcessJob(ventureClient, output_directory, table, archive_keys, progress_bar));
					} 

					Task.WaitAll(export_tasks.ToArray());
				}
			}

			Log.Information("All finished!");
		}
		
		public static async Task CloneOperation(DeveloperAccount client)
        {
			Console.WriteLine($"Select a game to clone. (1-{client.Games.Count()})");

			for (int i = 0; i < client.Games.Count; i++)
			{
				DeveloperGame game = client.Games[i];
				Console.WriteLine("{0} - {1}", i + 1, game.Name);
			}

			Console.WriteLine();
			Console.Write(">");

			// 'e_' is a prefix for 'export', and 'i_' for 'import'
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
			
			Log.Information("Operation 3. The database objects from {export} are being copied into {import}. This may take a long time depending on how many objects there are.", e_game.Name, i_game.Name);
			var bigdb_archive_path = Path.Combine(Environment.CurrentDirectory, $"bigdb-{e_game.GameId}.zip");

			await DownloadTables(bigdb_archive_path, e_bigDB);
			
			Log.Information("All database objects were downloaded successfully. Upload beginning.");

			using (var fs = new FileStream(bigdb_archive_path, FileMode.Open))
			{
				using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, true))
				{
					foreach (var entry in archive.Entries.OrderByDescending(t => uint.Parse(Path.GetFileNameWithoutExtension(t.Name))))
					{
						var table_name = entry.FullName.Split('/')[0];
						var database_objects = JsonConvert.DeserializeObject<DatabaseObjectSearchResult[]>(new StreamReader(entry.Open()).ReadToEnd());

						var table = i_bigDB.Tables.First(t => t.Name == table_name);

						foreach (var obj in database_objects)
						{
							var serialized_properties = JsonConvert.SerializeObject(new { properties = obj.properties });
							await i_bigDB.CreateOrModifyDatabaseObject(table, obj.title, serialized_properties); // TODO: add support for non-default gameDB

							Log.Information("Uploaded object {name} to table {table} with {count} properties.", obj.title, table_name, obj.properties.Count);
						}
					}
				}
			}

			Log.Information("The clone operation has successfully completed.");
			await Task.Delay(-1);
		}

		public static async Task DownloadTables(string bigdb_archive_path, BigDB bigDB)
		{
			foreach (var table in bigDB.Tables)
			{
				uint current_page = 0u;
				var web_export_utility = bigDB.GetWebExport(table); // TODO: support exporting from different game dbs beyond default

				using (var fs = new FileStream(bigdb_archive_path, FileMode.OpenOrCreate))
				{
					using (var archive = new ZipArchive(fs, ZipArchiveMode.Update, true))
					{
						// create directory for table if doesn't exist
						var directory = archive.Entries.Any(t => t.Name == table.Name)
							? new ZipArchiveDirectory(archive, table.Name)
							: archive.CreateDirectory(table.Name);

						current_page = directory.Archive.Entries.Where(t => t.FullName.StartsWith($"{table.Name}/")).Select(t => uint.Parse(Path.GetFileNameWithoutExtension(t.Name))).OrderByDescending(t => t)?.FirstOrDefault() ?? 0u;
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
		}

		static Task<List<DatabaseObject>> ProcessJob(Client client, string output_directory, string table, List<string> keys, ProgressBar progress_bar)
		{
			return Task.Run(() =>
			{
				var database_objects = new List<DatabaseObject>();

				for (var i = 0; i < keys.Count; i++)
				{
					var key = keys[i];

					progress_bar.Refresh(i, table + " - " + key);

					if (File.Exists(Path.Combine(output_directory, key + ".tson")))
						continue;

					try
					{
						var database_object = client.BigDB.Load(table, key);

						if (database_object == null)
							continue;

						database_objects.Add(database_object);
						File.WriteAllText(Path.Combine(output_directory, key + ".tson"), database_object.ToString());
					}
					catch (Exception ex)
					{
						Log.Error("Error occurred in ProcessJob() {ex}", ex);
						File.AppendAllLines("errorlog.txt", new[] { DateTime.Now.ToString() + " " + "ProcessJob() " + ex.Message });
					}
				}

				progress_bar.Refresh(keys.Count(), table + " - " + keys.Last());
				return database_objects;
			});
		}

		public static List<string> GetDatabaseObjectKeysFromArchive(string archiveFile)
		{
			var keys = new List<string>();

			using (var zipToOpen = new FileStream(archiveFile, FileMode.Open))
			{
				using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
				{
					var jsonEntry = archive.Entries.First();
					var pattern = new byte[] { 0x0D, 0x0A, 0x09, 0x22 };
					var stream = jsonEntry.Open();
					var positions = new List<long>();

					foreach (var position in stream.ScanAOB(pattern))
						positions.Add(position);

					stream.Position = 0;
					foreach (var position in positions)
					{
						stream.Position = position + 4;
						var key = "";

						while (true)
						{
							var b = stream.ReadByte();

							if (b == (byte)'"')
								break;

							key += (char)b;
						}

						keys.Add(key);
					}
				}
			}

			return keys;
		}

		public static ValueObject GetValue(ValueType type, object value) => type switch
		{
			ValueType.String => new ValueObject(ValueType.String, Convert.ToString(value), default, default, default, default, default, default, default, default, default, default),
			ValueType.Int => new ValueObject(ValueType.Int, default, Convert.ToInt32(value), default, default, default, default, default, default, default, default, default),
			ValueType.UInt => new ValueObject(ValueType.UInt, default, default, Convert.ToUInt32(value), default, default, default, default, default, default, default, default),
			ValueType.Long => new ValueObject(ValueType.Long, default, default, default, Convert.ToInt64(value), default, default, default, default, default, default, default),
			ValueType.Bool => new ValueObject(ValueType.Bool, default, default, default, default, Convert.ToBoolean(value), default, default, default, default, default, default),
			ValueType.Float => new ValueObject(ValueType.Float, default, default, default, default, default, Convert.ToSingle(value), default, default, default, default, default),
			ValueType.Double => new ValueObject(ValueType.Double, default, default, default, default, default, default, Convert.ToDouble(value), default, default, default, default),
			ValueType.ByteArray => new ValueObject(ValueType.ByteArray, default, default, default, default, default, default, default, Convert.FromHexString(Convert.ToString(value).Replace(" ", "")), default, default, default),
			ValueType.DateTime => new ValueObject(ValueType.DateTime, default, default, default, default, default, default, default, default, Convert.ToInt64(value), default, default),
			_ => throw new NotImplementedException(),
		};
	}
}