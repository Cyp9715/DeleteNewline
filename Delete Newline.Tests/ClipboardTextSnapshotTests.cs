using Delete_Newline.Services;

namespace Delete_Newline.Tests;

public class ClipboardTextSnapshotTests
{
    [Fact]
    public async Task StableClipboardNeedsOneRead()
    {
        int reads = 0;
        var snapshot = await ClipboardTextSnapshot.ReadAsync(() => 10, () =>
        {
            reads++;
            return Task.FromResult<string?>("a\r\nb");
        });
        Assert.Equal(new ClipboardTextSnapshot("a\r\nb", 10), snapshot);
        Assert.Equal(1, reads);
    }

    [Fact]
    public async Task DelayedRenderingMayChangeSequenceIfStableTextIsConfirmed()
    {
        uint version = 10;
        var snapshot = await ClipboardTextSnapshot.ReadAsync(() => version, () =>
        {
            version = 11;
            return Task.FromResult<string?>("a\r\nb");
        });
        Assert.Equal(new ClipboardTextSnapshot("a\r\nb", 11), snapshot);
    }

    [Fact]
    public async Task NewerCopyDuringReadIsNotTransformed()
    {
        uint version = 10;
        int reads = 0;
        var snapshot = await ClipboardTextSnapshot.ReadAsync(() => version, () =>
        {
            version = 11;
            return Task.FromResult<string?>(reads++ == 0 ? "old" : "new");
        });
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ChangeDuringConfirmationIsNotAcceptedEvenWithIdenticalText()
    {
        uint version = 10;
        var snapshot = await ClipboardTextSnapshot.ReadAsync(() => version, () =>
        {
            version++;
            return Task.FromResult<string?>("same");
        });
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task EmptyTextIsAValidSnapshot()
    {
        var snapshot = await ClipboardTextSnapshot.ReadAsync(() => 1, () => Task.FromResult<string?>(string.Empty));
        Assert.Equal(new ClipboardTextSnapshot(string.Empty, 1), snapshot);
    }
}
