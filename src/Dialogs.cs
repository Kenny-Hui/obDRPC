using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace obDRPC {
    internal class Dialogs {
        public static string ShowCreateDialog(List<Profile> profileList) {
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
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK) {
                return validate(textBox.Text, profileList);
            } else {
                return null;
            }
        }

        public static string ShowRenameDialog(string profileName, List<Profile> profileList) {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 130,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"Rename profile",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label()
            {
                Left = 15,
                Top = 10,
                AutoSize = true,
                Text = $"Please enter a new profile name for \"{profileName}\""
            };
            TextBox textBox = new TextBox()
            {
                Left = 15,
                Top = 30,
                Width = 300
            };
            Button confirmation = new Button()
            {
                Text = "OK",
                Left = 235,
                Width = 80,
                Top = 60,
                DialogResult = DialogResult.OK
            };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK) {
                return validate(textBox.Text, profileList);
            } else {
                return null;
            }
        }

        public static string ShowURLDialog(string initialURL)
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 130,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Set Button URL",
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            string ogURL = initialURL == null ? "" : initialURL;
            Label textLabel = new Label()
            {
                Left = 15,
                Top = 10,
                AutoSize = true,
                Text = "Please enter a valid URL"
            };
            TextBox textBox = new TextBox()
            {
                Left = 15,
                Top = 30,
                Width = 300,
                Text = ogURL ?? "https://"
            };
            Button confirmation = new Button()
            {
                Text = "OK",
                Left = 235,
                Width = 80,
                Top = 60,
                DialogResult = DialogResult.OK
            };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                Uri uri;
                if (Uri.TryCreate(textBox.Text, UriKind.Absolute, out uri)) {
                    return textBox.Text;
                } else {
                    MessageBox.Show("Entered URL is not valid.");
                    return null;
                }
            } else {
                return null;
            }
        }

        private static string validate(string res, List<Profile> profileList) {
            if (string.IsNullOrEmpty(res)) {
                MessageBox.Show("Profile name should not be empty!");
                return null;
            }

            bool hasExisting = profileList.Where(e => e.Name == res) != null;

            if (!hasExisting) {
                MessageBox.Show("Profile name already exists!");
                return null;
            } else {
                return res;
            }
        }
    }
}
