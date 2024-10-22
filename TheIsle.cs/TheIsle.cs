using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;


namespace WindowsGSM.Plugins
{
    public class TheIsle : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.TheIsleEvrima", // WindowsGSM.XXXX
            author = "ksduster",
            description = "WindowsGSM plugin for supporting TheIsle Evrima Dedicated Server",
            version = "1.3.1",
            url = "https://github.com/ksduster/WindowsGSM.TheIsle", // Github repository link (Best practice)
            color = "#34c9eb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "412680 -beta evrima"; // Game server appId, TheIsle is 412680

        // - Standard Constructor and properties
        public TheIsle(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => @"TheIsle\Binaries\Win64\TheIsleServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "The Isle Evrima Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?  // Evrima nolonger outputs to console
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "6777"; // Default port - adjusted from 7777 to 6777 to avoid accidently overlapping with other Unreal Engine Servers by default.
        public string QueryPort = "6000"; //Adjusted to start at 6000 to avoid overlapping in WGSM
        public string Defaultmap = "Gateway"; // Default map name
        public string Maxplayers = "75"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation for game.ini and engine.ini
        public async void CreateServerCFG()
        {
            // Game.ini path
            string gameIniPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(gameIniPath));

            // Engine.ini path
            string engineIniPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Engine.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(engineIniPath));
            
            // Download Game.ini if missing
            if (await DownloadGameServerConfig(gameIniPath, gameIniPath, "Game"))
            {
                string configText = File.ReadAllText(gameIniPath);
                configText = configText.Replace("{{session_name}}", _serverData.ServerName);
                File.WriteAllText(gameIniPath, configText);
            }

            // Download Engine.ini if missing
            if (!File.Exists(engineIniPath))
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        await webClient.DownloadFileTaskAsync($"https://raw.githubusercontent.com/ksduster/The-Isle-Evrima-ini/main/Engine.ini", engineIniPath);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Github.DownloadEngineIniConfig {e}");
                }
            }
        }


        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            // Check for files in Win64
            string win64 = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Binaries\Win64\"));
            string[] neededFiles = { "tbb.dll", "tbb12.dll", "tbbmalloc.dll" };

            foreach (string file in neededFiles)
            {
                if (!File.Exists(Path.Combine(win64, file)))
                {
                    File.Copy(Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), file), Path.Combine(win64, file));
                }
            }

            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            // Prepare start parameter

            /*Here we wanna update our Game.ini to have the most "Recent Server Name" from the GSM Program - instead of making it just keep the original name from CFG on creation.
            First we of course check the file exists
            - If not existing, we will download a fresh one.
            - If existing, we will update the server name from WGSM into Game.ini
            
             */
            string engineIniPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Engine.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(engineIniPath));

            if (await adaptIniOnLaunch(engineIniPath, engineIniPath, "Engine"));
            {
            }

            string gameIniPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(gameIniPath));

            /* if (await adaptIniOnLaunch(gameIniPath, gameIniPath, "Game"))
            {
                //Server Name Values
                string section = "/Script/TheIsle.TIGameSession";
                string newServerNameValue = _serverData.ServerName;
                string serverNameKey = "ServerName";
                
                // Max Player Count Values
                string newMaxPlayerValue = _serverData.ServerMaxPlayer;
                string maxPlayerKey = "MaxPlayerCount";

                string[] lines = File.ReadAllLines(gameIniPath);
                bool foundSection = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Equals("[" + section + "]"))
                    {
                        foundSection = true;
                        continue;
                    }
                    if (foundSection && lines[i].Trim().StartsWith(serverNameKey))
                    {
                        string[] parts = lines[i].Split('=');
                        if (parts.Length >= 2 && !parts[1].Equals(newServerNameValue))
                        {
                            lines[i] = serverNameKey + "=" + newServerNameValue;
                            File.WriteAllLines(gameIniPath, lines);
                            System.Diagnostics.Debug.WriteLine("Value updated in file: " + gameIniPath);
                            break;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Value already exists in file: " + gameIniPath);
                            break;
                        }
                    } else {
                        continue;
                    }
                }
            } */
            if (await adaptIniOnLaunch(gameIniPath, gameIniPath, "Game"))
            {
                //Server Name Values
                string section = "/Script/TheIsle.TIGameSession";
                string newServerNameValue = _serverData.ServerName;
                string serverNameKey = "ServerName";

                // Max Player Count Values
                string newMaxPlayerValue = _serverData.ServerMaxPlayer;
                string maxPlayerKey = "MaxPlayerCount";

                string[] lines = File.ReadAllLines(gameIniPath);
                bool foundSection = false;
                bool serverNameUpdated = false;
                bool maxPlayerCountUpdated = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Equals("[" + section + "]"))
                    {
                        foundSection = true;
                        continue;
                    }
        
                    if (foundSection)
                    {
                        // Update ServerName
                        if (!serverNameUpdated && lines[i].Trim().StartsWith(serverNameKey))
                        {
                            string[] parts = lines[i].Split('=');
                            if (parts.Length >= 2 && !parts[1].Equals(newServerNameValue))
                            {
                                lines[i] = serverNameKey + "=" + newServerNameValue;
                                serverNameUpdated = true;
                                System.Diagnostics.Debug.WriteLine("ServerName updated in file: " + gameIniPath);
                            }
                            else
                            {
                                serverNameUpdated = true; // Already up-to-date
                            }
                        }

                        // Update MaxPlayerCount
                        if (!maxPlayerCountUpdated && lines[i].Trim().StartsWith(maxPlayerKey))
                        {
                            string[] parts = lines[i].Split('=');
                            if (parts.Length >= 2 && !parts[1].Equals(newMaxPlayerValue))
                            {
                                lines[i] = maxPlayerKey + "=" + newMaxPlayerValue;
                                maxPlayerCountUpdated = true;
                                System.Diagnostics.Debug.WriteLine("MaxPlayerCount updated in file: " + gameIniPath);
                            }
                            else
                            {
                                maxPlayerCountUpdated = true; // Already up-to-date
                            }
                        }

                        // Break if both values are updated
                        if (serverNameUpdated && maxPlayerCountUpdated)
                        {
                            File.WriteAllLines(gameIniPath, lines);
                            break;
                        }
                    }
                }

                // If the MaxPlayerCount key doesn't exist, insert it after ServerName
                if (foundSection && serverNameUpdated && !maxPlayerCountUpdated)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().StartsWith(serverNameKey))
                        {
                            lines[i] += Environment.NewLine + maxPlayerKey + "=" + newMaxPlayerValue;
                            File.WriteAllLines(gameIniPath, lines);
                            System.Diagnostics.Debug.WriteLine("MaxPlayerCount added after ServerName in file: " + gameIniPath);
                            break;
                        }
                    }
                }
            }




            /*
               Update the Game.ini AdminsSteamIDs= with pre-set adminfiles containing Steam IDS if existing.
               
               Admin List Mode - OBS: NOT REQUIRED
               - Our goal here is to make adding admins on multiple servers easier than having to manually adjust each server every time they change admins

               Lets load an an admin list from a text file, but only if the adminList=XXX exists in the Param.
               - example: 
               game=Survival;adminList=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Asura/TheIsleLegacy/Adminlist.txt

               Some servers have different admins depending if the server is a Deathmatch type server or not - to help this we can add in a second admin list (adminListTwo) & combine the adminList & adminListTwo
               - example: 
               game=Survival;adminList=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Asura/TheIsleLegacy/Adminlist.txt;adminListTwo=https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/Asura/TheIsleLegacy/AdminlistDM.txt

               Then the server will merge and update the lists into the game.ini with the IDs of each line in the text files

               adminList.txt file example (Have each id on each line):
               76561197960419839
               76561197960419840
               76561197960419841
               76561197960419842

               then adminListTwo.txt file example:
               76561197960419843

               You can also add more lists like adminListThree.txt, adminListFour.txt
               - just make sure each list starts with adminList

               Will combine into a total of this when put into the game ini:
               AdminsSteamIDs=76561197960419839
               AdminsSteamIDs=76561197960419840
               AdminsSteamIDs=76561197960419841
               AdminsSteamIDs=76561197960419842
               AdminsSteamIDs=76561197960419843

               OBS: If admin list is specified, for each time you restart the server it will clear out all admins and re-apply accordingly from the list; if the source .txt files can be found - otherwise it will keep the original game.ini without refreshing the admins (In case the source is down so you suddenly dont have admin)
            */

            await UpdateSettings(_serverData.ServerParam, gameIniPath);

            string param = "";  

            {
                param = string.Empty;
            }

            //since the ServerStartParam can have multiple things here (such as adminLists) we divide it up using GetGameMode - to make sure we only put the gamemode into the actual Start Param of our game server. GetGameMode() splits out the relevant information to specify gamemode

            //param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MultiHome={_serverData.ServerIP}";  // Multihome is depreciated now for Evrima.
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?Port={_serverData.ServerPort}";
            //param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?QueryPort={_serverData.ServerQueryPort}";  // Query port is depreciated in Evrima
            //param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MaxPlayers={_serverData.ServerMaxPlayer}"; // Player limit is set in Game.ini now
            param += $"? -nosteamclient -game -server -log";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                // Start Process
                try
                {
                    p.Start();
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    return null; // return null if fail to start
                }

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                return p;
            }

            // Start Process
            try
            {
                p.Start();
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }


        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.CreateNoWindow)
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("^c");

                }
                else
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("^c");
                }
            });
        }

        // Get ini files
        public static async Task<bool> DownloadGameServerConfig(string fileSource, string filePath, string iniType)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            if (File.Exists(filePath))
            {
                // File.Delete(filePath);
                return true;
            }

            string downloadUrl = iniType == "Game" 
                ? $"https://raw.githubusercontent.com/ksduster/The-Isle-Evrima-ini/main/Game.ini"
                : $"https://raw.githubusercontent.com/ksduster/The-Isle-Evrima-ini/main/Engine.ini"; // Add Engine.ini URL here

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(downloadUrl, filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}");
            }

            return File.Exists(filePath);
        }

        public static async Task<bool> adaptIniOnLaunch(string fileSource, string filePath, string iniType)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            // if the file DOESN'T exist - lets re-create it.
            if (!File.Exists(filePath))
            {
                try
                {
                    await DownloadGameServerConfig(fileSource, filePath, iniType); // Add iniType to specify Game or Engine
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}");
                }
            }
            return File.Exists(filePath);
        }

        public static async Task UpdateSettings(string _serverData, string gameIniPath)
        {
            // Parse server data
            string[] splitServerData = _serverData.Split(';');
            var adminListFiles = new Dictionary<string, string>();
            var otherSettings = new Dictionary<string, string>();

            foreach (string s in splitServerData)
            {
                string[] splitSetting = s.Split('=');
                if (splitSetting.Length == 2)
                {
                    if (splitSetting[0].StartsWith("adminList", StringComparison.OrdinalIgnoreCase))
                    {
                        adminListFiles.Add(splitSetting[0].Trim(), splitSetting[1].Trim());
                    }
                    else
                    {
                        otherSettings.Add(splitSetting[0].Trim(), splitSetting[1].Trim());
                    }
                }
            }

            // Fetch and combine admin list
            var combinedAdminList = new List<string>();
            foreach (var kvp in adminListFiles)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        string txtFile = await client.DownloadStringTaskAsync(kvp.Value);
                        string[] fileLines = txtFile.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        foreach (string line in fileLines)
                        {
                            combinedAdminList.Add(line.Trim());
                        }
                    }
                }
                catch (Exception)
                {
                    // Handle exceptions (e.g., logging)
                    continue;
                }
            }

            // Read and update the configuration file
            var lines = File.ReadAllLines(gameIniPath).ToList();
            int startIndex = lines.FindIndex(x => x.StartsWith("[/Script/TheIsle.TIGameSession]"));
            int endIndex = lines.FindIndex(startIndex, x => x.StartsWith("[") && !x.StartsWith("[/Script/TheIsle.TIGameSession]"));

            if (startIndex == -1 || endIndex == -1) return; // Section not found

            // Remove old settings in the section
            int currentIndex = startIndex + 1;
            while (currentIndex < endIndex)
            {
                if (otherSettings.Keys.Any(key => lines[currentIndex].StartsWith(key)))
                {
                    lines.RemoveAt(currentIndex);
                    endIndex--;
                }
                else
                {
                    currentIndex++;
                }
            }

            // Insert new settings in the section
            int insertIndex = endIndex;
            foreach (var setting in otherSettings)
            {
                lines.Insert(insertIndex, $"{setting.Key}={setting.Value}");
                insertIndex++;
            }

            // Update admin list
            if (combinedAdminList.Count > 0)
            {
                int adminListStartIndex = lines.FindIndex(x => x.StartsWith("[/Script/TheIsle.TIGameStateBase]"));
                int adminListEndIndex = lines.FindIndex(adminListStartIndex, x => x.StartsWith("WhitelistIDs="));
                currentIndex = adminListStartIndex + 1;

                // Remove old AdminsSteamIDs entries
                while (currentIndex < adminListEndIndex)
                {
                    if (lines[currentIndex].StartsWith("AdminsSteamIDs="))
                    {
                        lines.RemoveAt(currentIndex);
                        adminListEndIndex--;
                    }
                    else
                    {
                        currentIndex++;
                    }
                }

                // Insert new AdminsSteamIDs
                int adminInsertIndex = adminListEndIndex;
                foreach (string adminId in combinedAdminList)
                {
                    lines.Insert(adminInsertIndex, $"AdminsSteamIDs={adminId}");
                    adminInsertIndex++;
                }
            }

            // Write the updated lines back to the file
            File.WriteAllLines(gameIniPath, lines);
        }
    }
}
