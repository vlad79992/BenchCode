using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BenchCode.AuthService.Entities;

public class User : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }
}