using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class SupplyRequestsRawMaterial
{
    public int SupplyRequestWarehouseRawMaterialId { get; set; }

    public int SupplyRequestId { get; set; }

    public int WarehouseId { get; set; }

    public int? RawMaterialId { get; set; }

    public int CountRawMaterial { get; set; }

    public decimal SupplyCoastToMaterial { get; set; }

    public virtual RawMaterial? RawMaterial { get; set; }

    public virtual SupplyRequest SupplyRequest { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
