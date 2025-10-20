using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using App.Domain.UserSecurity;

namespace App.Infrastructure.AppDatabase { }

public class AppDbContext : IdentityDbContext<User, Role, string,
                                                        IdentityUserClaim<string>,
                                                        UserRole, // <-- Use your custom UserRole here
                                                        IdentityUserLogin<string>,
                                                        RoleClaim, IdentityUserToken<string>>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        //
    }

    public DbSet<RoleClaim> RoleClaims { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<UserPasswordLog> UserPasswordLog { get; set; }
    public DbSet<UserLoginLog> UserLoginLog { get; set; }

    public DbSet<MeetingStatus> MeetingStatuses { get; set; }

    public DbSet<MeetingPriority> MeetingPriorities { get; set; }

    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
    public DbSet<MeetingAttachment> MeetingAttachments { get; set; }
    public DbSet<MeetingReminder> MeetingReminders { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    //public DbSet<MeetingNote> MeetingNotes { get; set; }

    public DbSet<MeetingFinishNote> MeetingFinishNotes { get; set; }

    public DbSet<DepartmentType> DepartmentTypes { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<MeetingLocation> MeetingLocations { get; set; }

    public DbSet<Audit> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Map RoleClaim to AspNetRoleClaims table
        modelBuilder.Entity<RoleClaim>().ToTable("RoleClaims");

        //Ensure other tables use standard Identity naming
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<UserRole>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.LastModifiedDate).HasDefaultValueSql("GETDATE()");
        });

        // Meeting Configuration
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.OrganizerId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(m => m.Organizer)
                .WithMany(u => u.OrganizedMeetings)
                .HasForeignKey(m => m.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MeetingParticipant Configuration
        modelBuilder.Entity<MeetingParticipant>(entity =>
        {
            entity.HasIndex(e => new { e.MeetingId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.InvitedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(mp => mp.Meeting)
                .WithMany(m => m.Participants)
                .HasForeignKey(mp => mp.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mp => mp.User)
                .WithMany(u => u.MeetingParticipants)
                .HasForeignKey(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MeetingAttachment Configuration
        modelBuilder.Entity<MeetingAttachment>(entity =>
        {
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(ma => ma.Meeting)
                .WithMany(m => m.Attachments)
                .HasForeignKey(ma => ma.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ma => ma.Uploader)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(ma => ma.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MeetingReminder Configuration
        modelBuilder.Entity<MeetingReminder>(entity =>
        {
            entity.HasIndex(e => new { e.ReminderTime, e.IsSent });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(mr => mr.Meeting)
                .WithMany(m => m.Reminders)
                .HasForeignKey(mr => mr.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mr => mr.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification Configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Meeting)
                .WithMany(m => m.Notifications)
                .HasForeignKey(n => n.MeetingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MeetingNote Configuration
        //modelBuilder.Entity<MeetingNote>(entity =>
        //{
        //    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        //    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

        //    entity.HasOne(mn => mn.Meeting)
        //        .WithMany(m => m.Notes)
        //        .HasForeignKey(mn => mn.MeetingId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    entity.HasOne(mn => mn.User)
        //        .WithMany(u => u.Notes)
        //        .HasForeignKey(mn => mn.UserId)
        //        .OnDelete(DeleteBehavior.Restrict);
        //});

        // MeetingFinishNote Configuration
        modelBuilder.Entity<MeetingFinishNote>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(mn => mn.Meeting)
                .WithMany(m => m.MeetingFinishNotes)
                .HasForeignKey(mn => mn.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mn => mn.User)
                .WithMany(u => u.MeetingFinishNotes)
                .HasForeignKey(mn => mn.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>().ToTable("UserRoles");
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
    }

    public virtual async Task<int> SaveChangesAsync(string UserId = "", string IpAddress = "")
    {
        OnBeforeSaveChanges(UserId);
        var result = await base.SaveChangesAsync();
        return result;
    }

    public virtual int SaveChanges(string userId = null)
    {
        OnBeforeSaveChanges(userId);
        var result = base.SaveChanges();
        return result;
    }

    public virtual async Task<int> CreateAsync(string userId = null)
    {
        OnBeforeSaveChanges(userId);
        var result = await base.SaveChangesAsync();
        return result;
    }

    private void OnBeforeSaveChanges(string UserId = "", string IpAddress = "")
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;
            var auditEntry = new AuditEntry(entry);
            auditEntry.TableName = entry.Entity.GetType().Name;
            auditEntry.UserId = UserId;
            auditEntry.IpAddress = IpAddress;
            auditEntries.Add(auditEntry);
            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.AuditType = AuditEntry.TypeAudit.Create;
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.AuditType = AuditEntry.TypeAudit.Delete;
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.ChangedColumns.Add(propertyName);
                            auditEntry.AuditType = AuditEntry.TypeAudit.Update;
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }
        foreach (var auditEntry in auditEntries)
        {
            AuditLogs.Add(auditEntry.ToAudit());
        }
    }
}

