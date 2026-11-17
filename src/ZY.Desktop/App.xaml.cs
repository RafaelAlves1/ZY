
using System.Windows;
using Microsoft.EntityFrameworkCore;
using ZY.Application.Interfaces;
using ZY.Application.Services;
using ZY.Core.Interfaces;
using ZY.Infrastructure.AI;
using ZY.Infrastructure.Persistence.PostgreSQL;
using ZY.Infrastructure.Persistence.PostgreSQL.Context;
using ZY.Infrastructure.Persistence.PostgreSQL.Repositories;

namespace ZY.Desktop;

/// <summary>
/// Inicialização e composição dos serviços do aplicativo ZY.
/// </summary>
public partial class App : System.Windows.Application
{
    public IConversationService ConversationService { get; private set; } = null!;

    public IAssistantStateService AssistantStateService { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            AssistantStateService = new AssistantStateService();

            var httpClient = new System.Net.Http.HttpClient
            {
                BaseAddress = new Uri(OllamaConfiguration.BaseUrl)
            };

            IAiService aiService = new OllamaAiService(httpClient);

            var dbOptions = new DbContextOptionsBuilder<ZYDbContext>()
                .UseNpgsql(PostgreSqlConfiguration.ConnectionString)
                .Options;

            var dbContext = new ZYDbContext(dbOptions);

            IConversationRepository conversationRepository =
                new ConversationRepository(dbContext);

            var conversationService = new ConversationService(
                aiService,
                conversationRepository
            );

            await conversationService.InitializeAsync();

            ConversationService = conversationService;

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível inicializar o ZY.\n\n{ex.Message}",
                "Erro de inicialização",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            Shutdown();
        }
    }
}
