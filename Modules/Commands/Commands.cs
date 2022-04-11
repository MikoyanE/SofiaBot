using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using static sofia.Program;

namespace sofia
{
    public class Commands : BaseCommandModule
    {
        [Command("fancytext"), CommandClass("OtherCommands"), Description("Produces fancy text\n\nUsage:\n```=fancy <type> <text>```\nTo list available types, replace type with `help` or `types`"), RequireExcludent()]
        public async Task Fancy(CommandContext e, string type, [RemainingText] string text)
		{
			try
			{
                Dictionary<string, Dictionary<string, string>> Types = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText("jsons/other/fancy.json"));
                if(type != "help" && type != "types" && type != "flipped" && type != "mirror")
                {
                    string finText = "";
                    foreach(char a in text)
                    {
                        try
                        {
                            finText += Types[type][a.ToString()];
                        }
                        catch
                        {
                            finText += a;
                        }
                    }
                    await e.Message.RespondAsync(finText);
                }
                else
                {
                    if(type == "flipped" || type == "mirror")
                    {
                        string finText = "";
                        foreach(char a in text)
                        {
                            try
                            {
                                finText += Types[type][a.ToString()];
                            }
                            catch
                            {
                                finText += a;
                            }
                        }
                        char[] finRev = finText.ToCharArray();
                        Array.Reverse(finRev);
                        finText = "";
                        foreach(char a in finRev)
                        {
                            finText += a;
                        }
                        await e.Message.RespondAsync(finText);
                    }
                    else
                    {
                        string Out = "";
                        foreach(KeyValuePair<string, Dictionary<string, string>> kvp in Types)
                        {
                            string ex = "looks like this";
                            string exOut = "";
                            foreach(char a in ex)
                            {
                                try
                                {
                                    exOut += Types[kvp.Key][a.ToString()];
                                }
                                catch
                                {
                                    exOut += a;
                                }
                            }
                            Out += $"`{kvp.Key}` : {exOut}\n";
                        }
                        await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = Out });
                    }
                }
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}

        [Command("ping"), CommandClass("OtherCommands"), Description("Displays the bots ping (it doesn't actually mean anything to you)\n\nUsage:\n```=ping```"), RequireExcludent()]
        public async Task Ping(CommandContext e)
        {
            try
            {
                DiscordEmbedBuilder d = new DiscordEmbedBuilder { Color = discord.Ping >= 500 ? DiscordColor.Red : discord.Ping >= 250 ? DiscordColor.Yellow : DiscordColor.Green, Description = $"`{discord.Ping.ToString()}`ms" };
                var b = await e.Message.RespondAsync("Pinging...", d);
                var a = DateTime.Now;
                await b.ModifyAsync(x => x.Content = $"Pong!");
                d.Description += $"\n{(DateTime.Now - a).TotalMilliseconds.ToString()}ms";
				await b.ModifyAsync(embed: (DiscordEmbed)d);
            }
            catch (Exception ex)
            {
				await AlertException(e, ex);
			}
        }
    }
}
