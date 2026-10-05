namespace DownloadAja.Infrastructure.Aria2;

public sealed record Aria2Status(
    string Gid,
    string Status,
    long CompletedLength,
    long? TotalLength,
    long DownloadSpeed,
    string? ErrorMessage);
