using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KleioHistoria.API.Data.Migrations
{
    /// <summary>
    /// Segunda migration: adiciona a coluna ImagemBanner em Artigos.
    /// É a imagem larga usada no carrossel de destaques da página inicial
    /// (opcional; sem ela, o carrossel usa a imagem de capa).
    ///
    /// Up() aplica a mudança e Down() desfaz — o EF Core registra cada migration
    /// aplicada na tabela __EFMigrationsHistory, então ela só roda uma vez por banco.
    /// </summary>
    public partial class AdicionaImagemBanner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagemBanner",
                table: "Artigos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagemBanner",
                table: "Artigos");
        }
    }
}
