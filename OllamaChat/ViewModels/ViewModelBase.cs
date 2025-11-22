using CommunityToolkit.Mvvm.ComponentModel;

namespace OllamaChat.ViewModels;

/// <summary>
/// Base class for all ViewModels
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    protected void ClearError() => ErrorMessage = null;

    protected void SetError(string message) => ErrorMessage = message;
}
