using Discord;
using Discord.Commands;
using LavaCake.Ollama;

namespace LavaCake.Kevin;

public class ChatModule : ModuleBase<SocketCommandContext>
{
    [Command("chat")]
    [Summary("Asks Kevin a question and prompts a response.")]
    public async Task ChatAsync([Remainder] [Summary("The chat message")] string msg)
    {
        Console.WriteLine("Received message, passing to model...");

        var usr = Context.User.GlobalName ?? Context.User.Username;

        string systemPrompt = $"""
        You are a chatbot designed for use in a discord server. Messages directed to you will contain the following format:
        [timestamp: timestamp][author: author][message: message]

        These two extra bits are context by which you may choose to format your message. Do not copy this format; simply respond as normal text to the message sent by the user.
        You may use timestamp information to contextualize your message. You may also use this to see how long it has been between messages.

        Please follow the following to flesh out your personality:
        """;

        string userPrompt = $"[timestamp: {DateTime.Now}][author: {usr}][message: {msg}]";
        string output;

        using (Context.Channel.EnterTypingState()) {
            output = await OllamaClient.Instance.GenerateDiscordResponse(systemPrompt, userPrompt, Context);
        }

        var embed = EmbedUtility.KevinEmbed(output);

        await Context.Message.ReplyAsync(embed: embed);
    }
}