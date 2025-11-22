# Ollama Chat Interface

A modern Windows desktop chat application for interacting with Ollama LLM models. Built with .NET 8 and WPF, featuring a clean interface inspired by Claude and ChatGPT.

## Features

- **Modern Dark Theme UI** - Clean, responsive interface similar to ChatGPT/Claude
- **Multiple Chat Support** - Create and manage multiple conversations
- **Project Organization** - Organize chats into projects and subdirectories
- **File Attachments** - Upload and attach files to your messages (including images for vision models)
- **Artifact Storage** - Automatically extracts and stores code blocks from responses
- **Configurable Ollama Connection** - Connect to local or remote Ollama servers
- **Streaming Responses** - Real-time streaming of model responses
- **Model Selection** - Choose from available models on your Ollama server
- **Persistent Storage** - All chats and artifacts stored locally in SQLite

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Ollama](https://ollama.ai/) running locally or on a remote server

## Installation

### From Source

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd chat-interface
   ```

2. Build the application:
   ```bash
   dotnet build OllamaChat.sln -c Release
   ```

3. Run the application:
   ```bash
   dotnet run --project OllamaChat/OllamaChat.csproj
   ```

### Publish as Standalone

To create a self-contained executable:

```bash
dotnet publish OllamaChat/OllamaChat.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

The executable will be in the `./publish` folder.

## Configuration

### appsettings.json

The application can be configured via `appsettings.json` located in the application directory:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3.2",
    "TimeoutSeconds": 300,
    "StreamResponses": true,
    "DefaultOptions": {
      "Temperature": 0.7,
      "NumCtx": 4096,
      "TopP": 0.9,
      "TopK": 40,
      "RepeatPenalty": 1.1
    }
  }
}
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `BaseUrl` | URL of your Ollama server | `http://localhost:11434` |
| `DefaultModel` | Default model for new chats | `llama3.2` |
| `TimeoutSeconds` | Request timeout in seconds | `300` |
| `StreamResponses` | Enable streaming responses | `true` |
| `Temperature` | Model creativity (0.0-2.0) | `0.7` |
| `NumCtx` | Context window size | `4096` |
| `TopP` | Nucleus sampling | `0.9` |
| `TopK` | Top-K sampling | `40` |
| `RepeatPenalty` | Repetition penalty | `1.1` |

### Remote Ollama Server

To connect to a remote Ollama server:

1. Click the ⚙ (Settings) button in the sidebar
2. Enter the remote server URL (e.g., `http://192.168.1.100:11434`)
3. Click Save

Or edit `appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://your-server-ip:11434"
  }
}
```

**Note:** Ensure your Ollama server is configured to accept remote connections by setting `OLLAMA_HOST=0.0.0.0` on the server.

## Usage

### Creating a New Chat

1. Click the **+ New Chat** button in the sidebar
2. Select a model from the dropdown in the header
3. Type your message and press Enter or click Send

### Organizing Chats

- **Projects**: Create projects to organize related chats
- **Search**: Use the search box to find chats by title or content
- **Recent Chats**: Recently accessed chats appear in the sidebar

### File Attachments

1. Click the 📎 (paperclip) button next to the input
2. Select one or more files
3. Files will be attached to your next message

**Supported for Vision Models:**
- Images: `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`

### Artifacts

The application automatically extracts code blocks from assistant responses:

- View artifacts in the right panel (click 📄 to toggle)
- Copy code with the 📋 button
- Save to file with the 💾 button

## Data Storage

All data is stored locally in:
- **Windows**: `%LOCALAPPDATA%\OllamaChat\`

Contents:
- `ollama_chat.db` - SQLite database with chats and messages
- `Uploads/` - Uploaded file attachments
- `Artifacts/` - Saved artifacts

## Architecture

The application follows the MVVM pattern:

```
OllamaChat/
├── Models/           # Data models
├── ViewModels/       # View models (MVVM)
├── Views/            # WPF views (XAML)
├── Services/         # Business logic services
├── Data/             # Database context
├── Converters/       # WPF value converters
└── Resources/        # Styles and colors
```

### Key Technologies

- **.NET 8** - Application framework
- **WPF** - Windows UI framework
- **Entity Framework Core** - Database ORM
- **SQLite** - Local database
- **CommunityToolkit.Mvvm** - MVVM framework
- **Markdig** - Markdown rendering

## Troubleshooting

### "Disconnected" Status

1. Ensure Ollama is running (`ollama serve`)
2. Check the server URL in settings
3. Click the ↻ button to refresh connection

### No Models Available

1. Install models via Ollama CLI: `ollama pull llama3.2`
2. Click refresh to reload available models

### Slow Responses

- Reduce context length in settings
- Use a smaller model
- Ensure adequate system resources

## License

MIT License - See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit pull requests.
