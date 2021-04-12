using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using DSharpPlus;
using DSharpPlus.Entities;
using Stone_Red_Utilities.ColorConsole;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Color = System.Drawing.Color;

namespace DiscordCLI
{
    internal class Program
    {
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private static DiscordClient client;
        private CommandsManager commandsManager;

        private static string input = "<";
        public const string tokenPath = "token.txt";

        public async Task MainAsync()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string token = string.Empty;
            if (File.Exists(tokenPath))
                token = File.ReadAllText(tokenPath);

            while (string.IsNullOrEmpty(token))
            {
                Console.Write("Enter auth token: ");
                token = Console.ReadLine();
            }

            File.WriteAllText(tokenPath, token);

            try
            {
                client = new DiscordClient(new DiscordConfiguration()
                {
                    Token = token,
                    TokenType = TokenType.User
                });

                client.MessageCreated += Client_MessageCreated;

                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteLine(ex, ConsoleColor.Red);

                if (ex.Message.Contains("Authentication failed"))
                    File.Delete(tokenPath);
                return;
            }

            commandsManager = new CommandsManager(client);

            await ReadInput();
            await Task.Delay(-1);
        }

        private async Task Client_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            await WriteMessage(e.Message, e.Channel, e.Guild);
        }

        public static async Task WriteMessage(DiscordMessage message, DiscordChannel channel, DiscordGuild guild)
        {
            try
            {
                if (channel.Id != GlobalInformation.currentTextChannel?.Id)
                    return;
                DiscordUser user = message.Author;
                DiscordMember discordMember = await guild.GetMemberAsync(user.Id);
                DiscordColor discordColor = discordMember.Color;

                Color color = Color.FromArgb(discordColor.R, discordColor.G, discordColor.B);

                WriteTop($"[{discordMember.DisplayName}]", color, message, $"{discordMember.Username}#{discordMember.Discriminator} {message.Timestamp.LocalDateTime}");

                if (!string.IsNullOrWhiteSpace(message.Content))
                    WriteTop(message.Content, Color.White, message);

                foreach (DiscordAttachment attachment in message.Attachments)
                {
                    WriteTop($"{attachment.Url}", Color.White, message, attachment.FileName);
                }
                foreach (DiscordEmbed embed in message.Embeds)
                {
                    if (!string.IsNullOrWhiteSpace(embed.Title))
                        WriteTop($">>> {{{embed.Title}}}", Color.FromArgb(embed.Color.Value), message);

                    if (!string.IsNullOrWhiteSpace(embed.Description))
                        WriteTop($">> {embed.Description}", Color.White, message);

                    WriteTop($"{string.Join(Environment.NewLine, embed.Fields.Select(x => $">{x.Name}{Environment.NewLine}{x.Value}"))}", Color.White, message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            WriteTop(string.Empty, Color.White, message);
            Console.Write($"[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{GlobalInformation.currentGuild?.Name}/{GlobalInformation.currentTextChannel?.Name}] ==> ");
            Console.Write(input);
        }

        private async Task ReadInput()
        {
            await Task.Run(() =>
            {
                bool exit = false;
                while (!exit)
                {
                    string infoString = $"\r[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{GlobalInformation.currentGuild?.Name}/{GlobalInformation.currentTextChannel?.Name}] ==> ";
                    if (input.StartsWith('<'))
                        Console.Write(Environment.NewLine + infoString);

                    input = string.Empty;

                    ConsoleKeyInfo keyInfo;
                    do
                    {
                        keyInfo = Console.ReadKey(true);
                        if (!char.IsControl(keyInfo.KeyChar))
                            input += keyInfo.KeyChar.ToString();

                        if (keyInfo.Key == ConsoleKey.Backspace)
                        {
                            if (input.Length > 0)
                            {
                                input = input.Remove(input.Length - 1);
                            }
                        }

                        Console.Write(keyInfo.KeyChar);
                        if (keyInfo.Key == ConsoleKey.Backspace)
                            Console.Write(" ");

                        Console.CursorLeft = infoString.Length + input.Length - 1;
                    } while (keyInfo.Key != ConsoleKey.Enter);

                    if (GlobalInformation.currentTextChannel == null && !input.StartsWith('<'))
                        input = "<" + input;

                    exit = commandsManager.CheckCommand(input);
                }
                Environment.Exit(0);
            });
        }

        private async static void WriteTop(string message, Color color, DiscordMessage discordMessage, string info = null)
        {
            ConsoleColor consoleColor = ClosestConsoleColor3(color);

            if (consoleColor == Console.BackgroundColor || consoleColor == ConsoleColor.DarkGray)
                consoleColor = ConsoleColor.White;

            //Console.SetCursorPosition(0, Console.WindowTop + Console.WindowHeight - 1);

            Console.Write('\r' + new string(' ', Console.WindowWidth) + '\r');

            string[] words = message.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                ConsoleColor mentionColor = ConsoleColor.Black;
                foreach (DiscordUser user in discordMessage.MentionedUsers)
                {
                    if (user?.Mention is not null)
                        if (words[i].Contains(user.Mention))
                        {
                            words[i] = words[i].Replace(user.Mention, $"@{user.Username}#{user.Discriminator}");
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
                            mentionColor = ClosestConsoleColor3(Color.FromArgb(discordColor.R, discordColor.G, discordColor.B));
                        }
                }

                ConsoleExt.Write(words[i] + " ", mentionColor == ConsoleColor.Black ? consoleColor : mentionColor);
            }

            ConsoleExt.WriteLine(" " + info, ConsoleColor.DarkGray);
        }

        public static ConsoleColor ClosestConsoleColor3(Color targetColor)
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