using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace KamiYomu.CrawlerAgents.MangaDex;

public class LoggingHandler(ILogger logger, HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Request: {Method} {RequestUri}", request.Method, request.RequestUri);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        logger.LogDebug("Response: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);

        return response;
    }
}
