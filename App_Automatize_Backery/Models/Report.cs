using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public int UserId { get; set; }

    public string ReportType { get; set; } = null!;

    public DateTime ReportDate { get; set; }

    public virtual ICollection<ExpencesReportsParish> ExpencesReportsParishes { get; set; } = new List<ExpencesReportsParish>();

    public virtual User User { get; set; } = null!;
}
