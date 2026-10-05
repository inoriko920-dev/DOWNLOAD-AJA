using DownloadAja.BrowserBridge;
using DownloadAja.Persistence.Settings;

namespace DownloadAja.Application.Downloads;

/// <summary>
/// RECONSTRUCTED R8 bridge from the versioned local handoff protocol to the R7
/// AddDownloadService. This guarantees that an Accepted response is emitted only
/// after validation, queue insertion, and persistence have completed.
/// </summary>
public sealed class BrowserDownloadHandoffService : IBrowserDownloadHandoff
{
    private readonly AddDownloadService _addDownloadService;
    private readonly string? _fixedDefaultDownloadDirectory;
    private readonly IAppSettingsStore? _settingsStore;

    public BrowserDownloadHandoffService(
        AddDownloadService addDownloadService,
        string defaultDownloadDirectory)
    {
        _addDownloadService = addDownloadService ?? throw new ArgumentNullException(nameof(addDownloadService));
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultDownloadDirectory);
        _fixedDefaultDownloadDirectory = defaultDownloadDirectory;
    }

    public BrowserDownloadHandoffService(
        AddDownloadService addDownloadService,
        IAppSettingsStore settingsStore)
    {
        _addDownloadService = addDownloadService ?? throw new ArgumentNullException(nameof(addDownloadService));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public async Task<BrowserHandoffResponse> HandleAsync(
        BrowserHandoffRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Version != BrowserHandoffProtocol.CurrentVersion)
        {
            return BrowserHandoffResponse.Reject(
                $"Versi protocol browser tidak didukung: {request.Version}.");
        }

        if (!string.Equals(
                request.Command,
                BrowserHandoffProtocol.AddUrlCommand,
                StringComparison.OrdinalIgnoreCase))
        {
            return BrowserHandoffResponse.Reject(
                $"Perintah handoff tidak didukung oleh download service: {request.Command}.");
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BrowserHandoffResponse.Reject("Browser tidak mengirim URL.");
        }

        try
        {
            var defaultDirectory = _settingsStore is null
                ? _fixedDefaultDownloadDirectory!
                : (await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false)).DefaultDownloadDirectory;

            var result = await _addDownloadService.AddAsync(
                new AddDownloadRequest(
                    request.Url,
                    defaultDirectory,
                    FileName: null,
                    StartQueueAfterAdd: request.StartQueueAfterAdd),
                cancellationToken).ConfigureAwait(false);

            return BrowserHandoffResponse.Accept(
                $"URL diterima: {result.Item.FileName}",
                result.Item.Id);
        }
        catch (AddDownloadValidationException ex)
        {
            return BrowserHandoffResponse.Reject(ex.Message);
        }
    }
}
