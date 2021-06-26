# DiscordCLI

> Use Discord in the terminal

## Warning

**Use this software at your own risk!**\
This isn't directly a selfbot, but Discord may count it as User account automation and this is against their TOS.\
I guess everything should be fine as long as you don't behave conspicuously but still proceed with caution!\
Currently there are no known cases where a user got banned for using this Discord client but that doesn't mean it will never happen.\
Don't blame me if you get banned!\
I will update the above if anything changes!

## Usage

1. [Download]("https://github.com/Stone-Red-Code/DiscordCLI/releases
) one of the releases
2. Execute the `DiscordCLI.exe` file on windows or run `dotnet DiscordCLI.dll` on linux
3. Enter your [User Token](https://github.com/Tyrrrz/DiscordChatExporter/wiki/Obtaining-Token-and-Channel-IDs#how-to-get-a-user-token">)
4. Enter one of the commands below

## Commands

### help

- lists all commands

### exit

- exits application

### logout

- deletes auth token and exits application

### clear

- clears the console

### guilds

- lists all guilds you are in

### dms

- lists all private channels

### channels

- lists all channels of guild

  - args: \<guild name/index>

### enterg

- enter guild
- args: \<guild name/index>

### enterc

- enter channel
- args: \<channel name/index>

### enterd

- enter DM channel
- args: \<channel name/index>

### userinfo

- gets information about a user
- args: \<user name>

## Limitations

DiscordCLI doesn't support everything the official Discord app does.
If you find a missing feature in DiscordCLI that is not listed below please tell me or create a pull request and update the list below.

### Current limitations

- No voice support
- Mentions can't be created easily
- You can't add/remove guilds
- You can't create/accept friend requests
- Discord online status not updating

And probably more.
