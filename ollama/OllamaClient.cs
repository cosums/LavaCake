using System.Text;
using Discord;
using Discord.Commands;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace LavaCake.Ollama;

public class OllamaClient
{
    public static readonly OllamaClient Instance = new ();
    private readonly OllamaApiClient _ollama;

    private OllamaClient()
    {
        _ollama = new OllamaApiClient("http://localhost:11434");
        _ollama.SelectedModel = "gemma3:12b";
    }

    public async Task<string> GenerateOneOffAsync(string userPrompt)
    {
        var chat = new Chat(_ollama);

        var sb = new StringBuilder();
        await foreach (var token in chat.SendAsync(userPrompt))
        {
            sb.Append(token);
        }
        return sb.ToString();
    }

    public async Task<string> GenerateOneOffAsync(string systemPrompt, string userPrompt)
    {
        var chat = new Chat(_ollama, systemPrompt);

        var sb = new StringBuilder();
        await foreach (var token in chat.SendAsync(userPrompt))
        {
            sb.Append(token);
        }
        return sb.ToString();
    }

    public async Task<string> GenerateDiscordResponse(string systemPrompt, string userPrompt, SocketCommandContext context)
    {
        Chat chat = new Chat(_ollama, systemPrompt)
        {
            Options = new OllamaSharp.Models.RequestOptions
            {
                NumPredict = 2048,
                NumCtx = 8192
            }
        };
        
        int tokenCount = systemPrompt.Length / 4;
        tokenCount += userPrompt.Length / 4;

        var messages = context.Channel.GetMessagesAsync(context.Message, Direction.Before, 50);
        List<Message> discordMessages = new();
        
        bool full = false;
        await foreach (var batch in messages)
        {
            if (full) break;
            foreach (var message in batch)
            {
                Message modelMessage;
                int estimatedTokens = 0;

                if (message.Author.Id == context.Client.CurrentUser.Id)
                {
                    string chatMessage = message.Content;
                    
                    if (message.Embeds.Count > 0)
                    {
                        chatMessage = message.Embeds.First().Description;
                    }
                    
                    estimatedTokens = chatMessage.Length / 4;

                    modelMessage = new Message(ChatRole.Assistant, chatMessage);
                } 
                else
                {
                    string text = message.Content;
                    string author = message.Author.Username;
                    string timestamp = message.Timestamp.ToString();

                    string chatMessage = $"[timestamp: {timestamp}][author: {author}][message: {text}]";
                    estimatedTokens = chatMessage.Length / 4;

                    modelMessage = new Message(ChatRole.User, chatMessage);
                }

                discordMessages.Add(modelMessage);
                tokenCount += estimatedTokens;
                if (tokenCount >= 4000) { full = true; break; }
            }
        }

        discordMessages.Reverse();
        chat.Messages.AddRange(discordMessages);

        var sb = new StringBuilder();
        await foreach (var token in chat.SendAsync(userPrompt))
        {
            sb.Append(token);
        }
        return sb.ToString();
    }
}