using DSharpPlus;
using System.IO;
using System.Threading.Tasks;
using System;
using DSharpPlus.Exceptions;
using Stone_Red_Utilities.ConsoleExtentions;
using System.Reflection;
using AlwaysUpToDate;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DiscordCLI
{
    internal class Program
    {
        public const string tokenPath = "token.txt";

        private DiscordClient client;
        private CommandsManager commandsManager;
        private InputManager inputManager;
        private OutputManager outputManager;

#if DEBUG
        private Updater updater = new Updater(new TimeSpan(0), "https://raw.githubusercontent.com/Stone-Red-Code/DiscordCLI/develop/update/updateInfo.json", "./", true);
#else
        private Updater updater = new Updater(new TimeSpan(0), "https://raw.githubusercontent.com/Stone-Red-Code/DiscordCLI/main/update/updateInfo.json", "./", true);
#endif

        public static void Main() => new Program().Setup().GetAwaiter().GetResult();

        private async Task Setup()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            updater.UpdateAvailible += Updater_UpdateAvailible;
            updater.NoUpdateAvailible += Updater_NoUpdateAvailible;
            updater.ProgressChanged += Updater_ProgressChanged;
            updater.OnException += Updater_OnException;

            updater.Start();
            await Task.Delay(-1);
        }

        private async Task Start()
        {
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

        private async void Updater_NoUpdateAvailible()
        {
            await Start();
        }

        private void Updater_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            Console.CursorLeft = 0;
            ConsoleExt.WriteLine($"Downloading Update: {progressPercentage}% {totalBytesDownloaded}/{totalFileSize}", ConsoleColor.Yellow);
        }

        private void Updater_UpdateAvailible(string version, string additionalInformation)
        {
            Console.WriteLine($"New version avalible! {version}");
            updater.Update();
        }

        private void Updater_OnException(Exception exception)
        {
            ConsoleExt.WriteLine(exception.Message, ConsoleColor.Red);
            Environment.Exit(-1);
        }

        private async Task Client_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            try
            {
                await outputManager.WriteMessage(e.Message, e.Channel, e.Guild);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}