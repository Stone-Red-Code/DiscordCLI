using DSharpPlus;
using System.Threading.Tasks;
using System;

namespace DiscordCLI
{
    internal class InputManager
    {
        private readonly CommandsManager commandsManager;
        private readonly DiscordClient client;
        public string Input { get; private set; } = "<";

        public InputManager(DiscordClient dicordClient, CommandsManager commandsMan)
        {
            client = dicordClient;
            commandsManager = commandsMan;
        }

        public async Task ReadInput()
        {
            await Task.Run(() =>
            {
                bool exit = false;
                while (!exit)
                {
                    string infoString = $"\r[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{GlobalInformation.currentGuild?.Name}/{GlobalInformation.currentTextChannel?.Name}] ==> ";
                    if (Input.StartsWith('<'))
                        Console.Write(Environment.NewLine + infoString);

                    Input = string.Empty;

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

                    if (GlobalInformation.currentTextChannel == null && !Input.StartsWith('<'))
                        Input = "<" + Input;

                    exit = commandsManager.CheckCommand(Input);
                }
                Environment.Exit(0);
            });
        }
    }
}