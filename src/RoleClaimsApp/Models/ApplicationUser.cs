using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RoleClaimsApp.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;
}