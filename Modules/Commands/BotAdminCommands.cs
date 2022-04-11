using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static sofia.Program;

namespace sofia
{
    public class BotAdminCommands : BaseCommandModule
    {
        [Command("addauth"), CommandClass("OwnerCommands"), Description("Adds/Removes a user to the list of bot admins\n\nUsage:\n```=addauth <ID / @mention >```"), RequireAuth()]
        public async Task AddAuth(CommandContext e, DiscordUser NewAdmin)
        {
            try
            {
                List<ulong> authorizedusers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("jsons/sys/auth.json"));
                DiscordMessage resmsg = await e.Message.RespondAsync(new DiscordEmbedBuilder {Color = DiscordColor.Red, Description = !authorizedusers.Contains(NewAdmin.Id) ? $"{NewAdmin.Mention} has been authorized" : $"{NewAdmin.Mention} has been unauthorized" } );
                DelInSeconds(resmsg);
                DelInSeconds(e.Message);
                if(!authorizedusers.Contains(NewAdmin.Id))
                {
                    authorizedusers.Add(NewAdmin.Id);
                }
                else
                {
                    authorizedusers.Remove(NewAdmin.Id);
                }
                File.WriteAllText("jsons/sys/auth.json", Newtonsoft.Json.JsonConvert.SerializeObject(authorizedusers));
            }
            catch (Exception ex)
            {
                await AlertException(e, ex);
            }
        }
        
        [Command("status"), CommandClass("OwnerCommands"), Description("Sets the bots status to a given text. \"clear\" to clear.\n\nUsage:\n```=status <New Status>```"), RequireAuth()]
        public async Task Status(CommandContext e, [RemainingText] string NewStatus)
        {
            try
            {
                if(NewStatus != "clear")
                {
                    g1.Name = NewStatus;
                    await discord.UpdateStatusAsync(g1);
                }
                else
                {
                    await discord.UpdateStatusAsync();
                }
                DiscordMessageBuilder msgb = new DiscordMessageBuilder();
                msgb.WithEmbed(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Updated status to {NewStatus}"});
                msgb.WithReply(e.Message.Id);
                await e.Message.RespondAsync(msgb);
            }
            catch (Exception ex)
            {
				await AlertException(e, ex);
			}
        }

        [Command("exclude"), CommandClass("OwnerCommands"), Description("Blacklists the specified user from using the bot\n\nUsage:\n```=exclude < ID / @mention >```"), Aliases("botban", "blacklist"), RequireAuth()]
        public async Task Exclude(CommandContext e, DiscordUser User)
        {
            List<ulong> excludedusers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("jsons/sys/exclude.json"));
            try
            {
                if(excludedusers.Contains(User.Id))
                {
                    excludedusers.Remove(User.Id);
                    await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"{User.Username}#{User.Discriminator} has been removed from the blacklist" });
                }
                else
                {
                    excludedusers.Add(User.Id);
                    await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"{User.Username}#{User.Discriminator} has been added to the blacklist" });
                }
                File.WriteAllText("jsons/sys/exclude.json", Newtonsoft.Json.JsonConvert.SerializeObject(excludedusers));
            }
            catch (Exception ex)
            {
				await AlertException(e, ex); 
            }
        }
    }
}