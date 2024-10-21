# WindowsGSM.TheIsleEvrima

🧩WindowsGSM plugin that provides TheIsle Evrima Dedicated server support! Updated for the newest Evrima version.

- A modified version of [@menix1337](https://www.github.com/menix1337)'s [Legacy](https://github.com/menix1337/WindowsGSM.TheIsleLegacy) 

# This version adds in support for:
- Automatic download of Game.ini and Engine.ini upon server bug from Evrima where the files are deleted when stopped.
- Game.ini settings under [/Script/TheIsle.TIGameSession] are updated from start params.  Do not update servername or maxplayercount in start params.
- Admin lists\*

\*Admin lists is a optional way for you to support having one or multiple text files (Admin lists are textfiles with lines of Steam IDs) and the ability to add one or multiple per server, leaving the server owners only having to update a a single text file - to update all the servers you want, with your admins

# The Game

https://store.steampowered.com/app/376210/The_Isle/ (Beta - Evrima!)

# Requirements

WindowsGSM >= 1.21.0

# Installation

1. Download the latest release
2. Move TheIsle.cs folder to plugins folder
3. Click [RELOAD PLUGINS] button or restart WindowsGSM

# Start Paramater Options (Optional)

Here is a default [Game.ini](https://github.com/ksduster/The-Isle-Evrima-ini/blob/main/Game.ini). 
Only values in the `/Script/TheIsle.TIGameSession` section can be modified.

After installing the server, select the "Edit Config" option. 
Under the "Server Start Param" enter the ConfigOption=true/false, and seperate with semicolon (`;`)

Example: `bEnableHumans=true;bQueueEnabled=true;QueuePort=9999`

The next time you start or restart your server the values will be added to the game.ini in the `/Script/TheIsle.TIGameSession` section

Do not add `Servername` or `MaxPlayerCount` values, as these are updated from the server config values in the edit window.

Availabe values:

`bEnableHumans=true/false`

`bQueueEnabled=true/false`

`QueuePort=portnumber`

`bServerPassword=true/false`

`ServerPassword="password"` // Keep the quotations. Change the `password`

`bRconEnabled=true/false` // Remote console commands

`RconPassword="password"`  // Do Not keep as `password` - Do not make this public

`RconPort=portnumber`

`bServerDynamicWeather=true/false` // Evrima update 0.15.191 - Temporarily disabled - has no effect

`ServerDayLengthMinutes=45`  // Value in minutes

`ServerNightLengthMinutes=20` // Value in minutes

`bServerWhitelist=true/false` // Enable/Disable whitelist - You must manually add steamIDs in the Game.ini `/Script/TheIsle.TIGameStateBase` section.  1 steam ID per line `WhitelistIDs=<steamid64>`  (future project)

`bEnableGlobalChat=true/false` // Turn on/off Global Chat. Disabled by default.

`bSpawnPlants=true/false` // Turn on/off Plants for herbivors. Enabled by default

`bSpawnAI=true/false` // Turn on/off AI for players to eat. Enabled by default

`AISpawnInterval=40` // Value in seconds to check if players are hungry and spawn if needed

`bEnableMigration=true/false` // Enabled by default. Makes players move across the map for a well balanced meal. Allows more likely chances of PvP encounters.

`MaxMigrationTime=5400` // Value in seconds. Default=5400 which is 90 minutes.  Math is seconds / 60 = minutes.  so 5400 / 60 = 90 minutes.

`GrowthMultiplier=1` // Value is 1 by default. Per developers: Universal multiplier for growth, putting this number too high will stop it to work (stay below ~20)

`bEnableMutations=true/false` // Enable/disable all mutations.  See Game.ini to enable/disable specific mutation types.

`Discord=""` // Example: Discord="https://discord.gg/abc1234" - Make sure the link does not expire from your discord otherwise users will click the discord button in game menu and the website that opens will go no where.


# Admin Lists (Not required!)   Credit: [@menix1337](https://www.github.com/menix1337)'s [Legacy](https://github.com/menix1337/WindowsGSM.TheIsleLegacy)

This plugin has the ability for the server hosters to specify one or multiple text files with lists of SteamIDs (Currently only tested with RAW Github Repo files) adding them automatically to the servers Game.ini file.
This means if you are a hoster having multiple servers, and you dislike having to spend hours on adjusting each Game.ini file for adding/removings - this might be an option for you.

1. In the servers Start Param option field add in the following (\*Examples with my test files, use your own!)

- `adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt`

You can add multiple lists by adding a semi-colon (`;`)with a new list entry such as:

- `adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlisttwo.txt`

-- And if you wish you can expand to this list with AdminListThree, AdminListFour - followed by the links as the examples above... etc (There should be no limit in theory)

## So what happens with these lists?

WindowsGSM will open the text file and merge each Steam ID on the list into a combined list & add `ServerAdmin=` in front & modify your Game.ini by adding them in there.
So make sure all admins you want to have as admin in game, are added to these lists, if you use decide it.

So lets say if you have 2 steamids in adminListOne and 1 steamid in adminListTwo, it will combine them into 3 Steam IDs, getting added as admin on your server.
(This gives you an option to add seperate lists for lets say';' Deathmatch, Event servers where you maybe need more people to be admin, that you don't want to have admin on the other servers)

So in theory adminListOne could be your main admins
adminListTwo could be trial admins
adminListThree could eventually be DM/Event related admins

- and combined they will make 1 admin list in your server.

**OBS: Currently only supporting text file, laying online in places such as GitHub etc. (Raw text files)**
**- In case your source for admin lists textfiles goes down, or you do not apply one - it will just keep using the Game.Ini you already have**

# So how could a final Server Start Param look? (With and Without the usage of admin lists)

`bEnableHumans=true;bQueueEnabled=true;QueuePort=9999` or
`bEnableHumans=true;bQueueEnabled=true;QueuePort=9999;adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt` or
`bEnableHumans=true;bQueueEnabled=true;QueuePort=9999;adminListOne=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Other/adminlisttwo.txt`

**OBS: Remember if you use Admin Lists to adjust them into your own Steam IDs. The Steam IDs & lists provided in the examples are only for an example purpose**

# License

This project is licensed under the MIT License - see the <a href="https://raw.githubusercontent.com/ksduster/WindowsGSM.TheIsle/main/LICENSE">LICENSE.md</a> file for details
