namespace VendorCompass;

internal static class ItemKey
{
    public static uint Normalize(ulong value) => value switch
    {
        > 0 and < 500_000 => (uint)value,
        > 500_000 and < 1_000_000 => (uint)(value - 500_000),
        > 1_000_000 and < 1_500_000 => (uint)(value - 1_000_000),
        _ => 0
    };
}
