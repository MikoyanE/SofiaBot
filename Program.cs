using System;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace sofia
{
    public class Program
    {
        public static CommandsNextExtension commands;
        public static DiscordClient discord;
        public static DiscordActivity g1;
        public static ulong LastHb = 0; // Last heartbeat message
        public static Dictionary<string, List<string>> keywords; // THe highlighter keywords ( UserId : { Keywords } )
		public static Dictionary<ulong, List<ulong>> channelblock; // The channelblock list for highlighter ( ChannelId : {UserIds})
        public static List<ulong> exclude; // List of users banned fom using the bot
        public static Dictionary<ulong, Dictionary<ulong, int>> xplist; // The list of xps ( GuildId : { UserId : xp } )
        public static Dictionary<ulong, Dictionary<ulong, DateTime>> timedoutedusers; // List of users in xp timeout ( GuildId : { UserId : TimeoutTime } )
		public static Dictionary<ulong, List<LevelRole>> lvlroles; // The list of level roles ( GuildId : { LevelRoles } )
        public static Dictionary<ulong, List<ulong>>  channelxpexclude; // The list of channels excluded from gaining xp ( GuildId : { ChannelIds } ) <-- GuildId is not needed, maybe remove
        public static DiscordWebhookClient webh = new DiscordWebhookClient(); // The webhook for the message link embeds
        public static SetupInfo cInf; // The setup info
		public static CommandsNextConfiguration cNcfg; // The commanddsnext config
		public static DiscordConfiguration dCfg; // The discord config

		static void Main(string[] args)
        {
            try
            {
                // Highlight
                keywords = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText("jsons/HL/keyset.json"));
                channelblock = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, List<ulong>>>(File.ReadAllText("jsons/HL/channelblock.json"));
                // XP
                xplist = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<ulong, int>>>(File.ReadAllText("jsons/xp/xp.json"));
                lvlroles = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, List<LevelRole>>>(File.ReadAllText("jsons/xp/levelroles.json"));
                channelxpexclude = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, List<ulong>>>(File.ReadAllText("jsons/xp/channelblock.json"));
                // System
                exclude = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("jsons/sys/exclude.json"));
                cInf = Newtonsoft.Json.JsonConvert.DeserializeObject<SetupInfo>(File.ReadAllText("jsons/sys/setupinfo.json"));
                if(cInf.Token == "")
                {
                    throw new Exception("Please enter your bot token in the setupinfo.json file");
                }
				if (cInf.ErrorHbChannel == 0)
                {
                    throw new Exception("Please enter the ID of your heartbeat and error channel in the setupinfo.json file");
                }
                if(cInf.Prefixes.Count == 0)
                {
                    throw new Exception("Please enter at least one prefix in the setupinfo.json file");
                }
				cNcfg = new CommandsNextConfiguration
				{
					StringPrefixes = cInf.Prefixes,
					CaseSensitive = false,
					EnableDefaultHelp = true,
					DefaultHelpChecks = new List<CheckBaseAttribute>()
				};
                dCfg = new DiscordConfiguration
                {
                    Token = cInf.Token,
                    TokenType = TokenType.Bot
                };

                g1 = new DiscordActivity($"Slava Ukraini :3 | {cInf.Prefixes[0]}help", ActivityType.Watching);

                timedoutedusers = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
            }
            catch(Exception ex)
            {
                var a = ex.GetType();
                if(ex.GetType() == typeof(System.IO.FileNotFoundException))
                {
                    Console.WriteLine("The json files are missing, please add all files required and try again, or add them from the repository \n\t- jsons/HL/Keyset.json \n\t- jsons/HL/channelblock.json \n\t- jsons/xp/xp.json \n\t- jsons/xp/levelroles.json \n\t- jsons/xp/channelblock.json \n\t- jsons/sys/exclude.json \n\t- jsons/sys/setupinfo.json");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
                Environment.Exit(0);
            }
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string [] args)
        {
            try
            {
                discord = new DiscordClient(dCfg);
                commands = discord.UseCommandsNext(cNcfg);

                // ExtensionS
                commands.RegisterCommands<Commands>();
                commands.RegisterCommands<BotAdminCommands>();
                commands.RegisterCommands<Highlightcommands>();
                commands.RegisterCommands<LevelCommands>();
                commands.CommandErrored += CmdErrorHandler;
				commands.SetHelpFormatter<CustomHelpFormatter>();

                // EVENTS
                discord.MessageCreated += async (client, msg) =>
                {
                    LevelSystem.DoTheTimer(msg);
                    
                    if (msg.Message.Content.Contains("discord.com/channels/") && msg.Message.Author.IsBot == false) // this doesn't quite work yet
                    {
                        await CrossEmbed.LinkPostedEvent(msg);
                    }
                    
                    if (msg.Message.Author.IsBot == false && !msg.Message.Content.StartsWith("=highlight") && (!channelblock.ContainsKey(msg.Message.Author.Id) || !channelblock[msg.Message.Author.Id].Contains(msg.Message.Channel.Id)))
                    {
                        await Highlighter.KeywordSent(msg);
                    }

                    if(msg.Message.Content.ToLower() == $"<@!{discord.CurrentUser.Id}> bad bot")
                    {
                        await discord.SendMessageAsync(await discord.GetChannelAsync(msg.Channel.Id), ":("); // Lol idk why I left this in here but it's kinda cute so like
                    }
					if (msg.Message.Content.ToLower() == $"<@!{discord.CurrentUser.Id}> good bot")
                    {
                        await discord.SendMessageAsync(await discord.GetChannelAsync(msg.Channel.Id), ":)");
                    }
                };

                discord.SocketClosed += async (client, e) =>
                {
                    await discord.ReconnectAsync();
                };

                await discord.ConnectAsync();
                await SendHeartbeatAsync().ConfigureAwait(false);
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                try
                {
                    Console.WriteLine("CONNECTION TERMINATED\nAttempting automatic restart...");
                    File.WriteAllText("Error.log", ex.ToString());
                    Main(args);
                }
                catch
                {
                    Console.WriteLine("Automatic restart failed.");
                }
            }
        }
        
        public static async Task AlertException(CommandContext e, Exception ex)
        {
			await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "An error occured" });
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
		}

        public static async Task AlertException(MessageCreateEventArgs e, Exception ex)
        {
			await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "An error occured" });
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
		}

        public static async Task AlertException(MessageReactionAddEventArgs e, Exception ex)
        {
			await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "An error occured" });
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
		}

        public static async Task AlertException(Exception ex)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex));
			await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
		}

        public static async void DelInSeconds(DiscordMessage e, int seconds = 5)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            await e.DeleteAsync();
        }
        public static async Task CmdErrorHandler(CommandsNextExtension _, CommandErrorEventArgs e)
        {
            try
            {
                var failedChecks = ((DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)e.Exception).FailedChecks;
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = "Command couldn't execute D:\nHere's why:" };
                bool canSend = true;
                foreach (var failedCheck in failedChecks)
                {
                    if (failedCheck is RequireBotPermissionsAttribute)
                    {
                        var botperm = (RequireBotPermissionsAttribute)failedCheck;
                        embed.AddField("My Required Permissions", $"```{botperm.Permissions.ToPermissionString()}```");
                        if(botperm.Permissions.HasFlag(Permissions.SendMessages))
                        {
                            canSend = false;
                        }
                    }
                    if (failedCheck is RequireUserPermissionsAttribute)
                    {
                        var botperm = (RequireUserPermissionsAttribute)failedCheck;
                        embed.AddField("Your Required Permissions", $"```{botperm.Permissions.ToPermissionString()}```");
                    }
                    if(failedCheck is RequireGuildAttribute)
                    {
                        RequireGuildAttribute guild = (RequireGuildAttribute)failedCheck;
                        embed.AddField("Server only", "This command can not be used in DMs.");
                    }
                }
                if(canSend == true)
                {
                    await e.Context.Message.RespondAsync(embed);
                }
                else
                {
                    await e.Context.Guild.Owner.SendMessageAsync("I can't send messages in your server but I'm lacking perms to work so have this list in DMs instead", embed);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }
        public static async Task SendHeartbeatAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"Heartbeat received!\n{discord.Ping.ToString()}ms"};
                    int ping = discord.Ping;
                    embed.WithFooter($"Today at [{System.DateTime.UtcNow.ToShortTimeString()}]");
                    if (ping < 200)
                    {
                        embed.Color = DiscordColor.Green;
                    }
                    else if (ping < 500)
                    {
                        embed.Color = DiscordColor.Orange;
                    }
                    else
                    {
                        embed.Color = DiscordColor.Red;
                    }
                    DiscordMessage msghb = null;
                    msghb = await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), embed);

                    
                    await discord.UpdateStatusAsync(g1);
                    Console.WriteLine($"{System.DateTime.UtcNow.ToShortTimeString()} Ping: {discord.Ping}ms ");
                    if(LastHb != 0)
                    {
                        try
                        {
                            DiscordChannel hbch = await discord.GetChannelAsync(cInf.ErrorHbChannel);
                            DiscordMessage hbmsg = await hbch.GetMessageAsync(LastHb);
                            await hbmsg.DeleteAsync();
                        }
                        catch { }
                    }
                    LastHb = msghb.Id;
                    foreach(KeyValuePair<ulong, Dictionary<ulong, DateTime>> kvp2 in timedoutedusers)
                    {
                        foreach(KeyValuePair<ulong, DateTime> kvp in timedoutedusers[kvp2.Key])
                        {
                            if(DateTime.Now - kvp.Value >= cInf.XpInfo.CoolDown)
                            {
                                timedoutedusers.Remove(kvp.Key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), $"Failed to heartbeat\n\n{ex.ToString()}");
                }
                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }
    }
    
    public class SetupInfo
    {
        public string Token { get; set; }
        public ulong ErrorHbChannel { get; set; }
        public List<string> Prefixes { get; set; }
        public string DiscordInvite { get; set; } = "https://discord.gg/uPBkBeyM86";
        public string GitHub { get; set; } = "https://duckduckgo.com/?t=ffab&q=coming+soon";
        public XpInfo XpInfo { get; set; } = new XpInfo();
    }
	public class XpInfo
	{
		public int MinXp { get; set; } = 10;
		public int MaxXp { get; set; } = 20;
		public TimeSpan CoolDown { get; set; } = TimeSpan.FromMinutes(2);
	}

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireExcludentAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext e, bool help)
        {
            List<ulong> excludedusers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("jsons/exclude.json"));
            return Task.FromResult(!excludedusers.Contains(e.Message.Author.Id));
        }
    }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	public class CommandClassAttribute : System.Attribute
	{
		public string classname { get; set; }
        public CommandClassAttribute(string e)
        {
            classname = e;
        }
	}
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAuthAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext e, bool help)
        {
            List<ulong> authorizedusers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("jsons/auth.json"));
            return Task.FromResult(authorizedusers.Contains(e.Message.Author.Id));
        }
    }
}
