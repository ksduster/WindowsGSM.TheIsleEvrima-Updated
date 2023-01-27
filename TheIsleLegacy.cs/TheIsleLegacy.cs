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


namespace WindowsGSM.Plugins
{
    public class TheIsleLegacy : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.TheIsleLegacy", // WindowsGSM.XXXX
            author = "MENIX",
            description = "WindowsGSM plugin for supporting TheIsle Legacy Dedicated Server",
            version = "1.0",
            url = "https://github.com/menix1337/WindowsGSM.TheIsleLegacy", // Github repository link (Best practice)
            color = "#34c9eb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "412680"; // Game server appId, TheIsle is 412680

        // - Standard Constructor and properties
        public TheIsleLegacy(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;


        // - Game server Fixed variables
        public override string StartPath => @"TheIsle\Binaries\Win64\TheIsleServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "The Isle Legacy Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()


        // - Game server default values
        public string Port = "7777"; // Default port
        public string QueryPort = "27020"; // Default query port
        public string Defaultmap = "Isle V3"; // Default map name
        public string Maxplayers = "150"; // Default maxplayers
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

            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MultiHome={_serverData.ServerIP}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?Port={_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?QueryPort={_serverData.ServerQueryPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $"?MaxPlayers={_serverData.ServerMaxPlayer}";
            param += $"?{_serverData.ServerParam} -nosteamclient -game -server -log";

            System.Console.WriteLine(param);





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
                    await webClient.DownloadFileTaskAsync($"https://raw.githubusercontent.com/menix1337/WindowsGSM.configs/main/TheIsleLegacy/Game.ini", filePath);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Github.DownloadGameServerConfig {e}");
            }

            return File.Exists(filePath);
        }
    }
}