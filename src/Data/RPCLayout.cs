using DiscordRPC;
using System.Collections.Generic;

namespace obDRPC {
    /// <summary>
    /// Informations Default Displaying Plugin (for Testing)
    /// </summary>
    public class RPCLayout {
		public Assets AssetsData;
		public List<ButtonData> Buttons { get; }
		public string Details;
		public string State;
		public bool HasTimestamp;

		public RPCLayout() {
			Buttons = new List<ButtonData>();
			AssetsData = new Assets();
		}

		public void AddLargeImageKey(string key) {
			AssetsData.LargeImageKey = key;
		}

		public void AddLargeImageText(string text) {
			AssetsData.LargeImageText = text;
		}

		public void AddSmallImageKey(string key) {
			AssetsData.SmallImageKey = key;
		}

		public void AddSmallImageText(string text) {
			AssetsData.SmallImageText = text;
		}

		public void AddButton(string name, string url) {
			Buttons.Add(new ButtonData(name, url));
		}
	}
}
