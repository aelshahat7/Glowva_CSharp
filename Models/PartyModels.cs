namespace GlowvaERP.Models;

public sealed class Customer
{
    public long    Id             { get; set; }
    public string  Code           { get; set; } = "";
    public string  Name           { get; set; } = "";
    public string  Phone          { get; set; } = "";
    public string  Phone2         { get; set; } = "";
    public string  Address        { get; set; } = "";
    public string  Notes          { get; set; } = "";
    public decimal OpeningBalance { get; set; }
    public bool    IsActive       { get; set; } = true;
}

public sealed class Supplier
{
    public long    Id             { get; set; }
    public string  Code           { get; set; } = "";
    public string  Name           { get; set; } = "";
    public string  Phone          { get; set; } = "";
    public string  ContactInfo    { get; set; } = "";
    public string  Address        { get; set; } = "";
    public string  Notes          { get; set; } = "";
    public decimal OpeningBalance { get; set; }
    public bool    IsActive       { get; set; } = true;
}
