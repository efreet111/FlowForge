using FlowForge.Installer.Infrastructure;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FlowForge.Installer.Tests;

public class GitHubReleasesClientTests
{
    [Fact]
    public async Task FR_001_GetLatestVersion_TimesOut()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"flowforge-log-{Guid.NewGuid():N}.log");
        var log = new InstallerLogger(logPath);
        using var http = new HttpClient(new DelayHandler(TimeSpan.FromSeconds(5))) { Timeout = TimeSpan.FromSeconds(1) };
        var client = new GitHubReleasesClient(http, log, downloadTimeoutSeconds: 1);
        await Assert.ThrowsAsync<TimeoutException>(() => client.GetLatestVersionAsync("efreet111/FlowForge", "stable"));
    }

    [Fact]
    public async Task FR_001_Download_CleansPartialFileOnFailure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var destPath = Path.Combine(tempDir, "engram-linux-x64");
            var log = new InstallerLogger(Path.Combine(tempDir, "installer.log"));
            using var http = new HttpClient(new FailStreamHandler()) { Timeout = TimeSpan.FromSeconds(5) };
            var client = new GitHubReleasesClient(http, log, downloadTimeoutSeconds: 1);
            var result = await client.DownloadEngramAsync("v0.0.0", destPath);
            Assert.False(result);
            Assert.False(File.Exists(destPath));
            Assert.False(File.Exists(destPath + ".tmp"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    sealed class DelayHandler : HttpMessageHandler
    {
        readonly TimeSpan _delay;

        public DelayHandler(TimeSpan delay) => _delay = delay;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{ \"tag_name\": \"v0.1.0\" }]" )
            };
        }
    }

    sealed class FailStreamHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsoluteUri.EndsWith(".sha256") == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("1234567890abcdef  asset")
                });
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FailStream())
            };
            return Task.FromResult(response);
        }
    }

    sealed class FailStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("boom");
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromException<int>(new IOException("boom"));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
