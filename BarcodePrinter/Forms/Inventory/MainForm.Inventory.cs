using BarcodePrinter.Controls.Common;
namespace BarcodePrinter;
public partial class Form1
{
    private void EditProduct(Product? p)
    {
        using var dialog=new ProductEditor(p,inventory);if(dialog.ShowDialog(this)==DialogResult.OK)ShowPage(page);
    }
    private void StockDialog(Product p,string initialKind="Stok girişi")
    {
        using var form=new AppDialog{Text="Stok işlemi",Size=new Size(640,420),MinimumSize=new Size(640,420),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Padding=new Padding(20)};
        var header=new Label{Text=$"{p.Name}\nMevcut stok: {p.Stock:0.##} {p.Unit}   •   Minimum: {p.Minimum:0.##}",Dock=DockStyle.Top,Height=66,Font=new Font("Segoe UI",11),Padding=new Padding(0,0,0,12)};
        var fields=new AppFieldGrid(160);
        var kind=new AppComboBox();kind.Items.AddRange(Inventory.MovementKinds);kind.SelectedItem=initialKind;
        fields.AddField("İşlem türü",kind);
        var qty=new AppNumericInput{Maximum=1000000000,DecimalPlaces=3,Value=1};var hint=fields.AddField("İşlem miktarı",qty);
        kind.SelectedIndexChanged+=(_,_)=>hint.Text=kind.SelectedIndex==2?"Yeni toplam stok":"İşlem miktarı";
        var note=new AppTextBox{Multiline=true,PlaceholderText="Açıklama / belge numarası"};fields.AddField("Açıklama",note,80);
        var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(fields);
        var actions=new AppToolbar{Dock=DockStyle.Bottom,FlowDirection=FlowDirection.RightToLeft};
        actions.Controls.Add(Button("İşlemi kaydet",()=>Attempt(()=>{inventory.Move(p.Id,kind.Text,qty.Value,note.Text);form.DialogResult=DialogResult.OK;})));
        actions.Controls.Add(Button("Vazgeç",()=>form.DialogResult=DialogResult.Cancel,false));
        form.Controls.Add(body);form.Controls.Add(header);form.Controls.Add(actions);if(form.ShowDialog(this)==DialogResult.OK)ShowPage(page);
    }
}
