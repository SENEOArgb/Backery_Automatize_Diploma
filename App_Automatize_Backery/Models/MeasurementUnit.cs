using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class MeasurementUnit
{
    public int MeasurementUnitId { get; set; }

    public string? MeasurementUnitName { get; set; }

    public virtual ICollection<MeasurementConversion> MeasurementConversionFromMeasureUnits { get; set; } = new List<MeasurementConversion>();

    public virtual ICollection<MeasurementConversion> MeasurementConversionToMeasureUnits { get; set; } = new List<MeasurementConversion>();

    public virtual ICollection<RawMaterialMeasurementUnitRecipe> RawMaterialMeasurementUnitRecipes { get; set; } = new List<RawMaterialMeasurementUnitRecipe>();

    public virtual ICollection<RawMaterial> RawMaterials { get; set; } = new List<RawMaterial>();

    public virtual ICollection<RawMaterialsWarehousesProduct> RawMaterialsWarehousesProducts { get; set; } = new List<RawMaterialsWarehousesProduct>();
}
