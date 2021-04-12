using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCLI
{
    internal static class GlobalInformation
    {
        public static DiscordGuild currentGuild;
        public static DiscordChannel currentTextChannel;
        public static int colorMode = 1;
    }
}