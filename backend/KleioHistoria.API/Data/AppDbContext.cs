using KleioHistoria.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KleioHistoria.API.Data;

/// <summary>
/// Ponto central de acesso ao banco (padrão Unit of Work do EF Core).
///
/// Herda de <see cref="IdentityDbContext{TUser,TRole,TKey}"/> para que o Identity
/// crie suas tabelas (usuários, perfis, claims...) no mesmo banco e na mesma migration.
/// As regras de cada tabela ficam em Data/Configuracoes (uma classe por entidade).
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<int>, int>(options)
{
    public DbSet<Artigo> Artigos => Set<Artigo>();
    public DbSet<Capitulo> Capitulos => Set<Capitulo>();
    public DbSet<Imagem> Imagens => Set<Imagem>();
    public DbSet<Sugestao> Sugestoes => Set<Sugestao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Precisa vir primeiro: configura as tabelas padrão do Identity.
        base.OnModelCreating(modelBuilder);

        // Extensão do PostgreSQL que remove acentos: permite buscar "revolucao"
        // e encontrar "Revolução". Vem instalada na imagem oficial do Postgres.
        modelBuilder.HasPostgresExtension("unaccent");

        // Renomeia as tabelas do Identity para nomes em português.
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Perfis");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UsuarioPerfis");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UsuarioClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UsuarioLogins");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UsuarioTokens");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("PerfilClaims");

        // Aplica todas as classes IEntityTypeConfiguration<T> deste projeto.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
