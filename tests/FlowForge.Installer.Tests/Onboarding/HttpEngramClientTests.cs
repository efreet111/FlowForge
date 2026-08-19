using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FlowForge.Installer.Commands;
using FlowForge.Installer.Onboarding;
using Xunit;

namespace FlowForge.Installer.Tests.Onboarding;

public class HttpEngramClientTests
{
    [Fact] // FR-013
    public async Task HealthCheck_Success_ReturnsTrue()
    {
        var handler = new MockHandler(req =>
        {
            Assert.Contains("/health", req.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var http = new HttpClient(handler);
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test", ownsHttpClient: false);

        var result = await client.HealthCheckAsync();
        Assert.True(result);
    }

    [Fact] // FR-013
    public async Task HealthCheck_ServerDown_ReturnsFalse()
    {
        var handler = new MockHandler(_ => Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused")));
        using var http = new HttpClient(handler);
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test");

        var result = await client.HealthCheckAsync();
        Assert.False(result);
    }

    [Fact] // FR-013, NFR-001
    public async Task Search_WithTypeFilter_ParsesResults()
    {
        var responseJson = """
        {
            "results": [
                {
                    "observation": {
                        "id": 97,
                        "type": "decision",
                        "title": "Use AOT compilation",
                        "content": "We decided to use AOT for performance.",
                        "project": "team/flowforge",
                        "scope": "team",
                        "topicKey": "aot",
                        "createdAt": "2026-08-01T10:00:00Z",
                        "sessionId": 1
                    },
                    "rank": 0.95
                }
            ]
        }
        """;

        var handler = new MockHandler(req =>
        {
            Assert.Contains("type=decision", req.RequestUri!.AbsoluteUri);
            Assert.Contains("project=team%2Fflowforge", req.RequestUri.AbsoluteUri);
            Assert.True(req.Headers.Contains("X-Engram-User"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            });
        });
        using var http = new HttpClient(handler);
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test");

        var results = await client.SearchAsync("", "decision", "team/flowforge", "team", 10);

        Assert.Single(results);
        Assert.Equal(97, results[0].Id);
        Assert.Equal("decision", results[0].Type);
        Assert.Equal("Use AOT compilation", results[0].Title);
    }

    [Fact] // FR-008
    public async Task Stats_ParsesCorrectly()
    {
        var responseJson = """
        {
            "totalSessions": 45,
            "totalObservations": 65,
            "totalPrompts": 23,
            "projects": ["flowforge", "other"],
            "backend": "postgres"
        }
        """;

        var handler = new MockHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        }));
        using var http = new HttpClient(handler);
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test");

        var stats = await client.GetStatsAsync();

        Assert.NotNull(stats);
        Assert.Equal(45, stats!.TotalSessions);
        Assert.Equal(65, stats.TotalObservations);
        Assert.Equal(23, stats.TotalPrompts);
        Assert.Equal(2, stats.Projects.Count);
        Assert.Equal("postgres", stats.Backend);
    }

    [Fact] // NFR-001
    public async Task Timeout_ThrowsTaskCanceledException()
    {
        var handler = new MockHandler(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetStatsAsync());
    }

    [Fact] // NFR-001 — Configurable timeout via constructor
    public async Task ConfigurableTimeout_RespectsCallerTimeout()
    {
        var handler = new MockHandler(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        // Caller sets a 100ms timeout
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test");

        // GetStatsAsync propagates the timeout exception (unlike HealthCheckAsync which catches)
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetStatsAsync());
    }

    [Fact] // FR-010
    public async Task GetObservation_ReturnsFullContent()
    {
        var responseJson = """
        {
            "id": 97,
            "type": "decision",
            "title": "Use AOT compilation",
            "content": "Full content of the decision.",
            "project": "team/flowforge",
            "scope": "team",
            "topicKey": "aot",
            "createdAt": "2026-08-01T10:00:00Z",
            "sessionId": 1
        }
        """;

        var handler = new MockHandler(req =>
        {
            Assert.Contains("/observations/97", req.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            });
        });
        using var http = new HttpClient(handler);
        using var client = new HttpEngramClient(http, "https://engram.test", "user@test");

        var obs = await client.GetObservationAsync(97);

        Assert.NotNull(obs);
        Assert.Equal(97, obs!.Id);
        Assert.Equal("Full content of the decision.", obs.Content);
    }

    // ── Mock HttpMessageHandler ───────────────────────────────────────────────

    sealed class MockHandler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public MockHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }

    // ── NFR-001: FLOWFORGE_API_TIMEOUT_SECONDS env var tests ────────────────

    [Fact] // NFR-001 — valid env var is honored
    public void GetApiTimeoutSeconds_ValidEnvVar_ReturnsEnvValue()
    {
        var previous = Environment.GetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS");
        try
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", "5");
            var result = OnboardCommand.GetApiTimeoutSeconds();
            Assert.Equal(5, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", previous);
        }
    }

    [Fact] // NFR-001 — absent env var returns default 30
    public void GetApiTimeoutSeconds_NoEnvVar_ReturnsDefault30()
    {
        var previous = Environment.GetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS");
        try
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", null);
            var result = OnboardCommand.GetApiTimeoutSeconds();
            Assert.Equal(30, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", previous);
        }
    }

    [Fact] // NFR-001 — non-numeric env var returns default 30
    public void GetApiTimeoutSeconds_NonNumericEnvVar_ReturnsDefault30()
    {
        var previous = Environment.GetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS");
        try
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", "not-a-number");
            var result = OnboardCommand.GetApiTimeoutSeconds();
            Assert.Equal(30, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", previous);
        }
    }

    [Fact] // NFR-001 — zero/negative env var returns default 30
    public void GetApiTimeoutSeconds_ZeroOrNegativeEnvVar_ReturnsDefault30()
    {
        var previous = Environment.GetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS");
        try
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", "0");
            Assert.Equal(30, OnboardCommand.GetApiTimeoutSeconds());

            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", "-5");
            Assert.Equal(30, OnboardCommand.GetApiTimeoutSeconds());
        }
        finally
        {
            Environment.SetEnvironmentVariable("FLOWFORGE_API_TIMEOUT_SECONDS", previous);
        }
    }
}
