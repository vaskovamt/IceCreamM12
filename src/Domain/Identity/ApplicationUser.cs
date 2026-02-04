using Microsoft.AspNetCore.Identity;

namespace IceCreamM12.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
