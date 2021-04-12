using ColorMine.ColorSpaces.Comparisons;
using ColorMine.ColorSpaces;
using DSharpPlus.Entities;
using DSharpPlus;
using Stone_Red_Utilities.ColorConsole;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System;

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

        public async Task WriteMessage(DiscordMessage message, DiscordChannel channel, DiscordGuild guild)
        {
            try
            {
                if (channel.Id != GlobalInformation.currentTextChannel?.Id)
                    return;

                DiscordUser user = message.Author;
                DiscordMember discordMember = await guild.GetMemberAsync(user.Id);
                DiscordColor discordColor = discordMember.Color;

                Color color = Color.FromArgb(discordColor.R, discordColor.G, discordColor.B);

                WriteTop($"[{discordMember.DisplayName}]", color, message, true, true, $"{discordMember.Username}#{discordMember.Discriminator} {message.Timestamp.LocalDateTime}");

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
                    foreach (DiscordEmbedField field in embed.Fields)
                    {
                        WriteTop(">>> ", Color.FromArgb(embed.Color.Value), message, true, false);
                        WriteTop($"{field.Name}{Environment.NewLine}{field.Value}", Color.White, message, false);
                    }
                    //WriteTop($"{string.Join(Environment.NewLine, embed.Fields.Select(x => $"> {x.Name}{Environment.NewLine}{x.Value}"))}", Color.White, message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            WriteTop(string.Empty, Color.White, message);
            Console.Write($"[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{GlobalInformation.currentGuild?.Name}/{GlobalInformation.currentTextChannel?.Name}] ==> ");
            Console.Write(InputManager.Input);
        }

        private void WriteTop(string message, Color color, DiscordMessage discordMessage, bool removeText = true, bool newLine = true, string info = null)
        {
            ConsoleColor consoleColor = ClosestConsoleColor(color);

            if (consoleColor == Console.BackgroundColor || consoleColor == ConsoleColor.DarkGray)
                consoleColor = ConsoleColor.White;

            if (removeText)
                Console.Write('\r' + new string(' ', Console.WindowWidth) + '\r');

            string[] words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                ConsoleColor mentionColor = ConsoleColor.Black;

                if (IsUri(words[i]))
                {
                    mentionColor = ConsoleColor.DarkCyan;
                }

                foreach (DiscordUser user in discordMessage.MentionedUsers)
                {
                    if (user?.Mention is not null)
                    {
                        if (words[i].Contains(user.Mention))
                        {
                            words[i] = words[i].Replace(user.Mention, $"@{user.Username}#{user.Discriminator}");
                            mentionColor = ConsoleColor.Blue;
                        }
                    }
                    else
                    {
                        if (words[i].Contains("<@") && words[i].Contains('>'))
                            mentionColor = ConsoleColor.Blue;
                    }
                }

                foreach (DiscordChannel channel in discordMessage.MentionedChannels)
                {
                    if (channel?.Mention is not null)
                        if (words[i].Contains(channel.Mention))
                        {
                            words[i] = words[i].Replace(channel.Mention, $"#{channel.Name}");
                            mentionColor = ConsoleColor.Blue;
                        }
                }

                foreach (DiscordRole role in discordMessage.MentionedRoles)
                {
                    if (role?.Mention is not null)
                        if (words[i].Contains(role.Mention))
                        {
                            words[i] = words[i].Replace(role.Mention, $"@{role.Name}");
                            DiscordColor discordColor = role.Color;
                            mentionColor = ClosestConsoleColor(Color.FromArgb(discordColor.R, discordColor.G, discordColor.B));
                        }
                }

                ConsoleExt.Write(words[i] + " ", mentionColor == ConsoleColor.Black ? consoleColor : mentionColor);
            }

            ConsoleExt.Write(info, ConsoleColor.DarkGray);

            if (newLine)
                Console.WriteLine();
        }

        private bool IsUri(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private ConsoleColor ClosestConsoleColor(Color targetColor)
        {
            double minDif = double.MaxValue;
            ConsoleColor bestColor = ConsoleColor.White;

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
                    bestColor = consoleColor;
                }
            }
            return bestColor;
        }
    }
}