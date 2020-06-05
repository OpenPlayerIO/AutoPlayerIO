using AngleSharp;
using AngleSharp.Dom;

using Flurl.Http;

using System.Threading;
using System.Threading.Tasks;

namespace PlayerIO
{
    internal static class FlurlClientExtensions
    {
        public static async Task<IDocument> LoadDocumentAsync(this IFlurlRequest flurlRequest, CancellationToken cancellationToken = default)
        {
            var angleSharpResponse = await flurlRequest.ToAngleSharpResponse(executionPredicate: null, cancellationToken).ConfigureAwait(false);

            var browsingContext = BrowsingContext.New(Configuration.Default);
            var document = await browsingContext.OpenAsync(angleSharpResponse, cancellationToken).ConfigureAwait(false);

            return document;
        }
    }
}