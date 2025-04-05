using System;
using System.Collections.Generic;

namespace App_Automatize_Backery.Models;

public partial class ExpencesReportsParish
{
    public int ExpenceReportParisheId { get; set; }

    public int? ExpenceId { get; set; }

    public int? ReportId { get; set; }

    public int? ParisheId { get; set; }

    public virtual Expence? Expence { get; set; }

    public virtual Parish? Parishe { get; set; }

    public virtual Report? Report { get; set; }
}
