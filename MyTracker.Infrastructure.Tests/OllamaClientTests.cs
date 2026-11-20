using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Exceptions;
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
        return CreateClient(handler);
    }

    private static OllamaClient CreateClient(FakeHttpMessageHandler handler)
        => new(new HttpClient(handler), new OllamaSettings { Model = "test-model" }, NullLogger<OllamaClient>.Instance);

    [Fact]
    public async Task GenerateCommentaryAsync_ReturnsTrimmedResponseText()
    {
        const string json = """{ "model": "test-model", "response": "  Bon travail !  ", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out _);

        var result = await client.GenerateCommentaryAsync(MakeActivity(), [], null);

        Assert.Equal("Bon travail !", result);
    }

    [Fact]
    public async Task GenerateCommentaryAsync_Throws_WhenResponseFieldIsMissing()
    {
        const string json = """{ "model": "test-model", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out _);

        await Assert.ThrowsAsync<ExternalServiceException>(() => client.GenerateCommentaryAsync(MakeActivity(), [], null));
    }

    [Fact]
    public async Task GenerateCommentaryAsync_ExplainsMissingModel_WhenOllamaReturns404()
    {
        var client = CreateClient(HttpStatusCode.NotFound, """{ "error": "model not found" }""", out _);

        var ex = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GenerateCommentaryAsync(MakeActivity(), [], null));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Contains("ollama pull test-model", ex.Message);
    }

    [Fact]
    public async Task GenerateCommentaryAsync_WrapsConnectionFailure_WhenOllamaIsNotRunning()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GenerateCommentaryAsync(MakeActivity(), [], null));

        Assert.Equal("Ollama", ex.ServiceName);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task GenerateCommentaryAsync_SendsPromptContainingKeyActivityMetrics()
    {
        const string json = """{ "model": "test-model", "response": "ok", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out var handler);

        await client.GenerateCommentaryAsync(MakeActivity(), [], null);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Morning Run", handler.LastRequestBody);
        Assert.Contains("Run", handler.LastRequestBody);
        Assert.Contains("test-model", handler.LastRequestBody);
    }

    [Fact]
    public async Task GenerateCommentaryAsync_IncludesProfileAndHeartRateZonePercentages_WhenProfileProvided()
    {
        const string json = """{ "model": "test-model", "response": "ok", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out var handler);
        var profile = new UserProfile(30, "Homme", 180, 75, 190, 55);

        await client.GenerateCommentaryAsync(MakeActivity(), [], profile);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("190", handler.LastRequestBody); // FC max du profil
        Assert.Contains("55", handler.LastRequestBody);  // FC repos du profil
        Assert.Contains("79%", handler.LastRequestBody);  // 150/190 (FC moyenne de l'activité de test)
    }

    [Fact]
    public async Task GenerateCommentaryAsync_IncludesFtpVmaAndCss_WhenProvidedInProfile()
    {
        const string json = """{ "model": "test-model", "response": "ok", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out var handler);
        var profile = new UserProfile(30, "Homme", 180, 75, 190, 55, FtpWatts: 250, VmaMinPerKm: 3.5, CriticalSwimSpeedMinPer100m: 1.5);

        await client.GenerateCommentaryAsync(MakeActivity(), [], profile);

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("FTP", handler.LastRequestBody);
        Assert.Contains("250", handler.LastRequestBody);
        Assert.Contains("VMA", handler.LastRequestBody);
        Assert.Contains("nage critique", handler.LastRequestBody);
    }

    [Fact]
    public async Task GenerateCommentaryAsync_OmitsFtpVmaAndCss_WhenNotProvidedInProfile()
    {
        const string json = """{ "model": "test-model", "response": "ok", "done": true }""";
        var client = CreateClient(HttpStatusCode.OK, json, out var handler);
        var profile = new UserProfile(30, "Homme", 180, 75, 190, 55);

        await client.GenerateCommentaryAsync(MakeActivity(), [], profile);

        Assert.NotNull(handler.LastRequestBody);
        Assert.DoesNotContain("FTP", handler.LastRequestBody);
        Assert.DoesNotContain("VMA", handler.LastRequestBody);
        Assert.DoesNotContain("nage critique", handler.LastRequestBody);
    }
}
