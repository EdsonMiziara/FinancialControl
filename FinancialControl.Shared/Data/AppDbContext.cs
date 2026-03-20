using FinancialControl.Shared.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categorias { get; set; }
    public DbSet<CategoryRule> RegrasCategoria { get; set; }
    public DbSet<LearningCategory> AprendizadoCategorias { get; set; }
    public DbSet<Transaction> Transacoes { get; set; }
    public DbSet<User> Usuarios { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .ToTable("categorias")
            .HasKey(c => c.Id);

        modelBuilder.Entity<CategoryRule>()
            .ToTable("regras_categoria")
            .HasKey(r => r.Id);

        modelBuilder.Entity<CategoryRule>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Rules)
            .HasForeignKey(r => r.CategoryId);

        modelBuilder.Entity<LearningCategory>()
            .ToTable("aprendizado_categoria")
            .HasKey(a => a.Id);

        modelBuilder.Entity<LearningCategory>()
            .HasIndex(a => a.CleanDescription);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transacoes");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Date).HasColumnName("Data");
            entity.Property(t => t.Value).HasColumnName("Valor");
            entity.Property(t => t.Description).HasColumnName("Descricao");
            entity.Property(t => t.CategoryId).HasColumnName("CategoriaId");
            entity.Property(t => t.Tipe).HasColumnName("Tipo");
            entity.Property(t => t.OriginalName).HasColumnName("NomeOriginal");
        });

        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.ToTable("regras_categoria");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.CategoryId).HasColumnName("categoria_id");
            entity.Property(r => r.weight).HasColumnName("peso");
            entity.Property(r => r.KeyWord).HasColumnName("palavra_chave");

        });
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categorias");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name).HasColumnName("nome");
        });
        modelBuilder.Entity<LearningCategory>(entity =>
        {
            entity.ToTable("aprendizado_categoria");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.CategoryId).HasColumnName("categoriaId");
            entity.Property(a => a.CleanDescription).HasColumnName("descricao_limpa");
            entity.Property(a => a.Times).HasColumnName("vezes");
            entity.Property(a => a.Description).HasColumnName("descricao");
        });
        modelBuilder.Entity<Transaction>()
    .HasOne(t => t.Category)
    .WithMany()
    .HasForeignKey(t => t.CategoryId)
    .OnDelete(DeleteBehavior.Restrict);


    }
}