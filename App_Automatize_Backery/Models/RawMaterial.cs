using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class RawMaterial
{
    public int RawMaterialId { get; set; }

    public int MeasurementUnitId { get; set; }

    public string RawMaterialName { get; set; } = null!;

    public double ShelfLifeDays { get; set; }

    public decimal RawMaterialCoast { get; set; }

    public string? StatusRawMaterial { get; set; }

    public virtual MeasurementUnit MeasurementUnit { get; set; } = null!;

    public virtual ICollection<RawMaterialMeasurementUnitRecipe> RawMaterialMeasurementUnitRecipes { get; set; } = new List<RawMaterialMeasurementUnitRecipe>();

    public virtual ICollection<RawMaterialsWarehousesProduct> RawMaterialsWarehousesProducts { get; set; } = new List<RawMaterialsWarehousesProduct>();

    public virtual ICollection<SupplyRequestsRawMaterial> SupplyRequestsRawMaterials { get; set; } = new List<SupplyRequestsRawMaterial>();
}
