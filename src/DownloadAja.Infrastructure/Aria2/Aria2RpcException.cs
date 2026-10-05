namespace DownloadAja.Infrastructure.Aria2;

public sealed class Aria2RpcException : Exception
{
    public Aria2RpcException(int code, string message)
        : base($"aria2 RPC error {code}: {message}")
    {
        Code = code;
    }

    public int Code { get; }
}
