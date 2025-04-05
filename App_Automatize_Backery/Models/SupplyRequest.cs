using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class SupplyRequest
{
    public int SupplyRequestId { get; set; }

    public int UserId { get; set; }

    public DateTime SupplyRequestDate { get; set; }

    public decimal? TotalSalary { get; set; }

    public int StatusId { get; set; }

    public virtual ICollection<Expence> Expences { get; set; } = new List<Expence>();

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<SupplyRequestsRawMaterial> SupplyRequestsRawMaterials { get; set; } = new List<SupplyRequestsRawMaterial>();

    public virtual User User { get; set; } = null!;
}
