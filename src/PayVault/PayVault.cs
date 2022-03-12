using Flurl.Http;

using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AutoPlayerIO
{
    public class PayVault
    {
        public long StartBalance { get; }
        public long CoinsAdded { get; }
        public long CoinsUsed { get; }
        public long EndBalance { get; }

        public static async Task<PayVault> LoadAsync(CookieSession client, string xsrfToken, DeveloperGame game, CancellationToken cancellationToken = default)
        {
            var payVaultAnalytics = await client.Request($"/my/payvault/start/{game.NavigationId}/{xsrfToken}")
                .LoadDocumentAsync(cancellationToken)
                .ConfigureAwait(false);

            var start_balance = long.Parse(payVaultAnalytics.QuerySelectorAll("h5").Where(h => h.TextContent == "Start Balance").First().NextElementSibling.TextContent);
            var coins_added = long.Parse(payVaultAnalytics.QuerySelectorAll("h5").Where(h => h.TextContent == "Coins Added").First().NextElementSibling.TextContent);
            var coins_used = long.Parse(payVaultAnalytics.QuerySelectorAll("h5").Where(h => h.TextContent == "Coins Used").First().NextElementSibling.TextContent);
            var end_balance = long.Parse(payVaultAnalytics.QuerySelectorAll("h5").Where(h => h.TextContent == "End Balance").First().NextElementSibling.TextContent);

            return new PayVault(client, xsrfToken, game, start_balance, coins_added, coins_used, end_balance);
        }

        private PayVault(CookieSession client, string xsrfToken, DeveloperGame game, long start_balance, long coins_added, long coins_used, long end_balance)
        {
            this.StartBalance = start_balance;
            this.CoinsAdded = coins_added;
            this.CoinsUsed = coins_used;
            this.EndBalance = end_balance;
        }
    }
}
