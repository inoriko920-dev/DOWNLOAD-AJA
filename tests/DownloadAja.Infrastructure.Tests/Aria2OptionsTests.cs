using DownloadAja.Infrastructure.Aria2;
using Xunit;

namespace DownloadAja.Infrastructure.Tests;

public sealed class Aria2OptionsTests
{
    [Fact]
    public void Historical_split_limit_20_is_preserved_but_per_server_is_clamped_to_16()
    {
        var options = new Aria2Options(
            "aria2c.exe",
            RpcPort: 6800,
            RpcSecret: "secret",
            SplitCount: 20).Validated();

        Assert.Equal(20, options.SplitCount);
        Assert.Equal(16, options.MaxConnectionsPerServer);
    }

    [Fact]
    public void Runtime_transfer_settings_update_future_split_and_speed_limit()
    {
        var options = new Aria2Options("aria2c.exe", 6800, "secret", 8).Validated();

        options.UpdateTransferSettings(12, 4 * 1024 * 1024);

        Assert.Equal(12, options.SplitCount);
        Assert.Equal(12, options.MaxConnectionsPerServer);
        Assert.Equal(4 * 1024 * 1024, options.GlobalDownloadLimitBytesPerSecond);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Split_count_outside_recovery_contract_is_rejected(int splitCount)
    {
        var options = new Aria2Options("aria2c.exe", 6800, "secret", splitCount);

        Assert.Throws<ArgumentOutOfRangeException>(options.Validated);
    }

    [Fact]
    public void Runtime_transfer_settings_reject_negative_speed_limit()
    {
        var options = new Aria2Options("aria2c.exe", 6800, "secret", 8).Validated();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.UpdateTransferSettings(8, -1));
    }
}
