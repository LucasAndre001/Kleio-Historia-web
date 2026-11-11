using KleioHistoria.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KleioHistoria.API.Data.Configuracoes;

public class SugestaoConfiguracao : IEntityTypeConfiguration<Sugestao>
{
    public void Configure(EntityTypeBuilder<Sugestao> builder)
    {
        builder.ToTable("Sugestoes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nome).IsRequired().HasMaxLength(120);
        builder.Property(s => s.Email).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Telefone).HasMaxLength(30);
        builder.Property(s => s.TemaSugerido).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Mensagem).HasMaxLength(2000);

        // O painel lista as sugestões da mais recente para a mais antiga.
        builder.HasIndex(s => s.DataEnvio);
    }
}
