using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EYEngage.Infrastructure;

public class EYEngageDbContext : IdentityDbContext<User, Role, Guid>
{
    public EYEngageDbContext(DbContextOptions<EYEngageDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
