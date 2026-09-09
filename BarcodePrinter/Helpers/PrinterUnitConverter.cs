namespace BarcodePrinter.Helpers;
public static class PrinterUnitConverter
{
    public static double MmToPixels(double mm, double dpi) { if (!double.IsFinite(mm) || !double.IsFinite(dpi) || dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi)); return mm * dpi / 25.4; }
    public static double PixelsToMm(double pixels, double dpi) { if (!double.IsFinite(pixels) || !double.IsFinite(dpi) || dpi <= 0) throw new ArgumentOutOfRangeException(nameof(dpi)); return pixels * 25.4 / dpi; }
    public static int MmToDots(double mm, int dpi) => checked((int)Math.Round(MmToPixels(mm, dpi), MidpointRounding.AwayFromZero));
    public static double DotsToMm(double dots, int dpi) => PixelsToMm(dots, dpi);
}
