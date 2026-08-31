using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GlowvaERP.Data;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Adds direct product-code entry to pending Sales Invoice rows without duplicating
/// the SalesForm invoice/business logic. The existing SalesForm keeps ownership of
/// the invoice items and refresh/save workflow; this helper only bridges the grid UI
/// to that existing state.
/// </summary>
public static class SalesDirectCodeEntry
{
    private static readonly HashSet<SalesForm> InstalledForms = new();
    private static readonly FieldInfo? ItemsField = typeof(SalesForm).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? QuantityField = typeof(SalesForm).GetField("_quantity", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo? RefreshGridMethod = typeof(SalesForm).GetMethod("RefreshGrid", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void InstallGlobal()
    {
        Application.Idle += (_, _) => ScanOpenSalesForms();
        ScanOpenSalesForms();
    }

    private static void ScanOpenSalesForms()
    {
        foreach (var form in Application.OpenForms.OfType<SalesForm>().ToArray())
        {
            if (InstalledForms.Add(form))
                Install(form);
        }
    }

    private static void Install(SalesForm form)
    {
        var grid = FindControl<DataGridView>(form);
        if (grid is null)
            return;

        grid.CellBeginEdit += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 0)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = !IsPendingRow(form, e.RowIndex);
        };

        grid.RowsAdded += (_, _) =>
        {
            MakePendingRowEditable(form, grid);
            if (GetItems(form)?.Cast<object>().Any(IsPendingDraft) == true)
            {
                form.BeginInvoke((Action)(() =>
                {
                    var index = FindPendingIndex(form);
                    if (index >= 0)
                        FocusPendingRow(grid, index);
                }));
            }
        };

        grid.EditingControlShowing += (_, e) =>
        {
            if (e.Control is TextBox textBox)
            {
                textBox.KeyDown -= CodeEditorKeyDown;
                if (grid.CurrentCell?.ColumnIndex == 0 && IsPendingRow(form, grid.CurrentRow?.Index ?? -1))
                    textBox.KeyDown += CodeEditorKeyDown;
            }
        };

        grid.CurrentCellChanged += (_, _) => MakePendingRowEditable(form, grid);
        grid.DataBindingComplete += (_, _) => MakePendingRowEditable(form, grid);
        form.FormClosed += (_, _) => InstalledForms.Remove(form);

        MakePendingRowEditable(form, grid);
    }

    private static void CodeEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || sender is not TextBox editor)
            return;

        var form = FindParent<SalesForm>(editor);
        if (form is null)
            return;

        var grid = FindControl<DataGridView>(form);
        if (grid?.CurrentCell?.ColumnIndex != 0 || !IsPendingRow(form, grid.CurrentRow?.Index ?? -1))
            return;

        var rowIndex = grid.CurrentCell.RowIndex;
        var code = editor.Text.Trim();
        e.SuppressKeyPress = true;
        e.Handled = true;

        if (string.IsNullOrWhiteSpace(code))
            return;

        ResolveCode(form, grid, rowIndex, code);
    }

    private static void ResolveCode(SalesForm form, DataGridView grid, int rowIndex, string code)
    {
        var items = GetItems(form);
        if (items is null || rowIndex < 0 || rowIndex >= items.Count || !IsPendingDraft(items[rowIndex]))
            return;

        try
        {
            ProductInfo? product;
            using (var connection = Database.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, name, code, sell_price, buy_price FROM products WHERE is_active = 1 AND code = $code LIMIT 1;";
                command.Parameters.AddWithValue("$code", code);
                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    ShowWarning(form, $"كود الصنف «{code}» غير صحيح أو غير موجود.", "كود صنف غير صالح");
                    KeepEditing(grid, rowIndex, code);
                    return;
                }

                product = new ProductInfo(
                    reader.GetInt64(0),
                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                    reader.IsDBNull(2) ? code : reader.GetString(2),
                    reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3)),
                    reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4)));
            }

            if (product is null)
                return;

            var inventory = new InventoryRepository();
            var stock = inventory.GetCurrentStock(product.Id);
            var pending = items[rowIndex];
            var pendingQuantity = GetDecimalProperty(pending, "Quantity");
            if (pendingQuantity <= 0m)
                pendingQuantity = GetQuantityValue(form);
            if (pendingQuantity <= 0m)
                pendingQuantity = 1m;

            if (stock <= 0m)
            {
                ShowWarning(form, $"الصنف «{product.Name}» موجود ولكن لا يوجد له رصيد متاح في المخزون.", "الصنف غير متاح");
                KeepEditing(grid, rowIndex, code);
                return;
            }

            var existingIndex = FindExistingProduct(items, product.Id, rowIndex);
            var existingQuantity = existingIndex >= 0 ? GetDecimalProperty(items[existingIndex], "Quantity") : 0m;
            var requestedTotal = existingQuantity + pendingQuantity;

            if (requestedTotal > stock)
            {
                ShowWarning(form, $"الرصيد المتاح للصنف «{product.Name}» هو {stock:N2} فقط، بينما الكمية المطلوبة إجمالياً {requestedTotal:N2}.", "الرصيد غير كافٍ");
                KeepEditing(grid, rowIndex, code);
                return;
            }

            if (existingIndex >= 0)
            {
                SetDecimalProperty(items[existingIndex], "Quantity", requestedTotal);
                items.RemoveAt(rowIndex);
            }
            else
            {
                SetProperty(items[rowIndex], "ProductId", product.Id);
                SetProperty(items[rowIndex], "ProductName", product.Name);
                SetDecimalProperty(items[rowIndex], "UnitPrice", product.SellPrice);
                SetDecimalProperty(items[rowIndex], "CostPrice", product.BuyPrice);
                SetDecimalProperty(items[rowIndex], "Quantity", pendingQuantity);
            }

            RefreshGrid(form);

            var target = existingIndex >= 0 ? Math.Min(existingIndex, items.Count - 1) : Math.Min(rowIndex, items.Count - 1);
            if (target >= 0 && target < grid.Rows.Count)
            {
                grid.CurrentCell = grid.Rows[target].Cells[Math.Min(1, grid.Columns.Count - 1)];
                grid.Rows[target].Selected = true;
            }
        }
        catch (Exception ex)
        {
            ShowWarning(form, $"تعذر التحقق من كود الصنف:\n{ex.Message}", "خطأ");
            KeepEditing(grid, rowIndex, code);
        }
    }

    private static void RefreshGrid(SalesForm form)
    {
        RefreshGridMethod?.Invoke(form, null);
    }

    private static void MakePendingRowEditable(SalesForm form, DataGridView grid)
    {
        var items = GetItems(form);
        if (items is null)
            return;

        for (var i = 0; i < grid.Rows.Count; i++)
        {
            var pending = i < items.Count && IsPendingDraft(items[i]);
            grid.Rows[i].ReadOnly = !pending;
            if (grid.Columns.Count > 0)
                grid.Rows[i].Cells[0].ReadOnly = !pending;
        }
    }

    private static void KeepEditing(DataGridView grid, int rowIndex, string code)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count || grid.Columns.Count == 0)
            return;

        grid.CancelEdit();
        grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
        grid.Rows[rowIndex].Cells[0].ReadOnly = false;
        grid.Rows[rowIndex].Cells[0].Value = code;
        grid.BeginEdit(true);
        if (grid.EditingControl is TextBox textBox)
            textBox.SelectAll();
    }

    private static void FocusPendingRow(DataGridView grid, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count || grid.Columns.Count == 0)
            return;

        grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
        grid.Rows[rowIndex].Selected = true;
        grid.Rows[rowIndex].Cells[0].ReadOnly = false;
        grid.BeginEdit(true);
    }

    private static int FindPendingIndex(SalesForm form)
    {
        var items = GetItems(form);
        if (items is null) return -1;
        for (var i = 0; i < items.Count; i++)
            if (IsPendingDraft(items[i])) return i;
        return -1;
    }

    private static bool IsPendingRow(SalesForm form, int rowIndex)
    {
        var items = GetItems(form);
        return items is not null && rowIndex >= 0 && rowIndex < items.Count && IsPendingDraft(items[rowIndex]);
    }

    private static bool IsPendingDraft(object value)
        => GetLongProperty(value, "ProductId") == 0;

    private static int FindExistingProduct(IList items, long productId, int excludedIndex)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i == excludedIndex) continue;
            if (GetLongProperty(items[i], "ProductId") == productId) return i;
        }
        return -1;
    }

    private static IList? GetItems(SalesForm form)
        => ItemsField?.GetValue(form) as IList;

    private static decimal GetQuantityValue(SalesForm form)
        => QuantityField?.GetValue(form) is NumericUpDown numeric ? numeric.Value : 1m;

    private static long GetLongProperty(object target, string name)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        var value = property?.GetValue(target);
        return value is null ? 0L : Convert.ToInt64(value);
    }

    private static decimal GetDecimalProperty(object target, string name)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        var value = property?.GetValue(target);
        return value is null ? 0m : Convert.ToDecimal(value);
    }

    private static void SetDecimalProperty(object target, string name, decimal value)
        => SetProperty(target, name, value);

    private static void SetProperty(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        property?.SetValue(target, Convert.ChangeType(value, property.PropertyType));
    }

    private static void ShowWarning(Form form, string message, string title)
        => MessageBox.Show(form, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static T? FindControl<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) return match;
            var nested = FindControl<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static T? FindParent<T>(Control control) where T : Control
    {
        Control? current = control;
        while (current is not null)
        {
            if (current is T match) return match;
            current = current.Parent;
        }
        return null;
    }

    private sealed record ProductInfo(long Id, string Name, string Code, decimal SellPrice, decimal BuyPrice);
}
