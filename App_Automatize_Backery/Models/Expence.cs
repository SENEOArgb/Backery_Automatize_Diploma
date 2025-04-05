using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Expence
{
    public int ExpenceId { get; set; }

    public int SupplyRequestId { get; set; }

    public decimal ExpenceCoast { get; set; }

    public DateTime ExpenceDate { get; set; }

    public virtual ICollection<ExpencesReportsParish> ExpencesReportsParishes { get; set; } = new List<ExpencesReportsParish>();

    public virtual SupplyRequest SupplyRequest { get; set; } = null!;
}
