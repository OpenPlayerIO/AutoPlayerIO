using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using Flurl.Http;

namespace PlayerIO
{
    public static class PlayerIO
    {
        public static async Task<DeveloperAccount> Login(string username, string password)
        {
            var client = new FlurlClient("https://playerio.com").EnableCookies();
            var context = BrowsingContext.New(Configuration.Default);
            var login_page = await client.Request("/login").GetStreamAsync();
            var document = await context.OpenAsync(req => req.Content(login_page));

            var form = document.QuerySelectorAll("form");
            var csrf = form.First().QuerySelector("input").GetAttribute("value");

            var response = await client.Request("https://playerio.com/login").PostUrlEncodedAsync(new
            {
                CSRF = csrf,
                Username = username,
                Password = password,
                RememberME = "on"
            });

            return new DeveloperAccount(client.Cookies.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value));
        }
    }
}
