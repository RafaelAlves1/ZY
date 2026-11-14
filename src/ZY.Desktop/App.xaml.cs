
using System.Windows;
using ZY.Application.Interfaces;
using ZY.Application.Services;
using ZY.Core.Interfaces;
using ZY.Infrastructure.AI;

namespace ZY.Desktop;

/// <summary>
/// Inicialização e composição dos serviços do aplicativo ZY.
/// </summary>
public partial class App : System.Windows.Application
{
    public IConversationService ConversationService { get; private set; } = null!;

    public IAssistantStateService AssistantStateService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AssistantStateService = new AssistantStateService();

        var httpClient = new System.Net.Http.HttpClient
        {
            BaseAddress = new Uri(OllamaConfiguration.BaseUrl)
        };

        IAiService aiService = new OllamaAiService(httpClient);

        ConversationService = new ConversationService(aiService);
    }
}