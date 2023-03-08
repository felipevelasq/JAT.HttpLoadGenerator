using System.Net;
using Moq;
using Moq.Protected;

namespace JatHttpLoadGenerator.Tests;

public class JatHttpLoadGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task When_GivenNConcurrentUsers_ReturnNUserResults(int concurrentUsers)
    {

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockedProtected = httpMessageHandlerMock.Protected();
        var setupRequest = mockedProtected.Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        setupRequest
            .Returns(() =>
            {
                return Task.Delay(TimeSpan.FromMilliseconds(10))
                    .ContinueWith(task =>
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(string.Empty),
                        };
                    });
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var generator = new HttpLoadGenerator(httpClient);
        
        // nSeconds is set to 0 so only the first request of each user gets emulated
        var loadResult = await generator.ExecuteLoad("http://dummy.com", concurrentUsers, nSeconds: 0);

        Assert.NotNull(loadResult);
        var usersResults = loadResult.Results;

        Assert.NotNull(usersResults);
        Assert.Equal(concurrentUsers, usersResults!.Count);
        Assert.All(usersResults, userResult =>
        {
            Assert.NotEqual(default, userResult.Key);

            Assert.NotNull(userResult.Value);
            Assert.NotEmpty(userResult.Value);

            Assert.All(userResult.Value, requestResult =>
            {
                Assert.NotEqual(default, requestResult.Status);
                Assert.NotEqual(default, requestResult.Time);
            });
        });
    }
}