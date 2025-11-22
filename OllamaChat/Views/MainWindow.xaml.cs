using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using OllamaChat.ViewModels;

namespace OllamaChat.Views;

/// <summary>
/// Main application window
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;

        // Auto-scroll messages
        if (_viewModel != null)
        {
            _viewModel.Messages.CollectionChanged += (s, e) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessagesScrollViewer.ScrollToEnd();
                });
            };
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel!.InitializeAsync();
        MessageInputBox.Focus();
    }

    private void MessageInputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && !System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
        {
            if (_viewModel!.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void RecentChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ChatListItemViewModel chatItem)
        {
            if (_viewModel!.SelectChatCommand.CanExecute(chatItem))
            {
                _viewModel.SelectChatCommand.Execute(chatItem);
            }
        }
    }

    private void ProjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ProjectViewModel projectItem)
        {
            if (_viewModel!.SelectProjectCommand.CanExecute(projectItem))
            {
                _viewModel.SelectProjectCommand.Execute(projectItem);
            }
        }
    }
}
