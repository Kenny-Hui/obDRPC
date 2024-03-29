namespace obDRPC {
    /// <summary>
    /// Wrapper for DiscordRPC.Button
    /// </summary>
    public class ButtonData {
        public string Label;
		public string Url;
		public ButtonData(string label, string url) {
            Label = label;
            Url = url;
        }

        /// <summary>
        /// True if the Label and the Url field is filled.
        /// </summary>
        public bool IsValid() {
            return !string.IsNullOrEmpty(Label) && !string.IsNullOrEmpty(Url);
        }
    }
}
