using DSharpPlus;
using Stone_Red_Utilities.ConsoleExtentions;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Timers;
using System.Threading.Tasks;

namespace DiscordCLI
{
    internal class CommandsManager : Commands
    {
        private readonly DiscordClient client;

        private readonly Dictionary<string, (string, Func<string, Task>)> CommandsList;
        private int cooldown = 0;

        public CommandsManager(DiscordClient dicordClient, OutputManager outputManager) : base(dicordClient, outputManager)
        {
            client = dicordClient;

            CommandsList = new()
            {
                { "exit", ("exits application", null) },
                { "logout", ("deletes auth token and exits application", DeleteToken) },
                { "clear", ("clears the console", Clear) },
                { "guilds", ("lists all guilds you are in", ListGuilds) },
                { "dms", ("lists all private channels", ListDms) },
                { "channels", ("lists all channels of guild args:<guild name/index>", ListGuildChannels) },
                { "enterg", ("enter guild args:<guild name/index>", ListGuildChannels) },
                { "enterc", ("enter channel args:<channel name/index>", EnterChannel) },
                { "enterd", ("enter DM channel args:<channel name/index>", EnterDmChannel) },
                { "userinfo", ("gets information about a user args:<username>", UserInfo) },
            };

            Timer cooldownTimer = new Timer(1000);
            cooldownTimer.Elapsed += CooldownTimer_Elapsed;
            cooldownTimer.Start();
        }

        private void CooldownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (cooldown > 0)
                cooldown--;
        }

        /// <summary>
        ///  Check the command and return true if the program should exit
        /// </summary>
        /// <param name="input"></param>
        /// <returns>exit bool and write override bool</returns>
        public async Task<(bool, bool)> CheckCommand(string rawInput)
        {
            string input = rawInput.Trim().ToLower().Replace('\n', '\0');

            if (string.IsNullOrWhiteSpace(input.StartsWith(InputManager.Prefix) ? input.Remove(0, 1) : input))
            {
                Console.CursorTop--;
                return (false, false);
            }

            cooldown++;

            if (cooldown > 1)
            {
                Console.WriteLine();
                ConsoleExt.WriteLine("You are beeing rate limited!", ConsoleColor.Yellow);
                ConsoleExt.WriteLine("Wait a few seconds before making another request!", ConsoleColor.Yellow);
                return (false, true);
            }

            if (input.StartsWith(InputManager.Prefix))
            {
                input = input.Remove(0, 1);
            }
            else if (GlobalInformation.currentTextChannel != null)
            {
                cooldown++;

                try
                {
                    await GlobalInformation.currentTextChannel.SendMessageAsync(rawInput);
                    Console.Write("\r" + new string(' ', Console.WindowWidth));
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    ConsoleExt.WriteLine(ex.Message, ConsoleColor.Red);
                }
                return (false, true);
            }

            Console.WriteLine();

            if (input == "exit" || input is null)
                return (true, false);

            string args = input.Contains(" ") ? input[input.IndexOf(" ")..].Trim() : null;
            input = input.Contains(" ") ? input.Substring(0, input.IndexOf(" ")) : input;

            if (input == "help" || input == "?")
            {
                Console.WriteLine($"Prefix: '{InputManager.Prefix}' (Only required in text channels)");
                Console.WriteLine();

                for (int i = 0; i < CommandsList.Count; i++)
                {
                    Console.Write($"{i + 1}. {CommandsList.Keys.ElementAt(i)}".PadRight(15));
                    Console.WriteLine(CommandsList.Values.ElementAt(i).Item1);
                }
            }
            else if (CommandsList.ContainsKey(input))
            {
                await CommandsList[input].Item2(args);
            }
            else
            {
                ConsoleExt.WriteLine("Command does not exist!", ConsoleColor.Red);
            }
            return (false, false);
        }
    }
}