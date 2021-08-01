using ColorMine.ColorSpaces.Comparisons;
using ColorMine.ColorSpaces;
using DSharpPlus.Entities;
using DSharpPlus;
using Stone_Red_Utilities.ConsoleExtentions;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DiscordCLI
{
    internal class OutputManager
    {
        private readonly DiscordClient client;
        public InputManager InputManager { get; set; }

        public OutputManager(DiscordClient discordClient)
        {
            client = discordClient;
        }

        public async Task WriteMessage(DiscordMessage message, DiscordChannel channel, DiscordGuild guild, bool writeInfo = true)
        {
            try
            {
                if (channel?.Id != GlobalInformation.currentTextChannel?.Id)
                    return;

                DiscordUser user = message.Author;

                if (GlobalInformation.currentGuild is not null)
                {
                    DiscordMember discordMember = await guild.GetMemberAsync(user.Id);
                    DiscordColor discordColor = discordMember.Color;
                    Color color = Color.FromArgb(discordColor.R, discordColor.G, discordColor.B);

                    WriteTop($"[{discordMember.DisplayName}]", color, message, true, true, $"{discordMember.Username}#{discordMember.Discriminator} {message.Timestamp.LocalDateTime}");
                }
                else
                {
                    WriteTop($"[{user.Username}]", Color.White, message, true, true, $"{user.Username}#{user.Discriminator} {message.Timestamp.LocalDateTime}");
                }

                if (!string.IsNullOrWhiteSpace(message.Content))
                    WriteTop(message.Content, Color.White, message);

                foreach (DiscordAttachment attachment in message.Attachments)
                {
                    WriteTop($"{attachment.Url}", Color.White, message, true, true, attachment.FileName);
                }

                foreach (DiscordEmbed embed in message.Embeds)
                {
                    if (!string.IsNullOrWhiteSpace(embed.Title))
                    {
                        WriteTop(">>> ", Color.FromArgb(embed.Color.Value), message, true, false);
                        WriteTop($"{{{embed.Title}}}", Color.White, message, false);
                    }

                    if (!string.IsNullOrWhiteSpace(embed.Description))
                    {
                        WriteTop(">>> ", Color.FromArgb(embed.Color.Value), message, true, false);
                        WriteTop($"{embed.Description}", Color.White, message, false);
                    }

                    if (embed.Fields is not null)
                    {
                        foreach (DiscordEmbedField field in embed.Fields)
                        {
                            WriteTop(">>> ", Color.FromArgb(embed.Color.Value), message, true, false);
                            WriteTop($"{field.Name}{Environment.NewLine}{field.Value}", Color.White, message, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            WriteTop(string.Empty, Color.White, message);
            DiscordDmChannel dmChannel = GlobalInformation.currentTextChannel as DiscordDmChannel;

            if (writeInfo)
            {
                string infoString = $"\r[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{(dmChannel is null ? GlobalInformation.currentGuild?.Name : "DMs")}/{(dmChannel is null ? GlobalInformation.currentTextChannel?.Name : string.Join(", ", dmChannel.Recipients.Select(x => x.Username)))}] ==> ";
                Console.Write(infoString);
                Console.Write(InputManager.Input);
            }
        }

        private void WriteTop(string message, Color color, DiscordMessage discordMessage, bool removeText = true, bool newLine = true, string info = null)
        {
            if (removeText)
                Console.Write('\r' + new string(' ', Console.WindowWidth) + '\r');

            foreach (Match match in Regex.Matches(message, "<@(.*?)>"))
            {
                foreach (DiscordUser user in discordMessage.MentionedUsers)
                {
                    if (user?.Mention is not null)
                    {
                        if (match.Value == user.Mention)
                        {
                            message = message.Replace(match.Value, $"\0<C{(int)ConsoleColor.Blue}C>@{user.Username}#{user.Discriminator}\0");
                        }
                    }
                    else
                    {
                        if (ulong.TryParse(match.Value.Replace("!", string.Empty).Replace("<@", string.Empty).Replace(">", string.Empty), out ulong id))
                        {
                            DiscordUser discordUser = client.GetUserAsync(id).Result;

                            message = message.Replace(match.Value, $"\0<C{(int)ConsoleColor.Blue}C>@{discordUser.Username}#{discordUser.Discriminator}\0");
                        }
                    }
                }
            }

            foreach (Match match in Regex.Matches(message, "<#(.*?)>"))
            {
                foreach (DiscordChannel channel in discordMessage.MentionedChannels)
                {
                    if (channel?.Mention is not null)
                    {
                        if (match.Value == channel.Mention)
                        {
                            message = message.Replace(match.Value, $"\0<C{(int)ConsoleColor.Blue}C>#{channel.Name}\0");
                        }
                    }
                    else
                    {
                        if (ulong.TryParse(match.Value.Replace("<#", string.Empty).Replace(">", string.Empty), out ulong id))
                        {
                            DiscordChannel discordChannel = client.GetChannelAsync(id).Result;
                            message = message.Replace(match.Value, $"\0<C{(int)ConsoleColor.Blue}C>#{discordChannel.Name}\0");
                        }
                    }
                }
            }

            foreach (Match match in Regex.Matches(message, @"<@&(.*?)>"))
            {
                foreach (DiscordRole role in discordMessage.MentionedRoles)
                {
                    if (role?.Mention is not null)
                    {
                        if (match.Value == role.Mention)
                        {
                            DiscordColor discordColor = role.Color;
                            message = message.Replace(match.Value, $"\0<C{(int)ClosestConsoleColor(Color.FromArgb(discordColor.R, discordColor.G, discordColor.B))}C>@{role.Name}\0");
                        }
                    }
                }
            }

            foreach (Match match in Regex.Matches(message, @"((https?)\:\/\/|www.)[A-Za-z0-9\.\-\&\#\?\/]*", RegexOptions.IgnoreCase))
            {
                message = message.Replace(match.Value, $"\0<C{(int)ConsoleColor.Blue}C>{match.Value}\0");
            }

            foreach (string part in message.Split("\0"))
            {
                ConsoleColor consoleColor;
                string messagePart = part;

                string stringColor = Regex.Match(messagePart, "(?<=(<C))(.*)(?=C>)").Value;
                bool succ = int.TryParse(stringColor, out int colorIndex);
                if (succ && colorIndex >= 0 && colorIndex < 16)
                {
                    messagePart = messagePart.Replace($"<C{stringColor}C>", string.Empty);
                    consoleColor = (ConsoleColor)colorIndex;
                }
                else
                {
                    consoleColor = ClosestConsoleColor(color);
                }

                if (consoleColor == Console.BackgroundColor || consoleColor == ConsoleColor.DarkGray)
                    consoleColor = ConsoleColor.White;
                ConsoleExt.Write(messagePart, consoleColor);
            }

            ConsoleExt.Write($" {info}", ConsoleColor.DarkGray);

            if (newLine)
                Console.WriteLine();
        }

        private ConsoleColor ClosestConsoleColor(Color targetColor)
        {
            double minDif = double.MaxValue;
            ConsoleColor closestColor = ConsoleColor.White;

            foreach (ConsoleColor consoleColor in Enum.GetValues(typeof(ConsoleColor)))
            {
                Color color = Color.FromName(consoleColor.ToString());

                var colorA = new Rgb
                {
                    R = targetColor.R,
                    G = targetColor.G,
                    B = targetColor.B
                };

                var colorB = new Rgb
                {
                    R = color.R,
                    G = color.G,
                    B = color.B
                };
                double diff = colorA.Compare(colorB, new Cie1976Comparison());

                if (diff < minDif)
                {
                    minDif = diff;
                    closestColor = consoleColor;
                }
            }
            return closestColor;
        }
    }
}