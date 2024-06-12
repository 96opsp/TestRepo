
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using Retail.Employee.Upsert.Common.Interfaces;
using Retail.Employee.Upsert.Common.Models;
using Retail.Employee.Upsert.Common.Utilities;
using Retail.OData.Client;

namespace Retail.Employee.Upsert.Common.Services
{
    [ExcludeFromCodeCoverage]
    public class HttpRequestClient : IHttpRequestClient
    {
        private static readonly HttpClient HttpClient;

        static HttpRequestClient()
        {
            HttpClient = new HttpClient();
        }

        public async Task<string> SendAsync(ILogger log, ServiceBusReceivedMessage message, Uri uri, HttpMethod method, string authKey, int retryCount = 3, int retryMs = 15)
        {
            int attempt = 0;

            // Final Timeout will be Override, or Infinite.
            int timeoutMs = System.Threading.Timeout.Infinite;

            // Timeout for individual context.Message
            System.Threading.CancellationTokenSource cancel
                = new System.Threading.CancellationTokenSource(timeoutMs);

            HttpRequestMessage request = new HttpRequestMessage(method, uri);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.APPLICATION_JSON));
            request.Headers.Add(Constants.APIGATEWAY_HEADER_SOURCE_SYSTEM, Constants.APIGATEWAY_HEADER_SOURCE_SYSTEM_VALUE);
            request.Headers.Add(Constants.APIGATEWAY_HEADER_MESSAGE_SOURCE, Constants.APIGATEWAY_HEADER_MESSAGE_SOURCE_VALUE);

            // GetSecret must LoadOnDemand as we shouldn't preload until we know what is required
            request.Headers.Add(Constants.APIGATEWAY_HEADER_KEY, authKey);

            // ServiceBus User Properties as Headers
            foreach (string userProperty in message.ApplicationProperties.Keys)
            {
                if (userProperty != "Transfer-Encoding")
                {
                    if (message.ApplicationProperties[userProperty] != null)
                        request.Headers.Add(userProperty, message.ApplicationProperties[userProperty].ToString());
                }
            }

            // ServiceBus System Properties as Headers
            if (message.CorrelationId.HasValue()) request.Headers.Add(nameof(message.CorrelationId), message.CorrelationId);

            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                // Body UTF8
                request.Content = new StringContent(Encoding.UTF8.GetString(message.Body), Encoding.UTF8, message.ContentType);
            }

            AsyncRetryPolicy<HttpResponseMessage> transientRetryPolicy = Policy
                .HandleResult<HttpResponseMessage>(res => !res.IsSuccessStatusCode && !HttpErrorCodes.PermanentErrorCodes.Contains(res.StatusCode))
                .WaitAndRetryAsync(retryCount,
               retryAttempt => TimeSpan.FromSeconds(retryCount * retryAttempt) + TimeSpan.FromMilliseconds(retryMs)
           );

            // Execute HttpRequest
            HttpResponseMessage response = await transientRetryPolicy.ExecuteAsync(async ctx =>
            {
                return await SendRequest(log, request, cancel.Token, attempt++);
            }, new Context("Retry Transient Error"));

            log.LogInformation($"Response Status Code: {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            // Extract Response Body (regardless of success, could be BadRequest with details)
            string responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }

        private async Task<HttpResponseMessage> SendRequest(ILogger log, HttpRequestMessage request, CancellationToken cancellationToken, int attempt)
        {
            log.LogInformation($"Http Request Attempt: {attempt}");

            request = request.Clone();

            var response = await HttpClient.SendAsync(request, cancellationToken);

            log.LogInformation("Request Uri :{Host}/{AbsolutePath}", response.RequestMessage.RequestUri.Host, response.RequestMessage.RequestUri.AbsolutePath);

            if (response.IsSuccessStatusCode)
            {
                log.LogInformation($"Attempt - {attempt}, Successful {response.RequestMessage.RequestUri.Host}/{response.RequestMessage.RequestUri.AbsolutePath} Status Code  {(int)response.StatusCode}", TelemetryEvent.Retry_Policy);
            }

            return response;
        }
    }
}
