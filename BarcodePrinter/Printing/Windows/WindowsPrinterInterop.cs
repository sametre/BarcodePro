using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace BarcodePrinter.Printing.Windows;
internal sealed class PrinterHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public PrinterHandle() : base(true) { }
    protected override bool ReleaseHandle() => WindowsPrinterInterop.ClosePrinter(handle);
}
internal static class WindowsPrinterInterop
{
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct DocInfo { public string DocName; public string? OutputFile; public string DataType; }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] internal struct PrinterInfo2
    {
        public string? ServerName,PrinterName,ShareName,PortName,DriverName,Comment,Location;
        public IntPtr DevMode;
        public string? SepFile,PrintProcessor,DataType,Parameters;
        public IntPtr SecurityDescriptor;
        public uint Attributes,Priority,DefaultPriority,StartTime,UntilTime,Status,Jobs,AveragePpm;
    }
    [DllImport("winspool.drv",EntryPoint="OpenPrinterW",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool OpenPrinter(string name,out PrinterHandle handle,IntPtr defaults);
    [DllImport("winspool.drv",SetLastError=true)] internal static extern bool ClosePrinter(IntPtr handle);
    [DllImport("winspool.drv",EntryPoint="StartDocPrinterW",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern uint StartDocPrinter(PrinterHandle handle,int level,ref DocInfo doc);
    [DllImport("winspool.drv",SetLastError=true)] internal static extern bool StartPagePrinter(PrinterHandle handle);
    [DllImport("winspool.drv",SetLastError=true)] internal static extern bool WritePrinter(PrinterHandle handle,IntPtr bytes,int length,out int written);
    [DllImport("winspool.drv",SetLastError=true)] internal static extern bool EndPagePrinter(PrinterHandle handle);
    [DllImport("winspool.drv",SetLastError=true)] internal static extern bool EndDocPrinter(PrinterHandle handle);
    [DllImport("winspool.drv",SetLastError=true)] internal static extern bool AbortPrinter(PrinterHandle handle);
    [DllImport("winspool.drv",EntryPoint="GetPrinterW",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool GetPrinter(PrinterHandle handle,int level,IntPtr buffer,uint size,out uint needed);
    internal static void Ensure(bool result) { if (!result) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    internal static PrinterInfo2 Info(string name)
    {
        Ensure(OpenPrinter(name,out var handle,IntPtr.Zero)); using(handle)
        {
            GetPrinter(handle,2,IntPtr.Zero,0,out uint needed); if (needed == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            var buffer=Marshal.AllocHGlobal(checked((int)needed)); try { Ensure(GetPrinter(handle,2,buffer,needed,out _)); return Marshal.PtrToStructure<PrinterInfo2>(buffer); } finally { Marshal.FreeHGlobal(buffer); }
        }
    }
}
public sealed class RawPrinterService
{
    public void Send(string printerName,byte[] data,string title)
    {
        if (data.Length == 0) throw new InvalidOperationException("Baskı verisi boş.");
        WindowsPrinterInterop.Ensure(WindowsPrinterInterop.OpenPrinter(printerName,out var handle,IntPtr.Zero));
        using(handle)
        {
            var doc = new WindowsPrinterInterop.DocInfo { DocName=title,DataType="RAW" };
            bool started=false;
            try
            {
                if (WindowsPrinterInterop.StartDocPrinter(handle,1,ref doc)==0) throw new Win32Exception(Marshal.GetLastWin32Error()); started=true;
                WindowsPrinterInterop.Ensure(WindowsPrinterInterop.StartPagePrinter(handle));
                var pin=GCHandle.Alloc(data,GCHandleType.Pinned);
                try { int offset=0; while(offset<data.Length) { WindowsPrinterInterop.Ensure(WindowsPrinterInterop.WritePrinter(handle,IntPtr.Add(pin.AddrOfPinnedObject(),offset),data.Length-offset,out int written)); if(written<=0) throw new IOException("Spooler veri kabul etmedi."); offset+=written; } } finally { pin.Free(); }
                WindowsPrinterInterop.Ensure(WindowsPrinterInterop.EndPagePrinter(handle)); WindowsPrinterInterop.Ensure(WindowsPrinterInterop.EndDocPrinter(handle)); started=false;
            }
            finally { if(started) WindowsPrinterInterop.AbortPrinter(handle); }
        }
    }
}
