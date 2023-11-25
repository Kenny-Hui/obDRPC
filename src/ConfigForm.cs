using DiscordRPC;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace obDRPC {
    public partial class ConfigForm : Form {
        private string DefaultTitle;
        private int SelectedProfile;
        private bool CapturingKeyboard;
        private HashSet<Key> ProfileKeysCombination;
        private List<Profile> Profiles;
        private TextBoxBase SelectedTextBox;
        private DiscordRpcClient Client;
        private Action<RPCLayout> UpdateRPC;

        public ConfigForm(DiscordRpcClient client, Placeholders placeholders, Action<RPCLayout> updateRPC) {
            InitializeComponent();
            Profiles = new List<Profile>(ConfigManager.Profiles);
            this.Client = client;
            this.SelectedProfile = 0;
            this.DefaultTitle = this.Text;
            this.UpdateRPC = updateRPC;

            if (ConfigManager.ProfileCycleKey.Count == 0) {
                this.ProfileKeysCombination = new HashSet<Key>();
                ProfileKeysCombination.Add(Key.ControlLeft);
                ProfileKeysCombination.Add(Key.PageDown);
            } else {
                this.ProfileKeysCombination = new HashSet<Key>(ConfigManager.ProfileCycleKey);
            }

            SetupProfileButtons();
            UpdateUIContext("menu");
            Init(placeholders);
            LoadProfile(SelectedProfile);
            UpdateTitle(SelectedProfile);
        }

        private void InsertableTextSelect(object sender, EventArgs e) {
            // Mono seems to always auto-focus on the 1st text box if selecting the main form background
            // So it would always trigger this method and jump to "menu" when clicking the background, don't think we can do anything about that<?>
            string type = ((Control)sender).Tag.ToString().Split(';')[1];
            SelectedTextBox = sender as TextBoxBase;
            UpdateUIContext(type);
        }

        private void InsertPlaceholder(object sender, EventArgs e) {
            string placeholder = ((Control)sender).Tag.ToString().Split(';')[0];
            if (SelectedTextBox != null && !SelectedTextBox.ReadOnly) {
                SelectedTextBox.Text = SelectedTextBox.Text.Insert(SelectedTextBox.SelectionStart, placeholder);
            }
        }

        private void PlaceholderMouseEnter(object sender, EventArgs e) {
            ((Control)sender).ForeColor = Color.White;
        }

        private void PlaceholderMouseLeave(object sender, EventArgs e) {
            ((Control)sender).ForeColor = Color.LightGray;
        }

        private void SetupProfileButtons() {
            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(addProfileBtn, "Add profile.");
            tooltip.SetToolTip(removeProfileBtn, "Remove selected profile.");

            List<Control> toBeRemoved = new List<Control>();
            foreach (Control control in this.Controls) {
                if (control.Tag != null && control.Tag.ToString().StartsWith("pfBtn", StringComparison.InvariantCulture)) {
                    toBeRemoved.Add(control);
                }
            }

            foreach (Control control in toBeRemoved) {
                Controls.Remove(control);
            }

            for (int i = 0; i < Profiles.Count; i++) {
                Profile profile = Profiles[i];
                string profileName = profile.Name;
                RadioButton btn = new RadioButton() {
                    Tag = "pfBtn;" + i,
                    Text = profileName,
                    UseVisualStyleBackColor = true,
                    Appearance = Appearance.Button
                };
                btn.Location = new Point(17 + (btn.Width * i), 213);
                btn.BackColor = SystemColors.ButtonFace;
                btn.Click += (sender, e) => {
                    int index = int.Parse(btn.Tag.ToString().Split(';')[1]);
                    /* User clicked the same button again */
                    if (index == SelectedProfile) {
                        string newName = Dialogs.ShowRenameDialog(Profiles[SelectedProfile].Name, Profiles);
                        if (newName == null) return;
                        Profile affectedProfile = Profiles[SelectedProfile];
                        affectedProfile.Name = newName;
                        Profiles[SelectedProfile].Name = newName;
                        SaveProfile(SelectedProfile);
                        SetupProfileButtons();
                    } else {
                        SaveProfile(SelectedProfile);
                        LoadProfile(index);
                    }
                };
                btn.Checked = i == SelectedProfile;
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
                lb.Tag = "{" + placeholder.VariableName + "};" + ContextHelper.ToString(placeholder.Context);
                lb.Cursor = Cursors.Hand;
                lb.Click += InsertPlaceholder;
                lb.MouseEnter += PlaceholderMouseEnter;
                lb.MouseLeave += PlaceholderMouseLeave;
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
            Profile profile = profileIndex < Profiles.Count ? Profiles[profileIndex] : null;

            if(profile != null) {
                UpdateRPC(profile.Presence[Context.Menu]);
            }

            foreach (Control control in this.Controls) {
                string tagName = control.Tag == null ? "" : control.Tag.ToString();

                if (tagName == "appId") {
                    control.Text = ConfigManager.AppId;
                    continue;
                }

                if (!tagName.Contains(";")) {
                    continue;
                }

                string prop = tagName.Split(';')[0];
                string contextString = tagName.Split(';')[1];
                Context context = ContextHelper.FromString(contextString);

                if (context == Context.None) {
                    continue;
                }

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(RichTextBox)) {
                    if (profile == null) {
                        ((TextBoxBase)control).Text = "";
                        ((TextBoxBase)control).ReadOnly = true;
                        continue;
                    } else {
                        ((TextBoxBase)control).ReadOnly = false;
                    }

                    RPCLayout data = profile.Presence[context];
                    if (data == null) continue;

                    if (prop == "details") {
                        control.Text = data.Details;
                    }

                    if (prop == "state") {
                        control.Text = data.State;
                    }

                    if (prop == "largeimgkey") {
                        control.Text = data.AssetsData?.LargeImageKey;
                    }

                    if (prop == "largeimgtext") {
                        control.Text = data.AssetsData?.LargeImageText;
                    }

                    if (prop == "smallimgkey") {
                        control.Text = data.AssetsData?.SmallImageKey;
                    }

                    if (prop == "smallimgtext") {
                        control.Text = data.AssetsData?.SmallImageText;
                    }

                    if (prop == "btn1text") {
                        if (data.Buttons != null && data.Buttons.Count >= 1) {
                            control.Text = data.Buttons[0].Label;
                        } else {
                            control.Text = "";
                        }
                    }

                    if (prop == "btn2text") {
                        if (data.Buttons != null && data.Buttons.Count >= 2) {
                            control.Text = data.Buttons[1].Label;
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
                        ((CheckBox)control).Checked = profile.Presence[context].HasTimestamp;
                    }
                }

                if(control.GetType() == typeof(System.Windows.Forms.Button)) {
                    if (profile == null)
                    {
                        ((System.Windows.Forms.Button)control).Enabled = false;
                        continue;
                    } else {
                        ((System.Windows.Forms.Button)control).Enabled = true;
                    }

                    RPCLayout data = profile.Presence[context];

                    if (prop == "btn1url")
                    {
                        if (data.Buttons != null && data.Buttons.Count >= 1)
                        {
                            string[] ogTag = control.Tag.ToString().Split(';');
                            ogTag[2] = data.Buttons[0].Url;
                            control.Tag = string.Join(";", ogTag);
                        }
                    }

                    if (prop == "btn2url")
                    {
                        if(data.Buttons.Count > 1) {
                            string[] ogTag = control.Tag.ToString().Split(';');
                            ogTag[2] = data.Buttons[1].Url;
                            control.Tag = string.Join(";", ogTag);
                        }
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

                    if(SelectedProfile >= Profiles.Count) {
                        control.Enabled = false;
                    } else {
                        if(control.Tag.ToString().StartsWith("contextTitle", StringComparison.Ordinal)) {
                            if (lblType == selectedType) {
                                control.Font = new Font(control.Font, FontStyle.Underline|FontStyle.Bold);
                            } else {
                                control.Font = new Font(control.Font, FontStyle.Regular);
                            }
                        } else {
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
                    }
                }

                if (control.GetType() == typeof(PictureBox)) {
                    control.Visible = false;
                }
            }
        }

        private void button15_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void setURLButton_Click(object sender, EventArgs e) {
            string[] tags = ((Control)sender).Tag?.ToString().Split(';');
            string newURL = Dialogs.ShowURLDialog(tags[2]);
            if(newURL != null) tags[2] = newURL;
            ((Control)sender).Tag = string.Join(";", tags);
        }

        private void appIdTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) {
                e.Handled = true;
            }
        }

        private void SaveProfile(int selectedProfile) {
            Profile profile = selectedProfile <= Profiles.Count - 1 ? Profiles[selectedProfile] : null;
            if (profile == null) return;

            Dictionary<Context, ButtonData> btn1Data = new Dictionary<Context, ButtonData>();
            Dictionary<Context, ButtonData> btn2Data = new Dictionary<Context, ButtonData>();

            foreach(Context category in profile.Presence.Keys) {
                profile.Presence[category].Buttons.Clear();
            }

            foreach (Control control in this.Controls) {
                if (control.Tag == null || !control.Tag.ToString().Contains(";")) {
                    continue;
                }

                if (control.GetType() != typeof(TextBox) && control.GetType() != typeof(RichTextBox) && control.GetType() != typeof(CheckBox) && control.GetType() != typeof(System.Windows.Forms.Button)) {
                    continue;
                }

                string prop = control.Tag.ToString().Split(';')[0];
                string contextString = control.Tag.ToString().Split(';')[1];
                Context context = ContextHelper.FromString(contextString);

                if (contextString.Length == 0) {
                    continue;
                }

                if (control.GetType() == typeof(CheckBox)) {
                    if (prop == "elapsed") {
                        profile.Presence[context].HasTimestamp = ((CheckBox)control).Checked;
                    }
                }

                if (control.GetType() == typeof(System.Windows.Forms.Button)) {
                    if (prop == "btn1url")
                    {
                        if (!btn1Data.ContainsKey(context))
                        {
                            btn1Data.Add(context, new ButtonData(null, control.Tag.ToString().Split(';')[2]));
                        }
                        else
                        {
                            btn1Data[context].Url = control.Tag.ToString().Split(';')[2];
                        }
                    }

                    if (prop == "btn2url")
                    {
                        if (!btn2Data.ContainsKey(context))
                        {
                            btn2Data.Add(context, new ButtonData(null, control.Tag.ToString().Split(';')[2]));
                        }
                        else
                        {
                            btn2Data[context].Url = control.Tag.ToString().Split(';')[2];
                        }
                    }
                }

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(RichTextBox)) {
                    if (prop == "details") {
                        profile.Presence[context].Details = control.Text;
                    }

                    if (prop == "state") {
                        profile.Presence[context].State = control.Text;
                    }

                    if (prop == "largeimgkey") {
                        profile.Presence[context].AddLargeImageKey(control.Text);
                    }

                    if (prop == "largeimgtext") {
                        profile.Presence[context].AddLargeImageText(control.Text);
                    }

                    if (prop == "smallimgkey") {
                        profile.Presence[context].AddSmallImageKey(control.Text);
                    }

                    if (prop == "smallimgtext") {
                        profile.Presence[context].AddSmallImageText(control.Text);
                    }

                    if (prop == "btn1text") {
                        if(!btn1Data.ContainsKey(context)) {
                            btn1Data.Add(context, new ButtonData(control.Text, null));
                        } else {
                            btn1Data[context].Label = control.Text;
                        }
                    }

                    if (prop == "btn2text") {
                        if (!btn2Data.ContainsKey(context)) {
                            btn2Data.Add(context, new ButtonData(control.Text, null));
                        } else {
                            btn2Data[context].Label = control.Text;
                        }
                    }
                }
            }

            foreach (KeyValuePair<Context, ButtonData> entry in btn1Data) {
                if (entry.Value.IsValid()) {
                    profile.Presence[entry.Key].Buttons.Add(entry.Value);
                }
            }

            foreach (KeyValuePair<Context, ButtonData> entry in btn2Data) {
                if (entry.Value.IsValid()) {
                    profile.Presence[entry.Key].Buttons.Add(entry.Value);
                }
            }

            Profiles[selectedProfile] = profile;
        }

        private void SaveCfg_Click(object sender, EventArgs e) {
            if (!appIdTextBox.Text.All(char.IsDigit)) {
                MessageBox.Show("Application ID should be number only.");
                return;
            }

            if (Profiles.Count == 0) {
                MessageBox.Show("No profile created, please create a profile.");
                return;
            }

            SaveProfile(SelectedProfile);
            ConfigManager.SetApplicationId(appIdTextBox.Text);
            ConfigManager.SetProfileCycleKey(ProfileKeysCombination);
            ConfigManager.SetProfiles(Profiles);
            ConfigManager.SaveConfigToDisk();
            this.Close();
        }

        private void UpdateTitle(int profileIndex) {
            string name = Profiles.Count > SelectedProfile ? Profiles[profileIndex].Name : null;
            if (name != null) {
                this.Text = $"{DefaultTitle} - Selected profile: {name}";
            } else {
                this.Text = DefaultTitle;
            }
        }

        private void addProfileBtn_Click(object sender, EventArgs e) {
            string profileName = Dialogs.ShowCreateDialog(Profiles);
            if (profileName == null) return;
            Profiles.Add(new Profile(profileName));
            int newProfileIndex = Profiles.Count - 1;
            SaveProfile(SelectedProfile);
            LoadProfile(newProfileIndex);
            SetupProfileButtons();
        }

        private void removeProfileBtn_Click(object sender, EventArgs e) {
            Profiles.RemoveAt(SelectedProfile);
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
