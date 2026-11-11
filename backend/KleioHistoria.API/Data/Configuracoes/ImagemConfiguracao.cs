using KleioHistoria.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KleioHistoria.API.Data.Configuracoes;

public class ImagemConfiguracao : IEntityTypeConfiguration<Imagem>
{
    public void Configure(EntityTypeBuilder<Imagem> builder)
    {
        builder.ToTable("Imagens");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.Legenda).HasMaxLength(300);
        builder.Property(i => i.TextoAlternativo).HasMaxLength(300);

        builder.HasIndex(i => new { i.CapituloId, i.Ordem });
    }
}
