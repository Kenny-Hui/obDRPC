using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using DiscordRPC;

namespace obDRPC {
    public partial class ConfigForm : Form {
        private string OptionsFolder;
        private Dictionary<string, RPCData> RichPresenceList = null;
        private TextBoxBase selectedTextBox = null;
        private DiscordRpcClient Client;
        
        public ConfigForm(Dictionary<string, RPCData> richPresenceList, DiscordRpcClient client, Placeholders placeholders, string optionsFolder) {
            InitializeComponent();
            RichPresenceList = richPresenceList;
            this.OptionsFolder = optionsFolder;
            this.Client = client;
            UpdateUI("menu");
            init(placeholders);
        }

        private void insertableTextSelect(object sender, EventArgs e) {
            string type = ((Control)sender).Tag.ToString().Split(';')[1];
            selectedTextBox = sender as TextBoxBase;
            UpdateUI(type);
        }

        private void insertPlaceholder(object sender, EventArgs e) {
            string placeholder = ((Control)sender).Tag.ToString().Split(';')[0];
            if (selectedTextBox != null) {
                selectedTextBox.Text = selectedTextBox.Text.Insert(selectedTextBox.SelectionStart, placeholder);
            }
        }

        private void placeholderMouseEnter(object sender, EventArgs e) {
            ((Control)sender).ForeColor = System.Drawing.Color.White;
        }

        private void placeholderMouseLeave(object sender, EventArgs e) {
            ((Control)sender).ForeColor = System.Drawing.Color.LightGray;
        }

        private void init(Placeholders placeholders) {
            int x = 0;
            int y = 0;
            int s = 0;
            double perRow = 7;
            foreach(Placeholder placeholder in placeholders.placeholderList) {
                if(s >= perRow) {
                    s = 0;
                    x += this.Width / (int)Math.Ceiling(placeholders.placeholderList.Count / perRow);
                    y = 0;
                }

                Label lb = new Label();
                lb.Text = "{" + placeholder.VariableName + "} - " + placeholder.Description;
                lb.AutoSize = true;
                lb.Location = new System.Drawing.Point(x, y);
                lb.ForeColor = System.Drawing.Color.LightGray;
                lb.Font = new System.Drawing.Font("Segoe UI Semibold", 9);
                lb.Tag = "{" + placeholder.VariableName + "};" + obDRPC.getContextName(placeholder.context);
                lb.Cursor = Cursors.Hand;
                lb.Click += insertPlaceholder;
                lb.MouseEnter += placeholderMouseEnter;
                lb.MouseLeave += placeholderMouseLeave;
                this.Controls.Add(lb);
                s++;
                y += 25;
            }

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

                if (!RichPresenceList.ContainsKey(category)) {
                    RichPresenceList.Add(category, new RPCData());
                }

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(RichTextBox)) {
                    RPCData data = RichPresenceList[category];

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

                    if (prop == "btn1text" && data.buttons?.Count >= 1) {
                        control.Text = data.buttons?[0].Label + "|" + data.buttons?[0].Url;
                    }

                    if (prop == "btn2text" && data.buttons?.Count >= 2) {
                        control.Text = data.buttons?[1].Label + "|" + data.buttons?[1].Url;
                    }
                }

                if (control.GetType() == typeof(CheckBox)) {
                    if (prop == "elapsed") {
                        ((CheckBox)control).Checked = RichPresenceList[category].hasTimestamp;
                    }
                }
            }
        }

        private void UpdateUI(string selectedType) {
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
                connectionLabel.Text = "Not connected.";
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

        private void SaveCfg_Click(object sender, EventArgs e) {
            Dictionary<string, ButtonData> btn1Data = new Dictionary<string, ButtonData>();
            Dictionary<string, ButtonData> btn2Data = new Dictionary<string, ButtonData>();

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

                if (!RichPresenceList.ContainsKey(category)) {
                    RichPresenceList.Add(category, new RPCData());
                }

                if (RichPresenceList[category].assetsData == null) {
                    RichPresenceList[category].assetsData = new Assets();
                }

                if (control.GetType() == typeof(CheckBox)) {
                    if (prop == "elapsed") {
                        RichPresenceList[category].hasTimestamp = ((CheckBox)control).Checked;
                    }
                }

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(RichTextBox)) {
                    if (prop == "details") {
                        RichPresenceList[category].details = control.Text;
                    }

                    if (prop == "state") {
                        RichPresenceList[category].state = control.Text;
                    }

                    if (prop == "largeimgkey") {
                        RichPresenceList[category].assetsData.LargeImageKey = control.Text;
                    }

                    if (prop == "largeimgtext") {
                        RichPresenceList[category].assetsData.LargeImageText = control.Text;
                    }

                    if (prop == "smallimgkey") {
                        RichPresenceList[category].assetsData.SmallImageKey = control.Text;
                    }

                    if (prop == "smallimgtext") {
                        RichPresenceList[category].assetsData.SmallImageText = control.Text;
                    }

                    if (prop == "btn1text" && control.Text.Contains("|")) {
                        btn1Data.Add(category, new ButtonData(control.Text.Split('|')[0], control.Text.Split('|')[1]));
                    }

                    if (prop == "btn2text" && control.Text.Contains("|")) {
                        btn2Data.Add(category, new ButtonData(control.Text.Split('|')[0], control.Text.Split('|')[1]));
                    }
                }
            }

            StringBuilder str = new StringBuilder();
            str.AppendLine("[appId]");
            str.AppendLine(appIdTextBox.Text);
            str.AppendLine("");

            foreach (KeyValuePair<string, RPCData> item in RichPresenceList) {
                string cate = item.Key;
                RichPresenceList[cate].buttons.Clear();
                foreach (var data in btn1Data) {
                    RichPresenceList[data.Key].buttons.Add(data.Value);
                }

                foreach (var data in btn2Data) {
                    RichPresenceList[data.Key].buttons.Add(data.Value);
                }

                str.AppendLine("[" + cate + "]");
                if(RichPresenceList[cate].details.Length > 0) str.AppendLine("details=" + RichPresenceList[cate].details);
                if (RichPresenceList[cate].state.Length > 0) str.AppendLine("state=" + RichPresenceList[cate].state);
                str.AppendLine("hasTimestamp=" + RichPresenceList[cate].hasTimestamp.ToString().ToLowerInvariant());
                if (RichPresenceList[cate].assetsData != null) {
                    if (!string.IsNullOrEmpty(RichPresenceList[cate].assetsData.LargeImageKey)) str.AppendLine("LargeImageKey=" + RichPresenceList[cate].assetsData.LargeImageKey);
                    if (!string.IsNullOrEmpty(RichPresenceList[cate].assetsData.LargeImageText)) str.AppendLine("LargeImageText=" + RichPresenceList[cate].assetsData.LargeImageText);
                    if (!string.IsNullOrEmpty(RichPresenceList[cate].assetsData.SmallImageKey)) str.AppendLine("SmallImageKey=" + RichPresenceList[cate].assetsData.SmallImageKey);
                    if (!string.IsNullOrEmpty(RichPresenceList[cate].assetsData.SmallImageText)) str.AppendLine("SmallImageText=" + RichPresenceList[cate].assetsData.SmallImageText);
                }

                if (RichPresenceList[cate].buttons != null) {
                    for (int i = 0; i < RichPresenceList[cate].buttons.Count; i++) {
                        if (RichPresenceList[cate].buttons[i].Label.Length == 0 || RichPresenceList[cate].buttons[i].Url.Length == 0) continue;
                        str.AppendLine("button" + (i + 1).ToString() + "=" + RichPresenceList[cate].buttons[i].Label + "|" + RichPresenceList[cate].buttons[i].Url);
                    }
                }
                str.AppendLine("");
            }

            if (!Directory.Exists(OptionsFolder)) {
                Directory.CreateDirectory(OptionsFolder);
            }
            string configFile = OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc.cfg");
            File.WriteAllText(configFile, str.ToString());
            this.Close();
        }
    }
}
