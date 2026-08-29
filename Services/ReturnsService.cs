using GlowvaERP.Data;
using Microsoft.Data.Sqlite;

namespace GlowvaERP.Services;

public sealed record ReturnLine(long OriginalItemId, long ProductId, string ProductName, decimal OriginalQuantity, decimal AlreadyReturned, decimal UnitPrice, decimal CostPrice)
{
    public decimal AvailableQuantity => Math.Max(0m, OriginalQuantity - AlreadyReturned);
}

public sealed record ReturnDraftLine(long OriginalItemId, decimal Quantity);

public sealed class ReturnsService
{
    public IReadOnlyList<ReturnLine> GetSalesLines(long orderId)
    {
        using var c = Database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT oi.id, oi.product_id, p.name, oi.quantity, oi.unit_price, oi.cost_price,
                   COALESCE((SELECT SUM(x.quantity) FROM sales_return_items x WHERE x.order_item_id=oi.id),0)
            FROM order_items oi JOIN products p ON p.id=oi.product_id
            WHERE oi.order_id=$id ORDER BY oi.id;
            """;
        cmd.Parameters.AddWithValue("$id", orderId);
        return ReadLines(cmd, hasCost: true);
    }

    public IReadOnlyList<ReturnLine> GetPurchaseLines(long purchaseId)
    {
        using var c = Database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT pi.id, pi.product_id, p.name, pi.quantity, pi.unit_price, pi.unit_price,
                   COALESCE((SELECT SUM(x.quantity) FROM purchase_return_items x WHERE x.purchase_item_id=pi.id),0)
            FROM purchase_items pi JOIN products p ON p.id=pi.product_id
            WHERE pi.purchase_id=$id ORDER BY pi.id;
            """;
        cmd.Parameters.AddWithValue("$id", purchaseId);
        return ReadLines(cmd, hasCost: true);
    }

    public long CreateSalesReturn(long orderId, IReadOnlyCollection<ReturnDraftLine> drafts, string? reason)
    {
        using var c = Database.OpenConnection();
        using var tx = c.BeginTransaction();
        try
        {
            var customerId = GetNullableId(c, tx, "SELECT customer_id FROM orders WHERE id=$id", orderId);
            var invoice = GetInvoiceNumber(c, tx, "orders", orderId);
            var lines = ValidateDrafts(c, tx, true, orderId, drafts);
            var total = lines.Sum(x => x.Quantity * x.Price);
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var number = NextNumber(c, tx, "sales_returns", "return_number");
            var returnId = InsertReturn(c, tx, "sales_returns", number, date, orderId, customerId, total, reason);
            foreach (var line in lines)
            {
                Exec(c, tx, "INSERT INTO sales_return_items (sales_return_id,order_item_id,product_id,quantity,unit_price,cost_price) VALUES ($rid,$item,$pid,$qty,$price,$cost);",
                    ("$rid", returnId), ("$item", line.ItemId), ("$pid", line.ProductId), ("$qty", line.Quantity), ("$price", line.Price), ("$cost", line.Cost));
                Exec(c, tx, "INSERT INTO inventory_ledger (product_id,movement_date,movement_type,reference_type,reference_id,quantity_in,quantity_out,unit_cost,notes) VALUES ($pid,$date,'مرتجع بيع','sales_return',$rid,$qty,0,$cost,$notes);",
                    ("$pid", line.ProductId), ("$date", date), ("$rid", returnId), ("$qty", line.Quantity), ("$cost", line.Cost), ("$notes", $"مرتجع مبيعات رقم {number} من الفاتورة {invoice}"));
            }
            var payment = GetString(c, tx, "SELECT payment_status FROM orders WHERE id=$id", orderId);
            if (PaymentMethods.IsImmediate(payment) && total > 0)
                Exec(c, tx, "INSERT INTO cash_transactions (transaction_date,transaction_type,reference_type,reference_id,amount_in,amount_out,notes) VALUES ($date,'مرتجع مبيعات','sales_return',$rid,0,$amount,$notes);", ("$date",date),("$rid",returnId),("$amount",total),("$notes",$"رد قيمة مرتجع مبيعات رقم {number}"));
            else if (payment == PaymentMethods.Credit && customerId.HasValue && total > 0)
                Exec(c, tx, "INSERT INTO account_transactions (transaction_date,account_type,party_id,reference_type,reference_id,debit,credit,notes) VALUES ($date,'customer',$party,'sales_return',$rid,0,$amount,$notes);", ("$date",date),("$party",customerId.Value),("$rid",returnId),("$amount",total),("$notes",$"تخفيض مديونية العميل بسبب مرتجع رقم {number}"));
            tx.Commit();
            return returnId;
        }
        catch { tx.Rollback(); throw; }
    }

    public long CreatePurchaseReturn(long purchaseId, IReadOnlyCollection<ReturnDraftLine> drafts, string? reason)
    {
        using var c = Database.OpenConnection();
        using var tx = c.BeginTransaction();
        try
        {
            var supplierId = GetNullableId(c, tx, "SELECT supplier_id FROM purchases WHERE id=$id", purchaseId);
            var invoice = GetInvoiceNumber(c, tx, "purchases", purchaseId);
            var lines = ValidateDrafts(c, tx, false, purchaseId, drafts);
            var total = lines.Sum(x => x.Quantity * x.Price);
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var number = NextNumber(c, tx, "purchase_returns", "return_number");
            var returnId = InsertReturn(c, tx, "purchase_returns", number, date, purchaseId, supplierId, total, reason);
            foreach (var line in lines)
            {
                Exec(c, tx, "INSERT INTO purchase_return_items (purchase_return_id,purchase_item_id,product_id,quantity,unit_price) VALUES ($rid,$item,$pid,$qty,$price);",
                    ("$rid", returnId), ("$item", line.ItemId), ("$pid", line.ProductId), ("$qty", line.Quantity), ("$price", line.Price));
                Exec(c, tx, "INSERT INTO inventory_ledger (product_id,movement_date,movement_type,reference_type,reference_id,quantity_in,quantity_out,unit_cost,notes) VALUES ($pid,$date,'مرتجع شراء','purchase_return',$rid,0,$qty,$price,$notes);",
                    ("$pid", line.ProductId), ("$date", date), ("$rid", returnId), ("$qty", line.Quantity), ("$price", line.Price), ("$notes", $"مرتجع مشتريات رقم {number} من الفاتورة {invoice}"));
            }
            var payment = GetString(c, tx, "SELECT payment_status FROM purchases WHERE id=$id", purchaseId);
            if (PaymentMethods.IsImmediate(payment) && total > 0)
                Exec(c, tx, "INSERT INTO cash_transactions (transaction_date,transaction_type,reference_type,reference_id,amount_in,amount_out,notes) VALUES ($date,'مرتجع مشتريات','purchase_return',$rid,$amount,0,$notes);", ("$date",date),("$rid",returnId),("$amount",total),("$notes",$"رد قيمة مرتجع مشتريات رقم {number}"));
            else if (payment == PaymentMethods.Credit && supplierId.HasValue && total > 0)
                Exec(c, tx, "INSERT INTO account_transactions (transaction_date,account_type,party_id,reference_type,reference_id,debit,credit,notes) VALUES ($date,'supplier',$party,'purchase_return',$rid,$amount,0,$notes);", ("$date",date),("$party",supplierId.Value),("$rid",returnId),("$amount",total),("$notes",$"تخفيض مديونية المورد بسبب مرتجع رقم {number}"));
            tx.Commit();
            return returnId;
        }
        catch { tx.Rollback(); throw; }
    }

    private sealed record Clean(long ItemId,long ProductId,decimal Quantity,decimal Price,decimal Cost);

    private static List<Clean> ValidateDrafts(SqliteConnection c, SqliteTransaction tx, bool sales, long invoiceId, IReadOnlyCollection<ReturnDraftLine> drafts)
    {
        var result = new List<Clean>();
        foreach (var d in drafts.Where(x => x.Quantity > 0))
        {
            using var cmd = c.CreateCommand(); cmd.Transaction=tx;
            cmd.CommandText = sales
                ? "SELECT oi.product_id,oi.quantity,oi.unit_price,oi.cost_price,COALESCE((SELECT SUM(x.quantity) FROM sales_return_items x WHERE x.order_item_id=oi.id),0) FROM order_items oi WHERE oi.id=$item AND oi.order_id=$invoice"
                : "SELECT pi.product_id,pi.quantity,pi.unit_price,pi.unit_price,COALESCE((SELECT SUM(x.quantity) FROM purchase_return_items x WHERE x.purchase_item_id=pi.id),0) FROM purchase_items pi WHERE pi.id=$item AND pi.purchase_id=$invoice";
            cmd.Parameters.AddWithValue("$item",d.OriginalItemId); cmd.Parameters.AddWithValue("$invoice",invoiceId);
            using var r=cmd.ExecuteReader();
            if(!r.Read()) throw new InvalidOperationException("أحد أصناف المرتجع غير موجود.");
            var original=Convert.ToDecimal(r.GetValue(1)); var returned=Convert.ToDecimal(r.GetValue(4));
            if(d.Quantity>original-returned+0.000001m) throw new InvalidOperationException("كمية المرتجع أكبر من الكمية المتاحة.");
            result.Add(new Clean(d.OriginalItemId,r.GetInt64(0),d.Quantity,Convert.ToDecimal(r.GetValue(2)),Convert.ToDecimal(r.GetValue(3))));
        }
        if(result.Count==0) throw new InvalidOperationException("اختر كمية واحدة على الأقل للمرتجع.");
        return result;
    }

    private static List<ReturnLine> ReadLines(SqliteCommand cmd,bool hasCost)
    {
        var list=new List<ReturnLine>(); using var r=cmd.ExecuteReader();
        while(r.Read()) list.Add(new ReturnLine(r.GetInt64(0),r.GetInt64(1),r.IsDBNull(2)?"":r.GetString(2),Convert.ToDecimal(r.GetValue(3)),Convert.ToDecimal(r.GetValue(6)),Convert.ToDecimal(r.GetValue(4)),hasCost?Convert.ToDecimal(r.GetValue(5)):0m));
        return list;
    }
    private static long NextNumber(SqliteConnection c,SqliteTransaction tx,string table,string col){using var x=c.CreateCommand();x.Transaction=tx;x.CommandText=$"SELECT COALESCE(MAX({col}),0)+1 FROM {table};";return Convert.ToInt64(x.ExecuteScalar());}
    private static long InsertReturn(SqliteConnection c,SqliteTransaction tx,string table,long number,string date,long invoice,long? party,decimal total,string? reason){using var x=c.CreateCommand();x.Transaction=tx;x.CommandText=table=="sales_returns"?"INSERT INTO sales_returns(return_number,return_date,order_id,customer_id,total,reason) VALUES($n,$d,$i,$p,$t,$r);SELECT last_insert_rowid();":"INSERT INTO purchase_returns(return_number,return_date,purchase_id,supplier_id,total,reason) VALUES($n,$d,$i,$p,$t,$r);SELECT last_insert_rowid();";x.Parameters.AddWithValue("$n",number);x.Parameters.AddWithValue("$d",date);x.Parameters.AddWithValue("$i",invoice);x.Parameters.AddWithValue("$p",party??(object)DBNull.Value);x.Parameters.AddWithValue("$t",total);x.Parameters.AddWithValue("$r",string.IsNullOrWhiteSpace(reason)?DBNull.Value:reason.Trim());return Convert.ToInt64(x.ExecuteScalar());}
    private static long? GetNullableId(SqliteConnection c,SqliteTransaction tx,string sql,long id){using var x=c.CreateCommand();x.Transaction=tx;x.CommandText=sql;x.Parameters.AddWithValue("$id",id);var v=x.ExecuteScalar();return v==null||v==DBNull.Value?null:Convert.ToInt64(v);}
    private static string GetString(SqliteConnection c,SqliteTransaction tx,string sql,long id){using var x=c.CreateCommand();x.Transaction=tx;x.CommandText=sql;x.Parameters.AddWithValue("$id",id);return Convert.ToString(x.ExecuteScalar())??"";}
    private static long GetInvoiceNumber(SqliteConnection c,SqliteTransaction tx,string table,long id){using var x=c.CreateCommand();x.Transaction=tx;x.CommandText=$"SELECT invoice_number FROM {table} WHERE id=$id";x.Parameters.AddWithValue("$id",id);return Convert.ToInt64(x.ExecuteScalar()??0);}
    private static void Exec(SqliteConnection c,SqliteTransaction tx,string sql,params(string n,object? v)[] p){using var x=c.CreateCommand();x.Transaction=tx;x.CommandText=sql;foreach(var q in p)x.Parameters.AddWithValue(q.n,q.v??DBNull.Value);x.ExecuteNonQuery();}
}
