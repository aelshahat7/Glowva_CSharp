using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Forms;

public sealed class ProductsForm : Form
{
    private readonly ProductRepository _repository = new();
    private readonly TextBox _searchBox = new();
    private readonly DataGridView _grid = new();

    public ProductsForm()
    {
        Text = "الأصناف";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 650);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);
        BuildUi();
        LoadProducts();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(12) };
        var add = CreateButton("＋ صنف جديد", Color.FromArgb(39,174,96));
        add.Dock = DockStyle.Left; add.Width = 145; add.Click += (_,_) => AddProduct();
        _searchBox.Dock = DockStyle.Right; _searchBox.Width = 360; _searchBox.Font = new Font("Segoe UI",11);
        _searchBox.PlaceholderText = "ابحث بالاسم أو الكود أو الباركود..."; _searchBox.TextAlign = HorizontalAlignment.Right;
        _searchBox.KeyDown += (_,e) => { if(e.KeyCode == Keys.Enter){LoadProducts(); e.SuppressKeyPress=true;} };
        var search = CreateButton("بحث", Color.FromArgb(52,152,219));
        search.Dock = DockStyle.Right; search.Width = 80; search.Click += (_,_) => LoadProducts();
        header.Controls.Add(add); header.Controls.Add(search); header.Controls.Add(_searchBox);

        _grid.Dock = DockStyle.Fill; _grid.BackgroundColor = Color.White; _grid.BorderStyle = BorderStyle.None;
        _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false; _grid.RightToLeft = RightToLeft.Yes; _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 42; _grid.ScrollBars = ScrollBars.Both;

        AddIdColumn();
        AddColumn("الكود","Code",100); AddColumn("الباركود","Barcode",150); AddColumn("الصنف","Name",420,true);
        AddColumn("التصنيف","Category",150); AddColumn("سعر البيع","SellPrice",120,false,"N2");
        AddColumn("سعر الشراء","BuyPrice",120,false,"N2"); AddColumn("الحد الأدنى","LowStockThreshold",110,false,"N2");
        AddColumn("الحالة","IsActive",90);

        _grid.CellDoubleClick += (_,e) => { if(e.RowIndex>=0) OpenProductCardAndEdit(); };
        _grid.KeyDown += (_,e) =>
        {
            if(e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3){ OpenProductCardAndEdit(); e.SuppressKeyPress=true; }
            else if(e.KeyCode == Keys.Delete){ ToggleSelectedProduct(); e.SuppressKeyPress=true; }
        };
        _grid.MouseDown += (_,e) =>
        {
            if(e.Button != MouseButtons.Right) return;
            var hit = _grid.HitTest(e.X,e.Y);
            if(hit.RowIndex<0) return;
            _grid.ClearSelection(); _grid.Rows[hit.RowIndex].Selected=true;
            _grid.CurrentCell = _grid.Rows[hit.RowIndex].Cells[1];
            ShowProductContextMenu(e.X,e.Y);
        };
        Controls.Add(_grid); Controls.Add(header);
    }

    private void AddIdColumn() => _grid.Columns.Add(new DataGridViewTextBoxColumn
    {
        Name="ProductId", HeaderText="", DataPropertyName="Id", Visible=false, Width=0, ReadOnly=true
    });

    private void AddColumn(string header,string property,int width,bool fill=false,string? format=null)
    {
        var c = new DataGridViewTextBoxColumn { HeaderText=header, DataPropertyName=property, Width=width,
            AutoSizeMode=fill?DataGridViewAutoSizeColumnMode.Fill:DataGridViewAutoSizeColumnMode.None,
            SortMode=DataGridViewColumnSortMode.NotSortable };
        if(format is not null) c.DefaultCellStyle.Format=format;
        c.DefaultCellStyle.Alignment = property=="IsActive" ? DataGridViewContentAlignment.MiddleCenter : DataGridViewContentAlignment.MiddleRight;
        _grid.Columns.Add(c);
    }

    private void LoadProducts()
    {
        try
        {
            var rows = _repository.GetAll(_searchBox.Text);
            _grid.DataSource = rows.Select(p => new { p.Id,p.Code,p.Barcode,p.Name,p.Category,p.SellPrice,p.BuyPrice,p.LowStockThreshold,IsActive=p.IsActive?"نشط":"موقوف" }).ToList();
        }
        catch(Exception ex){ MessageBox.Show(this,$"تعذر تحميل الأصناف:\n{ex.Message}","خطأ",MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }

    private Product? GetSelectedProduct()
    {
        if(_grid.CurrentRow?.DataBoundItem is null) return null;
        if(!long.TryParse(Convert.ToString(_grid.CurrentRow.Cells[0].Value),out var id)) return null;
        return _repository.GetAll().FirstOrDefault(p=>p.Id==id);
    }

    private void AddProduct()
    {
        using var dialog = new ProductEditorForm(new Product{Code=_repository.GenerateNextCode()},true);
        if(dialog.ShowDialog(this)!=DialogResult.OK) return;
        try{ _repository.Add(dialog.Product); LoadProducts(); }
        catch(Exception ex){ MessageBox.Show(this,$"تعذر حفظ الصنف:\n{ex.Message}","خطأ",MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }

    private void OpenProductCardAndEdit()
    {
        var product=GetSelectedProduct(); if(product is null) return;
        using var card=new ProductCardDialog(product.Id); card.ShowDialog(this); LoadProducts();
    }

    private void ShowProductContextMenu(int x,int y)
    {
        var p=GetSelectedProduct(); if(p is null) return;
        using var menu=new ContextMenuStrip{RightToLeft=RightToLeft.Yes,ShowImageMargin=false,Font=new Font("Segoe UI",10)};
        void Add(string text,Action action){var item=new ToolStripMenuItem(text){RightToLeft=RightToLeft.Yes,TextAlign=ContentAlignment.MiddleRight}; item.Click+=(_,_)=>action(); menu.Items.Add(item);}
        Add("كارت الصنف",OpenProductCardAndEdit); Add("تعديل بيانات الصنف",OpenProductCardAndEdit); menu.Items.Add(new ToolStripSeparator());
        Add(p.IsActive?"إيقاف الصنف":"تفعيل الصنف",ToggleSelectedProduct); Add("نسخ اسم الصنف",()=>Clipboard.SetText(p.Name??string.Empty));
        menu.Show(_grid,new Point(Math.Min(x,Math.Max(0,_grid.ClientSize.Width-180)),Math.Min(y,Math.Max(0,_grid.ClientSize.Height-160))));
    }

    private void ToggleSelectedProduct()
    {
        var p=GetSelectedProduct(); if(p is null) return; var action=p.IsActive?"إيقاف":"تفعيل";
        if(MessageBox.Show(this,$"هل تريد {action} الصنف «{p.Name}»؟",action,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
        _repository.SetActive(p.Id,!p.IsActive); LoadProducts();
    }

    private static Button CreateButton(string text,Color color)=>new(){Text=text,BackColor=color,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",10,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter};
}

internal sealed class ProductEditorForm : Form
{
    private readonly TextBox _code=new(),_name=new(),_barcode=new(),_category=new();
    private readonly NumericUpDown _sellPrice=CreateNumber(),_buyPrice=CreateNumber(),_openingStock=CreateNumber(),_lowStock=CreateNumber();
    private readonly CheckBox _active=new();
    public Product Product{get;}
    private readonly bool _isNew;

    public ProductEditorForm(Product product,bool isNew)
    {
        Product=new Product{Id=product.Id,Code=product.Code,Name=product.Name,Barcode=product.Barcode,Category=product.Category,SellPrice=product.SellPrice,BuyPrice=product.BuyPrice,OpeningStock=product.OpeningStock,LowStockThreshold=product.LowStockThreshold,IsActive=product.IsActive};
        _isNew=isNew; Text=isNew?"صنف جديد":"كارت الصنف - تعديل البيانات"; StartPosition=FormStartPosition.CenterParent;
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; ClientSize=new Size(620,520); RightToLeft=RightToLeft.Yes; RightToLeftLayout=true;
        BuildUi(); LoadProduct();
    }
    private void BuildUi()
    {
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=10,Padding=new Padding(18),RightToLeft=RightToLeft.Yes};
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,28)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,72));
        AddField(table,0,"الكود",_code); AddField(table,1,"اسم الصنف",_name); AddField(table,2,"الباركود",_barcode); AddField(table,3,"التصنيف",_category);
        AddField(table,4,"سعر البيع",_sellPrice); AddField(table,5,"سعر الشراء",_buyPrice); AddField(table,6,"الرصيد الافتتاحي",_openingStock); AddField(table,7,"حد إعادة الطلب",_lowStock);
        _active.Text="الصنف نشط"; _active.AutoSize=true; table.Controls.Add(_active,0,8); table.SetColumnSpan(_active,2);
        var save=new Button{Text=_isNew?"حفظ الصنف":"حفظ التعديلات",Dock=DockStyle.Fill,BackColor=Color.FromArgb(39,174,96),ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",10,FontStyle.Bold)};
        save.Click+=(_,_)=>Save(); table.Controls.Add(save,0,9); table.SetColumnSpan(save,2); Controls.Add(table); AcceptButton=save;
    }
    private static void AddField(TableLayoutPanel table,int row,string label,Control control)
    {
        table.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,Font=new Font("Segoe UI",10,FontStyle.Bold),Margin=new Padding(4)},0,row);
        control.Dock=DockStyle.Fill; control.Margin=new Padding(4); if(control is TextBox t)t.TextAlign=HorizontalAlignment.Right; table.Controls.Add(control,1,row);
    }
    private void LoadProduct(){_code.Text=Product.Code;_code.ReadOnly=!_isNew;_name.Text=Product.Name;_barcode.Text=Product.Barcode;_category.Text=Product.Category;_sellPrice.Value=Product.SellPrice;_buyPrice.Value=Product.BuyPrice;_openingStock.Value=Product.OpeningStock;_lowStock.Value=Product.LowStockThreshold;_active.Checked=Product.IsActive;}
    private void Save(){if(string.IsNullOrWhiteSpace(_name.Text)){MessageBox.Show(this,"اسم الصنف مطلوب.","تنبيه",MessageBoxButtons.OK,MessageBoxIcon.Warning);_name.Focus();return;} Product.Code=_code.Text.Trim();Product.Name=_name.Text.Trim();Product.Barcode=_barcode.Text.Trim();Product.Category=_category.Text.Trim();Product.SellPrice=_sellPrice.Value;Product.BuyPrice=_buyPrice.Value;Product.OpeningStock=_openingStock.Value;Product.LowStockThreshold=_lowStock.Value;Product.IsActive=_active.Checked;DialogResult=DialogResult.OK;Close();}
    private static NumericUpDown CreateNumber()=>new(){DecimalPlaces=2,Maximum=100000000,Minimum=0,Increment=1,ThousandsSeparator=true,TextAlign=HorizontalAlignment.Right};
}
