using System.Net;
using Moq;
using Moq.Protected;

namespace JatHttpLoadGenerator.Tests;

public class JatHttpLoadGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task When_GivenNConcurrentUsers_ReturnNResults(int concurrentUsers)
    {

        var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockedProtected = httpMessageHandlerMock.Protected();
        var setupRequest = mockedProtected.Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        setupRequest
            .Callback(()=> Thread.Sleep(10))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty),
            });

        var generator = new HttpLoadGenerator(httpMessageHandlerMock.Object);
        var loadResult = await generator.ExecuteLoad("http://dummy.com", concurrentUsers);

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