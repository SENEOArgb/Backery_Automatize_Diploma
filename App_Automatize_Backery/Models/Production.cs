using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Production
{
    public int ProductionId { get; set; }

    public DateTime DateTimeStart { get; set; }

    public DateTime DateTimeEnd { get; set; }

    public virtual ICollection<ProductionsRawMaterialsMeasurementUnitRecipe> ProductionsRawMaterialsMeasurementUnitRecipes { get; set; } = new List<ProductionsRawMaterialsMeasurementUnitRecipe>();
}
