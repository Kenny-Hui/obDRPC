using DiscordRPC;
using OpenBveApi.FileSystem;
using OpenBveApi.Interface;
using OpenBveApi.Runtime;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Button = DiscordRPC.Button;

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

		internal static Context CurrentContext;
		private string ProgramVersion;
		private bool IsInGame;
		private int SpeedLimit = 70;
		private VehicleSpecs specs;
		private FileSystem FileSystem;
		private int selectedProfile;
		private Timestamps StartTimestamp;
		private ElapseData LastElapseData;
		private DateTime LastRPCUpdate;
		private Placeholders Placeholder = new Placeholders();
		private DiscordRpcClient Client;
		private DoorStates doorState = DoorStates.None;
		private KeyboardState OldKeyboardState;
		private const int MAX_BTN_CHAR = 32;
		private const int RPC_REFRESH_INTERVAL = 1000;

		/// <summary>
		/// A function call when the plugin is loading
		/// </summary>
		/// <param name="fileSystem">The instance of FileSytem class</param>
		/// <returns>Check the plugin loading process is successfully</returns>
		public bool Load(FileSystem fileSystem)
		{
			ConfigManager.Initialize(fileSystem);
			Controls = new InputControl[1];
			FileSystem = fileSystem;
			StartTimestamp = Timestamps.Now;
			LastRPCUpdate = DateTime.UtcNow;
			ProgramVersion = getEntryVersion();
			CurrentContext = Context.Menu;
			ConfigManager.LoadConfig();
			selectedProfile = 0;

			if (!string.IsNullOrEmpty(ConfigManager.appId)) {
				Client = new DiscordRpcClient(ConfigManager.appId);
				if (!Client.Initialize()) {
					FileSystem.AppendToLogFile("[DRPC] Failed to login to Discord, please make sure your Application ID is correct and you have a stable internet connection.");
				}

                UpdatePresence(ConfigManager.ProfileList[selectedProfile].PresenceList["menu"]);
			}

            return true;
		}

		/// <summary>
		/// A function call when the plugin is unload
		/// </summary>
		public void Unload()
		{
			IsInGame = false;
			StartTimestamp = Timestamps.Now;
			Client?.Dispose();
		}

		/// <summary>
		/// A funciton call when the Config button pressed
		/// </summary>
		/// <param name="owner">The owner of the window</param>
		public void Config(IWin32Window owner) {
            using (var form = new ConfigForm(Client, ConfigManager.KeyCombination, Placeholder)) {
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
				CurrentContext = Context.InGame;
				StartTimestamp = Timestamps.Now;
			}

			StationManager.Update(data, doorState);
			LastElapseData = data;

			if (ConfigManager.ProfileList.Count > 0 && (DateTime.UtcNow - LastRPCUpdate).TotalMilliseconds >= RPC_REFRESH_INTERVAL) {
				if (StationManager.Boarding) {
					UpdatePresence(ConfigManager.ProfileList[selectedProfile].PresenceList["boarding"]);
				} else {
					UpdatePresence(ConfigManager.ProfileList[selectedProfile].PresenceList["game"]);
				}
				LastRPCUpdate = DateTime.UtcNow;
			}
        }

		/// <summary>
		/// A function that calls each frame
		/// </summary>
		public void OnUpdateFrame()
		{
			KeyboardState keyboardState = Keyboard.GetState();
			if (OldKeyboardState == null) {
				OldKeyboardState = keyboardState;
			}

			bool keyChanged = ConfigManager.KeyCombination.Any(key => OldKeyboardState[key] != keyboardState[key]);
			bool correctKeyHeld = ConfigManager.KeyCombination.Count > 0 ? ConfigManager.KeyCombination.All(key => keyboardState.IsKeyDown(key)) : false;

			if (keyChanged && correctKeyHeld) {
				selectedProfile = (selectedProfile + 1) % ConfigManager.ProfileList.Count;
			}
			OldKeyboardState = keyboardState;
		}

		protected virtual void OnKeyDown(InputEventArgs e)
		{
		}

		protected virtual void OnKeyUp(InputEventArgs e)
		{
		}

		private void UpdatePresence(RPCData data) {
			if (data == null || Client == null) {
				return;
			}
			
			RichPresence presence = new RichPresence();
            if (data.hasTimestamp) {
                presence.Timestamps = StartTimestamp;
            }
			
			presence.Details = ParsePlaceholders(data.details, 1024);
			presence.State = ParsePlaceholders(data.state, 1024);
			presence.Assets = new Assets();
			if (!string.IsNullOrEmpty(data.assetsData.LargeImageText)) presence.Assets.LargeImageText = ParsePlaceholders(data.assetsData.LargeImageText, 1024);
			if (!string.IsNullOrEmpty(data.assetsData.LargeImageKey)) presence.Assets.LargeImageKey = ParsePlaceholders(data.assetsData.LargeImageKey, 1024);
			if (!string.IsNullOrEmpty(data.assetsData.SmallImageText)) presence.Assets.SmallImageText = ParsePlaceholders(data.assetsData.SmallImageText, 1024);
			if (!string.IsNullOrEmpty(data.assetsData.SmallImageKey)) presence.Assets.SmallImageKey = ParsePlaceholders(data.assetsData.SmallImageKey, 1024);

			List<Button> buttons = new List<Button>();
			foreach (ButtonData btnData in data.buttons) {
				if (string.IsNullOrEmpty(btnData.Label) || string.IsNullOrEmpty(btnData.Url)) continue;

                Button btn = new Button
                {
                    Label = ParsePlaceholders(btnData.Label, MAX_BTN_CHAR),
                    Url = ParsePlaceholders(btnData.Url, MAX_BTN_CHAR)
                };

                buttons.Add(btn);
			}

			if (buttons.Count > 0) {
				presence.Buttons = buttons.ToArray();
			}

			Client.SetPresence(presence);
		}

		internal string getEntryVersion() {
			return Assembly.GetEntryAssembly().GetName().Version.ToString();
		}

        public void SetVehicleSpecs(VehicleSpecs specs) {
			this.specs = specs;
        }

		public string ParsePlaceholders(string input, int maxChar) {
			return Placeholder.ParsePlaceholders(input, ProgramVersion, LastElapseData, specs, doorState, CurrentContext, maxChar);
		}

        public void DoorChange(DoorStates oldState, DoorStates newState) {
			doorState = newState;
        }

        public void SetSignal(SignalData[] data) {
        }

		public void SetBeacon(BeaconData data) {
			// OpenBVE Speed limit
			if (data.Type == -16777214) {
				SpeedLimit = data.Optional;
			}
		}

		public static string getContextName(Context context) {
			switch (context) {
				case Context.Menu:
					return "menu";
				case Context.Boarding:
					return "boarding";
				case Context.InGame:
					return "game";
				default:
					return "";
			}
		}
	}
}
