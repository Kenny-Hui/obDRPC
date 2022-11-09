using System.Collections.Generic;
using DiscordRPC;

namespace obDRPC {
	/// <summary>
	/// Informations Default Displaying Plugin (for Testing)
	/// </summary>
	public class RPCData {
		public Assets assetsData;
		public List<ButtonData> buttons { get; }
		public string details;
		public string state;
		public bool hasTimestamp;

		public RPCData() {
			buttons = new List<ButtonData>();
			assetsData = new Assets();
		}

		public void AddLargeImageKey(string key) {
			assetsData.LargeImageKey = key;
		}

		public void AddLargeImageText(string text) {
			assetsData.LargeImageText = text;
		}

		public void AddSmallImageKey(string key) {
			assetsData.SmallImageKey = key;
		}

		public void AddSmallImageText(string text) {
			assetsData.SmallImageText = text;
		}

		public void AddButton(string name, string url) {
			buttons.Add(new ButtonData(name, url));
		}
	}
}
