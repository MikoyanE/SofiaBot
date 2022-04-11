using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using static sofia.Program;
namespace sofia
{
    public static class CrossEmbed
    {
        public static async Task LinkPostedEvent(MessageCreateEventArgs e)
        {
            try
            {
                DiscordWebhook webhook;
                DiscordMember a = await e.Message.Channel.Guild.GetMemberAsync(e.Message.Author.Id);
                if(!string.IsNullOrEmpty(a.Nickname)) { webhook = await e.Channel.CreateWebhookAsync(a.Nickname); } else { webhook = await e.Channel.CreateWebhookAsync(a.Username);}
                webh.AddWebhook(webhook);
                string enteredtext = e.Message.Content;
                string outtextheader = "";
                string link = enteredtext.Substring(enteredtext.IndexOf("https://discord.com/channels/"));
                ulong GuildID = Convert.ToUInt64(link.Substring(link.IndexOf("channels/") + 9, 18));

                if(GuildID != e.Guild.Id && discord.Guilds.ContainsKey(GuildID) || GuildID == 356823991215980544)
                {
                    link = link.Replace(Convert.ToString(GuildID), "");
                    ulong ChannelID = Convert.ToUInt64(link.Substring(link.IndexOf("channels/") + 10, 18));
                    link = link.Replace(Convert.ToString(ChannelID), "");
                    ulong MessageID = Convert.ToUInt64(link.Substring(link.IndexOf("channels/") + 11, 18));

                    if(string.IsNullOrEmpty(enteredtext.Substring(0, enteredtext.IndexOf("https://discord.com/channels/"))))
                    {
                        outtextheader += enteredtext.Substring(enteredtext.IndexOf("https://discord.com/channels/") + 85);
                    }
                    else
                    {
                        outtextheader += enteredtext.Substring(0, enteredtext.IndexOf("https://discord.com/channels/"));
                        outtextheader += enteredtext.Substring(enteredtext.IndexOf("https://discord.com/channels/") + 85);
                    }

                    DiscordChannel linkedchannel;
                    DiscordMessage linkedmessage;
                    DiscordEmbedBuilder embed;
                    DiscordGuild linkedguild;

                    embed = new DiscordEmbedBuilder();
                    linkedchannel = await discord.GetChannelAsync(ChannelID);
                    linkedguild = linkedchannel.Guild;
                    linkedmessage = await linkedchannel.GetMessageAsync(MessageID);
                    embed.WithFooter(linkedchannel.Name + "  |  " + linkedguild.Name + "  |  " + linkedmessage.CreationTimestamp.UtcDateTime.ToShortTimeString());

                    DiscordWebhookBuilder whmsg = new DiscordWebhookBuilder { Content = outtextheader, AvatarUrl = e.Message.Author.AvatarUrl};
                    embed.WithDescription(linkedmessage.Content);
                    embed.WithAuthor(linkedmessage.Author.Username, null, linkedmessage.Author.AvatarUrl);
                    whmsg.AddEmbed(embed);

                    if(linkedmessage.Attachments.Count > 0)
                    {
                        foreach(DiscordAttachment at in linkedmessage.Attachments)
                        {
                            DiscordEmbedBuilder embedattachment = new DiscordEmbedBuilder { ImageUrl = at.ProxyUrl, Description = $"[File Link]({at.ProxyUrl})" };
                            embedattachment.WithFooter($"{at.FileName} ({at.Width}x{at.Height} | {at.FileSize}B)");
                            whmsg.AddEmbed(embedattachment);
                        }
                    }

                    if(linkedmessage.Embeds.Count > 0)
                    {
                        foreach(DiscordEmbed emdmsg in linkedmessage.Embeds)
                        {
                            whmsg.AddEmbed(emdmsg);
                        }
                    }

                    await webh.BroadcastMessageAsync(whmsg);
                    await e.Message.DeleteAsync();
                }

                await webhook.DeleteAsync();
            }
            catch (Exception ex)
            {
                await discord.SendMessageAsync(await discord.GetChannelAsync(739501076880818267), Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
        }
    }
}