using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace App_Automatize_Backery.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int? TypeProductId { get; set; }

    public decimal ProductCoast { get; set; }

    public virtual ICollection<RawMaterialsWarehousesProduct> RawMaterialsWarehousesProducts { get; set; } = new List<RawMaterialsWarehousesProduct>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<SaleProduct> SaleProducts { get; set; } = new List<SaleProduct>();

    public virtual TypesProduct? TypeProduct { get; set; }

    [NotMapped]
    public string TypeProductName => TypeProduct.TypeProductName;
}
