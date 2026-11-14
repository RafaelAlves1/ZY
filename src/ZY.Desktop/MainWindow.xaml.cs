
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZY.Application.Interfaces;
using ZY.Core.Enums;
using ZY.Infrastructure.AI;

namespace ZY.Desktop;

public partial class MainWindow : Window
{
    private readonly IConversationService _conversationService;
    private readonly IAssistantStateService _assistantStateService;

    private Storyboard? _thinkingAnimation;
    private bool _thinkingAnimationRunning;

    private static readonly Dictionary<AssistantState, Brush> StateColors = new()
    {
        [AssistantState.Standby] = new SolidColorBrush(Color.FromRgb(84, 217, 255)),
        [AssistantState.Thinking] = new SolidColorBrush(Color.FromRgb(45, 145, 255)),
        [AssistantState.Listening] = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
        [AssistantState.Speaking] = new SolidColorBrush(Color.FromRgb(75, 190, 255)),
        [AssistantState.Executing] = new SolidColorBrush(Color.FromRgb(105, 135, 255)),
        [AssistantState.Error] = new SolidColorBrush(Color.FromRgb(255, 90, 105)),
        [AssistantState.GameMode] = new SolidColorBrush(Color.FromRgb(150, 120, 255))
    };

    public MainWindow()
    {
        InitializeComponent();

        _conversationService =
            ((App)System.Windows.Application.Current).ConversationService;

        _assistantStateService =
            ((App)System.Windows.Application.Current).AssistantStateService;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        SendButton.Click += SendButton_Click;
        MessageInput.KeyDown += MessageInput_KeyDown;

        _assistantStateService.StateChanged += AssistantStateService_StateChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _thinkingAnimation =
            (Storyboard)FindResource("ThinkingAuraAnimation");
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _assistantStateService.StateChanged -= AssistantStateService_StateChanged;

        StopBrainAnimation();
    }

    private void AssistantStateService_StateChanged(
        ZY.Core.Events.AssistantStateChangedEvent stateChangedEvent)
    {
        Dispatcher.Invoke(() =>
        {
            var currentState = stateChangedEvent.CurrentState;

            AssistantStateText.Text =
                currentState.ToString().ToUpperInvariant();

            AssistantStateText.Foreground = StateColors[currentState];

            UpdateBrainAnimation(currentState);
        });

        System.Diagnostics.Debug.WriteLine(
            $"ZY: {stateChangedEvent.PreviousState} -> {stateChangedEvent.CurrentState}"
        );
    }

    private void UpdateBrainAnimation(AssistantState state)
    {
        if (state == AssistantState.Thinking)
        {
            StartBrainAnimation();
        }
        else
        {
            StopBrainAnimation();
        }
    }

    private void StartBrainAnimation()
    {
        if (_thinkingAnimationRunning || _thinkingAnimation is null)
            return;

        _thinkingAnimation.Begin(
            this,
            HandoffBehavior.SnapshotAndReplace,
            true
        );

        _thinkingAnimationRunning = true;
    }

    private void StopBrainAnimation()
    {
        if (!_thinkingAnimationRunning || _thinkingAnimation is null)
            return;

        _thinkingAnimation.Stop(this);
        _thinkingAnimationRunning = false;
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void MessageInput_KeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        var message = MessageInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        AddMessage(message, true);

        MessageInput.Clear();
        SendButton.IsEnabled = false;

        try
        {
            _assistantStateService.SetState(AssistantState.Thinking);

            var response = await _conversationService.SendMessageAsync(message);

            AddMessage(response, false);
        }
        catch (OllamaModelNotFoundException)
        {
            AddMessage(
                "O modelo de inteligência artificial não está disponível. Verifique se ele está instalado no Ollama.",
                false
            );
        }
        catch (HttpRequestException)
        {
            AddMessage(
                "Não consegui me conectar ao serviço de IA. Verifique se o Ollama está iniciado.",
                false
            );
        }
        catch (TaskCanceledException)
        {
            AddMessage(
                "A solicitação demorou mais do que o esperado. Tente novamente.",
                false
            );
        }
        catch (Exception)
        {
            AddMessage(
                "Ocorreu um erro inesperado ao processar sua mensagem.",
                false
            );
        }
        finally
        {
            _assistantStateService.SetState(AssistantState.Standby);

            SendButton.IsEnabled = true;
            MessageInput.Focus();
        }
    }

    private void AddMessage(string message, bool isUser)
    {
        var messageBlock = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };

        var copyButton = new Button
        {
            Content = "Copiar",
            FontSize = 11,
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 10, 0, 0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(84, 217, 255)),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        copyButton.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(message);
                copyButton.Content = "Copiado!";

                Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        copyButton.Content = "Copiar";
                    });
                });
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível copiar a mensagem.",
                    "ZY",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        };

        var messageContent = new StackPanel();

        messageContent.Children.Add(messageBlock);
        messageContent.Children.Add(copyButton);

        var messageContainer = new Border
        {
            Background = isUser
                ? new SolidColorBrush(Color.FromRgb(18, 97, 138))
                : new SolidColorBrush(Color.FromRgb(27, 44, 66)),

            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            MaxWidth = 600,

            HorizontalAlignment = isUser
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,

            Child = messageContent
        };

        MessagesPanel.Children.Add(messageContainer);
    }
}
