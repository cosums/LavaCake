using Discord.Commands;

namespace LavaCake.Kevin;

public class PingModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Pings Kevin and LavaCake by extension.")]
    public async Task PingAsync()
    {
        await ReplyAsync("pong!");
    }
}