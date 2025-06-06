    using Microsoft.EntityFrameworkCore;
    using DigitalAssetAPI.Models;

    namespace DigitalAssetAPI.Data
    {
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions options) : base(options) { }

            public DbSet<User> Users => Set<User>();
            public DbSet<Asset> Assets => Set<Asset>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Asset -> UploadedBy (User) relationship
                modelBuilder.Entity<Asset>()
                    .HasOne(a => a.UploadedBy)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedById);
            }
        }
    }
