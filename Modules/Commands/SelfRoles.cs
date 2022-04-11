using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using static sofia.Program;

namespace sofia
{
    [Group("selfrole"), Description("Provides subcommands for selfroles")]
    public class SelfRoleCommands : BaseCommandModule
    {
        [Command("add"), CommandClass("SelfRoleCommands"), RequireExcludent(), Hidden(), RequireAuth()] // WIP Feature
        public async Task SelfAdd(CommandContext e, [Description("The selfrole to add")] [RemainingText] string role = "")
        {
            Dictionary<string, ulong[]> roles = JsonConvert.DeserializeObject<Dictionary<string, ulong[]>>(File.ReadAllText("jsons/other/selfroles.json"));
            
            if(roles[role][1] == 0)
            {
                foreach(DiscordRole userrole in e.Member.Roles)
                {
                    if(userrole.Id == roles[role][0])
                    {
                        await e.Member.RevokeRoleAsync(userrole);
                        break;
                    }
                }
                await e.Member.GrantRoleAsync(e.Guild.GetRole(roles[role][0]));
            }
            else
            {
                foreach(DiscordRole userrole in e.Member.Roles)
                {
                    if(userrole.Id == roles[role][0])
                    {
                        await e.Member.RevokeRoleAsync(e.Guild.GetRole(roles[role][0]));
                        break;
                    }
                }
            }
        }
    }
}