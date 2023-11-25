using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DiscordRPC;
using OpenBveApi.FileSystem;
using OpenBveApi.Interface;
using OpenBveApi.Runtime;
using OpenTK.Input;
using Button = DiscordRPC.Button;

namespace obDRPC
{
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

		internal static Context CurrentContext = Context.Menu;
        private int SpeedLimit = 80;
        private string ProgramVersion;
		private bool GameStarted;
		private VehicleSpecs VehicleSpecs;
		private FileSystem FileSystem;
		private int SelectedProfileIndex;
		private Timestamps StartTimestamp;
		private ElapseData LastElapseData;
		private DateTime LastRPCUpdate;
		private Placeholders Placeholder = new Placeholders();
		private DiscordRpcClient Client;
		private DoorStates DoorState = DoorStates.None;
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
			Controls = new InputControl[1];
			FileSystem = fileSystem;
            SelectedProfileIndex = 0;
            StartTimestamp = Timestamps.Now;
			LastRPCUpdate = DateTime.UtcNow;
			ProgramVersion = GetProgramVersion();
            ConfigManager.Initialize(fileSystem);
            ConfigManager.LoadConfig();
            InitRPC();
            return true;
		}

		/// <summary>
		/// A function call when the plugin is unload
		/// </summary>
		public void Unload()
		{
			GameStarted = false;
			StartTimestamp = Timestamps.Now;
			Client?.Dispose();
		}

		/// <summary>
		/// A funciton call when the Config button pressed
		/// </summary>
		/// <param name="owner">The owner of the window</param>
		public void Config(IWin32Window owner) {
            using (var form = new ConfigForm(Client, Placeholder, UpdatePresence)) {
                form.ShowDialog(owner);
                // Restart RPC after config is changed
                InitRPC();
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
			if (!GameStarted) {
				GameStarted = true;
				CurrentContext = Context.InGame;
				StartTimestamp = Timestamps.Now;
			}

			StationManager.Update(data, DoorState);
			LastElapseData = data;

			if (ConfigManager.Profiles.Count > 0 && (DateTime.UtcNow - LastRPCUpdate).TotalMilliseconds >= RPC_REFRESH_INTERVAL) {
				if (StationManager.Boarding) {
                    UpdatePresence(SelectedProfileIndex, Context.Boarding);
				} else {
                    UpdatePresence(SelectedProfileIndex, Context.InGame);
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
			bool keyChanged = ConfigManager.ProfileCycleKey.Any(key => OldKeyboardState[key] != keyboardState[key]);
			bool correctKeyHeld = ConfigManager.ProfileCycleKey.Count > 0 && ConfigManager.ProfileCycleKey.All(key => keyboardState.IsKeyDown(key));

			if (keyChanged && correctKeyHeld) {
				SelectedProfileIndex = (SelectedProfileIndex + 1) % ConfigManager.Profiles.Count;
			}
			OldKeyboardState = keyboardState;
		}

		protected virtual void OnKeyDown(InputEventArgs e)
		{
		}

		protected virtual void OnKeyUp(InputEventArgs e)
		{
		}

        private void UpdatePresence(int profileIndex, Context currentCntext) {
            if (SelectedProfileIndex >= ConfigManager.Profiles.Count) return;

            Profile profile = ConfigManager.Profiles[SelectedProfileIndex];
            if(profile != null) {
                UpdatePresence(profile.Presence[currentCntext]);
            }
        }

        private void UpdatePresence(RPCLayout data) {
			if (data == null || Client == null) {
				return;
			}
			
			RichPresence presence = new RichPresence();
            if (data.HasTimestamp) {
                presence.Timestamps = StartTimestamp;
            }
			
			presence.Details = ParsePlaceholders(data.Details, 1024);
			presence.State = ParsePlaceholders(data.State, 1024);
			presence.Assets = new Assets();
			if (!string.IsNullOrEmpty(data.AssetsData.LargeImageText)) presence.Assets.LargeImageText = ParsePlaceholders(data.AssetsData.LargeImageText, 1024);
			if (!string.IsNullOrEmpty(data.AssetsData.LargeImageKey)) presence.Assets.LargeImageKey = ParsePlaceholders(data.AssetsData.LargeImageKey, 1024);
			if (!string.IsNullOrEmpty(data.AssetsData.SmallImageText)) presence.Assets.SmallImageText = ParsePlaceholders(data.AssetsData.SmallImageText, 1024);
			if (!string.IsNullOrEmpty(data.AssetsData.SmallImageKey)) presence.Assets.SmallImageKey = ParsePlaceholders(data.AssetsData.SmallImageKey, 1024);

			List<Button> buttons = new List<Button>();
			foreach (ButtonData btnData in data.Buttons) {
				if (btnData.IsValid()) {
                    Button btn = new Button
                    {
                        Label = ParsePlaceholders(btnData.Label, MAX_BTN_CHAR),
                        Url = ParsePlaceholders(btnData.Url, MAX_BTN_CHAR)
                    };

                    buttons.Add(btn);
                }
			}

			if (buttons.Count > 0) {
				presence.Buttons = buttons.ToArray();
			}

			Client.SetPresence(presence);
		}

        private void InitRPC() {
            if(Client != null && !Client.IsDisposed) {
                Client.Dispose();
            }

            if(string.IsNullOrEmpty(ConfigManager.AppId)) {
                FileSystem.AppendToLogFile("[obDRPC] Cannot start Discord RPC: Application ID must not be empty!");
                return;
            }

            Client = new DiscordRpcClient(ConfigManager.AppId);
            if (!Client.Initialize()) {
                FileSystem.AppendToLogFile("[obDRPC] Cannot start Discord RPC: Failed to login to Discord, please ensure your Application ID is correct and you have a stable internet connection.");
            } else {
                UpdatePresence(SelectedProfileIndex, Context.Menu);
            }
        }

        internal string GetProgramVersion() {
			return Assembly.GetEntryAssembly().GetName().Version.ToString();
		}

        public void SetVehicleSpecs(VehicleSpecs specs) {
			this.VehicleSpecs = specs;
        }

		public string ParsePlaceholders(string input, int maxChar) {
			return Placeholder.ParsePlaceholders(input, ProgramVersion, LastElapseData, VehicleSpecs, DoorState, CurrentContext, maxChar);
		}

        public void DoorChange(DoorStates oldState, DoorStates newState) {
			DoorState = newState;
        }

        public void SetSignal(SignalData[] data) {
        }

		public void SetBeacon(BeaconData data) {
			// OpenBVE Speed limit
			if (data.Type == -16777214) {
				SpeedLimit = data.Optional;
			}
		}
    }
}
