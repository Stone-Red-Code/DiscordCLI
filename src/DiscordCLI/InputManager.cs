using DSharpPlus;
using System.Threading.Tasks;
using System;
using DSharpPlus.Entities;
using System.Linq;

namespace DiscordCLI
{
    internal class InputManager
    {
        private readonly CommandsManager commandsManager;
        private readonly DiscordClient client;
        public const string prefix = ">";
        public string Input { get; private set; } = string.Empty;

        public InputManager(DiscordClient dicordClient, CommandsManager commandsMan)
        {
            client = dicordClient;
            commandsManager = commandsMan;
        }

        public async Task ReadInput()
        {
            bool exit = false;
            bool printOverride = false;
            string lastInput = prefix;
            while (!exit)
            {
                DiscordDmChannel dmChannel = GlobalInformation.currentTextChannel as DiscordDmChannel;
                string infoString = $"\r[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{(dmChannel is null ? GlobalInformation.currentGuild?.Name : "DMs")}/{(dmChannel is null ? GlobalInformation.currentTextChannel?.Name : string.Join(", ", dmChannel.Recipients.Select(x => x.Username)))}] ==> ";
                if (lastInput.StartsWith(prefix) || printOverride)
                    Console.Write(Environment.NewLine + infoString);

                printOverride = false;

                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey(true);
                    if (!char.IsControl(keyInfo.KeyChar))
                        Input += keyInfo.KeyChar.ToString();

                    if (keyInfo.Key == ConsoleKey.Backspace && Input.Length > 0)
                    {
                        Input = Input.Remove(Input.Length - 1);
                    }

                    Console.Write(keyInfo.KeyChar);
                    if (keyInfo.Key == ConsoleKey.Backspace)
                        Console.Write(" ");

                    Console.CursorLeft = infoString.Length + Input.Length - 1;
                } while (keyInfo.Key != ConsoleKey.Enter);

                if (GlobalInformation.currentTextChannel == null && !Input.StartsWith(prefix))
                    Input = prefix + Input;

                lastInput = new string(Input);
                Input = string.Empty;

                (exit, printOverride) = await commandsManager.CheckCommand(lastInput);
            }
        }
    }
}