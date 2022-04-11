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
using Newtonsoft.Json;
using static sofia.Program;
using System.Linq;
namespace sofia
{
	public static class LevelSystem
	{
		
		public static async void DoTheTimer(MessageCreateEventArgs e)
		{
			try
			{
				if(e.Channel.IsPrivate == false && lvlroles.ContainsKey(e.Guild.Id))
				{
					if(e.Message.Author.IsBot == false && !exclude.Contains(e.Message.Author.Id) &&  (!channelxpexclude.ContainsKey(e.Guild.Id) || !channelxpexclude[e.Guild.Id].Contains(e.Channel.Id)))
					{
						if (timedoutedusers.ContainsKey(e.Guild.Id))
						{
							if(timedoutedusers[e.Guild.Id].ContainsKey(e.Message.Author.Id))
							{
								if(DateTime.Now - timedoutedusers[e.Guild.Id][e.Message.Author.Id] >= cInf.XpInfo.CoolDown)
								{
									timedoutedusers[e.Guild.Id][e.Message.Author.Id] = DateTime.Now;
									AddXp(e);
								}
							}
							else
							{
								timedoutedusers[e.Guild.Id].Add(e.Message.Author.Id, DateTime.Now);
								AddXp(e);
							}
						}
						else
						{
							timedoutedusers.Add(e.Guild.Id, new Dictionary<ulong, DateTime> { { e.Message.Author.Id, DateTime.Now } });
							AddXp(e);
						}
						int userslevel = 0;
						int j = 0;
						bool isDone = false;
						try
						{
							if(xplist.ContainsKey(e.Guild.Id))
							{
								if(xplist[e.Guild.Id].ContainsKey(e.Message.Author.Id))
								{
									foreach (LevelRole i in lvlroles[e.Guild.Id])
									{
										if (i.XpReq <= xplist[e.Guild.Id][e.Author.Id] && i.RoleId != 0)
										{
											userslevel++;
											j++;
											if (!(await e.Guild.GetMemberAsync(e.Author.Id)).Roles.Contains(e.Guild.GetRole(i.RoleId)))
											{
												await (await e.Guild.GetMemberAsync(e.Author.Id)).GrantRoleAsync(e.Guild.GetRole(i.RoleId));
												if(j == lvlroles[e.Guild.Id].Count - 1)
												{
													isDone = true;
													break;
												}
											}
										}
										else
										{
											if(i.RoleId != 0 && (await e.Guild.GetMemberAsync(e.Author.Id)).Roles.Contains(e.Guild.GetRole(i.RoleId)))
											{
												await (await e.Guild.GetMemberAsync(e.Author.Id))
												.RevokeRoleAsync(
													e
													.Guild
													.GetRole(
														lvlroles[e.Guild.Id][
															lvlroles[e.Guild.Id].FindIndex(
																x => x.RoleId == i.RoleId
															)
														]
														.RoleId
													)
												);
											}
										}
									}
									if(isDone == true)
									{
										await discord.SendMessageAsync(e.Channel, new DiscordEmbedBuilder { Description = $"**{e.Author.Mention}**'s level changed to level **{userslevel}**!", Color = DiscordColor.Green });
									}
								}
							}
						}
						catch(DSharpPlus.Exceptions.UnauthorizedException)
						{
							await e.Channel.SendMessageAsync("I don't have permission to manage roles!");
						}
					}
				}
			}
			catch(Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		public static void AddXp(MessageCreateEventArgs e, int amount = -1)
		{
			if (amount == -1)
			{
				amount = new Random().Next(cInf.XpInfo.MinXp, cInf.XpInfo.MaxXp + 1);
			}
			if (xplist.ContainsKey(e.Guild.Id))
			{
				if (xplist[e.Guild.Id].ContainsKey(e.Message.Author.Id))
				{
					xplist[e.Guild.Id][e.Message.Author.Id] += amount;
				}
				else
				{
					xplist[e.Guild.Id].Add(e.Message.Author.Id, amount);
				}
			}
			else
			{
				Dictionary<ulong, int> newxp = new Dictionary<ulong, int>();
				newxp.Add(e.Message.Author.Id, amount);
				xplist.Add(e.Guild.Id, newxp);
			}
			File.WriteAllText("jsons/xp/xp.json", Newtonsoft.Json.JsonConvert.SerializeObject(xplist));
		}
	}

	public class LevelCommands : BaseCommandModule
	{
		[Command("lvlroles"), CommandClass("LevelCommands"), RequireGuild(), Description("Displays the level roles with their required score\n\nUsage:\n```=lvlroles```"),  RequireExcludent, RequireBotPermissions(Permissions.SendMessages)]
        public async Task LvlRoles(CommandContext e)
        {
			try
			{
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Color = DiscordColor.Green, Title = $"Level Roles for {e.Guild.Name}", Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = e.Guild.IconUrl } };
				if(lvlroles.ContainsKey(e.Guild.Id))
				{
					List<LevelRole> roles = lvlroles[e.Guild.Id];
					int i = 0;
					string embedstring = "";
					if (roles.Count() == 0)
					{
						embedstring += "No roles are bound to levels so far.";
					}
					else
					{
						foreach (LevelRole kvp in lvlroles[e.Guild.Id])
						{
							if(kvp.XpReq != 0)
							{
								embedstring += $"**`[{i + 1}]`** | <@&{kvp.RoleId}> (**{kvp.XpReq}**xp)\n";
								i++;
							}
						}
					}
					embed.AddField("Chat to earn XP to get these roles!", embedstring, true);
				}
				else
				{
					embed.Description = "No roles are bound to levels so far. Do so by using the `=lvledit` command.";
				}
				await discord.SendMessageAsync(await discord.GetChannelAsync(e.Message.Channel.Id), embed);
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
        }

        [Command("top"), CommandClass("LevelCommands"), RequireGuild(), Description("Displays the servers level leaderboard\n\nUsage:\n```=top [page, defaults to 1]```"), Aliases("lb"),  RequireExcludent]
        public async Task Leaderboard(CommandContext e, int page = 1)
        {
			try
			{
				if(xplist.ContainsKey(e.Guild.Id))
				{
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Page {page}/{Math.Ceiling((double)xplist[e.Guild.Id].Count/5)}"} , Color = DiscordColor.Green, Title = "Server XP leaderboard", Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = e.Guild.IconUrl } };
					var sortedleederboard = from entry in xplist[e.Guild.Id] orderby entry.Value descending select entry;
					
					string embedstring = "";
					int i = 0;
					foreach (KeyValuePair<ulong, int> kvp in sortedleederboard)
					{
						if(i >= (page -1) * 5)
						{
							int userslevel = 0;
							foreach (var j in lvlroles[e.Guild.Id])
							{
								if (j.XpReq <= xplist[e.Guild.Id][kvp.Key] && j.XpReq != 0)
								{
									userslevel++;
								}
							}
							string role = "";
							if(userslevel != 0)
							{
								role = $"<@&{lvlroles[e.Guild.Id][userslevel].RoleId.ToString()}>";
							}
							else
							{
								role = "No role";
							}
							if(kvp.Key != e.Message.Author.Id)
							{
								embedstring += $"**```#{i + 1} | {(await e.Guild.GetMemberAsync(kvp.Key)).Username}``` {kvp.Value}xp | [{role}]**\n\n";
							}
							else
							{
								embedstring += $"**```< #{i + 1} | {(await e.Guild.GetMemberAsync(kvp.Key)).Username} >```{kvp.Value}xp | [{role}]**\n\n";
							}
							i++;
							if (i == ((page - 1) * 5) + 5)
							{
								break;
							}
						}
						else
						{
							i++;
						}
					}
					embed.Description = embedstring;

					await discord.SendMessageAsync(e.Channel, embed);
				}
				else
				{ 
					await discord.SendMessageAsync(e.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = "**No users have ever earned any XP so far.**" });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
        }
        
		[Command("rank"), CommandClass("LevelCommands"), RequireGuild(), Description("Displays yours or another users level\n\nUsage:\n```=rank [ ID / @mention ]```"), Aliases("lvl", "level"),  RequireExcludent, RequireBotPermissions(Permissions.SendMessages)]
        public async Task Rank(CommandContext e, DiscordUser user = null)
        {
			try
			{
				if (user == null)
				{
					user = e.Message.Author;
				}
				if (lvlroles.ContainsKey(e.Guild.Id))
				{
					if(xplist.ContainsKey(e.Guild.Id))
					{
						if (xplist[e.Guild.Id].ContainsKey(user.Id))
						{
							int userslevel = 0;
							foreach(var i in lvlroles[e.Guild.Id])
							{
								if (i.XpReq <= xplist[e.Guild.Id][user.Id] && i.XpReq != 0)
								{
									userslevel++;
								}
							}
							var sortedleederboard = from entry in xplist[e.Guild.Id] orderby entry.Value descending select entry;
							DiscordEmbedBuilder embed = new DiscordEmbedBuilder 
							{ 
								Title = "Server rank card",
								Description = $"**```{user.Username}#{user.Discriminator}  | Level {userslevel} | Rank #{sortedleederboard.ToList().FindIndex(x => x.Key == user.Id) + 1}```**\n",
								Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = user.AvatarUrl } 
							};
							if (userslevel == 0)
							{
								embed.Color = DiscordColor.Black;
							}
							else
							{
								embed.Color = e.Guild.GetRole(lvlroles[e.Guild.Id][userslevel].RoleId).Color;
								embed.Description += $"**[<@&{lvlroles[e.Guild.Id][userslevel].RoleId}>]\n**";
							}
							string progstring = "";
							embed.AddField("Total", $"**```{xplist[e.Guild.Id][user.Id]}xp```**", true);
							if(userslevel < lvlroles[e.Guild.Id].Count()-1)
							{
								// 🟦
								for(int i = 0; i < 10; i++)
								{
									if(xplist[e.Guild.Id][user.Id] - lvlroles[e.Guild.Id][userslevel].XpReq >= ((lvlroles[e.Guild.Id][userslevel+1].XpReq - lvlroles[e.Guild.Id][userslevel].XpReq) /10)*i)
									{
										progstring += "🟦";
									}
									else
									{
										progstring += "⬜";
									}
								}
								embed.AddField("Progress", $"**```{xplist[e.Guild.Id][user.Id] - lvlroles[e.Guild.Id][userslevel].XpReq}xp / {lvlroles[e.Guild.Id][userslevel + 1].XpReq - lvlroles[e.Guild.Id][userslevel].XpReq}xp```**\n" + progstring, true);
								embed.AddField("Next level", $"**[Level {lvlroles[e.Guild.Id].IndexOf(lvlroles[e.Guild.Id][userslevel + 1])}]** | **<@&{lvlroles[e.Guild.Id][userslevel + 1].RoleId}> | {lvlroles[e.Guild.Id][userslevel + 1].XpReq} Total xp required**", false);
							}
							else
							{
								embed.Fields[0].Value = "Max Level Reached!";
								// 🟦
								for (int i = 0; i < 10; i++)
								{
									progstring += "🟦";
								}
								embed.AddField("Progress", progstring, true);
							}
							await discord.SendMessageAsync(await discord.GetChannelAsync(e.Message.Channel.Id), embed);
						}
						else
						{
							await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"**{user.Username}** hasn't gained any xp in this server!", Color = DiscordColor.Green});
						}
					}
					else
					{

						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"**{user.Username}** hasn't gained any xp in this server!", Color = DiscordColor.Green });
					}
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"{e.Message.Author.Username}, this server doesn't have a levelling system yet!", Color = DiscordColor.Green });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		
		[Command("lvledit"), CommandClass("LevelCommands"), RequireGuild(), Description("Edits a level role in the server.\nIf no score is given, the role will be removed as a level role. Else the required score will be updated\n\nUsage:\n```=lvladd < ID / @mention > [score]```"),  RequireExcludent, RequireUserPermissions(Permissions.ManageGuild), RequireBotPermissions(Permissions.ManageRoles & Permissions.SendMessages)]
		public async Task LvlAdd(CommandContext e, DiscordRole role, int score = 0)
		{
			try
			{
				
				if(e.Guild.Roles.Values.Contains<DiscordRole>(role))
				{
					bool didExist = false;
					if (lvlroles.ContainsKey(e.Guild.Id))
					{
						if(lvlroles[e.Guild.Id].Find(x => x.XpReq == 0) != null)
						{}
						else
						{
							lvlroles[e.Guild.Id].Add(new LevelRole { XpReq = 0, RoleId = 0, Name = "No role" });
						}
						if(lvlroles[e.Guild.Id].Find(x => x.RoleId == role.Id) != null)
						{
							didExist = true;
							var therole = lvlroles[e.Guild.Id].Find(x => x.RoleId == role.Id);
							if(score <= 0)
							{
								lvlroles[e.Guild.Id].Remove(lvlroles[e.Guild.Id][lvlroles[e.Guild.Id].FindIndex(x => x.RoleId == role.Id)]);
								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"Role {role.Name} deleted", Color = DiscordColor.Green});
								if(lvlroles[e.Guild.Id].Count() == 1 && lvlroles[e.Guild.Id][0].XpReq == 0)
								{
									lvlroles.Remove(e.Guild.Id);
								}
							}
							else
							{
								lvlroles[e.Guild.Id].Find(x => x.RoleId == role.Id);
								lvlroles[e.Guild.Id][lvlroles[e.Guild.Id].FindIndex(x => x.RoleId == role.Id)].XpReq = score;
								var sortedleederboard = from entry in lvlroles[e.Guild.Id] orderby entry.XpReq ascending select entry;
								var list = sortedleederboard.ToList();
								lvlroles[e.Guild.Id] = list;

								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Description = $"Role {role.Name} updated", Color = DiscordColor.Green});
							}
						}
						else
						{
							lvlroles[e.Guild.Id].Add(new LevelRole { Name = role.Name, XpReq = score, RoleId = role.Id});
							var sortedleederboard = from entry in lvlroles[e.Guild.Id] orderby entry.XpReq ascending select entry;
							var list = sortedleederboard.ToList();
							lvlroles[e.Guild.Id] = list;
						}
					}
					else
					{
						List<LevelRole> newlvlroles = new List<LevelRole>();
						newlvlroles.Add(new LevelRole { Name = role.Name, XpReq = score, RoleId = role.Id });
						lvlroles.Add(e.Guild.Id, newlvlroles);
						var sortedleederboard = from entry in lvlroles[e.Guild.Id] orderby entry.XpReq ascending select entry;
						var list = sortedleederboard.ToList();
						lvlroles[e.Guild.Id] = list;
					}
					if(didExist == false) 
					{
						await e.Message.RespondAsync(new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Added role **{role.Name}** to **{e.Guild.Name}**'s level roles!" });
					}
					File.WriteAllText("jsons/xp/levelroles.json", Newtonsoft.Json.JsonConvert.SerializeObject(lvlroles));
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Role **{role.Name}** doesn't exist in this server!" });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		
		[Command("xpedit"), CommandClass("LevelCommands"), RequireGuild(), Description("Edits a users xp.\nIf no xp amount is given, it will be reset to 0, else it will be updated to the given amount\n\nUsage:\n```=addxp < ID / @mention > [xp]```"),  RequireExcludent, RequireUserPermissions(Permissions.ManageGuild), RequireBotPermissions(Permissions.SendMessages)]
		public async Task AddXpUser(CommandContext e, DiscordUser user, int xp = 0)
		{
			try
			{
				if(xp >= 0)
				{
					if(lvlroles.ContainsKey(e.Guild.Id))
					{
						if(await e.Guild.GetMemberAsync(user.Id) != null)
						{
							if(xplist.ContainsKey(e.Guild.Id))
							{
								if(xplist[e.Guild.Id].ContainsKey(user.Id))
								{
									if(xp == 0)
									{
										await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Reset **{user.Username}#{user.Discriminator}**'s xp to 0!\n**```Old: {xplist[e.Guild.Id][user.Id]}\nNew: 0```**" });
										xplist[e.Guild.Id].Remove(user.Id);
									}
									else
									{
										await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Updated **{user.Username}#{user.Discriminator}**'s xp!\n**```Old: {xplist[e.Guild.Id][user.Id]}xp\nNew: {xp}xp```**" });
										xplist[e.Guild.Id][user.Id] = xp;
									}
								}
								else
								{
									xplist[e.Guild.Id].Add(user.Id, xp);
									await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Added **{xp}**xp to **{user.Username}#{user.Discriminator}**! **```Old: 0\nNew: {xp}xp```**" });
								}
							}
							else
							{
								xplist.Add(e.Guild.Id, new Dictionary<ulong, int> { { user.Id, xp } });
								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Added **{xp}**xp to **{user.Username}#{user.Discriminator}**! **```Old: 0\nNew: {xp}xp```**" });
							}
						}
						else
						{
							await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"User not found!" });
						}
						File.WriteAllText("jsons/xp/xp.json", Newtonsoft.Json.JsonConvert.SerializeObject(xplist));
					}
					else
					{
						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"There are no level roles set up for this server!" });
					}
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"You can't add negative xp!" });
				}
			}
			catch (Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		
		[Command("channeledit"), CommandClass("LevelCommands"), Description("Enables/Disables the xp gaining in the given channel\n\nUsage:\n```=channeledit < ID / #mention >```"), RequireGuild(),  RequireUserPermissions(Permissions.ManageGuild), RequireBotPermissions(Permissions.SendMessages), RequireExcludent]
		public async Task ChannelEdit(CommandContext e, DiscordChannel channel)
		{
			try
			{
				if(lvlroles.ContainsKey(e.Guild.Id))
				{
					if(e.Guild.GetChannel(channel.Id) != null)
					{
						if(channelxpexclude.ContainsKey(e.Guild.Id))
						{
							if(channelxpexclude[e.Guild.Id].Contains(channel.Id))
							{
								channelxpexclude[e.Guild.Id].Remove(channel.Id);
								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Channel {channel.Mention} is now no longer excluded from xp gaining!" });
								if(channelxpexclude[e.Guild.Id].Count == 0)
								{
									channelxpexclude.Remove(e.Guild.Id);
								}
							}
							else
							{
								channelxpexclude[e.Guild.Id].Add(channel.Id);
								await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Channel {channel.Mention} is now excluded from xp gaining!" });
							}
						}
						else
						{
							channelxpexclude.Add(e.Guild.Id, new List<ulong>{ channel.Id });
							await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Channel {channel.Mention} is now excluded from xp gaining!" });
						}
						File.WriteAllText("jsons/xp/channelblock.json", Newtonsoft.Json.JsonConvert.SerializeObject(channelxpexclude));
					}
					else
					{
						await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"Channel not found!" });
					}
				}
				else
				{
					await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Red, Description = $"There are no level roles set up for this server!" });
				}
			}
			catch(Exception ex)
			{
				await AlertException(e, ex);
			}
		}
		
		[Command("xpreset"), CommandClass("LevelCommands"), RequireGuild(),  RequireAuth, Hidden()]
		public async Task ResetXp(CommandContext e, ulong serverid = 0)
		{
			try
			{
				if(serverid == 0)
				{
					xplist = new Dictionary<ulong, Dictionary<ulong, int>>();
				}
				else
				{
					xplist.Remove(serverid);
				}
				File.WriteAllText("jsons/xp/xp.json", Newtonsoft.Json.JsonConvert.SerializeObject(xplist));
				await discord.SendMessageAsync(e.Message.Channel, new DiscordEmbedBuilder { Color = DiscordColor.Green, Description = $"Reset xp for server {serverid}!" });
			}
			catch (Exception ex)
			{
				await AlertException(e, ex); // add addxp command for specific user!
			}
		}
	}
	public class LevelRole
	{
		public string Name { get; set; }
		public ulong RoleId { get; set; }
		public int XpReq{ get; set; }
	}
	// public static int[] Level = {
	// 	0,
	// 	500,
	// 	1250,
	// 	2500,
	// 	5000,
	// 	8500,
	// 	15000,
	// 	50000,
	// 	100000,
	// 	250000,
	// 	500000,
	// 	1000000
	// };
}