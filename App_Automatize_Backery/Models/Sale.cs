using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Sale
{
    public int SaleId { get; set; }

    public int UserId { get; set; }

    public string? TypeSale { get; set; }

    public DateTime DateTimeSale { get; set; }

    public decimal? CoastSale { get; set; }

    public string? SaleStatus { get; set; }

    public virtual ICollection<Parish> Parishes { get; set; } = new List<Parish>();

    public virtual ICollection<SaleProduct> SaleProducts { get; set; } = new List<SaleProduct>();

    public virtual User User { get; set; } = null!;
}
