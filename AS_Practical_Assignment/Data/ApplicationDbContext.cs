using AS_Practical_Assignment.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AS_Practical_Assignment.Data
{
    public class ApplicationDbContext : IdentityDbContext<Member>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
     : base(options)
  {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
   {
            base.OnModelCreating(builder);

 // Ensure email is unique
     builder.Entity<Member>()
      .HasIndex(m => m.Email)
   .IsUnique();

            // Configure AuditLog
      builder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

  builder.Entity<AuditLog>()
          .HasIndex(a => a.Action);
        }
    }
}
