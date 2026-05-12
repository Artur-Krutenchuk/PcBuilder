using Microsoft.AspNetCore.Identity;

namespace PcBuilder.Web.Models.Auth;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime? RegisteredAtUtc { get; set; }
}

