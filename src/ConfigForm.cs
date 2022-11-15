using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using DiscordRPC;
using System.Xml;
using System.Drawing;
using OpenTK.Input;

namespace obDRPC {
    public partial class ConfigForm : Form {
        private string defaultTitle;
        private string OptionsFolder;
        private int selectedProfile;
        private bool capturingKeyboard = false;
        private HashSet<Key> capturedKeys;
        private List<Profile> ProfileList = null;
        private TextBoxBase selectedTextBox = null;
        private Key[] KeyCombination;
        private DiscordRpcClient Client;
        
        public ConfigForm(List<Profile> profileList, DiscordRpcClient client, Key[] keyCombination, Placeholders placeholders, string optionsFolder) {
            InitializeComponent();
            ProfileList = new List<Profile>(profileList);
            this.OptionsFolder = optionsFolder;
            this.Client = client;
            this.selectedProfile = 0;
            this.defaultTitle = this.Text;
            this.KeyCombination = keyCombination;
            SetupProfileButtons();
            UpdateUIContext("menu");
            Init(placeholders);
            LoadProfile(selectedProfile);
            UpdateTitle();
        }

        private void insertableTextSelect(object sender, EventArgs e) {
            // Mono seems to always auto-focus on the 1st text box if selecting the main form background
            // So it would always trigger this method and jump to "menu" when clicking the background, don't think we can do anything about that<?>
            // Pure insanity
            string type = ((Control)sender).Tag.ToString().Split(';')[1];
            selectedTextBox = sender as TextBoxBase;
            UpdateUIContext(type);
        }

        private void insertPlaceholder(object sender, EventArgs e) {
            string placeholder = ((Control)sender).Tag.ToString().Split(';')[0];
            if (selectedTextBox != null) {
                selectedTextBox.Text = selectedTextBox.Text.Insert(selectedTextBox.SelectionStart, placeholder);
            }
        }

        private void placeholderMouseEnter(object sender, EventArgs e) {
            ((Control)sender).ForeColor = Color.White;
        }

        private void placeholderMouseLeave(object sender, EventArgs e) {
            ((Control)sender).ForeColor = Color.LightGray;
        }

        private void SetupProfileButtons() {
            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(addProfileBtn, "Add profile.");
            tooltip.SetToolTip(removeProfileBtn, "Remove selected profile.");

            List<Control> toBeRemoved = new List<Control>();
            foreach (Control control in this.Controls) {
                if (control.Tag != null && control.Tag.ToString().StartsWith("pfBtn")) {
                    toBeRemoved.Add(control);
                }
            }

            /* I actually have no idea why this has to be put in a list, tried removing it directly on this.Controls and the loop would stop executing after a certain button for whatever reason. */
            foreach (Control control in toBeRemoved) {
                Controls.Remove(control);
            }

            for (int i = 0; i < ProfileList.Count; i++) {
                Profile profile = ProfileList[i];
                string profileName = profile.Name;
                RadioButton btn = new RadioButton() {
                    Tag = "pfBtn;" + i,
                    Text = profileName,
                    UseVisualStyleBackColor = true,
                    Appearance = Appearance.Button
                };
                btn.Text = profile.Name;
                btn.Location = new Point(17 + (btn.Width * i), 213);
                // Mono more like mo...Noooooo
                btn.BackColor = SystemColors.ButtonFace;
                btn.Click += (sender, e) => {
                    int index = int.Parse(btn.Tag.ToString().Split(';')[1]);
                    /* User clicked the same button again */
                    if (index == selectedProfile) {
                        string newName = Dialogs.ShowRenameDialog(ProfileList[selectedProfile].Name, ProfileList);
                        if (newName == null) return;
                        Profile affectedProfile = ProfileList[selectedProfile];
                        affectedProfile.Name = newName;
                        ProfileList[selectedProfile].Name = newName;
                        SaveProfile(selectedProfile);
                        SetupProfileButtons();
                    } else {
                        SaveProfile(selectedProfile);
                        LoadProfile(index);
                    }
                };
                this.Controls.Add(btn);
            }
        }

        private void Init(Placeholders placeholders) {
            /* Placeholders */
            int x = 0;
            int y = 0;
            int s = 0;
            double perRow = 6;
            foreach (Placeholder placeholder in placeholders.placeholderList) {
                if (s >= perRow) {
                    s = 0;
                    x += this.Width / (int)Math.Ceiling(placeholders.placeholderList.Count / perRow);
                    y = 0;
                }

                Label lb = new Label();
                lb.Text = "{" + placeholder.VariableName + "} - " + placeholder.Description;
                lb.AutoSize = true;
                lb.Location = new Point(x, y);
                lb.ForeColor = Color.LightGray;
                lb.Font = new Font("Segoe UI Semibold", 9);
                lb.Tag = "{" + placeholder.VariableName + "};" + obDRPC.getContextName(placeholder.context);
                lb.Cursor = Cursors.Hand;
                lb.Click += insertPlaceholder;
                lb.MouseEnter += placeholderMouseEnter;
                lb.MouseLeave += placeholderMouseLeave;
                this.Controls.Add(lb);
                s++;
                y += 25;
            }

            /* Setup Key Combination Text */
            buttonPfShortcut.Text = string.Join(" + ", KeyCombination);
        }

        private void LoadProfile(int profileIndex) {
            Profile profile = profileIndex < ProfileList.Count ? ProfileList[profileIndex] : null;
            if (profile == null) return;
            selectedProfile = profileIndex;
            UpdateTitle();

            foreach (Control control in this.Controls) {
                string tagName = control.Tag == null ? "" : control.Tag.ToString();

                if (tagName == "appId") {
                    control.Text = Client?.ApplicationID;
                    continue;
                }

                if (!tagName.Contains(";")) {
                    continue;
                }

                string prop = tagName.Split(';')[0];
                string category = tagName.Split(';')[1];

                if (category.Length == 0) {
                    continue;
                }

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(RichTextBox)) {
                    RPCData data = profile.PresenceList[category];
                    if (data == null) continue;

                    if (prop == "details") {
                        control.Text = data.details;
                    }

                    if (prop == "state") {
                        control.Text = data.state;
                    }

                    if (prop == "largeimgkey") {
                        control.Text = data.assetsData?.LargeImageKey;
                    }

                    if (prop == "largeimgtext") {
                        control.Text = data.assetsData?.LargeImageText;
                    }

                    if (prop == "smallimgkey") {
                        control.Text = data.assetsData?.SmallImageKey;
                    }

                    if (prop == "smallimgtext") {
                        control.Text = data.assetsData?.SmallImageText;
                    }

                    if (prop == "btn1text") {
                        if (data.buttons != null && data.buttons.Count >= 1) {
                            control.Text = data.buttons[0].Label + "|" + data.buttons[0].Url;
                        } else {
                            control.Text = "";
                        }
                    }

                    if (prop == "btn2text") {
                        if (data.buttons != null && data.buttons.Count >= 2) {
                            control.Text = data.buttons[1].Label + "|" + data.buttons[0].Url;
                        } else {
                            control.Text = "";
                        }
                    }
                }

                if (control.GetType() == typeof(CheckBox)) {
                    if (prop == "elapsed") {
                        ((CheckBox)control).Checked = profile.PresenceList[category].hasTimestamp;
                    }
                }
            }
        }

        private void UpdateUIContext(string selectedType) {
            foreach (Control control in this.Controls) {
                if (control.Tag == null) {
                    continue;
                }

                if (control.GetType() == typeof(Label)) {
                    if (control.Tag == null) continue;

                    string lblType = control.Tag.ToString().Split(';')[1];

                    if (lblType == "menu") {
                        control.Enabled = true;
                    } else if (lblType == "boarding" && selectedType != "boarding") {
                        control.Enabled = false;
                    } else if (selectedType == "menu" && lblType != "menu") {
                        control.Enabled = false;
                    } else {
                        control.Enabled = true;
                    }
                }

                if (control.GetType() == typeof(PictureBox)) {
                    control.Visible = control.Tag.ToString() == selectedType;
                }
            }

            if (Client?.CurrentUser != null) {
                connectionLabel.Text = "Connected to " + Client.CurrentUser.Username + "#" + Client.CurrentUser.Discriminator;
            } else {
                if (Client == null) {
                    connectionLabel.Text = "Cannot login, please check your application ID.";
                } else {
                    connectionLabel.Text = "Not connected.";
                }
            }
        }

        private void button15_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void appIdTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) {
                e.Handled = true;
            }
        }

        private void SaveProfile(int selectedProfile) {
            Profile profile = selectedProfile <= ProfileList.Count - 1 ? ProfileList[selectedProfile] : null;
            if (profile == null) return;

            Dictionary<string, ButtonData> btn1Data = new Dictionary<string, ButtonData>();
            Dictionary<string, ButtonData> btn2Data = new Dictionary<string, ButtonData>();

            foreach(string category in profile.PresenceList.Keys) {
                profile.PresenceList[category].buttons.Clear();
            }

            foreach (Control control in this.Controls) {
                if (control.Tag == null || !control.Tag.ToString().Contains(";")) {
                    continue;
                }

                if (control.GetType() != typeof(TextBox) && control.GetType() != typeof(RichTextBox) && control.GetType() != typeof(CheckBox)) {
                    continue;
                }

                string prop = control.Tag.ToString().Split(';')[0];
                string category = control.Tag.ToString().Split(';')[1];

                if (category.Length == 0) {
                    continue;
                }

                if (control.GetType() == typeof(CheckBox)) {
                    if (prop == "elapsed") {
                        profile.PresenceList[category].hasTimestamp = ((CheckBox)control).Checked;
                    }
                }

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(RichTextBox)) {
                    if (prop == "details") {
                        profile.PresenceList[category].details = control.Text;
                    }

                    if (prop == "state") {
                        profile.PresenceList[category].state = control.Text;
                    }

                    if (prop == "largeimgkey") {
                        profile.PresenceList[category].AddLargeImageKey(control.Text);
                    }

                    if (prop == "largeimgtext") {
                        profile.PresenceList[category].AddLargeImageText(control.Text);
                    }

                    if (prop == "smallimgkey") {
                        profile.PresenceList[category].AddSmallImageKey(control.Text);
                    }

                    if (prop == "smallimgtext") {
                        profile.PresenceList[category].AddSmallImageText(control.Text);
                    }

                    if (prop == "btn1text" && control.Text.Contains("|")) {
                        profile.PresenceList[category].buttons.Add(new ButtonData(control.Text.Split('|')[0], control.Text.Split('|')[1]));
                    }

                    if (prop == "btn2text" && control.Text.Contains("|")) {
                        profile.PresenceList[category].buttons.Add(new ButtonData(control.Text.Split('|')[0], control.Text.Split('|')[1]));
                    }
                }
            }
        }

        private void SaveCfg_Click(object sender, EventArgs e) {
            if (!appIdTextBox.Text.All(char.IsDigit)) {
                MessageBox.Show("Application ID should be number only.");
                return;
            }

            SaveProfile(selectedProfile);

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement rootElement = xmlDoc.CreateElement("data");
            XmlElement appIdElement = xmlDoc.CreateElement("appId");
            XmlElement switchKeyElement = xmlDoc.CreateElement("profileSwitchKey");
            XmlElement presenceListElement = xmlDoc.CreateElement("presenceList");
            Dictionary<string, Dictionary<string, string>> presenceName = new Dictionary<string, Dictionary<string, string>>();
            appIdElement.InnerText = appIdTextBox.Text;
            switchKeyElement.InnerText = string.Join("+", capturedKeys);
            rootElement.AppendChild(appIdElement);
            rootElement.AppendChild(switchKeyElement);

            foreach (Profile entry in ProfileList) {
                Profile profile = entry;
                foreach (KeyValuePair<string, RPCData> pair in profile.PresenceList) {
                    string context = pair.Key;
                    string presenceID = context + profile.Name;
                    RPCData presence = pair.Value;
                    Dictionary<string, string> contextName = presenceName.ContainsKey(profile.Name) ? presenceName[profile.Name] : new Dictionary<string, string>();
                    contextName.Add(context, presenceID);
                    presenceName[profile.Name] = contextName;

                    XmlElement presenceElement = xmlDoc.CreateElement("presence");
                    presenceElement.SetAttribute("id", presenceID);

                    if (presence.details.Length > 0) {
                        XmlElement detailsElement = xmlDoc.CreateElement("details");
                        detailsElement.InnerText = presence.details;
                        presenceElement.AppendChild(detailsElement);
                    }

                    if (presence.state.Length > 0) {
                        XmlElement stateElement = xmlDoc.CreateElement("state");
                        stateElement.InnerText = presence.state;
                        presenceElement.AppendChild(stateElement);
                    }

                    XmlElement hasTimestampElement = xmlDoc.CreateElement("hasTimestamp");
                    hasTimestampElement.InnerText = presence.hasTimestamp.ToString().ToLowerInvariant();
                    presenceElement.AppendChild(hasTimestampElement);

                    if (!string.IsNullOrEmpty(presence.assetsData?.LargeImageKey)) {
                        XmlElement largeImgKeyElement = xmlDoc.CreateElement("largeImageKey");
                        largeImgKeyElement.InnerText = presence.assetsData.LargeImageKey;
                        presenceElement.AppendChild(largeImgKeyElement);
                    }

                    if (!string.IsNullOrEmpty(presence.assetsData?.LargeImageText)) {
                        XmlElement largeImgTextElement = xmlDoc.CreateElement("largeImageText");
                        largeImgTextElement.InnerText = presence.assetsData.LargeImageText;
                        presenceElement.AppendChild(largeImgTextElement);
                    }

                    if (!string.IsNullOrEmpty(presence.assetsData?.LargeImageKey)) {
                        XmlElement smallImgKeyElement = xmlDoc.CreateElement("smallImageKey");
                        smallImgKeyElement.InnerText = presence.assetsData.LargeImageKey;
                        presenceElement.AppendChild(smallImgKeyElement);
                    }

                    if (!string.IsNullOrEmpty(presence.assetsData?.LargeImageText)) {
                        XmlElement smallImgTextElement = xmlDoc.CreateElement("smallImageText");
                        smallImgTextElement.InnerText = presence.assetsData.LargeImageText;
                        presenceElement.AppendChild(smallImgTextElement);
                    }

                    if (presence.buttons != null) {
                        for (int i = 0; i < presence.buttons.Count; i++) {
                            if (presence.buttons[i].Label.Length == 0 || presence.buttons[i].Url.Length == 0) continue;
                            XmlElement buttonElement = xmlDoc.CreateElement("button");
                            XmlElement textElement = xmlDoc.CreateElement("text");
                            XmlElement urlElement = xmlDoc.CreateElement("url");

                            textElement.InnerText = presence.buttons[i].Label;
                            urlElement.InnerText = presence.buttons[i].Url;
                            buttonElement.AppendChild(textElement);
                            buttonElement.AppendChild(urlElement);

                            presenceElement.AppendChild(buttonElement);
                        }
                    }
                    presenceListElement.AppendChild(presenceElement);
                }
                rootElement.AppendChild(presenceListElement);
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> nameEntry in presenceName) {
                XmlElement profileElement = xmlDoc.CreateElement("profile");
                string profileName = nameEntry.Key;

                profileElement.SetAttribute("name", profileName);
                foreach (KeyValuePair<string, string> contextEntry in nameEntry.Value) {
                    string context = contextEntry.Key;
                    string presenceID = contextEntry.Value;
                    XmlElement contextElement = xmlDoc.CreateElement(context);
                    contextElement.InnerText = presenceID;
                    profileElement.AppendChild(contextElement);

                    if (profileElement.ChildNodes.Count > 0) {
                        rootElement.AppendChild(profileElement);
                    }
                }
            }

            if (!Directory.Exists(OptionsFolder)) {
                Directory.CreateDirectory(OptionsFolder);
            }
            
            xmlDoc.AppendChild(rootElement);
            xmlDoc.Save(OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc2.xml"));
            this.Close();
        }

        private void UpdateTitle() {
            string name = ProfileList.Count > selectedProfile ? ProfileList[selectedProfile].Name : null;
            if (name != null) {
                this.Text = $"{defaultTitle} - Selected profile: {name}";
            } else {
                this.Text = defaultTitle;
            }
        }

        private void addProfileBtn_Click(object sender, EventArgs e) {
            string profileName = Dialogs.ShowCreateDialog(ProfileList);
            if (profileName == null) return;
            ProfileList.Add(new Profile(profileName));
            int newProfileIndex = ProfileList.Count - 1;
            SaveProfile(selectedProfile);
            LoadProfile(newProfileIndex);
            SetupProfileButtons();
        }

        private void removeProfileBtn_Click(object sender, EventArgs e) {
            ProfileList.RemoveAt(selectedProfile);
            LoadProfile(0);
            SetupProfileButtons();
        }

        private void buttonPfShortcut_Click(object sender, EventArgs e) {
            capturingKeyboard = true;
            capturedKeys = new HashSet<Key>();
            ((Control)sender).Text = "Press any key...";
        }

        private void buttonPfShortcut_KeyDown(object sender, KeyEventArgs e) {
            if (capturingKeyboard) {
                KeyboardState state = Keyboard.GetState();
                capturedKeys.Clear();
                var values = Enum.GetValues(typeof(Key));
                foreach(Key key in values) {
                    if (state.IsKeyDown(key)) {
                        capturedKeys.Add(key);
                    }
                }

                buttonPfShortcut.Text = string.Join(" + ", capturedKeys);
            }
        }

        private void buttonPfShortcut_KeyUp(object sender, KeyEventArgs e) {
            if (capturingKeyboard) {
                KeyboardState state = Keyboard.GetState();
                if (!state.IsAnyKeyDown) {
                    capturingKeyboard = false;
                }
            }
        }
    }
}
