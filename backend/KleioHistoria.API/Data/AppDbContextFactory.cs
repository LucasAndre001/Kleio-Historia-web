using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KleioHistoria.API.Data;

/// <summary>
/// Usada SOMENTE pelas ferramentas do EF Core (dotnet ef migrations add / dotnet ef database update).
/// Permite criar migrations sem banco em execução e sem configurar JWT/admin,
/// pois o comando não precisa iniciar a API inteira.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Padrao")
                               ?? "Host=localhost;Port=5433;Database=kleio_historia;Username=kleio;Password=design-time";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            // mesma regra da API: divergência entre snapshot e modelo vira aviso, não erro
            .ConfigureWarnings(avisos => avisos.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new AppDbContext(options);
    }
}
