using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Pastas.Presentation.ViewModels;

namespace Pastas.Presentation.Views;

public partial class PreviewPanelView : UserControl
{
    private bool _isCopyFeedbackActive;

    public PreviewPanelView()
    {
        InitializeComponent();
    }

    private async void CopyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || viewModel.SelectedItem is null)
        {
            return;
        }

        if (!viewModel.CopyItemCommand.CanExecute(viewModel.SelectedItem))
        {
            return;
        }

        viewModel.CopyItemCommand.Execute(viewModel.SelectedItem);

        if (_isCopyFeedbackActive)
        {
            return;
        }

        _isCopyFeedbackActive = true;
        CopyButton.Content = "Copied";

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1200));
        }
        finally
        {
            CopyButton.Content = "Copy";
            _isCopyFeedbackActive = false;
        }
    }
}
