﻿using AngleSharp;
using AngleSharp.Common;
using AngleSharp.Dom;
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

namespace AutoPlayerIO
{
    /// <summary>
    /// Rather than having some ugly OpenAsync with .Results to convert <see cref="Task{T}"/>s to Ts, having his class allows us to
    /// execute the exact method of AngleSharp which takes an <see cref="IResponse"/>. This prevents that .Result hell, and allows
    /// for more truly asynchronous code.
    /// <para>
    /// The primary usecase of this is in <see cref="FlurlClientExtensions.LoadDocumentAsync(IFlurlRequest, CancellationToken)"/>,
    /// which utilizes <see cref="ASMessageResponseExtensions.ToAngleSharpResponse(IFlurlRequest, Func{IFlurlRequest, Task{HttpResponseMessage}}?, CancellationToken)"/>.
    /// </para>
    /// </summary>
    internal class ASMessageResponse : IResponse
    {
        private readonly HttpResponseMessage _httpResponseMessage;

        public ASMessageResponse(Url address, HttpResponseMessage httpResponseMessage, Stream content)
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

    internal static class ASMessageResponseExtensions
    {
        public static async Task<ASMessageResponse> ToAngleSharpResponse(
            this IFlurlRequest flurlRequest,
            Func<IFlurlRequest, Task<HttpResponseMessage>> executionPredicate,
            CancellationToken cancellationToken = default
        )
        {
            // conversion from *flurl's* custom URL to *anglesharp's* custom URL
            // apparently .NET's URI just sucks that much
            var uri = flurlRequest.Url.ToUri();
            var url = Url.Convert(uri);

            
            if (executionPredicate == null)
            {
                Task<IFlurlResponse> flurlResponseMessageTask = flurlRequest.GetAsync(cancellationToken);

                var flurlResponseMessage = await flurlResponseMessageTask.ConfigureAwait(false);

                var content = await flurlResponseMessage.ResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

                return new ASMessageResponse(url, flurlResponseMessage.ResponseMessage, content);
            }
            else
            {

                Task<HttpResponseMessage> httpResponseMessageTask = executionPredicate(flurlRequest);

                var httpResponseMessage = await httpResponseMessageTask.ConfigureAwait(false);

                var content = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

                return new ASMessageResponse(url, httpResponseMessage, content);
            }
        }
    }
}