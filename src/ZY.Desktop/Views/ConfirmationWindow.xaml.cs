using System.Windows;

namespace ZY.Desktop.Views;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow()
    {
        InitializeComponent();
    }

    private void ConfirmButton_Click(
        object sender,
        RoutedEventArgs e
    )
    {
        DialogResult = true;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e
    )
    {
        DialogResult = false;
    }
}
