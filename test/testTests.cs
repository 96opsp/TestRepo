using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Retail.Employee.Upsert.Common.Interfaces;
using Retail.Employee.Upsert.Common.Models;
using Xunit;

namespace Retail.Employee.Upsert.Common.Tests
{
    public class HttpRequestClientTests
    {
        [Fact]
        public async Task SendAsync_SuccessfulRequest_ReturnsResponseBody()
        {
            // Arrange
            ILogger logger = new LoggerFactory().CreateLogger("TestLogger");
            ServiceBusReceivedMessage message = new ServiceBusReceivedMessage();
            Uri uri = new Uri("https://example.com/api");
            HttpMethod method = HttpMethod.Get;
            string authKey = "authKey";
            HttpRequestClient httpRequestClient = new HttpRequestClient();

            // Act
            string responseBody = await httpRequestClient.SendAsync(logger, message, uri, method, authKey);

            // Assert
            Assert.NotNull(responseBody);
            Assert.NotEmpty(responseBody);
        }
    }
}