using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class RawMaterialMeasurementUnitRecipe
{
    public int RawMaterialMeasurementUnitRecipeId { get; set; }

    public int RawMaterialId { get; set; }

    public int MeasurementUnitId { get; set; }

    public int RecipeId { get; set; }

    public double CountRawMaterial { get; set; }

    public virtual MeasurementUnit MeasurementUnit { get; set; } = null!;

    public virtual ICollection<ProductionsRawMaterialsMeasurementUnitRecipe> ProductionsRawMaterialsMeasurementUnitRecipes { get; set; } = new List<ProductionsRawMaterialsMeasurementUnitRecipe>();

    public virtual RawMaterial RawMaterial { get; set; } = null!;

    public virtual Recipe Recipe { get; set; } = null!;
}
