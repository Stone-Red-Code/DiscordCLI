using DSharpPlus.Entities;
using DSharpPlus;
using Stone_Red_Utilities.ConsoleExtentions;
using Stone_Red_Utilities.StringExtentions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace DiscordCLI
{
    internal class Commands
    {
        private readonly DiscordClient client;

        private readonly OutputManager outputManager;

        public Commands(DiscordClient dicordClient, OutputManager outputMan)
        {
            client = dicordClient;
            outputManager = outputMan;
        }

        protected async Task ListGuilds(string args)
        {
            int index = 1;
            foreach (DiscordGuild guild in client.Guilds.Values)
            {
                Console.WriteLine($"{index}. {guild.Name}");
                index++;
            }

            await Task.CompletedTask;
        }

        protected async Task ListDms(string args)
        {
            int index = 1;
            foreach (DiscordDmChannel dmChannel in client.PrivateChannels)
            {
                Console.WriteLine($"{index}. {string.Join(", ", dmChannel.Recipients.Select(x => x.Username))}");
                index++;
            }
            await Task.CompletedTask;
        }

        protected async Task ListGuildChannels(string args)
        {
            IReadOnlyCollection<DiscordChannel> discordChannels;
            DiscordGuild guild;

            if (args != null)
            {
                if (int.TryParse(args, out int ind))
                {
                    guild = client.Guilds?.Values.ElementAtOrDefault(ind - 1);
                }
                else
                {
                    guild = client.Guilds?.FirstOrDefault(x => x.Value.Name == args).Value;
                }
                GlobalInformation.currentTextChannel = null;
            }
            else
            {
                guild = GlobalInformation.currentGuild;
            }

            if (guild is null)
            {
                ConsoleExt.WriteLine("Guild not found!", ConsoleColor.Red);
                GlobalInformation.currentTextChannel = null;
                return;
            }

            GlobalInformation.currentGuild = guild;
            discordChannels = guild.Channels;

            int index = 0;
            foreach (DiscordChannel channel in discordChannels.OrderBy(x => x.Position))
            {
                if (channel.Type == ChannelType.Category)
                {
                    Console.WriteLine();
                    Console.WriteLine($"--- {channel.Name} ---");
                    foreach (DiscordChannel child in channel.Children.OrderBy(x => x.Position))
                    {
                        if (child.Type == ChannelType.Text)
                        {
                            index++;

                            Console.WriteLine($"{index}. {child.Name}");
                        }
                        else
                        {
                            ConsoleExt.WriteLine($"-. {child.Name}", ConsoleColor.DarkGray);
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        protected async Task EnterChannel(string args)
        {
            DiscordChannel textChannel;

            if (GlobalInformation.currentGuild is null)
            {
                ConsoleExt.WriteLine("You are not in a guild!", ConsoleColor.Red);
                return;
            }

            if (int.TryParse(args, out int ind))
            {
                textChannel = GlobalInformation.currentGuild?.Channels.Where(x => x.Type == ChannelType.Text).OrderBy(x => x.Position).ElementAtOrDefault(ind - 1);
            }
            else
            {
                textChannel = GlobalInformation.currentGuild?.Channels.Where(x => x.Type == ChannelType.Text).OrderBy(x => x.Position).FirstOrDefault(x => x.Name == args);
            }

            GlobalInformation.currentTextChannel = textChannel;

            if (textChannel is null)
            {
                ConsoleExt.WriteLine("Channel not found!", ConsoleColor.Red);
                return;
            }

            try
            {
                foreach (DiscordMessage message in (textChannel.GetMessagesAsync(10).Result).Reverse())
                {
                    await outputManager.WriteMessage(message, textChannel, GlobalInformation.currentGuild, false);
                }
                Console.CursorTop--;
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteLine(ex.Message, ConsoleColor.Red);
                GlobalInformation.currentTextChannel = null;
            }

            await Task.CompletedTask;
        }

        protected async Task EnterDmChannel(string args)
        {
            DiscordDmChannel dmChannel;

            GlobalInformation.currentGuild = null;

            if (int.TryParse(args, out int ind))
            {
                dmChannel = client.PrivateChannels.ElementAtOrDefault(ind - 1);
            }
            else
            {
                dmChannel = client.PrivateChannels.FirstOrDefault(x => x.Name == args);
            }

            GlobalInformation.currentTextChannel = dmChannel;

            if (dmChannel is null)
            {
                ConsoleExt.WriteLine("Channel not found!", ConsoleColor.Red);
                return;
            }

            try
            {
                foreach (DiscordMessage message in (await dmChannel.GetMessagesAsync(10)).Reverse())
                {
                    await outputManager.WriteMessage(message, dmChannel, GlobalInformation.currentGuild, false);
                }
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteLine(ex, ConsoleColor.Red);
                GlobalInformation.currentTextChannel = null;
            }
        }

        protected async Task DeleteToken(string args)
        {
            File.Delete(Program.tokenPath);
            Environment.Exit(0);

            await Task.CompletedTask;
        }

        protected async Task UserInfo(string userName)
        {
            if (GlobalInformation.currentGuild is null)
            {
                ConsoleExt.WriteLine("You are not in a guild!", ConsoleColor.Red);
                return;
            }

            if (userName is null)
            {
                ConsoleExt.WriteLine("User not found!", ConsoleColor.Red);
                return;
            }

            DiscordUser discordUser = GlobalInformation.currentGuild?.Members?.FirstOrDefault(user => user.Username.EqualsIgnoreSpacesAndCase(userName));

            if (discordUser is null)
            {
                discordUser = GlobalInformation.currentGuild?.Members?.FirstOrDefault(user => $"{user.Username}#{user.Discriminator}".EqualsIgnoreSpacesAndCase(userName));
            }

            if (discordUser is null)
            {
                discordUser = GlobalInformation.currentGuild?.Members?.FirstOrDefault(user => $"{user.Username}".EqualsIgnoreSpacesAndCase(userName));
            }

            if (discordUser is null)
            {
                discordUser = GlobalInformation.currentGuild?.Members?.FirstOrDefault(user => $"{user.Nickname}".EqualsIgnoreSpacesAndCase(userName));
            }

            if (discordUser is not null)
            {
                string status = discordUser.Presence?.Status.ToString();
                status ??= "N/A";

                string infoString =
                    $"Username: {discordUser.Username}#{discordUser.Discriminator}" + Environment.NewLine +
                    $"Status: {status}" + Environment.NewLine +
                    $"Created at: {discordUser.CreationTimestamp}" + Environment.NewLine +
                    $"ID: {discordUser.Id}" + Environment.NewLine +
                    $"Bot: {discordUser.IsBot}";
                Console.WriteLine(infoString);
            }
            else
            {
                ConsoleExt.WriteLine("User not found!", ConsoleColor.Red);
            }

            await Task.CompletedTask;
        }

        protected async Task Clear(string args)
        {
            Console.Clear();
            await Task.CompletedTask;
        }
    }
}