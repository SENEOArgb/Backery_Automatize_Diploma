using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Parish
{
    public int ParisheId { get; set; }

    public int SaleId { get; set; }

    public decimal ParisheSize { get; set; }

    public DateTime ParisheDateTime { get; set; }

    public virtual ICollection<ExpencesReportsParish> ExpencesReportsParishes { get; set; } = new List<ExpencesReportsParish>();

    public virtual Sale Sale { get; set; } = null!;
}
