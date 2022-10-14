using System.Collections.Generic;
using DiscordRPC;

namespace obDRPC {
	/// <summary>
	/// Informations Default Displaying Plugin (for Testing)
	/// </summary>
	public class RPCData {
		public Assets assetsData;
		public List<ButtonData> buttons;
		public string details;
		public string state;
		public bool hasTimestamp;

		public RPCData() {
			this.buttons = new List<ButtonData>();
		}
	}
}
