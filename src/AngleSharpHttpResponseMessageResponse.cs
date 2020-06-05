using AngleSharp;
using AngleSharp.Common;
using AngleSharp.Io;

using Flurl.Http;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace PlayerIO
{
    // yes the name is ugly, idk what to name it
    internal class AngleSharpHttpResponseMessageResponse : IResponse
    {
        private readonly HttpResponseMessage _httpResponseMessage;

        public AngleSharpHttpResponseMessageResponse(Url address, HttpResponseMessage httpResponseMessage, Stream content)
        {
            Address = address;
            _httpResponseMessage = httpResponseMessage;
            Content = content;
        }

        public HttpStatusCode StatusCode => _httpResponseMessage.StatusCode;
        public Url Address { get; }
        public IDictionary<string, string> Headers => _httpResponseMessage.Headers.ToDictionary();
        public Stream Content { get; }

        public void Dispose()
        {
            _httpResponseMessage.Dispose();
            Content.Dispose();
        }
    }

    internal static class AngleSharpHttpResponseMessageResponseExtensions
    {
        public static async Task<AngleSharpHttpResponseMessageResponse> ToAngleSharpResponse(
            this IFlurlRequest flurlRequest,
            Func<IFlurlRequest, Task<HttpResponseMessage>>? executionPredicate,
            CancellationToken cancellationToken = default
        )
        {
            // conversion from *flurl's* custom URL to *anglesharp's* custom URL
            // apparently .NET's URI just sucks that much
            var uri = flurlRequest.Url.ToUri();
            var url = Url.Convert(uri);

            var httpResponseMessageTask = executionPredicate == null
                ? flurlRequest.GetAsync(cancellationToken)
                : executionPredicate(flurlRequest);

            var httpResponseMessage = await httpResponseMessageTask.ConfigureAwait(false);

            var content = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new AngleSharpHttpResponseMessageResponse(url, httpResponseMessage, content);
        }
    }
}