using DSharpPlus;
using System.IO;
using System.Threading.Tasks;
using System;
using DSharpPlus.Exceptions;
using Stone_Red_Utilities.ConsoleExtentions;
using AlwaysUpToDate;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DiscordCLI
{
    internal class Program
    {
        public const string tokenPath = "token.txt";

        private DiscordClient client;
        private CommandsManager commandsManager;
        private InputManager inputManager;
        private OutputManager outputManager;
        private Updater updater;

        public static void Main() => new Program().Setup().GetAwaiter().GetResult();

        private async Task Setup()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            PlatformType platformType;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    platformType = PlatformType.LinuxARM;
                }
                else
                {
                    platformType = PlatformType.Linux;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformType = PlatformType.Windows;
            }
            else
            {
                throw new PlatformNotSupportedException("DiscordCLI only supports Windows and Linux!");
            }
#if DEBUG
            updater = new Updater(new TimeSpan(0), $"https://raw.githubusercontent.com/Stone-Red-Code/DiscordCLI/develop/update/updateInfo{platformType}.json", "./", true);
#else
            updater = new Updater(new TimeSpan(0), $"https://raw.githubusercontent.com/Stone-Red-Code/DiscordCLI/main/update/updateInfo{platformType}.json", "./", true);
#endif

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
            Environment.Exit(0);
        }

        private async void Updater_NoUpdateAvailible()
        {
            await Start();
        }

        private void Updater_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            Console.CursorLeft = 0;
            ConsoleExt.WriteLine($"Downloading update: {progressPercentage}% {totalBytesDownloaded}/{totalFileSize}", ConsoleColor.Yellow);
        }

        private void Updater_UpdateAvailible(string version, string additionalInformation)
        {
            Console.WriteLine($"New version avalible! {version}");
            updater.Update();
        }

        private void Updater_OnException(Exception exception)
        {
            ConsoleExt.WriteLine("Update failed!", ConsoleColor.Red);
            ConsoleExt.WriteLine(exception.Message, ConsoleColor.Red);
            Environment.Exit(-1);
        }

        private async Task Client_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            try
            {
                await outputManager.WriteMessage(e.Message, e.Channel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}