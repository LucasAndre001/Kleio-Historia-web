using KleioHistoria.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KleioHistoria.API.Data.Configuracoes;

/// <summary>Regras de integridade da tabela Artigos (Fluent API).</summary>
public class ArtigoConfiguracao : IEntityTypeConfiguration<Artigo>
{
    public void Configure(EntityTypeBuilder<Artigo> builder)
    {
        builder.ToTable("Artigos", tabela =>
        {
            // Regra de negócio reforçada no próprio banco:
            // um artigo publicado precisa ter data de publicação.
            tabela.HasCheckConstraint(
                "CK_Artigos_PublicadoTemData",
                "\"Status\" <> 'Publicado' OR \"DataPublicacao\" IS NOT NULL");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Slug).IsRequired().HasMaxLength(220);
        builder.Property(a => a.Resumo).IsRequired().HasMaxLength(600);
        builder.Property(a => a.Categoria).IsRequired().HasMaxLength(80);
        builder.Property(a => a.ImagemCapa).HasMaxLength(500);
        builder.Property(a => a.ImagemBanner).HasMaxLength(500);
        builder.Property(a => a.Autor).HasMaxLength(150);
        builder.Property(a => a.ArquivoPdfOrigem).HasMaxLength(260);

        // O enum é gravado como texto ("Rascunho"/"Publicado"): fica legível
        // para quem consulta o banco diretamente (pgAdmin, DBeaver...).
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Slug único: garante URLs sem ambiguidade.
        builder.HasIndex(a => a.Slug).IsUnique();

        // Índices para as consultas mais comuns da área pública.
        builder.HasIndex(a => a.Categoria);
        builder.HasIndex(a => new { a.Status, a.DataPublicacao });

        // Um usuário publica vários artigos. Restrict impede apagar um usuário
        // que ainda possui artigos (evita perder conteúdo sem querer).
        builder.HasOne(a => a.Usuario)
            .WithMany(u => u.Artigos)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Capítulos pertencem ao artigo: ao excluir o artigo, os capítulos
        // (e, em cascata, suas imagens) também são excluídos.
        builder.HasMany(a => a.Capitulos)
            .WithOne(c => c.Artigo)
            .HasForeignKey(c => c.ArtigoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
