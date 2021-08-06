using DSharpPlus;
using System.Threading.Tasks;
using System;
using DSharpPlus.Entities;
using System.Linq;
using System.Collections.Generic;

namespace DiscordCLI
{
    internal class InputManager
    {
        private readonly CommandsManager commandsManager;
        private readonly DiscordClient client;
        private readonly List<string> previousInputs = new List<string>();
        public const string Prefix = ">";
        public string Input { get; private set; } = string.Empty;

        public InputManager(DiscordClient dicordClient, CommandsManager commandsMan)
        {
            client = dicordClient;
            commandsManager = commandsMan;
        }

        private static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public async Task ReadInput()
        {
            int inputListIndex = 0;
            bool exit = false;
            bool printOverride = false;
            string lastInput = Prefix;
            while (!exit)
            {
                DiscordDmChannel dmChannel = GlobalInformation.currentTextChannel as DiscordDmChannel;
                string infoString = $"\r[{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}] [{(dmChannel is null ? GlobalInformation.currentGuild?.Name : "DMs")}/{(dmChannel is null ? GlobalInformation.currentTextChannel?.Name : string.Join(", ", dmChannel.Recipients.Select(x => x.Username)))}] ==> ";
                if (lastInput.StartsWith(Prefix) || printOverride)
                    Console.Write(Environment.NewLine + infoString);

                printOverride = false;

                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey(true);
                    if (!char.IsControl(keyInfo.KeyChar))
                        Input += keyInfo.KeyChar.ToString();

                    System.Diagnostics.Debug.WriteLine(inputListIndex);
                    System.Diagnostics.Debug.WriteLine(previousInputs.Count - inputListIndex - 1);
                    if (keyInfo.KeyChar == (int)ConsoleKey.Backspace && Input.Length > 0)
                    {
                        Input = Input.Remove(Input.Length - 1);
                    }
                    else if (keyInfo.Key == ConsoleKey.UpArrow)
                    {
                        int previousLength = Input.Length;
                        if (previousInputs.Count > 0)
                            Input = previousInputs[previousInputs.Count - inputListIndex - 1];

                        if (inputListIndex < previousInputs.Count - 2)
                            inputListIndex++;
                        Console.CursorLeft = infoString.Length - 1;
                        Console.Write(Input + new string(' ', Math.Max(previousLength - Input.Length, 0)));
                    }
                    else if (keyInfo.Key == ConsoleKey.DownArrow)
                    {
                        int previousLength = Input.Length;
                        if (previousInputs.Count > 0)
                            Input = previousInputs[previousInputs.Count - inputListIndex - 1];

                        if (inputListIndex > 0)
                            inputListIndex--;
                        Console.CursorLeft = infoString.Length - 1;
                        Console.Write(Input + new string(' ', Math.Max(previousLength - Input.Length, 0)));
                    }

                    Console.Write(keyInfo.KeyChar);
                    if (keyInfo.Key == ConsoleKey.Backspace)
                        Console.Write(" ");

                    Console.CursorLeft = infoString.Length + Input.Length - 1;
                } while (keyInfo.Key != ConsoleKey.Enter);

                if (GlobalInformation.currentTextChannel == null)
                {
                    Input = Input.Trim();
                    if (!Input.StartsWith(Prefix))
                        Input = Prefix + Input;

                    if (previousInputs.Count > 10)
                        previousInputs.RemoveAt(0);

                    if (previousInputs.Count == 0 || previousInputs[previousInputs.Count - 1] != Input)
                        previousInputs.Add(new string(Input));
                }

                inputListIndex = 0;
                lastInput = new string(Input);
                Input = string.Empty;

                (exit, printOverride) = await commandsManager.CheckCommand(lastInput);
            }
        }
    }
}