using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using OllamaChat.Models;

namespace OllamaChat.Views;

/// <summary>
/// Settings window for configuring Ollama connection and options
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly OllamaConfig _config;
    private readonly string _configPath;

    public SettingsWindow(OllamaConfig config)
    {
        InitializeComponent();

        _config = config;
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        LoadSettings();
    }

    private void LoadSettings()
    {
        BaseUrlTextBox.Text = _config.BaseUrl;
        DefaultModelTextBox.Text = _config.DefaultModel;
        TimeoutTextBox.Text = _config.TimeoutSeconds.ToString();
        StreamResponsesCheckBox.IsChecked = _config.StreamResponses;

        TemperatureTextBox.Text = _config.DefaultOptions.Temperature.ToString("F1");
        TopPTextBox.Text = _config.DefaultOptions.TopP.ToString("F1");
        TopKTextBox.Text = _config.DefaultOptions.TopK.ToString();
        ContextLengthTextBox.Text = _config.DefaultOptions.NumCtx.ToString();
        RepeatPenaltyTextBox.Text = _config.DefaultOptions.RepeatPenalty.ToString("F1");
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update config
            _config.BaseUrl = BaseUrlTextBox.Text.Trim();
            _config.DefaultModel = DefaultModelTextBox.Text.Trim();
            _config.TimeoutSeconds = int.TryParse(TimeoutTextBox.Text, out var timeout) ? timeout : 300;
            _config.StreamResponses = StreamResponsesCheckBox.IsChecked ?? true;

            _config.DefaultOptions.Temperature = float.TryParse(TemperatureTextBox.Text, out var temp) ? temp : 0.7f;
            _config.DefaultOptions.TopP = float.TryParse(TopPTextBox.Text, out var topP) ? topP : 0.9f;
            _config.DefaultOptions.TopK = int.TryParse(TopKTextBox.Text, out var topK) ? topK : 40;
            _config.DefaultOptions.NumCtx = int.TryParse(ContextLengthTextBox.Text, out var ctx) ? ctx : 4096;
            _config.DefaultOptions.RepeatPenalty = float.TryParse(RepeatPenaltyTextBox.Text, out var penalty) ? penalty : 1.1f;

            // Save to file asynchronously
            await SaveConfigToFileAsync();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async Task SaveConfigToFileAsync()
    {
        var configObject = new
        {
            Ollama = new
            {
                _config.BaseUrl,
                _config.DefaultModel,
                _config.TimeoutSeconds,
                _config.StreamResponses,
                DefaultOptions = new
                {
                    _config.DefaultOptions.Temperature,
                    _config.DefaultOptions.NumCtx,
                    _config.DefaultOptions.TopP,
                    _config.DefaultOptions.TopK,
                    _config.DefaultOptions.RepeatPenalty
                }
            },
            Application = new
            {
                Theme = "Dark",
                AutoSaveArtifacts = true,
                MaxRecentChats = 50
            }
        };

        var json = JsonSerializer.Serialize(configObject, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_configPath, json);
    }
}
