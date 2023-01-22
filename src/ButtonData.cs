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

        /// <summary>
        /// True if the Label and the Url field is filled.
        /// </summary>
        public bool isFinished() {
            return Label != null && Url != null;
        }
    }
}
