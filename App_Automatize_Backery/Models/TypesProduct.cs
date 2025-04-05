using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class TypesProduct
{
    public int TypeProductId { get; set; }

    public string TypeProductName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
