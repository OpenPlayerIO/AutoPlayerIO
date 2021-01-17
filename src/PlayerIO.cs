using AngleSharp;
using Flurl.Http;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoPlayerIO
{
    public static class PlayerIO
    {
        public const string API_ENDPOINT = "https://playerio.com";

        public static async Task<DeveloperAccount> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            // DON'T DISPOSE: This FlurlClient is used throughout the lifetime of the DeveloperAccount
            var client = new FlurlClient(API_ENDPOINT);

            var context = BrowsingContext.New(Configuration.Default);
            var loginPage = await client.Request("/login").GetStreamAsync(cancellationToken).ConfigureAwait(false);
            var document = await context.OpenAsync(req => req.Content(loginPage), cancellationToken).ConfigureAwait(false);

            var form = document.QuerySelectorAll("form");
            var csrf = form.First().QuerySelector("input").GetAttribute("value");

            var response = await client.Request("https://playerio.com/login").PostUrlEncodedAsync(new
            {
                CSRF = csrf,
                Username = username,
                Password = password,
                RememberME = "on"
            }, cancellationToken).ConfigureAwait(false);

            return await DeveloperAccount.LoadAsync(client, cancellationToken).ConfigureAwait(false);
        }
    }
}