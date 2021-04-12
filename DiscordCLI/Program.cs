using DSharpPlus;
using Stone_Red_Utilities.ColorConsole;
using System.IO;
using System.Threading.Tasks;
using System;

namespace DiscordCLI
{
    internal class Program
    {
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

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
                    TokenType = TokenType.User,
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