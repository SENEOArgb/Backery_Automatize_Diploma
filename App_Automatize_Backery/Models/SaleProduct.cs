using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class SaleProduct
{
    public int SaleProductId { get; set; }

    public int SaleId { get; set; }

    public int ProductId { get; set; }

    public int CountProductSale { get; set; }

    public decimal? CoastToProduct { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
