using BarcodePrinter.Controls.Common;
using BarcodePrinter.Forms.Designer;
namespace BarcodePrinter;
public partial class Form1
{
    private void Products(bool status)
    {
        var grid=Grid();grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None;grid.RowTemplate.Height=32;
        var toolbar=new AppToolbar();var search=new AppTextBox{Width=240,PlaceholderText="Ürün, barkod, SKU veya kategori"};var filter=new AppComboBox{Width=160};filter.Items.AddRange(["Tüm ürünler","Kritik stok","Tükenen","Aktif","Pasif","Menüde","Menü dışında"]);filter.SelectedIndex=0;
        int currentPage=0;const int pageSize=75;string sort="Ürün";bool descending=false;var pageLabel=Label("");var images=new List<Image>();
        grid.Columns.Add(new DataGridViewImageColumn{Name="Görsel",ImageLayout=DataGridViewImageCellLayout.Zoom,Width=58,DefaultCellStyle=new DataGridViewCellStyle{NullValue=null}});
        foreach(var name in new[]{"Ürün","Barkod","SKU","Kategori","Fiyat","Stok","Minimum","Maksimum","Birim","Durum","Menüde Göster","Son Güncelleme"})grid.Columns.Add(new DataGridViewTextBoxColumn{Name=name,Width=name=="Ürün"?190:name=="Barkod"?130:110,SortMode=DataGridViewColumnSortMode.Programmatic});
        List<Product> Selected()=>grid.SelectedRows.Cast<DataGridViewRow>().Where(r=>r.Tag is Product).Select(r=>(Product)r.Tag!).ToList();
        void Refresh()
        {
            var selectedIds=Selected().Select(p=>p.Id).ToHashSet();int firstRow=grid.FirstDisplayedScrollingRowIndex;
            grid.Rows.Clear();foreach(var image in images)image.Dispose();images.Clear();
            var q=inventory.Data.Products.Where(p=>$"{p.Name} {p.Barcode} {p.Sku} {p.Category}".Contains(search.Text.Trim(),StringComparison.CurrentCultureIgnoreCase));
            q=filter.SelectedIndex switch{1=>q.Where(p=>p.Stock>0&&p.Stock<=p.Minimum),2=>q.Where(p=>p.Stock==0),3=>q.Where(p=>p.Active),4=>q.Where(p=>!p.Active),5=>q.Where(p=>p.OnMenu),6=>q.Where(p=>!p.OnMenu),_=>q};
            Func<Product,object> key=sort switch{"Barkod"=>p=>p.Barcode,"SKU"=>p=>p.Sku,"Kategori"=>p=>p.Category,"Fiyat"=>p=>p.Price,"Stok"=>p=>p.Stock,"Minimum"=>p=>p.Minimum,"Maksimum"=>p=>p.Maximum,"Birim"=>p=>p.Unit,"Durum"=>p=>p.Active,"Menüde Göster"=>p=>p.OnMenu,"Son Güncelleme"=>p=>p.UpdatedAt,_=>p=>p.Name};
            var all=(descending?q.OrderByDescending(key):q.OrderBy(key)).ToList();currentPage=Math.Clamp(currentPage,0,Math.Max(0,(all.Count-1)/pageSize));
            foreach(var p in all.Skip(currentPage*pageSize).Take(pageSize))
            {
                Image? thumbnail=null;try{if(p.ImageData is {Length:>0}){using var stream=new MemoryStream(p.ImageData);using var original=Image.FromStream(stream);thumbnail=new Bitmap(original,new Size(28,28));}else if(File.Exists(p.ImagePath)){using var original=Image.FromFile(p.ImagePath);thumbnail=new Bitmap(original,new Size(28,28));}if(thumbnail!=null)images.Add(thumbnail);}catch(ArgumentException){}catch(IOException){}catch(OutOfMemoryException){}
                int row=grid.Rows.Add(thumbnail,p.Name,p.Barcode,p.Sku,p.Category,p.Price.ToString("C2"),p.Stock,p.Minimum,p.Maximum,p.Unit,p.Active?"Aktif":"Pasif",p.OnMenu?"Evet":"Hayır",p.UpdatedAt.ToString("dd.MM.yyyy HH:mm"));grid.Rows[row].Tag=p;if(p.Stock<=p.Minimum)grid.Rows[row].DefaultCellStyle.ForeColor=Color.FromArgb(195,115,39);
            }
            pageLabel.Text=$"{all.Count:N0} ürün · Sayfa {currentPage+1}/{Math.Max(1,(all.Count+pageSize-1)/pageSize)}";
            if(selectedIds.Count>0){grid.ClearSelection();foreach(DataGridViewRow row in grid.Rows)row.Selected=row.Tag is Product item&&selectedIds.Contains(item.Id);}
            if(firstRow>=0&&firstRow<grid.Rows.Count)grid.FirstDisplayedScrollingRowIndex=firstRow;
        }
        void One(Action<Product> action){var p=Selected().FirstOrDefault();if(p!=null)action(p);else MessageBox.Show(this,"Bir ürün seçin.");}
        void Delete(Product p){if(MessageBox.Show(this,$"'{p.Name}' silinsin mi? İşlem geçmişi korunur.","Ürün sil",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)Attempt(()=>{inventory.Delete(p.Id);Refresh();});}
        toolbar.Controls.Add(search);toolbar.Controls.Add(filter);toolbar.Controls.Add(Button("+ Ürün ekle",()=>EditProduct(null)));toolbar.Controls.Add(Button("Düzenle",()=>One(EditProduct),false));toolbar.Controls.Add(Button("Etiket bas",()=>OpenPrint(Selected()),false));
        var columns=new AppContextMenu();foreach(DataGridViewColumn column in grid.Columns){var item=new ToolStripMenuItem(column.Name){Checked=true,CheckOnClick=true};item.CheckedChanged+=(_,_)=>column.Visible=item.Checked;columns.Items.Add(item);}var showColumns=Button("Kolonlar",()=>columns.Show(Cursor.Position),false);toolbar.Controls.Add(showColumns);
        var actions=new AppToolbar{Dock=DockStyle.Bottom};actions.Controls.Add(Button("‹ Önceki",()=>{currentPage--;Refresh();},false));actions.Controls.Add(pageLabel);actions.Controls.Add(Button("Sonraki ›",()=>{currentPage++;Refresh();},false));actions.Controls.Add(Button("Menü durumu",()=>One(p=>Attempt(()=>{var c=p.Copy();c.OnMenu=!c.OnMenu;inventory.SaveProduct(c);Refresh();})),false));actions.Controls.Add(Button("Aktif / pasif",()=>One(p=>Attempt(()=>{var c=p.Copy();c.Active=!c.Active;inventory.SaveProduct(c);Refresh();})),false));
        var menu=new AppContextMenu();menu.Items.Add("Düzenle",null,(_,_)=>One(EditProduct));menu.Items.Add("Stok Girişi",null,(_,_)=>One(p=>StockDialog(p,"Stok girişi")));menu.Items.Add("Stok Çıkışı",null,(_,_)=>One(p=>StockDialog(p,"Stok çıkışı")));menu.Items.Add("Barkod Yazdır",null,(_,_)=>OpenPrint(Selected()));menu.Items.Add("Etiket Tasarla",null,(_,_)=>One(p=>{using var f=new LabelDesignerForm(product:p);f.ShowDialog(this);}));menu.Items.Add("Pasif Yap",null,(_,_)=>One(p=>Attempt(()=>{var c=p.Copy();c.Active=false;inventory.SaveProduct(c);Refresh();})));menu.Items.Add("Sil",null,(_,_)=>One(Delete));grid.ContextMenuStrip=menu;
        grid.CellMouseDown+=(_,e)=>{if(e.Button==MouseButtons.Right&&e.RowIndex>=0&&!grid.Rows[e.RowIndex].Selected){grid.ClearSelection();grid.Rows[e.RowIndex].Selected=true;grid.CurrentCell=grid.Rows[e.RowIndex].Cells[Math.Max(0,e.ColumnIndex)];}};
        grid.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0)One(EditProduct);};grid.ColumnHeaderMouseClick+=(_,e)=>{sort=grid.Columns[e.ColumnIndex].Name;descending=!descending;Refresh();};search.TextChanged+=(_,_)=>{currentPage=0;Refresh();};filter.SelectedIndexChanged+=(_,_)=>{currentPage=0;Refresh();};
        grid.Disposed+=(_,_)=>{foreach(var image in images)image.Dispose();menu.Dispose();columns.Dispose();};content.Controls.Add(grid);content.Controls.Add(toolbar);content.Controls.Add(actions);refreshPage=Refresh;Refresh();
    }
}

