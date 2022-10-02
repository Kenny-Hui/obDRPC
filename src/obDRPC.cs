using System;
using System.Collections.Generic;
using DiscordRPC;
using System.Windows.Forms;
using OpenBveApi.FileSystem;
using OpenBveApi.Interface;
using OpenBveApi.Runtime;
using Button = DiscordRPC.Button;
using System.Reflection;

namespace obDRPC {
	/// <summary>
	/// Informations Default Displaying Plugin (for Testing)
	/// </summary>
	public class obDRPC : ITrainInputDevice
	{
		/// <summary>
		/// Define KeyDown event
		/// </summary>
		public event EventHandler<InputEventArgs> KeyDown;

		/// <summary>
		/// Define KeyUp event
		/// </summary>
		public event EventHandler<InputEventArgs> KeyUp;

		/// <summary>
		/// The control list that is using for plugin
		/// </summary>
		public InputControl[] Controls { get; private set; }

		private static string ClientId = "";
		private static string OptionsFolder;
		private static string oBVersion = "Unknown";
		private static bool IsInGame;
		private static int EMNotch;
		private static FileSystem FileSystem;
		private static Dictionary<string, RPCData> RichPresenceList = new Dictionary<string, RPCData>();
		private static Timestamps StartTimestamp;
		private static ElapseData LastElapseData;
		private static DateTime LastRPCUpdate;
		internal static DiscordRpcClient Client;
		internal static DoorStates doorState = DoorStates.None;
		private const int MAX_BTN_CHAR = 32;
		private const int RPC_REFRESH_INTERVAL = 1000;

		/// <summary>
		/// A function call when the plugin is loading
		/// </summary>
		/// <param name="fileSystem">The instance of FileSytem class</param>
		/// <returns>Check the plugin loading process is successfully</returns>
		public bool Load(FileSystem fileSystem)
		{
			Controls = new InputControl[1];
			FileSystem = fileSystem;
			StartTimestamp = Timestamps.Now;
			LastRPCUpdate = DateTime.UtcNow;
			oBVersion = getEntryVersion();
			OptionsFolder = OpenBveApi.Path.CombineDirectory(FileSystem.SettingsFolder, "1.5.0");
			LoadConfig();

			if (string.IsNullOrEmpty(ClientId)) {
				return false;
			}

			Client = new DiscordRpcClient(ClientId);
			if (Client.Initialize() == false) {
				FileSystem.AppendToLogFile("[DRPC] Failed to login to Discord, please make sure your Application ID is correct and you have a stable internet connection.");
				return false;
			}

            UpdatePresence(RichPresenceList.ContainsKey("menu") ? RichPresenceList["menu"] : null);
            return true;
		}

		/// <summary>
		/// A function call when the plugin is unload
		/// </summary>
		public void Unload()
		{
			IsInGame = false;
			StartTimestamp = Timestamps.Now;
			Client.Dispose();
		}

		/// <summary>
		/// A funciton call when the Config button pressed
		/// </summary>
		/// <param name="owner">The owner of the window</param>
		public void Config(IWin32Window owner) {
            using (var form = new ConfigForm(RichPresenceList, OptionsFolder)) {
                form.ShowDialog(owner);
            }
        }

		/// <summary>
		/// The function what the notify to the plugin that the train maximum notches
		/// </summary>
		/// <param name="powerNotch">Maximum power notch number</param>
		/// <param name="brakeNotch">Maximum brake notch number</param>
		public void SetMaxNotch(int powerNotch, int brakeNotch)
		{
		}

		/// <summary>
		/// The function what notify to the plugin that the train existing status
		/// </summary>
		/// <param name="data">Data</param>
		public void SetElapseData(ElapseData data)
		{
			if (!IsInGame) {
				IsInGame = true;
				StartTimestamp = Timestamps.Now;
			}

			StationManager.Update(data);

			LastElapseData = data;

			if ((DateTime.UtcNow - LastRPCUpdate).TotalMilliseconds >= RPC_REFRESH_INTERVAL) {
				if (StationManager.Boarding) {
					UpdatePresence(RichPresenceList.ContainsKey("boarding") ? RichPresenceList["boarding"] : null);
				} else {
					UpdatePresence(RichPresenceList.ContainsKey("game") ? RichPresenceList["game"] : null);
				}
				LastRPCUpdate = DateTime.UtcNow;
			}
        }

		/// <summary>
		/// A function that calls each frame
		/// </summary>
		public void OnUpdateFrame()
		{
		}

		protected virtual void OnKeyDown(InputEventArgs e)
		{
		}

		protected virtual void OnKeyUp(InputEventArgs e)
		{
		}

		private static void UpdatePresence(RPCData data) {
			if (data == null) {
				return;
			}
			
			RichPresence presence = new RichPresence();
            if (data.hasTimestamp) {
                presence.Timestamps = StartTimestamp;
            }
			
			presence.Details = ParsePlaceholders(data.details);
			presence.State = ParsePlaceholders(data.state);
			presence.Assets = new Assets();
			if (!string.IsNullOrEmpty(data.assetsData.LargeImageText)) presence.Assets.LargeImageText = ParsePlaceholders(data.assetsData.LargeImageText);
			if (!string.IsNullOrEmpty(data.assetsData.LargeImageKey)) presence.Assets.LargeImageKey = ParsePlaceholders(data.assetsData.LargeImageKey);
			if (!string.IsNullOrEmpty(data.assetsData.SmallImageText)) presence.Assets.SmallImageText = ParsePlaceholders(data.assetsData.SmallImageText);
			if (!string.IsNullOrEmpty(data.assetsData.SmallImageKey)) presence.Assets.SmallImageKey = ParsePlaceholders(data.assetsData.SmallImageKey);

			List<Button> buttons = new List<Button>();
			foreach (ButtonData btnData in data.buttons) {
				if (string.IsNullOrEmpty(btnData.Label) || string.IsNullOrEmpty(btnData.Url)) continue;

				Button btn = new Button();
				btn.Label = ParsePlaceholders(btnData.Label, MAX_BTN_CHAR);
				btn.Url = ParsePlaceholders(btnData.Url, MAX_BTN_CHAR);
				buttons.Add(btn);
			}

			if (buttons.Count > 0) {
				presence.Buttons = buttons.ToArray();
			}

			Client.SetPresence(presence);
		}

		private static string ParsePlaceholders(string str, int maxChar = 32767) {
			string modifiedStr = str == null ? "" : str;
			if (LastElapseData != null) {
				string speedKmh = Math.Abs(LastElapseData.Vehicle.Speed.KilometersPerHour).ToString("0");
				string speedMph = Math.Abs(LastElapseData.Vehicle.Speed.MilesPerHour).ToString("0");
				string reverser = LastElapseData.Handles.Reverser == -1 ? "B" : LastElapseData.Handles.Reverser == 0 ? "N" : "F";
				string powerNotch = LastElapseData.Handles.PowerNotch > 0 ? "P" + LastElapseData.Handles.PowerNotch : "N";
				string brakeNotch = LastElapseData.Handles.BrakeNotch == EMNotch ? "EMG" : LastElapseData.Handles.BrakeNotch > 0 ? "B" + LastElapseData.Handles.BrakeNotch : "N";
				string nextStationName = StationManager.NextStation == null ? "" : StationManager.NextStation.Name;
				string prevStationName = StationManager.PreviousStation == null ? "" : StationManager.PreviousStation.Name;
				string nextStationDist = Math.Abs(StationManager.NextStnDist) < 1 ? StationManager.NextStnDist.ToString("F") : ((int)Math.Round(StationManager.NextStnDist)).ToString();

				modifiedStr = modifiedStr
					.Replace("{time}", DateTimeOffset.FromUnixTimeMilliseconds((long)LastElapseData.TotalTime.Milliseconds).ToString("HH:mm:ss"))
					.Replace("{speedKmh}", speedKmh)
					.Replace("{speedMph}", speedMph)
					.Replace("{reverser}", reverser)
					.Replace("{powerNotch}", powerNotch)
					.Replace("{brakeNotch}", brakeNotch)
					.Replace("{notch}", LastElapseData.Handles.BrakeNotch > 0 ? brakeNotch : powerNotch)
					.Replace("{nextStnName}", nextStationName)
					.Replace("{prevStnName}", prevStationName)
					.Replace("{nextStnDist}", nextStationDist);

				if (StationManager.Boarding) {
					string doors = doorState == DoorStates.Left ? "<<< Doors" : doorState == DoorStates.Right ? "Doors >>>" : "<<< Doors >>>";
					modifiedStr = modifiedStr
						.Replace("{doors}", doors)
						.Replace("{curStnName}", StationManager.CurrentStation.Name)
						.Replace("{curStnDwell}", StationManager.CurrentStation.StopTime.ToString())
						.Replace("{curStnDwellLeft}", ((int)StationManager.DwellLeft).ToString());
				}
			}

			modifiedStr = modifiedStr.Replace("{programVersion}", oBVersion);
			return modifiedStr.Substring(0, Math.Min(modifiedStr.Length, maxChar));
		}

		internal static void LoadConfig()
		{
			if (!System.IO.Directory.Exists(OptionsFolder)) {
				System.IO.Directory.CreateDirectory(OptionsFolder);
			}

			string configFile = OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc.cfg");
			if (System.IO.File.Exists(configFile)) {
				string[] Lines = System.IO.File.ReadAllLines(configFile, new System.Text.UTF8Encoding());
				string Section = "";
				for (int i = 0; i < Lines.Length; i++) {
					string currentLine = Lines[i].Trim();
					if (currentLine.Length == 0 || currentLine.StartsWith(";")) {
						continue;
					}

					if (currentLine.StartsWith("[") && currentLine.EndsWith("]")) {
						Section = currentLine.Substring(1, currentLine.Length - 2);
						continue;
					}

					if (Section == "appId") {
						ClientId = currentLine;
						continue;
					}

					if (!currentLine.Contains("=")) continue;
					string key = currentLine.Split('=')[0].Trim().ToLowerInvariant();
					string value = currentLine.Split('=')[1].Trim();
                    RPCData presence;
                    if (RichPresenceList.ContainsKey(Section)) {
						presence = RichPresenceList[Section];
					} else {
						presence = new RPCData();
					}

					List<ButtonData> buttons = presence.buttons != null ? presence.buttons : new List<ButtonData>();
					Assets assets = presence.assetsData != null ? presence.assetsData : new Assets();

					if (key == "details") {
						presence.details = value;
					}

					if (key == "state") {
						presence.state = value;
					}

					if (key == "hastimestamp") {
						presence.hasTimestamp = value == "true";
					}

					if (key.StartsWith("button") && value.Contains("|")) {
						string text = value.Split('|')[0];
						string url = value.Split('|')[1];
						buttons.Add(new ButtonData(text, url));
					}

					if (key == "largeimagekey") {
						assets.LargeImageKey = value;
					}

					if (key == "largeimagetext") {
						assets.LargeImageText = value;
					}

					if (key == "smallimagekey") {
						assets.SmallImageKey = value;
					}

					if (key == "smallimagetext") {
						assets.SmallImageText = value;
					}

					presence.buttons = buttons;
					presence.assetsData = assets;

					if (RichPresenceList.ContainsKey(Section)) {
						RichPresenceList[Section] = presence;
					} else {
						RichPresenceList.Add(Section, presence);
					}
				}
			}
		}

		internal static string getEntryVersion() {
			return Assembly.GetEntryAssembly().GetName().Version.ToString();
		}

        internal static void SaveConfig() {

		}

        public void SetVehicleSpecs(VehicleSpecs specs) {
			EMNotch = specs.BrakeNotches + 1;
        }

        public void DoorChange(DoorStates oldState, DoorStates newState) {
			doorState = newState;
        }

        public void SetSignal(SignalData[] data) {
        }

        public void SetBeacon(BeaconData data) {
        }

		public static void RestartRPCClient() {
			//if (string.IsNullOrEmpty(ClientId)) {
			//	return;
			//}

			//LoadConfig();

			//Client = new DiscordRpcClient(ClientId);
			//if (Client.Initialize() == false) {
			//	return;
			//}

			//UpdatePresence(RichPresenceList.ContainsKey("menu") ? RichPresenceList["menu"] : null);
		}
    }
}
