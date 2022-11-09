using System.Collections.Generic;
using System.Windows.Forms;

namespace obDRPC {
    internal class ProfileDialog {
        public static string Show(Dictionary<string, Profile> profileList) {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 130,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Create profile",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
        };
            Label textLabel = new Label() {
                Left = 15,
                Top = 10,
                AutoSize = true,
                Text = "Please enter profile name"
            };
            TextBox textBox = new TextBox() {
                Left = 15,
                Top = 30,
                Width = 300
            };
            Button confirmation = new Button() {
                Text = "OK",
                Left = 235,
                Width = 80,
                Top = 60,
                DialogResult = DialogResult.OK
            };
            confirmation.Click += (sender, e) => {
                if (profileList.ContainsKey(textBox.Text)) {
                    MessageBox.Show("Profile name already exists!");
                } else {
                    prompt.Close();
                }
            };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }
    }
}
