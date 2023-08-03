using FirstMinimalApi.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FirstMinimalApi.WebApi.Data;

public sealed class MinimalContextDb : DbContext
{
    public MinimalContextDb(DbContextOptions options) : base(options)
    { }

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fornecedor>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Fornecedor>()
            .Property(x => x.Nome)
            .IsRequired()
            .HasColumnType("varchar(200)");

        modelBuilder.Entity<Fornecedor>()
            .Property(x => x.Documento)
            .IsRequired()
            .HasColumnType("varchar(14)");

        modelBuilder.Entity<Fornecedor>()
            .ToTable("Fornecedores");

        base.OnModelCreating(modelBuilder);
    }
}