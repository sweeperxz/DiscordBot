
using DiscordBot.Commands;
using DiscordBot.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;

namespace DiscordBot;

internal abstract class Program
{
    private static DiscordClient? Client { get; set; }
    private static CommandsNextExtension? Commands { get; set; }

    public static async Task Main(string[] args)
    {
        JsonReader reader = new();
        await reader.ReadJsonAsync();

        ConfigureDiscordClient(reader);

        ConfigureCommandsNext(reader);

        var endpoint = new ConnectionEndpoint()
        {
            Hostname = "",
            Port = default,
            Secured = true,
        };

        var lavalinkConfiguration = new LavalinkConfiguration()
        {
            Password = "your password",
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint,
        };

        var lavalink = Client.UseLavalink();

        await Client!.ConnectAsync();
        await lavalink.ConnectAsync(lavalinkConfiguration);
        await Task.Delay(-1);
    }

    private static void ConfigureDiscordClient(JsonReader reader)
    {
        var discordConfig = new DiscordConfiguration()
        {
            Intents = DiscordIntents.All,
            Token = reader.Token,
            TokenType = TokenType.Bot,
            AutoReconnect = true
        };

        Client = new DiscordClient(discordConfig);
        Client.Ready += Client_Ready;
    }

    private static void ConfigureCommandsNext(JsonReader reader)
    {
        var commandConfig = new CommandsNextConfiguration()
        {
            StringPrefixes = new[] { reader.Prefix },
            EnableMentionPrefix = true,
            EnableDms = true,
            EnableDefaultHelp = false,
        };

        Commands = Client!.UseCommandsNext(commandConfig);
     
        Commands.RegisterCommands<MyCommands>();
    }

    private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
    {
        return Task.CompletedTask;
    }
}