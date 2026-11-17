
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using ZY.Application.Interfaces;
using ZY.Core.Enums;
using ZY.Core.Interfaces;
using ZY.Desktop.Views;
using ZY.Infrastructure.AI;

namespace ZY.Desktop;

public partial class MainWindow : Window
{
    private readonly IConversationService _conversationService;
    private readonly IAssistantStateService _assistantStateService;

    private bool _thinkingAnimationRunning;
    private Border? _thinkingIndicator;
    private bool _isProcessing;

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

    private static readonly Dictionary<AssistantState, string> StateDescriptions = new()
    {
        [AssistantState.Standby] = "AGUARDANDO INTERAÇÃO",
        [AssistantState.Listening] = "OUVINDO VOCÊ",
        [AssistantState.Thinking] = "PROCESSANDO INFORMAÇÕES",
        [AssistantState.Speaking] = "RESPONDENDO",
        [AssistantState.Executing] = "EXECUTANDO TAREFA",
        [AssistantState.Error] = "OCORREU UM ERRO",
        [AssistantState.GameMode] = "MODO DE JOGO ATIVO"
    };

    public MainWindow()
    {
        InitializeComponent();

        _conversationService =
            ((App)System.Windows.Application.Current).ConversationService;

        _assistantStateService =
            ((App)System.Windows.Application.Current).AssistantStateService;

        SendButton.Click += SendButton_Click;
        ClearConversationButton.Click += ClearConversation;
        MessageInput.KeyDown += MessageInput_KeyDown;

        PreviewKeyDown += MainWindow_PreviewKeyDown;

        _assistantStateService.StateChanged += AssistantStateService_StateChanged;

        UpdateVisualState(_assistantStateService.CurrentState);

        if (_conversationService.History.Count > 0)
        {
            foreach (var message in _conversationService.History)
            {
                AddMessage(
                    message.Content,
                    message.Role == "user"
                );
            }
        }
        else
        {
            AddMessage(
                "Olá! Estou pronto para conversar. Como posso ajudar?",
                false
            );
        }

        MessageInput.TextChanged += (_, _) =>
        {
            MessagePlaceholder.Visibility =
                string.IsNullOrEmpty(MessageInput.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        };
    }

    // ATALHOS TEMPORÁRIOS PARA TESTAR OS ESTADOS
    private void MainWindow_PreviewKeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        var testState = e.Key switch
        {
            System.Windows.Input.Key.F1 => AssistantState.Standby,
            System.Windows.Input.Key.F2 => AssistantState.Listening,
            System.Windows.Input.Key.F3 => AssistantState.Thinking,
            System.Windows.Input.Key.F4 => AssistantState.Speaking,
            System.Windows.Input.Key.F5 => AssistantState.Executing,
            System.Windows.Input.Key.F6 => AssistantState.Error,
            System.Windows.Input.Key.F7 => AssistantState.GameMode,
            _ => (AssistantState?)null
        };

        if (testState is null)
            return;

        e.Handled = true;
        _assistantStateService.SetState(testState.Value);
    }

    private void AssistantStateService_StateChanged(
        ZY.Core.Events.AssistantStateChangedEvent stateChangedEvent
    )
    {
        Dispatcher.Invoke(() =>
        {
            UpdateVisualState(stateChangedEvent.CurrentState);
        });

        System.Diagnostics.Debug.WriteLine(
            $"ZY: {stateChangedEvent.PreviousState} -> {stateChangedEvent.CurrentState}"
        );
    }

    private void UpdateVisualState(AssistantState state)
    {
        AssistantStateText.Text = state.ToString().ToUpperInvariant();
        AssistantStateText.Foreground = StateColors[state];

        AssistantStateIndicator.Fill = StateColors[state];

        AssistantStateIndicator.Effect = new DropShadowEffect
        {
            Color = ((SolidColorBrush)StateColors[state]).Color,
            BlurRadius = 9,
            ShadowDepth = 0,
            Opacity = 0.8
        };

        AssistantStateDescription.Text = StateDescriptions[state];

        UpdateBrainAnimation(state);
    }

    private void UpdateBrainAnimation(AssistantState state)
    {
        var thinkingAnimation =
            (Storyboard)FindResource("ThinkingAuraAnimation");

        if (_thinkingAnimationRunning)
        {
            thinkingAnimation.Stop(this);
            _thinkingAnimationRunning = false;
        }

        ResetAuraAppearance();
        ResetWaveAnimations();

        switch (state)
        {
            case AssistantState.Standby:
                SetStaticWaves(0.20, Color.FromRgb(39, 141, 203));
                break;

            case AssistantState.Listening:
                AnimateWaves(
                    0.15,
                    0.85,
                    0.38,
                    Color.FromRgb(0, 229, 255)
                );
                break;

            case AssistantState.Thinking:
                thinkingAnimation.Begin(this, true);
                _thinkingAnimationRunning = true;
                break;

            case AssistantState.Speaking:
                AnimateWaves(
                    0.20,
                    1.00,
                    0.42,
                    Color.FromRgb(75, 190, 255)
                );
                break;

            case AssistantState.Executing:
                AnimateSequentialWaves();
                break;

            case AssistantState.Error:
                AnimateWaves(
                    0.20,
                    0.80,
                    0.30,
                    Color.FromRgb(255, 90, 105)
                );
                break;

            case AssistantState.GameMode:
                SetStaticWaves(0.12, Color.FromRgb(92, 112, 145));
                break;
        }
    }

    private void ResetAuraAppearance()
    {
        BrainAura.BeginAnimation(UIElement.OpacityProperty, null);

        BrainAura.RenderTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty, null);

        BrainAura.RenderTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty, null);

        BrainAuraInner.BeginAnimation(UIElement.OpacityProperty, null);

        BrainAuraInner.RenderTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty, null);

        BrainAuraInner.RenderTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty, null);

        BrainAura.Opacity = 0.12;
        BrainAuraInner.Opacity = 0.15;

        if (BrainAura.Effect is BlurEffect outerBlur)
            outerBlur.Radius = 38;

        if (BrainAuraInner.Effect is BlurEffect innerBlur)
            innerBlur.Radius = 30;
    }

    private void ResetWaveAnimations()
    {
        foreach (var bar in GetWaveBars())
        {
            if (bar.RenderTransform is not ScaleTransform scale)
                continue;

            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleY = 0.25;
        }
    }

    private Border[] GetWaveBars()
    {
        return
        [
            WaveBar01, WaveBar02, WaveBar03, WaveBar04, WaveBar05,
            WaveBar06, WaveBar07, WaveBar08, WaveBar09, WaveBar10,
            WaveBar11, WaveBar12, WaveBar13, WaveBar14, WaveBar15
        ];
    }

    private void SetStaticWaves(double scale, Color color)
    {
        foreach (var bar in GetWaveBars())
        {
            if (bar.RenderTransform is not ScaleTransform transform)
                continue;

            transform.ScaleY = scale;
            bar.Background = new SolidColorBrush(color);
        }
    }

    private void AnimateWaves(
        double minimum,
        double maximum,
        double duration,
        Color color
    )
    {
        var bars = GetWaveBars();

        for (var i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];

            if (bar.RenderTransform is not ScaleTransform transform)
                continue;

            bar.Background = new SolidColorBrush(color);

            var variation = (i % 5) * 0.08;

            var animation = new DoubleAnimation
            {
                From = minimum,
                To = Math.Min(maximum, maximum - variation),
                Duration = TimeSpan.FromSeconds(
                    duration + ((i % 4) * 0.09)
                ),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            transform.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace
            );
        }
    }

    private void AnimateSequentialWaves()
    {
        var bars = GetWaveBars();

        for (var i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];

            if (bar.RenderTransform is not ScaleTransform transform)
                continue;

            bar.Background = new SolidColorBrush(
                Color.FromRgb(105, 135, 255)
            );

            var animation = new DoubleAnimation
            {
                From = 0.15,
                To = 0.95,
                Duration = TimeSpan.FromSeconds(0.38),
                BeginTime = TimeSpan.FromMilliseconds(i * 65),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            transform.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace
            );
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void MessageInput_KeyDown(
        object sender,
        System.Windows.Input.KeyEventArgs e
    )
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        if (_isProcessing)
            return;

        var message = MessageInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(message))
            return;

        _isProcessing = true;

        AddMessage(message, true);

        MessageInput.Clear();

        SendButton.IsEnabled = false;
        MessageInput.IsEnabled = false;

        try
        {
            _assistantStateService.SetState(AssistantState.Thinking);

            ShowThinkingIndicator();

            string response;

            try
            {
                response = await _conversationService.SendMessageAsync(message);
            }
            finally
            {
                HideThinkingIndicator();
            }

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

            _isProcessing = false;

            SendButton.IsEnabled = true;
            MessageInput.IsEnabled = true;

            MessageInput.Focus();
        }
    }

    private void AddMessage(string message, bool isUser)
    {
        // Identificação neutra para combinar com a paleta grafite.
        var labelColor = isUser
            ? Color.FromRgb(190, 190, 190)
            : Color.FromRgb(234, 234, 234);

        var header = new TextBlock
        {
            Text = isUser ? "VOCÊ" : "ZY",
            Foreground = new SolidColorBrush(labelColor),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var messageBlock = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Color.FromRgb(226, 226, 226)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 19
        };

        var copyIcon = new TextBlock
        {
            Text = "\uE8C8",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var copyButton = new Button
        {
            Content = copyIcon,
            ToolTip = "Copiar mensagem",
            Width = 27,
            Height = 25,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 5, 0, -3),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right,
            Focusable = false
        };

        copyButton.Template = CreateCopyButtonTemplate();

        copyButton.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(message);

                copyIcon.Text = "\uE73E";
                copyIcon.Foreground = new SolidColorBrush(
                    Color.FromRgb(72, 223, 169)
                );

                copyButton.ToolTip = "Mensagem copiada";

                Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        copyIcon.Text = "\uE8C8";
                        copyIcon.Foreground = new SolidColorBrush(
                            Color.FromRgb(170, 170, 170)
                        );
                        copyButton.ToolTip = "Copiar mensagem";
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

        messageContent.Children.Add(header);
        messageContent.Children.Add(messageBlock);
        messageContent.Children.Add(copyButton);

        var messageContainer = new Border
        {
            Background = new SolidColorBrush(
                isUser
                    ? Color.FromRgb(48, 48, 48)
                    : Color.FromRgb(37, 37, 37)
            ),

            BorderBrush = new SolidColorBrush(Color.FromRgb(65, 65, 65)),

            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(13, 10, 13, 8),
            Margin = new Thickness(0, 0, 0, 10),
            MinWidth = 120,
            MaxWidth = 540,
            HorizontalAlignment = isUser
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,

            Child = messageContent
        };

        MessagesPanel.Children.Add(messageContainer);

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => MessagesScrollViewer.ScrollToEnd())
        );
    }

    private ControlTemplate CreateCopyButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));

        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ButtonBorder";

        border.SetValue(
            Border.BackgroundProperty,
            Brushes.Transparent
        );

        border.SetValue(
            Border.CornerRadiusProperty,
            new CornerRadius(6)
        );

        border.SetValue(
            Border.PaddingProperty,
            new Thickness(3)
        );

        var presenter = new FrameworkElementFactory(
            typeof(ContentPresenter)
        );

        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            HorizontalAlignment.Center
        );

        presenter.SetValue(
            ContentPresenter.VerticalAlignmentProperty,
            VerticalAlignment.Center
        );

        border.AppendChild(presenter);
        template.VisualTree = border;

        var hoverTrigger = new Trigger
        {
            Property = Button.IsMouseOverProperty,
            Value = true
        };

        hoverTrigger.Setters.Add(
            new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(54, 54, 54)),
                "ButtonBorder"
            )
        );

        template.Triggers.Add(hoverTrigger);

        var pressedTrigger = new Trigger
        {
            Property = Button.IsPressedProperty,
            Value = true
        };

        pressedTrigger.Setters.Add(
            new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                "ButtonBorder"
            )
        );

        template.Triggers.Add(pressedTrigger);

        return template;
    }

    private void ShowThinkingIndicator()
    {
        if (_thinkingIndicator != null)
            return;

        var header = new TextBlock
        {
            Text = "ZY",
            Foreground = new SolidColorBrush(
                Color.FromRgb(234, 234, 234)
            ),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var message = new TextBlock
        {
            Text = "ZY está pensando",
            Foreground = new SolidColorBrush(
                Color.FromRgb(226, 226, 226)
            ),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };

        var dots = new TextBlock
        {
            Text = "...",
            Foreground = new SolidColorBrush(
                Color.FromRgb(190, 190, 190)
            ),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(3, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        dots.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation
            {
                From = 0.25,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(650),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            }
        );

        var content = new StackPanel();

        content.Children.Add(header);

        var messageRow = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        messageRow.Children.Add(message);
        messageRow.Children.Add(dots);

        content.Children.Add(messageRow);

        _thinkingIndicator = new Border
        {
            Background = new SolidColorBrush(
                Color.FromRgb(37, 37, 37)
            ),
            BorderBrush = new SolidColorBrush(
                Color.FromRgb(65, 65, 65)
            ),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(13, 10, 13, 8),
            Margin = new Thickness(0, 0, 0, 10),
            MinWidth = 120,
            MaxWidth = 540,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = content
        };

        MessagesPanel.Children.Add(_thinkingIndicator);

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => MessagesScrollViewer.ScrollToEnd())
        );
    }

    private void HideThinkingIndicator()
    {
        if (_thinkingIndicator == null)
            return;

        MessagesPanel.Children.Remove(_thinkingIndicator);
        _thinkingIndicator = null;
    }

    private async void ClearConversation(object? sender, RoutedEventArgs e)
    {
        if (_isProcessing)
            return;

        var confirmationWindow = new ConfirmationWindow
        {
            Owner = this
        };

        var result = confirmationWindow.ShowDialog();

        if (result != true)
            return;

        try
        {
            await _conversationService.ClearHistoryAsync();

            MessagesPanel.Children.Clear();

            AddMessage(
                "Olá! Estou pronto para conversar. Como posso ajudar?",
                false
            );
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Não foi possível limpar a conversa.",
                "ZY",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
    }
}
