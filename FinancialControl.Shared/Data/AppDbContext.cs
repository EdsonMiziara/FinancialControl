using FinancialControl.Shared.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<RegraCategoria> RegrasCategoria { get; set; }
    public DbSet<AprendizadoCategoria> AprendizadoCategorias { get; set; }
    public DbSet<Transacao> Transacoes { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>()
            .ToTable("categorias")
            .HasKey(c => c.Id);

        modelBuilder.Entity<RegraCategoria>()
            .ToTable("regras_categoria")
            .HasKey(r => r.Id);

        modelBuilder.Entity<RegraCategoria>()
            .HasOne(r => r.Categoria)
            .WithMany(c => c.Regras)
            .HasForeignKey(r => r.CategoriaId);

        modelBuilder.Entity<AprendizadoCategoria>()
            .ToTable("aprendizado_categoria")
            .HasKey(a => a.Id);

        modelBuilder.Entity<AprendizadoCategoria>()
            .HasIndex(a => a.DescricaoLimpa);

        modelBuilder.Entity<Transacao>(entity =>
        {
            entity.ToTable("transacoes");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Data).HasColumnName("Data");
            entity.Property(t => t.Valor).HasColumnName("Valor");
            entity.Property(t => t.Descricao).HasColumnName("Descricao");
            entity.Property(t => t.CategoriaId).HasColumnName("CategoriaId");
            entity.Property(t => t.Tipo).HasColumnName("Tipo");
            entity.Property(t => t.NomeOriginal).HasColumnName("NomeOriginal");
        });

        modelBuilder.Entity<RegraCategoria>(entity =>
        {
            entity.ToTable("regras_categoria");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.CategoriaId).HasColumnName("categoria_id");
            entity.Property(r => r.Peso).HasColumnName("peso");
            entity.Property(r => r.PalavraChave).HasColumnName("palavra_chave");

        });
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Nome).HasColumnName("nome");
        });
        modelBuilder.Entity<AprendizadoCategoria>(entity =>
        {
            entity.ToTable("aprendizado_categoria");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.CategoriaId).HasColumnName("categoriaId");
            entity.Property(a => a.DescricaoLimpa).HasColumnName("descricao_limpa");
            entity.Property(a => a.Vezes).HasColumnName("vezes");
            entity.Property(a => a.Descricao).HasColumnName("descricao");
        });
        modelBuilder.Entity<Transacao>()
    .HasOne(t => t.Categoria)
    .WithMany() // você não tem lista de transações em Categoria (ok)
    .HasForeignKey(t => t.CategoriaId)
    .OnDelete(DeleteBehavior.Restrict);


    }
}