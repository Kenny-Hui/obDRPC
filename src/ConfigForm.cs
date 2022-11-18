using DiscordRPC;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace obDRPC {
    public partial class ConfigForm : Form {
        private string defaultTitle;
        private int SelectedProfile;
        private bool CapturingKeyboard = false;
        private HashSet<Key> ProfileKeysCombination;
        private List<Profile> ProfileList = null;
        private TextBoxBase selectedTextBox = null;
        private DiscordRpcClient Client;

        public ConfigForm(DiscordRpcClient client, Placeholders placeholders) {
            InitializeComponent();
            ProfileList = new List<Profile>(ConfigManager.ProfileList);
            this.Client = client;
            this.SelectedProfile = 0;
            this.defaultTitle = this.Text;

            if (ConfigManager.KeyCombination.Count == 0) {
                this.ProfileKeysCombination = new HashSet<Key>();
                ProfileKeysCombination.Add(Key.ControlLeft);
                ProfileKeysCombination.Add(Key.PageDown);
            } else {
                this.ProfileKeysCombination = new HashSet<Key>(ConfigManager.KeyCombination);
            }

            SetupProfileButtons();
            UpdateUIContext("menu");
            Init(placeholders);
            LoadProfile(SelectedProfile);
            UpdateTitle(SelectedProfile);
        }

        private void insertableTextSelect(object sender, EventArgs e) {
            // Mono seems to always auto-focus on the 1st text box if selecting the main form background
            // So it would always trigger this method and jump to "menu" when clicking the background, don't think we can do anything about that<?>
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
                btn.Location = new Point(17 + (btn.Width * i), 213);
                // Mono more like mo...Noooooo
                btn.BackColor = SystemColors.ButtonFace;
                btn.Click += (sender, e) => {
                    int index = int.Parse(btn.Tag.ToString().Split(';')[1]);
                    /* User clicked the same button again */
                    if (index == SelectedProfile) {
                        string newName = Dialogs.ShowRenameDialog(ProfileList[SelectedProfile].Name, ProfileList);
                        if (newName == null) return;
                        Profile affectedProfile = ProfileList[SelectedProfile];
                        affectedProfile.Name = newName;
                        ProfileList[SelectedProfile].Name = newName;
                        SaveProfile(SelectedProfile);
                        SetupProfileButtons();
                    } else {
                        SaveProfile(SelectedProfile);
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
            buttonPfShortcut.Text = string.Join(" + ", ProfileKeysCombination);
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

        private void LoadProfile(int profileIndex) {
            SelectedProfile = profileIndex;
            UpdateTitle(profileIndex);
            Profile profile = profileIndex < ProfileList.Count ? ProfileList[profileIndex] : null;

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
                    if (profile == null) {
                        ((TextBoxBase)control).ReadOnly = true;
                        continue;
                    } else {
                        ((TextBoxBase)control).ReadOnly = false;
                    }

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
                    if (profile == null) {
                        control.Enabled = false;
                        continue;
                    } else {
                        control.Enabled = true;
                    }

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

            ProfileList[selectedProfile] = profile;
        }

        private void SaveCfg_Click(object sender, EventArgs e) {
            if (!appIdTextBox.Text.All(char.IsDigit)) {
                MessageBox.Show("Application ID should be number only.");
                return;
            }

            if (ProfileList.Count == 0) {
                MessageBox.Show("No profile created, please create a profile.");
                return;
            }

            SaveProfile(SelectedProfile);
            ConfigManager.UpdateApplicationId(appIdTextBox.Text);
            ConfigManager.UpdateKeyCombination(ProfileKeysCombination);
            ConfigManager.UpdateProfileList(ProfileList);
            ConfigManager.SaveConfigToDisk();
            this.Close();
        }

        private void UpdateTitle(int profileIndex) {
            string name = ProfileList.Count > SelectedProfile ? ProfileList[profileIndex].Name : null;
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
            SaveProfile(SelectedProfile);
            LoadProfile(newProfileIndex);
            SetupProfileButtons();
        }

        private void removeProfileBtn_Click(object sender, EventArgs e) {
            ProfileList.RemoveAt(SelectedProfile);
            LoadProfile(0);
            SetupProfileButtons();
        }

        private void buttonPfShortcut_Click(object sender, EventArgs e) {
            CapturingKeyboard = true;
            ProfileKeysCombination = new HashSet<Key>();
            ((Control)sender).Text = "Press any key...";
        }

        private void buttonPfShortcut_KeyDown(object sender, KeyEventArgs e) {
            if (CapturingKeyboard) {
                KeyboardState state = Keyboard.GetState();
                ProfileKeysCombination.Clear();
                if (!state.IsKeyDown(Key.Escape)) {
                    var values = Enum.GetValues(typeof(Key));
                    foreach (Key key in values) {
                        if (state.IsKeyDown(key)) {
                            ProfileKeysCombination.Add(key);
                        }
                    }

                    buttonPfShortcut.Text = string.Join(" + ", ProfileKeysCombination);
                } else {
                    buttonPfShortcut.Text = "None";
                }

            }
        }

        private void buttonPfShortcut_KeyUp(object sender, KeyEventArgs e) {
            if (CapturingKeyboard) {
                KeyboardState state = Keyboard.GetState();
                if (!state.IsAnyKeyDown) {
                    CapturingKeyboard = false;
                }
            }
        }
    }
}
