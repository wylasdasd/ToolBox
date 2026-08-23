using CommonHelp;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace TestCommonHelp;

public class CommonHelpTests
{
    [Fact]
    public void WriteAtomic_WritesJsonObjectInsteadOfQuotedJsonString()
    {
        var filePath = CreateTempFilePath();
        var model = new SampleModel { Name = "Alice", Count = 2 };

        AtomicJsonFileHelper.WriteAtomic(filePath, model);

        var text = File.ReadAllText(filePath, Encoding.UTF8);

        Assert.StartsWith("{", text.TrimStart());
        Assert.Contains("\"name\"", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\\"", text, StringComparison.Ordinal);
        var roundTrip = text.JsonDe<SampleModel>();
        Assert.NotNull(roundTrip);
        Assert.Equal("Alice", roundTrip!.Name);
        Assert.Equal(2, roundTrip.Count);
    }

    [Fact]
    public void ReadAtomic_Commit_PersistsUpdatedObject()
    {
        var filePath = CreateTempFilePath();
        AtomicJsonFileHelper.WriteAtomic(filePath, new SampleModel { Name = "Before", Count = 1 });

        using (var handle = AtomicJsonFileHelper.ReadAtomic<SampleModel>(filePath))
        {
            Assert.NotNull(handle.Data);
            handle.Data!.Name = "After";
            handle.Data.Count = 3;
            handle.Commit();
        }

        var text = File.ReadAllText(filePath, Encoding.UTF8);
        var saved = text.JsonDe<SampleModel>();
        Assert.NotNull(saved);
        Assert.Equal("After", saved!.Name);
        Assert.Equal(3, saved.Count);
    }

    [Fact]
    public void CountFileLinesAndChars_DoesNotInventTrailingNewlineBytes()
    {
        var filePath = CreateTempFilePath();
        const string content = "ab\r\ncd";
        File.WriteAllText(filePath, content, Encoding.UTF8);

        var result = StringHelp.CountFileLinesAndChars(filePath, Encoding.UTF8);

        Assert.Equal(2, result.Lines);
        Assert.Equal(Encoding.UTF8.GetByteCount(content), result.Count);
    }

    [Fact]
    public void CountFileCharacters_ReturnsCharacterCountWhenEncodingIsNull()
    {
        var filePath = CreateTempFilePath();
        const string content = "你A";
        File.WriteAllText(filePath, content, Encoding.UTF8);

        var charCount = StringHelp.CountFileCharacters(filePath);
        var byteCount = StringHelp.CountFileCharacters(filePath, Encoding.UTF8);

        Assert.Equal(content.Length, charCount);
        Assert.Equal(Encoding.UTF8.GetByteCount(content), byteCount);
    }

    [Fact]
    public async Task PutRequestAsync_OnTimeout_ReturnsDefaultInsteadOfThrowing()
    {
        var service = new HttpService(
            new FakeHttpClientFactory(new DelayedHandler(TimeSpan.FromMilliseconds(200))),
            NullLogger<IHttpService>.Instance);

        var result = await service.PutRequestAsync<SampleModel>(
            "https://example.com/api/test",
            "{}",
            timeoutSeconds: 0);

        Assert.Null(result);
    }

    private static string CreateTempFilePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ToolBoxCommonHelpTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "data.json");
    }

    private sealed class SampleModel
    {
        public string Name { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class DelayedHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                RequestMessage = request
            };
        }
    }
}
