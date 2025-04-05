using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class MeasurementConversion
{
    public int MeasurementConversionId { get; set; }

    public int FromMeasureUnitId { get; set; }

    public int ToMeasureUnitId { get; set; }

    public double ConversionFactor { get; set; }

    public virtual MeasurementUnit FromMeasureUnit { get; set; } = null!;

    public virtual MeasurementUnit ToMeasureUnit { get; set; } = null!;
}
