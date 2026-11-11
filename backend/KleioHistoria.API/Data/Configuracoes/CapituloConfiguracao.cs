using KleioHistoria.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KleioHistoria.API.Data.Configuracoes;

public class CapituloConfiguracao : IEntityTypeConfiguration<Capitulo>
{
    public void Configure(EntityTypeBuilder<Capitulo> builder)
    {
        builder.ToTable("Capitulos", tabela =>
            tabela.HasCheckConstraint("CK_Capitulos_OrdemPositiva", "\"Ordem\" >= 1"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Titulo).IsRequired().HasMaxLength(200);

        // "text" no PostgreSQL não tem limite prático de tamanho.
        builder.Property(c => c.ConteudoHtml).IsRequired().HasColumnType("text");
        builder.Property(c => c.ConteudoTexto).IsRequired().HasColumnType("text");

        // Índice composto: a leitura sempre busca os capítulos de um artigo em ordem.
        builder.HasIndex(c => new { c.ArtigoId, c.Ordem });

        builder.HasMany(c => c.Imagens)
            .WithOne(i => i.Capitulo)
            .HasForeignKey(i => i.CapituloId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
