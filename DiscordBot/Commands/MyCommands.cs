using System.Net;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using HtmlAgilityPack;
using DSharpPlus.Lavalink;

namespace DiscordBot.Commands;

public class MyCommands : BaseCommandModule
{
    [Command("ping")]
    public async Task Ping(CommandContext ctx)
    {
        var embed = new DiscordEmbedBuilder
        {
            Title = "Ping",
            Description = "Pong!",
            Color = DiscordColor.Blue
        };
        await ctx.RespondAsync(embed);
    }

    [Command("time")]
    [Description("Returns current server time")]
    public async Task Time(CommandContext ctx)
    {
        var embed = new DiscordEmbedBuilder
        {
            Title = "Time command",
            Description = $"Current time: " + DateTime.Now.ToString("T"),
            Color = DiscordColor.Green
        };
        await ctx.RespondAsync(embed);
    }

    [Command("clear")]
    [Aliases("cls", "с", "сдуфк")]
    [Description("Deletes specified number of messages.")]
    [RequirePermissions(Permissions.ManageMessages)]
    public async Task DeleteMessagesCommand(CommandContext ctx,
        [Description("Number of messages to delete.")]
        int deleteNumber)
    {
        if (deleteNumber <= 0)
        {
            await ctx.RespondAsync("Delete number must be greater than zero!");
            return;
        }

        IReadOnlyList<DiscordMessage> messages;
        try
        {
            messages = await ctx.Channel.GetMessagesAsync(deleteNumber + 1);
        }
        catch (Exception)
        {
            await ctx.RespondAsync("Failed to fetch messages!");
            return;
        }

        try
        {
            await ctx.Channel.DeleteMessagesAsync(messages);
        }
        catch (Exception)
        {
            await ctx.RespondAsync("Failed to delete messages!");
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = "Clear command",
            Description = $"{deleteNumber} messages deleted successfully!",
            Color = DiscordColor.Red
        };

        var msg = await ctx.RespondAsync(embed);
        await Task.Delay(5000);

        try
        {
            await msg.DeleteAsync();
        }
        catch (Exception)
        {
            await ctx.RespondAsync("Failed to delete message!");
        }
    }

    [Command("getsteamprofile")]
    [Description("Retrieve and display information from a Steam profile.")]
    [Obsolete("Obsolete")]
    public async Task GetSteamProfile(CommandContext ctx, [Description("Steam username")] string steamUsername)
    {
        var url = $"https://steamcommunity.com/id/{steamUsername}";
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.UserAgent = "Mozilla/5.0";

        using var response = request.GetResponse();
        using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
        var html = await reader.ReadToEndAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var usernameNode = doc.DocumentNode.SelectSingleNode("//span[@class='actual_persona_name']");
        var username = usernameNode?.InnerText.Trim();

        var onlineStatusNode = doc.DocumentNode.SelectSingleNode("//div[@class='profile_in_game_header']");
        var onlineStatus = onlineStatusNode == null ? "Offline" : onlineStatusNode.InnerHtml.Trim();

        var profileAvatarNode = doc.DocumentNode.SelectSingleNode("//div[@class='playerAvatarAutoSizeInner']/img");
        var profileImage = profileAvatarNode.GetAttributeValue("src", "No Image Found");

        var countryNode = doc.DocumentNode.SelectSingleNode("//div[@class='profile_flag']");
        var country = countryNode == null ? "Not set" : countryNode.GetAttributeValue("title", "Not set");

        var memberSinceNode = doc.DocumentNode.SelectSingleNode("//div[@class='profile_created']/div");
        var memberSince = memberSinceNode == null ? "Not available" : memberSinceNode.InnerHtml.Trim();

        var levelNode = doc.DocumentNode.SelectSingleNode("//span[@class='friendPlayerLevelNum']");
        var level = levelNode == null ? "Not available" : levelNode.InnerHtml.Trim();

        var embed = new DiscordEmbedBuilder
        {
            Title = $"{username}'s Steam Profile",
            ImageUrl = profileImage,
            Color = DiscordColor.Green
        };

        embed.AddField("Online Status", onlineStatus);
        embed.AddField("Country", country);
        embed.AddField("Member Since", memberSince);
        embed.AddField("Profile Level", level);

        await ctx.RespondAsync(embed);
    }

    [Command("play")]
    [Aliases("p", "здфн")]
    public async Task Play(CommandContext ctx, [RemainingText] string input)
    {
        try
        {
            var userVc = ctx.Member!.VoiceState?.Channel;
            var lavalink = ctx.Client.GetLavalink();
            if (ctx.Member.VoiceState is null || userVc is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Play command",
                    Description = "Please join a voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (!lavalink.ConnectedNodes.Any())
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Play command",
                    Description = "Lavalink is not connected!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (userVc.Type is not ChannelType.Voice)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Play command",
                    Description = "You must be in a voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            var node = lavalink.ConnectedNodes.Values.First();
            await node.ConnectAsync(userVc);
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Play command",
                    Description = "Could not connect to voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            var search = await node.Rest.GetTracksAsync(input, LavalinkSearchType.SoundCloud);


            if (search.LoadResultType is LavalinkLoadResultType.NoMatches or LavalinkLoadResultType.LoadFailed ||
                !search.Tracks.Any())
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Play command",
                    Description = "Could not find the song!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            var music = search.Tracks.First();
            await conn.PlayAsync(music);

            var nowPlayEmbed = new DiscordEmbedBuilder()
            {
                Title = "Now playing",
                Description = music.Title,
                Color = DiscordColor.Purple,
                Url = music.Uri.ToString(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Requested by {ctx.Member.Username}",
                    IconUrl = ctx.Member.AvatarUrl
                }
            };

            await ctx.Channel.SendMessageAsync(nowPlayEmbed);
        }
        catch (Exception ex)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Play command",
                Description = $"An error occurred:\n{ex.Message}",
                Color = DiscordColor.Red
            };
            await ctx.RespondAsync(embed);
        }
    }

    [Command("pause")]
    public async Task Pause(CommandContext ctx)
    {
        try
        {
            var userVc = ctx.Member!.VoiceState?.Channel;
            var lavalink = ctx.Client.GetLavalink();
            if (ctx.Member.VoiceState is null || userVc is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Please join a voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (!lavalink.ConnectedNodes.Any())
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Lavalink is not connected!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (userVc.Type is not ChannelType.Voice)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "You must be in a voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            var node = lavalink.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Could not connect to voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (conn.CurrentState.CurrentTrack is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Nothing is playing!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            await conn.PauseAsync();
            var pausedEmbed = new DiscordEmbedBuilder()
            {
                Title = "Paused",
                Description = conn.CurrentState.CurrentTrack.Title,
                Color = DiscordColor.Red,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Requested by {ctx.Member.Username}",
                    IconUrl = ctx.Member.AvatarUrl
                }
            };
            await ctx.Channel.SendMessageAsync(pausedEmbed);
        }
        catch (Exception ex)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Pause command",
                Description = $"An error occurred:\n{ex.Message}",
                Color = DiscordColor.Red
            };
            await ctx.RespondAsync(embed);
        }
    }

    [Command("resume")]
    public async Task Resume(CommandContext ctx)
    {
        try
        {
            var userVc = ctx.Member!.VoiceState?.Channel;
            var lavalink = ctx.Client.GetLavalink();
            if (ctx.Member.VoiceState is null || userVc is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Please join a voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (!lavalink.ConnectedNodes.Any())
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Lavalink is not connected!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (userVc.Type is not ChannelType.Voice)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "You must be in a voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            var node = lavalink.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Could not connect to voice channel!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            if (conn.CurrentState.CurrentTrack is null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Pause command",
                    Description = "Nothing is playing!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed);
                return;
            }

            await conn.ResumeAsync();
            var pausedEmbed = new DiscordEmbedBuilder()
            {
                Title = "Paused",
                Description = conn.CurrentState.CurrentTrack.Title,
                Color = DiscordColor.Green,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Requested by {ctx.Member.Username}",
                    IconUrl = ctx.Member.AvatarUrl
                }
            };
            await ctx.Channel.SendMessageAsync(pausedEmbed);
        }
        catch (Exception ex)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Pause command",
                Description = $"An error occurred:\n{ex.Message}",
                Color = DiscordColor.Red
            };
            await ctx.RespondAsync(embed);
        }
    }

    [Command("попустить")]
    [Description("Assigns a random nickname from a list of words to a specific user")]
    [RequirePermissions(Permissions.ManageNicknames)]
    public async Task AssignRandomNick(CommandContext ctx, DiscordMember targetMember)
    {
        var wordList = new[]
        {
            "архипиздрит", "басран", "бздение", "бздеть", "бздех", "бзднуть", "бздун", "бздунья", "бздюха", "бикса",
            "блежник",
            "блудилище", "бляд", "блябу", "блябуду", "блядун", "блядунья", "блядь", "блядюга", "взьебка",
            "волосянка",
            "взьебывать", "вз'ебывать", "выблядок", "выблядыш", "выебать", "выеть", "выпердеть", "высраться",
            "выссаться", "говенка",
            "говенный", "говешка", "говназия", "говнецо", "говно", "говноед", "говночист", "говнюк", "говнюха",
            "говнядина",
            "говняк", "говняный", "говнять", "гондон", "дермо", "долбоеб", "дрисня", "дрист", "дристать", "дристун",
            "дристунья",
            "дристучник", "дристануть", "дристун", "дристуха", "дрочена", "дрочила", "дрочилка", "дрочить",
            "дрочка",
            "ебало", "ебальник", "ебануть", "ебаный", "ебарь", "ебатория", "ебать", "ебаться",
            "ебец", "ебливый", "ебля", "ебнуть", "ебнуться", "ебня", "ебун", "елда", "елдак", "елдачить",
            "заговнять", "задристать", "задрока", "заеба", "заебанец", "заебать", "заебаться", "заебываться",
            "заеть", "залупа", "залупаться", "залупить", "залупиться", "замудохаться", "засерун", "засеря",
            "засерать", "засирать", "засранец", "засрун", "захуячить", "злоебучий", "изговнять", "изговняться",
            "кляпыжиться", "курва", "курвенок", "курвин", "курвяжник", "курвяжница", "курвяжный", "манда",
            "мандавошка", "мандей", "мандеть", "мандища", "мандюк", "минет", "минетчик", "минетчица",
            "мокрохвостка", "мокрощелка", "мудак", "муде", "мудеть", "мудила", "мудистый", "мудня", "мудоеб",
            "мудозвон", "муйня", "набздеть", "наговнять", "надристать", "надрочить", "наебать", "наебнуться",
            "наебывать", "нассать", "нахезать", "нахуйник", "насцать", "обдристаться", "обдристаться", "обосранец",
            ""
        };
        var randomNick = wordList[new Random().Next(wordList.Length)];

        await targetMember.ModifyAsync(m => m.Nickname = randomNick);

        var embed = new DiscordEmbedBuilder
        {
            Title = "Nickname Changed",
            Description = $"{targetMember.Mention} your nickname has been changed to {randomNick}",
            Color = DiscordColor.Green
        };
        await ctx.RespondAsync(embed);
    }
}