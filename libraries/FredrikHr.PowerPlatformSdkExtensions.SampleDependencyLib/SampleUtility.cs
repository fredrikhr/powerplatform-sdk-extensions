using System.IO.Hashing;

namespace FredrikHr.PowerPlatformSdkExtensions.SampleDependencyLib;

public static class SampleUtility
{
    public static ReadOnlyMemory<byte> SampleXxHash32 { get; } =
        XxHash32.Hash("SampleUtility"u8);
}
