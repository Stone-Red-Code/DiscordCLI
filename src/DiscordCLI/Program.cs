using DSharpPlus;
using System.IO;
using System.Threading.Tasks;
using System;
using DSharpPlus.Exceptions;
using Stone_Red_Utilities.ConsoleExtentions;

namespace DiscordCLI
{
    internal class Program
    {
        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordClient client;
        private InputManager inputManager;
        private OutputManager outputManager;
        private CommandsManager commandsManager;

        public const string tokenPath = "token.txt";

        public async Task MainAsync()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string token = string.Empty;
            if (File.Exists(tokenPath))
                token = File.ReadAllText(tokenPath);

            tokenInput:
            while (string.IsNullOrWhiteSpace(token))
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
                    TokenType = TokenType.User,
                });

                client.MessageCreated += Client_MessageCreated;

                await client.ConnectAsync();
                ConsoleExt.WriteLine("Welcome to DiscordCLI", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteLine(ex.Message, ConsoleColor.Red);

                if (ex.InnerException is UnauthorizedException)
                {
                    File.Delete(tokenPath);
                    token = string.Empty;
                    goto tokenInput;
                }
                return;
            }

            outputManager = new OutputManager(client);
            commandsManager = new CommandsManager(client, outputManager);
            inputManager = new InputManager(client, commandsManager);
            outputManager.InputManager = inputManager;

            await inputManager.ReadInput();
        }

        private async Task Client_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            await outputManager.WriteMessage(e.Message, e.Channel, e.Guild);
        }
    }
}