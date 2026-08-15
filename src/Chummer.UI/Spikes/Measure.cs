using System.Globalization;

namespace Chummer.UI.Spikes;

internal static class Measure
{
    public static string Bytes(long value)
    {
        if (value < 0)
            return "-" + Bytes(-value);
        if (value < 1024)
            return value.ToString(CultureInfo.InvariantCulture) + " B";
        double kib = value / 1024d;
        if (kib < 1024)
            return kib.ToString("0.0", CultureInfo.InvariantCulture) + " KiB";
        double mib = kib / 1024d;
        if (mib < 1024)
            return mib.ToString("0.00", CultureInfo.InvariantCulture) + " MiB";
        return (mib / 1024d).ToString("0.00", CultureInfo.InvariantCulture) + " GiB";
    }

    public static string Bytes(long? value) => value.HasValue ? Bytes(value.Value) : "n/d";

    public static string Millis(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture) + " ms";

    public static string Count(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
