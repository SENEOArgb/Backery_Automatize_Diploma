using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class ProductionsRawMaterialsMeasurementUnitRecipe
{
    public int ProductionRawMaterialMeasurementUnitRecipeId { get; set; }

    public int ProductionId { get; set; }

    public int RawMaterialMeasurementUnitRecipeId { get; set; }

    public int CountProduct { get; set; }

    public virtual Production Production { get; set; } = null!;

    public virtual RawMaterialMeasurementUnitRecipe RawMaterialMeasurementUnitRecipe { get; set; } = null!;
}
