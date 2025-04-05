using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public string WarehouseType { get; set; } = null!;

    public virtual ICollection<RawMaterialsWarehousesProduct> RawMaterialsWarehousesProducts { get; set; } = new List<RawMaterialsWarehousesProduct>();

    public virtual ICollection<SupplyRequestsRawMaterial> SupplyRequestsRawMaterials { get; set; } = new List<SupplyRequestsRawMaterial>();
}
