using System.Collections.Generic;
using DiscordRPC;
using Button = DiscordRPC.Button;

namespace obDRPC {
	/// <summary>
	/// Informations Default Displaying Plugin (for Testing)
	/// </summary>
	public class ButtonData {
		public string Label;
		public string Url;
		public ButtonData(string label, string url) {
            Label = label;
            Url = url;
        }
    }
}
