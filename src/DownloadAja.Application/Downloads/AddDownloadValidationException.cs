namespace DownloadAja.Application.Downloads;

public sealed class AddDownloadValidationException : Exception
{
    public AddDownloadValidationException(string message)
        : base(message)
    {
    }
}
