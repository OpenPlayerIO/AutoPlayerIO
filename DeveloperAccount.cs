using System.Collections.Generic;
using AngleSharp;
using AngleSharp.Html.Dom;
using Flurl.Http;

namespace PlayerIO
{
    public class DeveloperAccount
    {
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
        public List<DeveloperGame> Games { get; }

        internal DeveloperAccount(Dictionary<string, string> cookies)
        {
            this.Client = new FlurlClient("https://playerio.com/").WithCookies(cookies);
            this.Games = new List<DeveloperGame>();

            var account_details = BrowsingContext.New(Configuration.Default)
                .OpenAsync(req => req.Content(this.Client.Request("/my/account/details").GetStreamAsync().Result)).Result;

            this.Username = account_details.QuerySelector("#accountinfo").QuerySelector("a").TextContent;
            this.Email = account_details.QuerySelector("#Email").GetAttribute("value");

            foreach (var element in account_details.QuerySelector("#mygamesdropdown .scrollcontainer").Children)
                this.Games.Add(new DeveloperGame(this, ((IHtmlAnchorElement)element).PathName));
        }

        internal FlurlClient Client { get; set; }
    }
}
