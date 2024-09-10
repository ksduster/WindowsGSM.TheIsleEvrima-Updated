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
            name = "WindowsGSM.TheIsle", // WindowsGSM.XXXX
            author = "ksduster",
            description = "WindowsGSM plugin for supporting TheIsle Dedicated Server",
            version = "1.1",
            url = "https://github.com/kduster/TheIsleEvrima-Updated", // Github repository link (Best practice)
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
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "6777"; // Default port - adjusted from 7777 to 6777 to avoid accidently overlapping with other Unreal Engine Servers by default.
       // public string QueryPort = ""; //Adjusted to start at 6000 to avoid overlapping in WGSM
       // public string Defaultmap = ""; // Default map name
        public string Maxplayers = "75"; // Default maxplayers
        public string Additional = ""; // Additional server start parameter


        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            string name = String.Concat(FullName.Where(c => !Char.IsWhiteSpace(c)));

            //Download Game.ini
            if (await DownloadGameServerConfig(configPath, configPath))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{session_name}}", _serverData.ServerName);
                File.WriteAllText(configPath, configText);
            }
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            // Check for files in Win64
            string win64 = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Binaries\Win64\"));
            string[] neededFiles = { "steamclient64.dll", "tier0_s64.dll", "vstdlib_s64.dll" };

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

            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, @"TheIsle\Saved\Config\WindowsServer\Game.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            if (await adaptGameIniOnLaunch(configPath, configPath))
            {
                //Server Name Values
                string section = "/Script/TheIsle.IGameSession";
                string newServerNameValue = _serverData.ServerName;
                string serverNameKey = "ServerName";

                string[] lines = File.ReadAllLines(configPath);
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
                            File.WriteAllLines(configPath, lines);
                            System.Diagnostics.Debug.WriteLine("Value updated in file: " + configPath);
                            break;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Value already exists in file: " + configPath);
                            break;
                        }
                    } else {
                        continue;
                    }
                }
            }


            /*
               Update the Game.ini ServerAdmins= with pre-set adminfiles containing Steam IDS if existing.
               
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
               ServerAdmins=76561197960419839
               ServerAdmins=76561197960419840
               ServerAdmins=76561197960419841
               ServerAdmins=76561197960419842
               ServerAdmins=76561197960419843

               OBS: If admin list is specified, for each time you restart the server it will clear out all admins and re-apply accordingly from the list; if the source .txt files can be found - otherwise it will keep the original game.ini without refreshing the admins (In case the source is down so you suddenly dont have admin)
            */

            await UpdateAdminList(_serverData.ServerParam, configPath);


            /*
            if server default map contains either Isle V3 or Thenyaw or DV_TestLevel then add the string
            /Game/TheIsle/Maps/Landscape3/Isle_V3 for Isle V3
            /Game/TheIsle/Maps/Thenyaw_Island/Thenyaw_Island for Thenyaw
            /Game/TheIsle/Maps/Developer/DV_TestLevel for Dev Map
            */
            List<string> IsleV3Variations = new List<string>() { "Isle V3", "isle v3", "v3", "islev3" };
            List<string> ThenyawVariations = new List<string>() { "Thenyaw", "thenyaw", "ThenyawIsland", "Thenyaw Island" };
            List<string> TestlevelVariations = new List<string>() { "testlevel", "DV_TestLevel", "dm", "Test Level", "Dev Map", "Dev level" };

            string param = "";
            if (IsleV3Variations.Any(x => x.Equals(_serverData.ServerMap, StringComparison.OrdinalIgnoreCase)))
            {
                param += "/Game/TheIsle/Maps/Landscape3/Isle_V3";
            }
            else if (ThenyawVariations.Any(x => x.Equals(_serverData.ServerMap, StringComparison.OrdinalIgnoreCase)))
            {
                param += "/Game/TheIsle/Maps/Thenyaw_Island/Thenyaw_Island";
            }
            else if (TestlevelVariations.Any(x => x.Equals(_serverData.ServerMap, StringComparison.OrdinalIgnoreCase)))
            {
                param += "/Game/TheIsle/Maps/Developer/DV_TestLevel";
            }
            else
            {
                param = string.Empty;
            }

            //since the ServerStartParam can have multiple things here (such as adminLists) we divide it up using GetGameMode - to make sure we only put the gamemode into the actual Start Param of our game server. GetGameMode() splits out the relevant information to specify gamemode
            string gameMode = await GetGameMode(_serverData.ServerParam);

            //param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MultiHome={_serverData.ServerIP}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?Port={_serverData.ServerPort}";
            //param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?QueryPort={_serverData.ServerQueryPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MaxPlayers={_serverData.ServerMaxPlayer}";
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
        public static async Task<bool> DownloadGameServerConfig(string fileSource, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync($"https://raw.githubusercontent.com/ksduster/The-Isle-Evrima-ini/main/Game.ini", filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}");
            }

            return File.Exists(filePath);
        }

        public static async Task<bool> adaptGameIniOnLaunch(string fileSource, string filePath)
        {

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            //if the file DOESN'T exist - lets re-create it.
            if (!File.Exists(filePath))
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        await webClient.DownloadFileTaskAsync($"https://raw.githubusercontent.com/ksduster/The-Isle-Evrima-ini/main/Game.ini", filePath);
                    }
                }
                catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}"); 
                }
            }
            return File.Exists(filePath);
        }

        public static async Task<string> GetGameMode(string serverData)
        {
            string defaultGameMode = "game=Survival";
            string[] parts = serverData.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("game=", StringComparison.OrdinalIgnoreCase))
                {
                    if (part.Equals("game=Survival", StringComparison.OrdinalIgnoreCase) ||
                        part.Equals("game=Sandbox", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultGameMode = part;
                        break;
                    }
                }
            }

            return defaultGameMode;
        }

        public static async Task UpdateAdminList(string _serverData, string gameIniPath)
        {
            string[] splitServerData = _serverData.Split(';');
            Dictionary<string, string> adminListFiles = new Dictionary<string, string>();
            foreach (string s in splitServerData)
            {
                if (s.StartsWith("adminList", StringComparison.OrdinalIgnoreCase))
                {
                    string[] splitAdminList = s.Split('=');
                    adminListFiles.Add(splitAdminList[0], splitAdminList[1]);
                }
            }
            List<string> combinedAdminList = new List<string>();
            foreach (KeyValuePair<string, string> kvp in adminListFiles)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        string txtFile = await client.DownloadStringTaskAsync(kvp.Value);
                        string[] lines = txtFile.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        foreach (string line in lines)
                        {
                            combinedAdminList.Add(line.Trim());
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            if (combinedAdminList.Count > 0)
            {
                var lines = File.ReadAllLines(gameIniPath).ToList();
                int startIndex = lines.FindIndex(x => x.StartsWith("[/Script/TheIsle.TIGameStateBase]"));
                int endIndex = lines.FindIndex(startIndex, x => x.StartsWith("WhitelistIDs="));
                int currentIndex = startIndex + 1;
                while (currentIndex < endIndex)
                {
                    if (lines[currentIndex].StartsWith("ServerAdmins="))
                    {
                        lines.RemoveAt(currentIndex);
                        endIndex--;
                    }
                    else
                    {
                        currentIndex++;
                    }
                }
                int insertIndex = endIndex;
                foreach (string adminId in combinedAdminList)
                {
                    lines.Insert(insertIndex, $"ServerAdmins={adminId}");
                    insertIndex++;
                }
                File.WriteAllLines(gameIniPath, lines);
            }
        }

    }
}
