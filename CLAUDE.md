# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Windows WPF desktop application for chatting with Ollama LLM models. Built with .NET 8, C#, and Entity Framework Core with SQLite.

## Build Commands

```bash
# Build
dotnet build OllamaChat.sln -c Release

# Run (debug)
dotnet run --project OllamaChat/OllamaChat.csproj

# Publish standalone executable
dotnet publish OllamaChat/OllamaChat.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

## Architecture

**Pattern:** MVVM using CommunityToolkit.Mvvm

**Key Components:**
- `MainViewModel.cs` - Central orchestration of chat functionality, message streaming, web search, artifacts
- `OllamaService` - Ollama API integration (streaming/non-streaming chat, model listing)
- `ChatService` - Data operations for projects, chats, messages, artifacts
- `WebSearchService` - DuckDuckGo web search integration
- `FileService` - File attachments and uploads
- `UserPreferencesService` - Settings persistence

**Entry Point:** `App.xaml.cs` sets up DI container, loads configuration, initializes database

**Data Storage:** SQLite at `%LOCALAPPDATA%\OllamaChat\`
- `ollama_chat.db` - Database
- `Uploads/` - File attachments
- `Artifacts/` - Saved code/artifacts

## Key Patterns

- `[ObservableProperty]` and `[RelayCommand]` attributes from CommunityToolkit.Mvvm
- Async/await with CancellationToken support throughout
- IAsyncEnumerable for streaming responses
- Interface-based DI (singleton for stateless services, scoped for DbContext, transient for ViewModels)
- Configuration via strongly-typed classes bound from `appsettings.json`

## Configuration

`appsettings.json` contains Ollama connection settings (BaseUrl, DefaultModel, streaming options) and WebSearch settings. Models are strongly typed: `OllamaConfig`, `WebSearchConfig`.
