using System.Net;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace MyTracker.Infrastructure.Tests;

public class OllamaClientTests
{
    private static Activity MakeActivity() => new(
        "1", "Morning Run", DateTime.UtcNow, "Run", 5000, 1800, 50,
        150, 180, null, null, 80, 300, 60);

    private static OllamaClient CreateClient(HttpStatusCode statusCode, string responseContent, out FakeHttpMessageHandler handler)
    {
        handler = new FakeHttpMessageHandler(statusCode, responseContent);
        var httpClient = new HttpClient(handler);
        return new OllamaClient(httpClient, new OllamaSettings { Model = "test-model" });
    }

    [Fact]
    public async Task GenerateCommentaryAsync_ReturnsTrimmedResponseText()
    {
        const string json = """{ "model": "test-model", "response": "  Bon travail !  ", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out _);

        var result = await client.GenerateCommentaryAsync(MakeActivity(), []);

        Assert.Equal("Bon travail !", result);
    }

    [Fact]
    public async Task GenerateCommentaryAsync_Throws_WhenResponseFieldIsMissing()
    {
        const string json = """{ "model": "test-model", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out _);

        await Assert.ThrowsAsync<Exception>(() => client.GenerateCommentaryAsync(MakeActivity(), []));
    }

    [Fact]
    public async Task GenerateCommentaryAsync_SendsPromptContainingKeyActivityMetrics()
    {
        const string json = """{ "model": "test-model", "response": "ok", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out var handler);

        await client.GenerateCommentaryAsync(MakeActivity(), []);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Morning Run", handler.LastRequestBody);
        Assert.Contains("Run", handler.LastRequestBody);
        Assert.Contains("test-model", handler.LastRequestBody);
    }
}
