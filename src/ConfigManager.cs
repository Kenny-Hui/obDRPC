using OpenBveApi.FileSystem;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace obDRPC {
    internal class ConfigManager {
        private static FileSystem FileSystem;
        private static string OptionsFolder;
        public static string appId { get; private set; }
        public static HashSet<Key> KeyCombination { get; private set; }
        public static List<Profile> ProfileList { get; private set; }
        public static void Initialize(FileSystem fs) {
            FileSystem = fs;
            ProfileList = new List<Profile>();
            KeyCombination = new HashSet<Key>();
            OptionsFolder = OpenBveApi.Path.CombineDirectory(FileSystem.SettingsFolder, "1.5.0");
        }

        public static void LoadConfig() {
            ProfileList.Clear();
            if (!Directory.Exists(OptionsFolder)) {
                Directory.CreateDirectory(OptionsFolder);
            }

            string configFile = OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc.xml");
            if (File.Exists(configFile)) {
                Dictionary<string, RPCData> presenceList = new Dictionary<string, RPCData>();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(configFile);
                if (xmlDoc.GetElementsByTagName("appId").Count > 0) {
                    appId = xmlDoc.GetElementsByTagName("appId")[0].InnerText;
                }

                if (xmlDoc.GetElementsByTagName("profileSwitchKey").Count > 0) {
                    string value = xmlDoc.GetElementsByTagName("profileSwitchKey")[0].InnerText;
                    int totalKeys = value.Split('+').Length;
                    KeyCombination.Clear();
                    if (totalKeys > 0) {
                        for (int i = 0; i < totalKeys; i++) {
                            string keyStr = value.Split('+')[i].Trim();
                            Key key;
                            if (Enum.TryParse(keyStr, out key)) {
                                KeyCombination.Add(key);
                            }
                        }
                    }
                }

                if (xmlDoc.GetElementsByTagName("presenceList")[0] != null) {
                    foreach (XmlElement element in xmlDoc.GetElementsByTagName("presenceList")[0].ChildNodes) {
                        RPCData presence = new RPCData();
                        string id = element.GetAttribute("id");
                        if (id == null) continue;

                        presence.details = element.GetElementsByTagName("details")[0]?.InnerText;
                        presence.state = element.GetElementsByTagName("state")[0]?.InnerText;

                        if (element.GetElementsByTagName("hasTimestamp")[0]?.InnerText != null) {
                            presence.hasTimestamp = XmlConvert.ToBoolean(element.GetElementsByTagName("hasTimestamp")[0].InnerText);
                        }

                        /* Assets */
                        if (element.GetElementsByTagName("largeImageKey")[0] != null) {
                            presence.AddLargeImageKey(element.GetElementsByTagName("largeImageKey")[0].InnerText);
                        }

                        if (element.GetElementsByTagName("largeImageText")[0] != null) {
                            presence.AddLargeImageText(element.GetElementsByTagName("largeImageText")[0].InnerText);
                        }

                        if (element.GetElementsByTagName("smallImageKey")[0] != null) {
                            presence.AddSmallImageKey(element.GetElementsByTagName("smallImageKey")[0].InnerText);
                        }

                        if (element.GetElementsByTagName("smallImageText")[0] != null) {
                            presence.AddSmallImageText(element.GetElementsByTagName("smallImageText")[0].InnerText);
                        }

                        /* Button */
                        foreach (XmlElement button in element.GetElementsByTagName("button")) {
                            string text = button.GetElementsByTagName("text")[0]?.InnerText;
                            string url = button.GetElementsByTagName("url")[0]?.InnerText;
                            if (text != null && url != null) {
                                presence.AddButton(text, url);
                            }
                        }

                        presenceList.Add(id, presence);
                    }

                    /* Parse profile */
                    foreach (XmlElement profile in xmlDoc.GetElementsByTagName("profile")) {
                        string name = profile.GetAttribute("name");
                        if (name == null) continue;
                        string menu = profile.GetElementsByTagName("menu")[0]?.InnerText;
                        string game = profile.GetElementsByTagName("game")[0]?.InnerText;
                        string boarding = profile.GetElementsByTagName("boarding")[0]?.InnerText;
                        RPCData menuPresence = menu != null && presenceList.ContainsKey(menu) ? presenceList[menu] : null;
                        RPCData gamePresence = game != null && presenceList.ContainsKey(game) ? presenceList[game] : null;
                        RPCData boardingPresence = boarding != null && presenceList.ContainsKey(boarding) ? presenceList[boarding] : null;
                        ProfileList.Add(new Profile(name, menuPresence, gamePresence, boardingPresence));
                    }
                }
            }
        }

        internal static bool SaveConfigToDisk() {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement rootElement = xmlDoc.CreateElement("data");
            XmlElement appIdElement = xmlDoc.CreateElement("appId");
            XmlElement switchKeyElement = xmlDoc.CreateElement("profileSwitchKey");
            XmlElement presenceListElement = xmlDoc.CreateElement("presenceList");
            Dictionary<string, Dictionary<string, string>> presenceName = new Dictionary<string, Dictionary<string, string>>();
            appIdElement.InnerText =  appId;
            switchKeyElement.InnerText = string.Join("+", KeyCombination);
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

            try {
                if (!Directory.Exists(OptionsFolder)) {
                    Directory.CreateDirectory(OptionsFolder);
                }

                xmlDoc.AppendChild(rootElement);
                xmlDoc.Save(OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc2.xml"));
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public static void UpdateProfileList(List<Profile> profileList) {
            ProfileList.Clear();
            ProfileList.AddRange(profileList);
        }
    }
}
