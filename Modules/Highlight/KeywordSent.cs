using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using static sofia.Program;
namespace sofia
{
    public static class Highlighter
    {
        public static async Task KeywordSent(MessageCreateEventArgs e)
        {
            try
            {
                foreach(KeyValuePair<string, List<string>> kvp in keywords)
                {
                    if(e.Message.Content.ToLower().Contains(kvp.Key.ToLower()))
                    {
                        foreach(string user in kvp.Value)
                        {
                            DiscordMember member = await e.Guild.GetMemberAsync(Convert.ToUInt64(user));
                            if(member.PermissionsIn(e.Channel).HasFlag(Permissions.AccessChannels) && e.Message.Author.Id != member.Id)
                            {
                                IReadOnlyList<DiscordMessage> last5 = await e.Channel.GetMessagesAsync(10);
                                string desc = "";
                                for(int i = last5.Count - 1; i >= 0; i--)
                                {
                                    if(last5[i].Content.ToLower().Contains(kvp.Key.ToLower()))
                                    {
                                        desc += $"**[{last5[i].CreationTimestamp.UtcDateTime.ToShortTimeString()} UTC] {last5[i].Author.Username}: {last5[i].Content}**\n";
                                    }
                                    else
                                    {
                                        desc += $"[{last5[i].CreationTimestamp.UtcDateTime.ToShortTimeString()} UTC] {last5[i].Author.Username}: {last5[i].Content}\n";
                                    }
                                }
                                if(desc.Length > 2047)
                                {
                                    desc = desc.Remove(0, desc.Length - 1900);
                                    desc = desc.Insert(0, "**[...]**");
                                }
                                desc += $"[Jump](https://discord.com/channels/{e.Guild.Id}/{e.Channel.Id}/{e.Message.Id})";
                                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                                embed = new DiscordEmbedBuilder { Description = desc, Title = kvp.Key, Color = DiscordColor.Blurple};
                                await member.SendMessageAsync($"Triggered with the keyword **{kvp.Key}**", embed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }
    }
}