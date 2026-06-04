using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace LavaCake.Kevin;

public class KevinService : BackgroundService
{
    private DiscordSocketClient _client = null!;
    private CommandService _commands = null!;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Kevin starting...");

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | 
                            GatewayIntents.GuildMessages |
                            GatewayIntents.MessageContent
        };

        _client = new DiscordSocketClient(config);
        _commands = new CommandService();

        _client.Log += Log;

        await InitCommands();

        var token = Environment.GetEnvironmentVariable("KEVIN_TOKEN");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        Console.WriteLine("Kevin logged in!");

        await Task.Delay(-1);
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task InitCommands()
    {
        _client.MessageReceived += HandleCommandAsync;

        // command will automatically find all modules in the code base, which is nice for me!
        await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        var msg = arg as SocketUserMessage;
        if (msg == null || msg.Author.Id == _client.CurrentUser.Id) return;

        var usr = arg.Author.GlobalName ?? arg.Author.Username;

        Console.WriteLine($"[{DateTime.Now}][{usr}][{msg}]");

        int argPos = 0;

        // determine if command, and make sure no bots are triggering
        if (!(msg.HasCharPrefix('!', ref argPos) || 
            msg.HasMentionPrefix(_client.CurrentUser, ref argPos)) || 
            msg.Author.IsBot)
            return;

        var context = new SocketCommandContext(_client, msg);

        await _commands.ExecuteAsync(
            context: context,
            argPos: argPos,
            services : null
        );
    }
}