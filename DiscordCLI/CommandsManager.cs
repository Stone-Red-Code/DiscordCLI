using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using Stone_Red_Utilities.ColorConsole;

using DSharpPlus.Entities;
using System.Diagnostics;
using System.IO;

namespace DiscordCLI
{
    internal class CommandsManager : Commands
    {
        private readonly DiscordClient client;

        private readonly Dictionary<string, (string, Action<string>)> CommandsList;

        public CommandsManager(DiscordClient dicordClient) : base(dicordClient)
        {
            client = dicordClient;

            CommandsList = new()
            {
                { "exit", ("exits application", null) },
                { "logout", ("deletes auth token and exits application", DeleteToken) },
                { "guilds", ("lists all guilds you are in", ListGuilds) },
                { "dms", ("lists all private channels (Not implemented yet)", ListDms) },
                { "channels", ("lists all channels of guild args:<guild name/index>", ListGuildChannels) },
                { "enterg", ("enter guild args:<guild name/index>", ListGuildChannels) },
                { "enterc", ("enter chat args:<channel name/index>", EnterChannel) },
            };
        }

        public bool CheckCommand(string input)
        {
            input = input.Trim().ToLower().Replace('\n', '\0');

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.StartsWith('<'))
            {
                input = input.Remove(0, 1);
            }
            else if (GlobalInformation.currentTextChannel != null)
            {
                Console.CursorTop--;
                Console.Write("\r" + new string(' ', Console.WindowWidth));
                GlobalInformation.currentTextChannel.SendMessageAsync(input);
                return false;
            }

            Console.WriteLine();

            if (input == "exit" || input is null)
                return true;

            string args = input.Contains(" ") ? input[input.IndexOf(" ")..].Trim() : null;
            input = input.Contains(" ") ? input.Substring(0, input.IndexOf(" ")) : input;

            if (input == "help")
            {
                for (int i = 0; i < CommandsList.Count; i++)
                {
                    Console.Write($"{i + 1}. {CommandsList.Keys.ElementAt(i)}".PadRight(15));
                    Console.WriteLine(CommandsList.Values.ElementAt(i).Item1);
                }
            }
            else if (CommandsList.ContainsKey(input))
            {
                CommandsList[input].Item2(args);
            }
            else
            {
                ConsoleExt.WriteLine("Command does not exist!", ConsoleColor.Red);
            }
            return false;
        }
    }

    internal class Commands
    {
        private IReadOnlyDictionary<ulong, DiscordGuild> socketGuildsCache;

        private readonly DiscordClient client;

        public Commands(DiscordClient dicordClient)
        {
            client = dicordClient;
        }

        protected void ListGuilds(string args)
        {
            socketGuildsCache ??= client.Guilds;

            int index = 1;
            foreach (DiscordGuild guild in socketGuildsCache.Values)
            {
                Console.WriteLine($"{index}. {guild.Name}");
                index++;
            }
        }

        protected void ListDms(string args)
        {
            throw new NotImplementedException();
        }

        protected void ListGuildChannels(string args)
        {
            IReadOnlyCollection<DiscordChannel> textChannels;
            DiscordGuild guild;

            if (args != null)
            {
                if (int.TryParse(args, out int ind))
                {
                    guild = socketGuildsCache?.Values.ElementAtOrDefault(ind - 1);
                }
                else
                {
                    guild = socketGuildsCache?.FirstOrDefault(x => x.Value.Name == args).Value;
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
            textChannels = guild.Channels;

            int index = 1;
            foreach (DiscordChannel channel in textChannels)
            {
                switch (channel.Type)
                {
                    case ChannelType.Category:
                        //Console.WriteLine($"[{channel.Name}]");
                        break;

                    case ChannelType.Text:
                        Console.WriteLine($"    {index}. {channel.Name}");
                        index++;
                        break;
                }
            }
        }

        protected async void EnterChannel(string args)
        {
            DiscordChannel textChannel;

            if (GlobalInformation.currentGuild is null)
            {
                ConsoleExt.WriteLine("You are not in a guild!", ConsoleColor.Red);
                return;
            }

            if (int.TryParse(args, out int ind))
            {
                textChannel = GlobalInformation.currentGuild?.Channels.Where(x => x.Type == ChannelType.Text).ElementAtOrDefault(ind - 1);
            }
            else
            {
                textChannel = GlobalInformation.currentGuild?.Channels.Where(x => x.Type == ChannelType.Text).FirstOrDefault(x => x.Name == args);
            }

            GlobalInformation.currentTextChannel = textChannel;

            if (textChannel is null)
            {
                ConsoleExt.WriteLine("Channel not found!", ConsoleColor.Red);
                return;
            }

            foreach (DiscordMessage message in (await textChannel.GetMessagesAsync(10)).Reverse())
            {
                try
                {
                    await Program.WriteMessage(message, textChannel, GlobalInformation.currentGuild);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        protected void DeleteToken(string args)
        {
            File.Delete(Program.tokenPath);
            Environment.Exit(0);
        }
    }
}