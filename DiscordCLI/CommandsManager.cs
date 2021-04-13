using DSharpPlus;
using Stone_Red_Utilities.ColorConsole;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DiscordCLI
{
    internal class CommandsManager : Commands
    {
        private readonly DiscordClient client;

        private readonly Dictionary<string, (string, Action<string>)> CommandsList;

        public CommandsManager(DiscordClient dicordClient, OutputManager outputManager) : base(dicordClient, outputManager)
        {
            client = dicordClient;

            CommandsList = new()
            {
                { "exit", ("exits application", null) },
                { "logout", ("deletes auth token and exits application", DeleteToken) },
                { "guilds", ("lists all guilds you are in", ListGuilds) },
                { "dms", ("lists all private channels", ListDms) },
                { "channels", ("lists all channels of guild args:<guild name/index>", ListGuildChannels) },
                { "enterg", ("enter guild args:<guild name/index>", ListGuildChannels) },
                { "enterc", ("enter channel args:<channel name/index>", EnterChannel) },
                { "enterd", ("enter DM channel args:<channel name/index>", EnterDmChannel) },
            };
        }

        /// <summary>
        ///  Check the command and return true if the program should exit
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool CheckCommand(string input)
        {
            input = input.Trim().ToLower().Replace('\n', '\0');

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.StartsWith(InputManager.prefix))
            {
                input = input.Remove(0, 1);
            }
            else if (GlobalInformation.currentTextChannel != null)
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth));
                GlobalInformation.currentTextChannel.SendMessageAsync(input);
                return false;
            }

            Console.WriteLine();

            if (input == "exit" || input is null)
                return true;

            string args = input.Contains(" ") ? input[input.IndexOf(" ")..].Trim() : null;
            input = input.Contains(" ") ? input.Substring(0, input.IndexOf(" ")) : input;

            if (input == "help" || input == "?")
            {
                Console.WriteLine($"Prefix: '{InputManager.prefix}' (Only required in text channels)");
                Console.WriteLine();

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
}