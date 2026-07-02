using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace CampaignsApi.Service;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var services = scope.ServiceProvider;
            
            var logger = services.GetRequiredService<ILogger<Program>>();
            var context = services.GetRequiredService<ApplicationDbContext>(); 
            
            // Configuração do Loop de Tentativas
            var retryCount = 10; // Tenta 10 vezes
            var waitTime = TimeSpan.FromSeconds(3); // Espera 3 segundos entre cada tentativa

            for (var i = 0; i < retryCount; i++)
            {
                try
                {
                    logger.LogWarning($"Tentativa {i + 1} de {retryCount}: Conectando ao banco para migrar...");
                    
                    context.Database.Migrate();

                    logger.LogWarning("✅ Migrations aplicadas com sucesso!");
                    return; // Sai do método se funcionou
                }
                catch (Exception ex) when (ex.Message.Contains("42P07"))
                {
                    logger.LogWarning("⚠️ Tabelas já existem no banco (42P07). Considerando migrations como aplicadas.");
                    return; // Trata como sucesso
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"⚠️ Falha ao conectar no banco (Tentativa {i + 1}). O container pode estar subindo ainda.");
                    logger.LogWarning($"Erro: {ex.Message}");

                    // Se foi a última tentativa, joga o erro pra fechar a aplicação
                    if (i == retryCount - 1)
                    {
                        logger.LogError("❌ Todas as tentativas falharam. A aplicação será encerrada.");
                        throw; 
                    }

                    // Espera antes de tentar de novo
                    System.Threading.Thread.Sleep(waitTime);
                }
            }
        }
    }
}