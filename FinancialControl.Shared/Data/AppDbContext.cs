using FinancialControl.Shared.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryRule> CategoryRules { get; set; }
    public DbSet<CategoryLearning> CategoryLearning { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .ToTable("categories")
            .HasKey(c => c.Id);

        modelBuilder.Entity<CategoryRule>()
            .ToTable("category_rules")
            .HasKey(r => r.Id);

        modelBuilder.Entity<CategoryRule>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Rules)
            .HasForeignKey(r => r.CategoryId);

        modelBuilder.Entity<CategoryLearning>()
            .ToTable("category_learning")
            .HasKey(a => a.Id);

        modelBuilder.Entity<CategoryLearning>()
            .HasIndex(a => a.CleanDescription);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Date).HasColumnName("Date");
            entity.Property(t => t.Value).HasColumnName("Value");
            entity.Property(t => t.Description).HasColumnName("Description");
            entity.Property(t => t.CategoryId).HasColumnName("CategoryId");
            entity.Property(t => t.Type).HasColumnName("Type");
            entity.Property(t => t.OriginalName).HasColumnName("OriginalName");
        });

        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.ToTable("category_rules");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.CategoryId).HasColumnName("CategoryId");
            entity.Property(r => r.weight).HasColumnName("Weight");
            entity.Property(r => r.KeyWord).HasColumnName("Keyword");

        });
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name).HasColumnName("Name");
        });
        modelBuilder.Entity<CategoryLearning>(entity =>
        {
            entity.ToTable("category_learning");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.CategoryId).HasColumnName("categoryId");
            entity.Property(a => a.CleanDescription).HasColumnName("CleanDescription");
            entity.Property(a => a.Count).HasColumnName("Count");
            entity.Property(a => a.Description).HasColumnName("Description");
        });
        modelBuilder.Entity<Transaction>()
    .HasOne(t => t.Category)
    .WithMany()
    .HasForeignKey(t => t.CategoryId)
    .OnDelete(DeleteBehavior.Restrict);


    }
}