using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static sofia.Program;

namespace sofia
{
    [Group("highlight"), CommandClass("HighlightCommands"),  Description("Have the Bot DM you when one of your set keywords is used in chat"), Aliases("highlighter")]
    public class Highlightcommands : BaseCommandModule
    {
        [Command("removekey"), CommandClass("HighlightCommands"), Description("Removes a key from your collection\n\nUsage:\n```=highlight removekey <keyname>```"), Aliases("remove", "rm", "rmk"), RequireExcludent()]
        public async Task RemoveKey(CommandContext e,[RemainingText] string keyname)
        {
            try
            {
                if(keywords.ContainsKey(keyname.ToLower()))
                {
                    foreach(KeyValuePair<string, List<string>> kvp in keywords)
                    {
                        if (kvp.Key.ToLower() == keyname.ToLower() && keywords[kvp.Key].Contains(e.Message.Author.Id.ToString()))
                        {
                            keywords[kvp.Key].Remove(e.Message.Author.Id.ToString());
                            await e.Message.RespondAsync(new DiscordEmbedBuilder { Description = $"Removed key {kvp.Key} from {e.Message.Author.Username} list of trigger words", Color = DiscordColor.Green});
                            if(kvp.Value.Count == 0)
                            {
                                keywords.Remove(keyname.ToLower());
                            }
                            File.WriteAllText("jsons/HL/keyset.json", Newtonsoft.Json.JsonConvert.SerializeObject(keywords));
                            break;
                        }
                    }
                }
                else
                {
                    await e.Message.RespondAsync(new DiscordEmbedBuilder { Description = $"The key {keyname} does not exist", Color = DiscordColor.Red});
                }
            }
            catch(Exception ex)
            {
                await AlertException(e, ex);
            }
        }

        [Command("exclude"), CommandClass("HighlightCommands"), Description("Blocks/Unblocks a channel from triggering the highlighter for you\n\nUsage:\n```=highlight exclude < ID / #mention >```"), Aliases("excluse", "unexclude", "unexcluse"),  RequireExcludent()]
        public async Task Exclude(CommandContext e, DiscordChannel ch)
        {
            try
            {
                if(!channelblock.ContainsKey(e.Message.Author.Id))
                {
                    channelblock.Add(e.Message.Author.Id, new List<ulong> { ch.Id });
                    await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"The channel {ch.Mention} ({ch.Id}) has been excluded"});
                }
                else
                {
                    if(channelblock[e.Message.Author.Id].Contains(ch.Id))
                    {
                        channelblock[e.Message.Author.Id].Remove(ch.Id);
                        if(channelblock[e.Message.Author.Id].Count == 0)
                        {
                            channelblock.Remove(e.Message.Author.Id);
                        }
                        await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"The channel {ch.Mention} ({ch.Id}) has been removed from the exclusion list"});
                    }
                    else
                    {
                        await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"The channel {ch.Mention} ({ch.Id}) has been excluded"}); 
                        channelblock[e.Message.Author.Id].Add(ch.Id);
                    }
                }
                File.WriteAllText("jsons/HL/channelblock.json", Newtonsoft.Json.JsonConvert.SerializeObject(channelblock));
            }
            catch (System.Exception ex)
            {
                await AlertException(e, ex);
            }
        }
        [Command("listkeys"), CommandClass("HighlightCommands"), Description("Sends a human-readable list of your keywords\n\nUsage:\n```=highlight listkeys```The message will get deleted after 10 seconds"), Aliases("list", "lk"),  RequireExcludent()]
        public async Task ListKeys(CommandContext e)
        {
            try
            {
                string outtext = "";
                int i = 1;
                foreach(KeyValuePair<string, List<string>> kvp in keywords)
                {
                    if(kvp.Value.Contains(e.Message.Author.Id.ToString()))
                    {
                        outtext += $"`{i++}: {kvp.Key}`\n";
                    }
                }
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Title = $"Keys for user {e.Message.Author.Username}", Description = outtext, Color = DiscordColor.Green };
                DiscordMessage resmsg = await e.Message.RespondAsync(embed);
                DelInSeconds(resmsg, 10);
                DelInSeconds(e.Message, 10);
            }
            catch (Exception ex)
            {
                await AlertException(e, ex);
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }

        [Command("addkey"), CommandClass("HighlightCommands"), Description("Adds a new key to the highlighter feature\n\nUsage:\n```=highlight addkey <keyname>```"), Aliases("add", "ak"),  RequireExcludent()]
        public async Task AddKey(CommandContext e, [RemainingText] string keyname)
        {
            try
            {
                if(!keywords.ContainsKey(keyname.ToLower()))
                {
                    List<string> userskey = new List<string>();
                    userskey.Add(e.Message.Author.Id.ToString());
                    keywords.Add(keyname.ToLower(), userskey);
                }
                else
                {
                    keywords[keyname].Add(e.Message.Author.Id.ToString());
                }
                File.WriteAllText("jsons/HL/keyset.json", Newtonsoft.Json.JsonConvert.SerializeObject(keywords));
                DiscordMessage resmsg = await e.Message.RespondAsync(new DiscordEmbedBuilder {Description = $"Key {keyname} added to user {e.Message.Author.Username}s list of trigger words", Color = DiscordColor.Green});
                DelInSeconds(resmsg, 5);
                DelInSeconds(e.Message, 5);
            }
            catch (Exception ex)
            {
                await AlertException(e, ex);
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }

        // HIDDENCMDS
        [Command("sendkeys"), CommandClass("HighlightCommands"),  RequireAuth(), Description("Sends a serialized collection of all keys in the set (owner only for debug purposes)"), Hidden(), Aliases("send", "sk")]
        public async Task SendKey(CommandContext e, bool delete = true)
        {
            try
            {
                DiscordMessage resmsg = await e.Message.RespondAsync(new DiscordEmbedBuilder {Title = "Bot owner command", Description = Newtonsoft.Json.JsonConvert.SerializeObject(keywords)});
                if(delete == true) { DelInSeconds(resmsg, 5); DelInSeconds(e.Message, 5); }
            }
            catch (Exception ex)
            {
                await AlertException(e, ex);
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }
        
        [Command("sendexclude"), CommandClass("HighlightCommands"),  RequireAuth(), Description("Sends a serialized collection of all excluded channels and users (owner only for debug purposes)"), Hidden(), Aliases("sx", "sendex")]
        public async Task SendExclude(CommandContext e, bool delete = true)
        {
            try
            {
                DiscordMessage resmsg = await e.Message.RespondAsync(new DiscordEmbedBuilder {Title = "Bot owner command", Description = File.ReadAllText("jsons/HL/channelblock.json")});
                if(delete == true) { DelInSeconds(resmsg, 5); DelInSeconds(e.Message, 5); }
            }
            catch (Exception ex)
            {
                await AlertException(e, ex);
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }

        [Command("insertnewjson"), CommandClass("HighlightCommands"),  RequireAuth(), Description("With this command you (I) can insert a new json string for the keyset"), Hidden(), Aliases("insertjson", "insertnew", "inj")]
        public async Task InsertJson(CommandContext e, [Description("The new json string to insert")] [RemainingText] string newjson)
        {
            try
            {
                keywords = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(newjson);
                File.WriteAllText("jsons/HL/keyset.json", newjson);
                DiscordMessage resmsg = await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Title = "Bot owner command", Description = ":white_check_mark: Json keylist updated"});
                DelInSeconds(resmsg, 5);
                DelInSeconds(e.Message, 5);
            }
            catch (Exception ex)
            {
                await AlertException(e, ex);
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }
    }
}