using BarcodePrinter;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Services.Printing;
using BarcodePrinter.Forms.Printing;
using System.Reflection;
internal static class PrinterRoutingChecks
{
    public static int Run()
    {
        int count=0;void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
        var old=new PrinterProfile{PrinterName="TSC TTP-244CE",Dpi=300,Mode=PrintMode.WindowsDriver,Calibration=new(){XOffsetMm=1.5}};
        var routed=PrinterRouting.Resolve(old);Check(routed.Mode==PrintMode.RawTspl&&routed.Dpi==203&&routed.Calibration.XOffsetMm==1.5&&old.Mode==PrintMode.WindowsDriver,"Legacy TSC profile routes direct without mutating saved calibration");
        Check(PrinterRouting.Resolve(new(){PrinterName="Raf etiketi"},"TSC TTP-244CE").Mode==PrintMode.RawTspl,"Renamed TSC detected by Windows driver");
        Check(PrinterRouting.Resolve(new(){PrinterName="TTP 244CE"}).Model==Ttp244CePrinter.Model,"TTP model detected without brand prefix");
        Check(PrinterRouting.Resolve(new(){PrinterName="Office",Dpi=600}).Mode==PrintMode.WindowsDriver,"Other Windows printers retain driver mode");
        foreach(string port in new[]{"FILE:"," file: ","PORTPROMPT:","USB001,FILE:","C:\\Temp\\test.prn",""})
        {
            try{PrinterRouting.ValidatePort("TSC",port);throw new Exception("Expected port rejection");}catch(InvalidOperationException){Check(true,"File or unknown destination blocked: "+port);}
        }
        foreach(string port in new[]{"USB001","IP_192.168.1.10","WSD-123","\\\\server\\TSC"}){PrinterRouting.ValidatePort("TSC",port);Check(true,"Physical queue port accepted: "+port);}
        Check(PrinterRouting.AutomaticCandidates(["FILE:","COM1:","USB002","USB001","IP_10.0.0.4"]).SequenceEqual(["USB002","USB001"]),"Automatic routing prioritizes USB and excludes file ports");
        Check(PrinterRouting.IsUsbPort("USB2")&&PrinterRouting.IsUsbPort("USB002"),"USB2 and USB002 are recognized as physical ports");
        using var form=new BarcodePrintForm([]);form.Show();Application.DoEvents();
        object Field(string name)=>typeof(BarcodePrintForm).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        var printers=(ComboBox)Field("printers");var mode=(ComboBox)Field("mode");var dpi=(ComboBox)Field("dpi");var devices=(Dictionary<string,PrinterInfo>)Field("discovered");
        devices["Test TSC"]=new("Test TSC","TSC TTP-244CE","USB001","Hazır",false,"","203",true);printers.Items.Add("Test TSC");printers.SelectedItem="Test TSC";
        Check(mode.SelectedIndex==(int)PrintMode.RawTspl&&!mode.Enabled&&Equals(dpi.SelectedItem,203),"Selecting TSC in print form automatically uses direct TSPL and 203 DPI");
        Check(((ComboBox)Field("portInfo")).Text=="USB001","Print screen shows actual selected printer port");form.Close();
        return count;
    }
}
