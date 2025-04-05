using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App_Automatize_Backery.Models;

public partial class RawMaterialsWarehousesProduct
{
    public int RawMaterialWarehouseId { get; set; }

    public int? RawMaterialId { get; set; }

    public int WarehouseId { get; set; }

    public int? ProductId { get; set; }

    public double RawMaterialCount { get; set; }

    public int? MeasurementUnitId { get; set; }

    public DateTime DateSupplyOrProduction { get; set; }

    public virtual MeasurementUnit? MeasurementUnit { get; set; }

    public virtual Product? Product { get; set; }

    public virtual RawMaterial? RawMaterial { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;

    [NotMapped]

    public DateTime ExpirationDate => DateSupplyOrProduction.AddDays(RawMaterial.ShelfLifeDays);
}
