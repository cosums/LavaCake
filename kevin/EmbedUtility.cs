using Discord;

namespace LavaCake.Kevin;

public static class EmbedUtility
{
    public static Embed KevinEmbed(string description)
    {
        EmbedBuilder embedBuilder = new();
        embedBuilder.Description = description;
        embedBuilder.Color = Color.Red;
        return embedBuilder.Build();
    }
}
