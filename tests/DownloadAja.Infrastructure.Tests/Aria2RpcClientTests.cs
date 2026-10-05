using System.Net;
using System.Text;
using System.Text.Json;
using DownloadAja.Infrastructure.Aria2;
using Xunit;

namespace DownloadAja.Infrastructure.Tests;

public sealed class Aria2RpcClientTests
{
    [Fact]
    public async Task AddUri_sends_token_destination_and_connection_options()
    {
        var handler = new RecordingHandler("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"result\":\"gid-123\"}");
        var options = new Aria2Options("aria2c.exe", 6800, "top-secret", 20).Validated();
        var client = new Aria2RpcClient(options, new HttpClient(handler));

        var gid = await client.AddUriAsync(
            new Uri("https://example.com/file.iso"),
            @"C:\Downloads\file.iso");

        Assert.Equal("gid-123", gid);
        Assert.NotNull(handler.RequestBody);

        using var document = JsonDocument.Parse(handler.RequestBody!);
        var root = document.RootElement;
        Assert.Equal("aria2.addUri", root.GetProperty("method").GetString());

        var parameters = root.GetProperty("params");
        Assert.Equal("token:top-secret", parameters[0].GetString());
        Assert.Equal("https://example.com/file.iso", parameters[1][0].GetString());

        var rpcOptions = parameters[2];
        Assert.Equal("file.iso", rpcOptions.GetProperty("out").GetString());
        Assert.Equal("20", rpcOptions.GetProperty("split").GetString());
        Assert.Equal("16", rpcOptions.GetProperty("max-connection-per-server").GetString());
        Assert.Equal("true", rpcOptions.GetProperty("continue").GetString());
    }

    [Fact]
    public async Task TellStatus_maps_numeric_strings_and_error_message()
    {
        const string response = """
        {
          "jsonrpc":"2.0",
          "id":"1",
          "result":{
            "gid":"abc",
            "status":"active",
            "totalLength":"1000",
            "completedLength":"250",
            "downloadSpeed":"125",
            "errorMessage":""
          }
        }
        """;

        var handler = new RecordingHandler(response);
        var client = new Aria2RpcClient(
            new Aria2Options("aria2c.exe", 6800, "secret", 8).Validated(),
            new HttpClient(handler));

        var status = await client.TellStatusAsync("abc");

        Assert.Equal("abc", status.Gid);
        Assert.Equal("active", status.Status);
        Assert.Equal(250, status.CompletedLength);
        Assert.Equal(1000, status.TotalLength);
        Assert.Equal(125, status.DownloadSpeed);
        Assert.Equal(string.Empty, status.ErrorMessage);
    }

    [Fact]
    public async Task Rpc_error_is_exposed_as_typed_exception()
    {
        const string response = """
        {"jsonrpc":"2.0","id":"1","error":{"code":1,"message":"bad request"}}
        """;

        var client = new Aria2RpcClient(
            new Aria2Options("aria2c.exe", 6800, "secret", 8).Validated(),
            new HttpClient(new RecordingHandler(response)));

        var exception = await Assert.ThrowsAsync<Aria2RpcException>(() => client.PauseAsync("gid"));

        Assert.Equal(1, exception.Code);
        Assert.Contains("bad request", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public RecordingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
