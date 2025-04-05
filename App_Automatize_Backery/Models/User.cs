using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App_Automatize_Backery.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string? UserSurname { get; set; }

    public string UserLogin { get; set; } = null!;

    public byte[] UserHashPassword { get; set; } = null!;

    public int UserRoleId { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<SupplyRequest> SupplyRequests { get; set; } = new List<SupplyRequest>();

    public virtual UserRole UserRole { get; set; } = null!;

    [NotMapped]
    public string UserRoleName { get; set; } = null!;

    [NotMapped]
    public string FullName => UserName + " " + UserSurname;
}
