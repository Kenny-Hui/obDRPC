using System;
using System.Linq;
using System.Collections.Generic;
using DiscordRPC;
using System.Windows.Forms;
using OpenBveApi.FileSystem;
using OpenBveApi.Interface;
using OpenBveApi.Runtime;
using Button = DiscordRPC.Button;
using System.Reflection;
using System.Xml;

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
		private string ClientId;
		private string OptionsFolder;
		private string ProgramVersion;
		private bool IsInGame;
		private int SpeedLimit = 70;
		private VehicleSpecs specs;
		private FileSystem FileSystem;
		private string selectedProfile;
		private Dictionary<string, Profile> ProfileList = new Dictionary<string, Profile>();
		private Timestamps StartTimestamp;
		private ElapseData LastElapseData;
		private DateTime LastRPCUpdate;
		private Placeholders Placeholder = new Placeholders();
		private DiscordRpcClient Client;
		private DoorStates doorState = DoorStates.None;
		private const int MAX_BTN_CHAR = 32;
		private const int RPC_REFRESH_INTERVAL = 1000;

		/// <summary>
		/// A function call when the plugin is loading
		/// </summary>
		/// <param name="fileSystem">The instance of FileSytem class</param>
		/// <returns>Check the plugin loading process is successfully</returns>
		public bool Load(FileSystem fileSystem)
		{
			LastElapseData = null;
			Controls = new InputControl[1];
			FileSystem = fileSystem;
			StartTimestamp = Timestamps.Now;
			LastRPCUpdate = DateTime.UtcNow;
			ProgramVersion = getEntryVersion();
			OptionsFolder = OpenBveApi.Path.CombineDirectory(FileSystem.SettingsFolder, "1.5.0");
			CurrentContext = Context.Menu;
			LoadConfig();
			selectedProfile = ProfileList.Count == 0 ? null : ProfileList.Keys.First();

			if (!string.IsNullOrEmpty(ClientId)) {
				Client = new DiscordRpcClient(ClientId);
				if (!Client.Initialize()) {
					FileSystem.AppendToLogFile("[DRPC] Failed to login to Discord, please make sure your Application ID is correct and you have a stable internet connection.");
				}

                UpdatePresence(ProfileList[selectedProfile].PresenceList["menu"]);
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
            using (var form = new ConfigForm(ProfileList, Client, Placeholder, OptionsFolder)) {
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

			if (selectedProfile != null && (DateTime.UtcNow - LastRPCUpdate).TotalMilliseconds >= RPC_REFRESH_INTERVAL) {
				if (StationManager.Boarding) {
					UpdatePresence(ProfileList[selectedProfile].PresenceList["boarding"]);
				} else {
					UpdatePresence(ProfileList[selectedProfile].PresenceList["game"]);
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

		internal void LoadConfig()
		{
			ProfileList.Clear();
			if (!System.IO.Directory.Exists(OptionsFolder)) {
				System.IO.Directory.CreateDirectory(OptionsFolder);
			}

			string configFile = OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc.xml");
			if (System.IO.File.Exists(configFile)) {
				Dictionary<string, RPCData> presenceList = new Dictionary<string, RPCData>();
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(configFile);
				if (xmlDoc.GetElementsByTagName("appId") != null) {
					ClientId = xmlDoc.GetElementsByTagName("appId")[0].InnerText;
				}

				if (xmlDoc.GetElementsByTagName("presenceList")[0] != null) {
					foreach (XmlElement element in xmlDoc.GetElementsByTagName("presenceList")[0].ChildNodes) {
						RPCData presence = new RPCData();
						string id = element.GetAttribute("id");
						if (id == null) continue;

						presence.details = element.GetElementsByTagName("details")[0]?.InnerText;
						presence.state = element.GetElementsByTagName("state")[0]?.InnerText;

						if (element.GetElementsByTagName("hasTimestamp")[0]?.InnerText != null) {
							presence.hasTimestamp = XmlConvert.ToBoolean(element.GetElementsByTagName("hasTimestamp")[0].InnerText);
						}

						/* Assets */
						if (element.GetElementsByTagName("largeImageKey")[0] != null) {
							presence.AddLargeImageKey(element.GetElementsByTagName("largeImageKey")[0].InnerText);
						}

						if (element.GetElementsByTagName("largeImageText")[0] != null) {
							presence.AddLargeImageText(element.GetElementsByTagName("largeImageText")[0].InnerText);
						}

						if (element.GetElementsByTagName("smallImageKey")[0] != null) {
							presence.AddSmallImageKey(element.GetElementsByTagName("smallImageKey")[0].InnerText);
						}

						if (element.GetElementsByTagName("smallImageText")[0] != null) {
							presence.AddSmallImageText(element.GetElementsByTagName("smallImageText")[0].InnerText);
						}

						/* Button */
						foreach (XmlElement button in element.GetElementsByTagName("button")) {
							string text = button.GetElementsByTagName("text")[0]?.InnerText;
							string url = button.GetElementsByTagName("url")[0]?.InnerText;
							if (text != null && url != null) {
								presence.AddButton(text, url);
							}
						}

						presenceList.Add(id, presence);
					}

					/* Parse profile */
					foreach(XmlElement profile in xmlDoc.GetElementsByTagName("profile")) {
						string name = profile.GetAttribute("name");
						if (name == null) continue;
						string menu = profile.GetElementsByTagName("menu")[0]?.InnerText;
						string game = profile.GetElementsByTagName("game")[0]?.InnerText;
						string boarding = profile.GetElementsByTagName("boarding")[0]?.InnerText;
						RPCData menuPresence = menu != null && presenceList.ContainsKey(menu) ? presenceList[menu] : null;
						RPCData gamePresence = game != null && presenceList.ContainsKey(game) ? presenceList[game] : null;
						RPCData boardingPresence = boarding != null && presenceList.ContainsKey(boarding) ? presenceList[boarding] : null;
						ProfileList.Add(name, new Profile(menuPresence, gamePresence, boardingPresence));
					}
				}
			}
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
