using CommonHelp;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

    [Fact]
    public async Task SendAsync_ReturnsRawResponse_WithoutSwallowingNonSuccess()
    {
        var service = new HttpService(
            new FakeHttpClientFactory(new FixedStatusHandler(HttpStatusCode.BadGateway, "raw body")),
            NullLogger<IHttpService>.Instance);

        using var response = await service.SendAsync(
            HttpMethod.Post,
            "https://example.com/api/test",
            "{\"a\":1}");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("raw body", await response.Content.ReadAsStringAsync());
        Assert.NotNull(response.RequestMessage?.Content);
        Assert.Equal("application/json", response.RequestMessage!.Content!.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void JsonSeNoLow_SupportsValueTypes_SuchAsJsonElement()
    {
        using var doc = JsonDocument.Parse("{\"名称\":\"工具箱\"}");

        var json = doc.RootElement.JsonSeNoLow();

        Assert.Contains("\"名称\": \"工具箱\"", json);
    }

    [Fact]
    public void JsonSeToLow_LowerCasesPropertyNames_AndKeepsChineseUnescaped()
    {
        var json = new SampleModel { Name = "工具箱", Count = 2 }.JsonSeToLow();

        Assert.Contains("\"name\": \"工具箱\"", json);
    }

    [Fact]
    public void HttpErrorHelp_TryExtractMessage_SupportsOpenAiStyleNestedError()
    {
        var message = HttpErrorHelp.TryExtractMessage("{\"error\":{\"message\":\"Rate limit exceeded\"}}");

        Assert.Equal("Rate limit exceeded", message);
    }

    [Fact]
    public void HttpErrorHelp_TryExtractMessage_ReturnsNullOnNonJsonOrMissingFields()
    {
        Assert.Null(HttpErrorHelp.TryExtractMessage("<html>502 Bad Gateway</html>"));
        Assert.Null(HttpErrorHelp.TryExtractMessage("{\"ok\":true}"));
        Assert.Null(HttpErrorHelp.TryExtractMessage(""));
    }

    [Fact]
    public void HttpErrorHelp_FormatFailure_AppendsDetailWhenAvailable()
    {
        var withDetail = HttpErrorHelp.FormatFailure("调用失败", HttpStatusCode.TooManyRequests, "{\"message\":\"额度不足\"}");
        var withoutDetail = HttpErrorHelp.FormatFailure("调用失败", HttpStatusCode.BadGateway, "oops");

        Assert.Equal("调用失败：HTTP 429，额度不足", withDetail);
        Assert.Equal("调用失败：HTTP 502。", withoutDetail);
    }

    [Fact]
    public void MarkdownJsonHelp_NormalizeToJson_StripsFenceAndIndents()
    {
        var raw = "前置说明\n```json\n{\"名称\":\"工具箱\"}\n```\n后置说明";

        var json = MarkdownJsonHelp.NormalizeToJson(raw);

        Assert.Contains("\"名称\": \"工具箱\"", json);
    }

    [Fact]
    public void MarkdownJsonHelp_StripCodeFence_ReturnsTrimmedTextWhenNoFence()
    {
        Assert.Equal("  {\"a\":1}  ".Trim(), MarkdownJsonHelp.StripCodeFence("  {\"a\":1}  "));
        Assert.Equal("{\"a\":1}", MarkdownJsonHelp.StripCodeFence("```\n{\"a\":1}\n```"));
    }

    [Fact]
    public void MimeHelp_GuessContentType_MapsCommonExtensions()
    {
        Assert.Equal("image/png", MimeHelp.GuessContentType("a.PNG"));
        Assert.Equal("application/json", MimeHelp.GuessContentType("b.json"));
        Assert.Equal("text/plain", MimeHelp.GuessContentType("c.unknown"));
    }

    [Fact]
    public void MimeHelp_DetectImageMimeType_AndIsValidBase64()
    {
        Assert.Equal("image/png", MimeHelp.DetectImageMimeType("iVBORw0KGgoAAAANS"));
        Assert.Equal(string.Empty, MimeHelp.DetectImageMimeType("not-an-image"));

        Assert.True(MimeHelp.IsValidBase64("aGVsbG8="));
        Assert.False(MimeHelp.IsValidBase64("aGVsb-G!"));
        Assert.False(MimeHelp.IsValidBase64(""));
    }

    [Fact]
    public void MimeHelp_TryParseDataUrl_AndBuildDataUrl_RoundTrip()
    {
        var dataUrl = MimeHelp.BuildDataUrl("image/png", [1, 2, 3]);

        Assert.StartsWith("data:image/png;base64,", dataUrl);

        var parsed = MimeHelp.TryParseDataUrl(dataUrl, out var normalized, out var base64Part, out var mimeType);

        Assert.True(parsed);
        Assert.Equal("image/png", mimeType);
        Assert.Equal(dataUrl, normalized);
        Assert.True(MimeHelp.IsValidBase64(base64Part));
        Assert.False(MimeHelp.TryParseDataUrl("https://example.com/a.png", out _, out _, out _));
    }

    [Fact]
    public async Task FileHelp_ReadAllBytesAsync_ResetsSeekableStreamAndReadsAll()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);
        stream.Position = 3;

        var bytes = await FileHelp.ReadAllBytesAsync(stream);

        Assert.Equal([1, 2, 3, 4, 5], bytes);
    }

    [Fact]
    public void FileHelp_FormatFileSize_Uses1024StepsAndTrimsDecimals()
    {
        Assert.Equal("0 B", FileHelp.FormatFileSize(0));
        Assert.Equal("512 B", FileHelp.FormatFileSize(512));
        Assert.Equal("1 KB", FileHelp.FormatFileSize(1024));
        Assert.Equal("1.5 KB", FileHelp.FormatFileSize(1024 + 512));
        Assert.Equal("1.5 MB", FileHelp.FormatFileSize(1024L * 1024 + 512 * 1024));
        Assert.Equal("1 TB", FileHelp.FormatFileSize(1024L * 1024 * 1024 * 1024));
        Assert.Equal("1024 TB", FileHelp.FormatFileSize(1024L * 1024 * 1024 * 1024 * 1024));
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

    private sealed class FixedStatusHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
                RequestMessage = request
            });
        }
    }
}
