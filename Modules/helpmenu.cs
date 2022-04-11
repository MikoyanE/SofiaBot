using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Converters;
using static sofia.Program;
namespace sofia
{
	public class CustomHelpFormatter : BaseHelpFormatter
	{
		protected DiscordEmbedBuilder _embed;
		// protected StringBuilder _strBuilder;

		public CustomHelpFormatter(CommandContext ctx) : base(ctx)
		{
			_embed = new DiscordEmbedBuilder();
			
			// _strBuilder = new StringBuilder();

			// Help formatters do support dependency injection.
			// Any required services can be specified by declaring constructor parameters. 

			// Other required initialization here ...
		}

		public override BaseHelpFormatter WithCommand(Command command)
		{
			// _strBuilder.AppendLine($"{command.Name} - {command.Description}");
			_embed.Title = "Help";
			_embed.Color = DiscordColor.Green;
			if(string.IsNullOrEmpty(command.Description))
			{
				_embed.AddField(command.Name, "No description available.");
			}
			else
			{
				_embed.AddField(command.Name, command.Description);
			}
			if(command.Aliases.Count != 0)
			{
				string alstring = "";
				foreach(var alias in command.Aliases)
				{
					alstring += $"{alias} ";
				}
				_embed.AddField("Aliases", alstring);
			}
			var p = command.ExecutionChecks.ToList();
			string permstr = "";
			string permstr2 = "";
			foreach(var p1 in p)
			{
				if(p1.GetType() == typeof(RequireBotPermissionsAttribute))
				{
					permstr += ((RequireBotPermissionsAttribute)p1).Permissions.ToString() + " ";
				}
				if (p1.GetType() == typeof(RequireUserPermissionsAttribute))
				{
					permstr2 += ((RequireUserPermissionsAttribute)p1).Permissions.ToString() + " ";
				}
			}
			if(permstr != "")
			{
				permstr = $"**My permissions:** ```{permstr}```\n";
			}
			if(permstr2 != "")
			{
				permstr2 = $"**Your permissions:** ```{permstr2}```";
			}
			if(permstr != "" || permstr2 != "")
			{
				_embed.AddField("Permissions", permstr + permstr2);
			}
			_embed.WithFooter("<> are required arguments, [] are optional arguments");
			return this;
		}

		public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
		{
			_embed.Title = "Help";
			_embed.Description = "Listing all top-level commands and groups. You can get more information on a command using `=help <name of command>`";
			_embed.Color = DiscordColor.Green;
			string owneronlycommands = "";
			string othercommands = "";
			string selfrolecommands = "";
			string highlightcommands = $"`[Must be preceded by {sofia.Program.cInf.Prefixes[0]}highlight]`\n";
			string xpcommands = "";
			string leetcommands = "";
			List<string> e = new List<string>();
			foreach (var cmd in cmds)
			{
				var a = cmd.CustomAttributes.ToList().Find(x => x.GetType() == typeof(CommandClassAttribute));
				CommandClassAttribute t = (CommandClassAttribute)a;
				try
				{
					switch(t.classname)
					{
						case "OwnerCommands":
							owneronlycommands += $" `{cmd.Name}`";
							break;
						case "OtherCommands":
							othercommands += $" `{cmd.Name}`";
							break;
						case "SelfRoleCommands":
							selfrolecommands += $" `{cmd.Name}`";
							break;
						case "HighlightCommands":
							CommandGroup cmdg = (CommandGroup)cmd;
							cmdg.Children.ToList().ForEach(x => highlightcommands += $" `{x.Name}`");
							break;
						case "LevelCommands":
							xpcommands += $" `{cmd.Name}`";
							break;
						case "LeetCommands":
							leetcommands += $" `{cmd.Name}`";
							break;
						default:
							Console.WriteLine("err");
							break;
					}
				}
				catch
				{
					
				}
				// _strBuilder.AppendLine($"{cmd.Name} - {cmd.Description}");
			}
			_embed.AddField("Owner Commands", owneronlycommands, true);
			_embed.AddField("Other Commands", othercommands, true);
			//_embed.AddField("Self Role Commands", selfrolecommands);
			_embed.AddField("Highlight Commands", highlightcommands, true);
			_embed.AddField("Level Commands", xpcommands, true);
			_embed.AddField("Leet Commands", leetcommands, true);
			_embed.AddField("Auto Features", "Levelling | Message Link Embedding", false);
			_embed.AddField("Useful Links", $"[Support Discord]({cInf.DiscordInvite}) : Please report bugs or drop suggestions there :)\n[GitHub]({cInf.GitHub}) : Feel free to review the code or clone it to host your own\n[Bot Invite](https://discord.com/oauth2/authorize?client_id={discord.CurrentUser.Id}&scope=bot&permissions=805317632) : Invite the bot to your server", false);
			return this;
		}
		public override CommandHelpMessage Build()
		{
			return new CommandHelpMessage(embed: _embed);
			// return new CommandHelpMessage(content: _strBuilder.ToString());
		}
	}
}