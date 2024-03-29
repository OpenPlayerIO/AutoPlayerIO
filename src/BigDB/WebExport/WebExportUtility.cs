﻿using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlayerIO
{
    public enum WebExportOutputFormat
    {
        /// <summary>
        /// The raw JSON returned by Player.IO BigDB web interface.
        /// </summary>
        RawJSON,
    }

    public class WebExportUtility
    {
        private readonly CookieSession _client;
        private readonly string _xsrfToken;
        private readonly DeveloperGame _game;
        private readonly Table _table;
        private readonly uint _gameDbId;

        /// <summary>
        /// This class enables you to download the database object contents of a specified table.
        /// </summary>
        /// <param name="game"> The game to download objects from. </param>
        /// <param name="table"> The table to download objects from. </param>
        /// <param name="gameDbId"> The ID of the BigDB database to use. The default database is 0. </param>
        internal WebExportUtility(CookieSession client, string xsrfToken, DeveloperGame game, Table table, uint gameDbId = 0)
        {
            _client = client;
            _xsrfToken = xsrfToken;
            _game = game;
            _table = table;
            _gameDbId = gameDbId;
        }

        /// <summary>
        /// Download a page of database objects from the specified table, in the specified format.
        /// </summary>
        /// <param name="format"> The format to deserialize into. </param>
        /// <param name="pageNum"> The page number. </param>
        /// <param name="pageSize"> The page size. </param>
        public async Task<string> DownloadPage(WebExportOutputFormat format, uint pageNum, uint pageSize = 100)
        {
            if (pageSize == 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "The page size has to be a non-zero value.");

            dynamic arguments = new ExpandoObject();
            arguments.page = pageNum;
            arguments.pageSize = pageSize;

            var response = await _client.Request((string)$"/my/bigdb/searchall/{_game.NavigationId}/{_gameDbId}/{_table.Id}/{_xsrfToken}")
                .PostUrlEncodedAsync((object)arguments)
                .ConfigureAwait(false);

            var json = await response.GetStringAsync();

            switch (format)
            {
                case WebExportOutputFormat.RawJSON:
                    return json;

                default:
                    throw new ArgumentException(nameof(format));
            }
        }
    }
}
