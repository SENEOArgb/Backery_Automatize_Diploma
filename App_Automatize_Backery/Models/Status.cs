using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Status
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<SupplyRequest> SupplyRequests { get; set; } = new List<SupplyRequest>();
}
