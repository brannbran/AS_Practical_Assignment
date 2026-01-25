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
      public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

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

        // Configure PasswordHistory
 builder.Entity<PasswordHistory>()
 .HasOne(ph => ph.Member)
        .WithMany(m => m.PasswordHistories)
   .HasForeignKey(ph => ph.MemberId)
     .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<PasswordHistory>()
            .HasIndex(ph => ph.MemberId);

     builder.Entity<PasswordHistory>()
  .HasIndex(ph => ph.CreatedDate);

            // Configure PasswordResetToken
     builder.Entity<PasswordResetToken>()
         .HasOne(prt => prt.Member)
            .WithMany(m => m.PasswordResetTokens)
 .HasForeignKey(prt => prt.MemberId)
  .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PasswordResetToken>()
          .HasIndex(prt => prt.Token)
          .IsUnique();

    builder.Entity<PasswordResetToken>()
  .HasIndex(prt => prt.MemberId);
  }
    }
}
