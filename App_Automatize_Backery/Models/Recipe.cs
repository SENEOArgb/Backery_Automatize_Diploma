using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Recipe
{
    public int RecipeId { get; set; }

    public int ProductId { get; set; }

    public string? RecipeDescription { get; set; }

    public string? StatusRecipe { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<RawMaterialMeasurementUnitRecipe> RawMaterialMeasurementUnitRecipes { get; set; } = new List<RawMaterialMeasurementUnitRecipe>();
}
