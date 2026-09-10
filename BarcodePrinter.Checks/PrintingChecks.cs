using System.Text;
using BarcodePrinter;
using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Printing.Rendering;
using BarcodePrinter.Printing.TSPL;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Services.Validation;
using BarcodePrinter.Forms.Designer;
using BarcodePrinter.Forms.Printing;
using BarcodePrinter.Forms.Printers;
using ZXing;
internal static class PrintingChecks
{
    public static int Run(string directory)
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
        void Reject(Action action,string name){try{action();}catch(InvalidOperationException){Check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        foreach(int dpi in new[]{203,300,600})
        {Check(PrinterUnitConverter.MmToDots(25.4,dpi)==dpi,"MmToDots "+dpi);Check(Math.Abs(PrinterUnitConverter.DotsToMm(dpi,dpi)-25.4)<.00001,"DotsToMm "+dpi);Check(Math.Abs(PrinterUnitConverter.PixelsToMm(PrinterUnitConverter.MmToPixels(17.345,dpi),dpi)-17.345)<.0000001,"No accumulated mm rounding "+dpi);}
        Check(ValidationService.Ean("4006381333931",13),"Valid EAN13");Check(!ValidationService.Ean("4006381333932",13),"Invalid EAN13 checksum");Check(ValidationService.Ean("96385074",8),"Valid EAN8");Check(!ValidationService.Ean("96385075",8),"Invalid EAN8 checksum");Reject(()=>ValidationService.Barcode("abc",BarcodeKind.EAN8),"EAN text rejected");
        var t=TemplateService.Standard();var clone=JsonStore.Clone(t);Check(clone.Elements[1] is BarcodeElement && clone.WidthMm==50,"Polymorphic template roundtrip");
        var all=new LabelTemplate{WidthMm=100,HeightMm=100,Elements=[new TextElement(),new CompositeTextElement(),new BarcodeElement(),new QrCodeElement(),new ImageElement{ImageBase64="AA=="},new RectangleElement(),new LineElement()]};Check(JsonStore.Clone(all).Elements.Select(e=>e.GetType()).SequenceEqual(all.Elements.Select(e=>e.GetType())),"All seven element types roundtrip");
        var invalid=JsonStore.Clone(t);invalid.WidthMm=0;Reject(()=>ValidationService.Template(invalid),"Invalid label size");invalid=JsonStore.Clone(t);invalid.Elements[0].Xmm=49;Reject(()=>ValidationService.Template(invalid),"Out of bounds element");invalid=JsonStore.Clone(t);invalid.Dpi=72;Reject(()=>ValidationService.Template(invalid),"Unsupported print DPI");
        var p=DynamicFields.Sample;p.Barcode="4006381333931";var at=new DateTime(2026,9,9,13,45,0);
        var text=DynamicFields.Resolve("{{ProductName}}|{{Barcode}}|{{SKU}}|{{Price}}|{{OldPrice}}|{{Stock}}|{{Date}}|{{Time}}",p,PriceFormat.SuffixSymbol,at);
        Check(text.Contains("49,90 ₺")&&text.Contains("59,90 ₺")&&text.EndsWith("09.09.2026|13:45"),"Dynamic product fields and fixed timestamp");Check(DynamicFields.Price(49.9m,PriceFormat.DotTL)=="49.90 TL","Price dot format");Reject(()=>DynamicFields.Resolve("{{Unknown}}",p,PriceFormat.DotTL),"Unknown dynamic field");
        var templates=new TemplateService(Path.Combine(directory,"templates"));templates.Save(t);Check(templates.List().Count==1,"Template save and list");templates.Save(t);Check(File.Exists(Path.Combine(directory,"templates",t.Id+".json.bak")),"Template atomic backup");templates.InstallPresets();Check(templates.List().Count>=7,"Built-in templates");
        foreach(var preset in templates.List()){using var bitmap=new LabelPreviewRenderer().Render(preset,p,203,at);Check(bitmap.Width>0,"Preset renders "+preset.Name);}
        templates.SaveDraft(t);Check(templates.Drafts().Count==1&&templates.Draft(t.Id)?.Id==t.Id,"Unsaved draft discovery and recovery");
        string oldJson="{\"Products\":[{\"Name\":\"Legacy\",\"Barcode\":\"1234\",\"Sku\":\"OLD\",\"Stock\":3}],\"Movements\":[]}";
        var legacyPath=Path.Combine(directory,"legacy.json");File.WriteAllText(legacyPath,oldJson);var legacy=new Inventory(legacyPath);Check(legacy.Data.Products.Single().OldPrice==null&&legacy.Data.Products.Single().Stock==3&&File.Exists(legacyPath+".migrated.bak"),"Legacy JSON migrates to SQLite with backup");
        var history=new DesignerHistory();for(int i=0;i<35;i++){history.Push(t);t.Name="Edit "+i;}for(int i=0;i<35;i++)t=history.Undo(t);Check(t.Name=="Standart Ürün Barkodu","35 undo operations");t=history.Redo(t);Check(t.Name=="Edit 0","Redo");
        t=TemplateService.Standard();
        foreach(int dpi in new[]{203,300,600})
        {
            using var bitmap=new LabelPreviewRenderer().Render(t,p,dpi,at);Check(bitmap.Width==PrinterUnitConverter.MmToDots(50,dpi)&&bitmap.Height==PrinterUnitConverter.MmToDots(30,dpi),"Exact raster dimensions "+dpi);
            var profile=new PrinterProfile{Dpi=dpi};var raw=new TsplLabelRenderer().Render(t,[p],profile,at);var header=Encoding.ASCII.GetString(raw.Take(140).ToArray());Check(header.StartsWith("SIZE 50 mm,30 mm\r\nGAP 2 mm,0 mm\r\nDIRECTION 1,0\r\nREFERENCE 0,0\r\nCLS\r\nBITMAP 0,0,"),"TSPL header "+dpi);Check(Encoding.ASCII.GetString(raw.TakeLast(13).ToArray()).Contains("PRINT 1,1"),"TSPL print trailer "+dpi);
        }
        var rawCheck=new TsplLabelRenderer().Render(t,[p],new PrinterProfile(),at);using(var rasterCheck=new LabelPreviewRenderer().RenderSheet(t,[p],new PrinterProfile(),at))
        {
            string marker=$"BITMAP 0,0,{(rasterCheck.Width+7)/8},{rasterCheck.Height},0,";int offset=Encoding.ASCII.GetString(rawCheck).IndexOf(marker,StringComparison.Ordinal)+marker.Length;bool matches=true;int black=0;
            for(int y=0;y<rasterCheck.Height;y++)for(int x=0;x<rasterCheck.Width;x++){var color=rasterCheck.GetPixel(x,y);bool dark=(color.B*114+color.G*587+color.R*299)/1000<160;bool tsplDark=(rawCheck[offset+y*((rasterCheck.Width+7)/8)+x/8]&(0x80>>(x%8)))==0;if(dark)black++;matches&=dark==tsplDark;}
            Check(matches&&black>0,"TSPL bitmap polarity and pixels match shared preview");
        }
        var builder=new TsplCommandBuilder();Reject(()=>builder.AddText(1,1,"x\"\r\nPRINT 10"),"TSPL injection rejected");Reject(()=>builder.AddBitmap(0,0,2,2,[0]),"BITMAP length validation");
        var media=TemplateService.Standard();media.Media=MediaKind.BlackMark;Check(Encoding.ASCII.GetString(new TsplLabelRenderer().Render(media,[p],new(),at).Take(70).ToArray()).Contains("BLINE 2 mm,0 mm"),"Black mark TSPL");media.Media=MediaKind.Continuous;Check(Encoding.ASCII.GetString(new TsplLabelRenderer().Render(media,[p],new(),at).Take(70).ToArray()).Contains("GAP 0 mm,0 mm"),"Continuous TSPL");
        var cal=new PrinterCalibration{XOffsetMm=1,YOffsetMm=-.5};cal.Validate();Check(PrinterUnitConverter.MmToDots(cal.XOffsetMm,203)==8&&PrinterUnitConverter.MmToDots(cal.YOffsetMm,203)==-4,"Signed calibration offsets");Reject(()=>new PrinterCalibration{HorizontalScale=0}.Validate(),"Invalid calibration scale");
        var sheet=TemplateService.Standard();sheet.Rows=5;sheet.Columns=2;using(var bitmap=new LabelPreviewRenderer().RenderSheet(sheet,[p,p],new(),at)){Check(bitmap.Width==PrinterUnitConverter.MmToDots(102,203)&&bitmap.Height==PrinterUnitConverter.MmToDots(158,203),"2 by 5 sheet geometry");}
        foreach(var pair in new[]{(BarcodeKind.EAN13,"4006381333931"),(BarcodeKind.EAN8,"96385074"),(BarcodeKind.Code128,"8691234567890"),(BarcodeKind.Code39,"ABC-123"),(BarcodeKind.UPCA,"042100005264"),(BarcodeKind.UPCE,"04252614"),(BarcodeKind.ITF,"12345678"),(BarcodeKind.Codabar,"A1234B"),(BarcodeKind.QRCode,"Barcode Pro Türkçe")})
        {
            var barcode=new LabelTemplate{WidthMm=100,HeightMm=50,Elements=[new BarcodeElement{BarcodeType=pair.Item1,Value=pair.Item2,Xmm=2,Ymm=2,WidthMm=96,HeightMm=46,HumanReadableText=false}]};
            using var bitmap=new LabelPreviewRenderer().Render(barcode,p,203,at);var pixels=new byte[bitmap.Width*bitmap.Height*4];var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);try{System.Runtime.InteropServices.Marshal.Copy(data.Scan0,pixels,0,pixels.Length);}finally{bitmap.UnlockBits(data);}
            var reader=new BarcodeReaderGeneric{Options=new ZXing.Common.DecodingOptions{TryHarder=true,PossibleFormats=[ValidationService.Format(pair.Item1)],ReturnCodabarStartEnd=true}};var result=reader.Decode(pixels,bitmap.Width,bitmap.Height,RGBLuminanceSource.BitmapFormat.BGRA32);Check(result!=null&&result.Text==pair.Item2,"Barcode roundtrip decoding "+pair.Item1);
        }
        using(var form=new LabelDesignerForm()){form.Show();Application.DoEvents();Check(form.Controls.Count>0,"Designer opens");using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(directory,"designer.png"));form.Close();}
        using(var form=new BarcodePrintForm([p],[p])){form.Show();Application.DoEvents();Check(form.Controls.Count>0,"Batch print opens without physical printer");using(var shot=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));shot.Save(Path.Combine(directory,"batch-layout.png"));}form.Close();}
        using(var editor=(Form)Activator.CreateInstance(typeof(Form1).Assembly.GetType("BarcodePrinter.ProductEditor")!,[null,new Inventory(Path.Combine(directory,"editor.json"))])!)
        {
            editor.Show();Application.DoEvents();using var shot=new Bitmap(editor.Width,editor.Height);editor.DrawToBitmap(shot,new Rectangle(Point.Empty,editor.Size));shot.Save(Path.Combine(directory,"product-editor.png"));editor.Close();
        }
        var demoPath=Path.Combine(directory,"demo.json");var demo=new Inventory(demoPath);demo.SeedDemo();
        using(var main=new Form1(demoPath))
        {
            main.Show();main.Size=new Size(1366,768);Application.DoEvents();
            foreach(var page in new[]{"Dashboard","Ürünler","Etiket Şablonları","Raporlar","Baskı Kuyruğu","Ayarlar"})
            {
                typeof(Form1).GetMethod("ShowPage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(main,[page]);Application.DoEvents();
                Check(main.ClientSize.Width>1000,"Main screen "+page);using var image=new Bitmap(main.Width,main.Height);main.DrawToBitmap(image,new Rectangle(Point.Empty,main.Size));image.Save(Path.Combine(directory,"main-"+page+".png"));
            }
            Check(!main.Controls.OfType<BarcodePrinter.Controls.Common.AppSidebar>().Any(),"Sidebar removed");
            var modules=main.Controls.OfType<BarcodePrinter.Controls.Common.AppModuleMenu>().Single();
            var tabs=main.Controls.OfType<BarcodePrinter.Controls.Common.AppWorkspaceTabs>().Single();
            var caption=main.Controls.OfType<Panel>().Single(p=>Equals(p.Tag,"caption"));
            Check(caption.Visible && caption.Bottom<=modules.Top,"Branded caption stays above navigation");
            Check(main.Icon!=null && caption.Controls.OfType<PictureBox>().Single().Image!=null,"Window icon and logo available");
            var restore=main.Bounds;main.WindowState=FormWindowState.Maximized;Application.DoEvents();
            Check(Screen.FromControl(main).WorkingArea.Contains(main.Bounds),"Custom frame maximizes inside taskbar work area");
            main.WindowState=FormWindowState.Normal;Application.DoEvents();Check(main.Bounds==restore,"Custom frame restores previous window bounds: "+restore+" -> "+main.Bounds);
            Check(modules.Items.OfType<ToolStripDropDownButton>().Count()==8,"Eight compact module menus");
            Check(modules.Height+tabs.Height<=100,"Navigation limited to two compact rows");
            Check(modules.Items.OfType<ToolStripDropDownButton>().All(m=>m.DropDownItems.Count>0),"All module menus have working subcommands");
            var productsMenu=modules.Items.OfType<ToolStripDropDownButton>().Single(m=>m.Text=="Ürünler");productsMenu.DropDownItems[0].PerformClick();Check(tabs.ActivePage=="Ürünler","Product submenu activates workspace tab");
            tabs.Items.OfType<ToolStripButton>().Single(t=>t.Text=="Dashboard").PerformClick();Check(tabs.ActivePage=="Dashboard","Workspace tab switches screen");
            foreach(int width in new[]{1100,1366,1920})
            {
                main.Size=new Size(width,768);Application.DoEvents();Check(modules.Height+tabs.Height<=100,"Compact top menu at "+width);
            }
            main.Size=new Size(1920,1040);Application.DoEvents();using(var full=new Bitmap(main.Width,main.Height)){main.DrawToBitmap(full,new Rectangle(Point.Empty,main.Size));full.Save(Path.Combine(directory,"main-reference-layout.png"));}            BarcodePrinter.Themes.ThemeManager.Toggle();Check(BarcodePrinter.Themes.ThemeManager.IsDark,"Dark theme applied");BarcodePrinter.Themes.ThemeManager.Toggle();main.Close();
        }
        using(var designer=new LabelDesignerForm())
        {
            designer.Show();var canvas=designer.Controls.OfType<BarcodePrinter.Controls.Designer.LabelCanvas>().Single();canvas.Focus();canvas.Selection.Add(designer.Template.Elements[0].Id);
            Check(canvas.ContextMenuStrip?.Items.Count > 5,"Designer context commands available");
            int original=designer.Template.Elements.Count;
            var method=typeof(LabelDesignerForm).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            void Key(Keys key){object?[] arguments=[new Message(),key];method.Invoke(designer,arguments);}
            Key(Keys.Control|Keys.D);Check(designer.Template.Elements.Count==original+1,"Designer duplicate command");Key(Keys.Control|Keys.Z);Check(designer.Template.Elements.Count==original,"Designer undo command");Key(Keys.Control|Keys.Y);Check(designer.Template.Elements.Count==original+1,"Designer redo command");
            canvas.Selection.Clear();canvas.Selection.Add(designer.Template.Elements[0].Id);double x=designer.Template.Elements[0].Xmm;Key(Keys.Right);Check(designer.Template.Elements[0].Xmm>x,"Designer nudge");
            Key(Keys.Delete);Check(designer.Template.Elements.Count==original,"Designer delete");
            // Dispose a test-only unsaved design without presenting the close/save prompt.
        }
        Console.WriteLine("Artifacts: "+directory);return count;
    }
}










