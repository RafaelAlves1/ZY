
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZY.Application.Interfaces;

namespace ZY.Desktop;

public partial class MainWindow : Window
{
    private readonly IConversationService _conversationService;

    public MainWindow()
    {
        InitializeComponent();

        _conversationService =
            ((App)System.Windows.Application.Current).ConversationService;

        SendButton.Click += SendButton_Click;

        MessageInput.KeyDown += MessageInput_KeyDown;
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
            var response = await _conversationService.SendMessageAsync(message);

            AddMessage(response, false);
        }
        catch (Exception ex)
        {
            AddMessage(
                $"Não foi possível obter uma resposta: {ex.Message}",
                false
            );
        }
        finally
        {
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
