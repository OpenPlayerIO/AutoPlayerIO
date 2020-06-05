using AngleSharp;
using AngleSharp.Html.Dom;

using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerIO
{
    public class DeveloperAccount
    {
        internal static async Task<DeveloperAccount> LoadAsync(
            FlurlClient client,
            CancellationToken cancellationToken = default
        )
        {
            var flurlRequest = client.Request("/my/account/details");
            var angleSharpResponse = await flurlRequest.ToAngleSharpResponse(executionPredicate: null, cancellationToken).ConfigureAwait(false);

            var browsingContext = BrowsingContext.New(Configuration.Default);
            var accountDetailsDocument = await browsingContext.OpenAsync(angleSharpResponse, cancellationToken).ConfigureAwait(false);

            var username = accountDetailsDocument.QuerySelector("#accountinfo").QuerySelector("a").TextContent;
            var email = accountDetailsDocument.QuerySelector("#Email").GetAttribute("value");

            var gamesTasks = new List<Task<DeveloperGame>>();

            // TODO: lazy load games
            foreach (var element in accountDetailsDocument.QuerySelector("#mygamesdropdown .scrollcontainer").Children)
            {
                var path = (element as IHtmlAnchorElement)?.PathName ?? throw new Exception("library broke, pls fix");

                gamesTasks.Add(DeveloperGame.LoadAsync(client, path, cancellationToken));
            }

            var games = await Task.WhenAll(gamesTasks).ConfigureAwait(false);

            // we cast down List<T> to an IReadOnlyList<T> so we can claim type safety and not permit modifications,
            // but also allow the end user to cast back to a List<T> if they need to mess with it for whatever reason.
            return new DeveloperAccount(username, email, games);
        }

        /// <summary>
        /// The username associated with the Player.IO account.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The email associated with the Player.IO account.
        /// </summary>
        public string Email { get; }

        /// <summary>
        /// A list of games the account has access to manage.
        /// </summary>
        public IReadOnlyList<DeveloperGame> Games { get; }

        private DeveloperAccount(string username, string email, IReadOnlyList<DeveloperGame> games)
        {
            Username = username;
            Email = email;
            Games = games;
        }
    }
}