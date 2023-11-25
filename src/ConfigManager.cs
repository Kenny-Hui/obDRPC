using OpenBveApi.FileSystem;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace obDRPC {
    internal static class ConfigManager {
        private static FileSystem FileSystem;
        private static string OptionsFolder;
        private static string ConfigFile;
        public static string AppId { get; private set; }
        public static HashSet<Key> ProfileCycleKey { get; private set; }
        public static List<Profile> Profiles { get; private set; }
        public static void Initialize(FileSystem fs) {
            FileSystem = fs;
            Profiles = new List<Profile>();
            ProfileCycleKey = new HashSet<Key>();
            OptionsFolder = OpenBveApi.Path.CombineDirectory(FileSystem.SettingsFolder, "1.5.0");
            ConfigFile = OpenBveApi.Path.CombineFile(OptionsFolder, "options_drpc.xml");
        }

        public static void LoadConfig() {
            ProfileCycleKey.Clear();
            Profiles.Clear();
            if (!Directory.Exists(OptionsFolder)) {
                Directory.CreateDirectory(OptionsFolder);
            }

            if (File.Exists(ConfigFile)) {
                Dictionary<string, RPCLayout> presenceList = new Dictionary<string, RPCLayout>();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                if (xmlDoc.GetElementsByTagName("appId").Count > 0) {
                    AppId = xmlDoc.GetElementsByTagName("appId")[0].InnerText;
                }

                if (xmlDoc.GetElementsByTagName("profileSwitchKey").Count > 0) {
                    string value = xmlDoc.GetElementsByTagName("profileSwitchKey")[0].InnerText;
                    int totalKeys = value.Split('+').Length;
                    if (totalKeys > 0) {
                        for (int i = 0; i < totalKeys; i++) {
                            string keyStr = value.Split('+')[i].Trim();
                            Key key;
                            if (Enum.TryParse(keyStr, out key)) {
                                ProfileCycleKey.Add(key);
                            }
                        }
                    }
                }

                if (xmlDoc.GetElementsByTagName("presenceList")[0] != null) {
                    foreach (XmlElement element in xmlDoc.GetElementsByTagName("presenceList")[0].ChildNodes) {
                        RPCLayout presence = new RPCLayout();
                        string id = element.GetAttribute("id");
                        if (id == null) continue;

                        presence.Details = element.GetElementsByTagName("details")[0]?.InnerText;
                        presence.State = element.GetElementsByTagName("state")[0]?.InnerText;

                        if (element.GetElementsByTagName("hasTimestamp")[0]?.InnerText != null) {
                            presence.HasTimestamp = XmlConvert.ToBoolean(element.GetElementsByTagName("hasTimestamp")[0].InnerText);
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
                        RPCLayout menuPresence = menu != null && presenceList.ContainsKey(menu) ? presenceList[menu] : null;
                        RPCLayout gamePresence = game != null && presenceList.ContainsKey(game) ? presenceList[game] : null;
                        RPCLayout boardingPresence = boarding != null && presenceList.ContainsKey(boarding) ? presenceList[boarding] : null;
                        Profiles.Add(new Profile(name, menuPresence, gamePresence, boardingPresence));
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
            appIdElement.InnerText = AppId;
            switchKeyElement.InnerText = string.Join("+", ProfileCycleKey);
            rootElement.AppendChild(appIdElement);
            rootElement.AppendChild(switchKeyElement);

            foreach (Profile entry in Profiles)
            {
                Profile profile = entry;
                foreach (KeyValuePair<Context, RPCLayout> pair in profile.Presence)
                {
                    string context = ContextHelper.ToString(pair.Key);
                    string presenceID = context + profile.Name;
                    RPCLayout presence = pair.Value;
                    Dictionary<string, string> contextName = presenceName.ContainsKey(profile.Name) ? presenceName[profile.Name] : new Dictionary<string, string>();
                    contextName.Add(context, presenceID);
                    presenceName[profile.Name] = contextName;

                    XmlElement presenceElement = xmlDoc.CreateElement("presence");
                    presenceElement.SetAttribute("id", presenceID);

                    if (!string.IsNullOrEmpty(presence.Details))
                    {
                        XmlElement detailsElement = xmlDoc.CreateElement("details");
                        detailsElement.InnerText = presence.Details;
                        presenceElement.AppendChild(detailsElement);
                    }

                    if (!string.IsNullOrEmpty(presence.State))
                    {
                        XmlElement stateElement = xmlDoc.CreateElement("state");
                        stateElement.InnerText = presence.State;
                        presenceElement.AppendChild(stateElement);
                    }

                    XmlElement hasTimestampElement = xmlDoc.CreateElement("hasTimestamp");
                    hasTimestampElement.InnerText = presence.HasTimestamp.ToString().ToLowerInvariant();
                    presenceElement.AppendChild(hasTimestampElement);

                    if (!string.IsNullOrEmpty(presence.AssetsData?.LargeImageKey))
                    {
                        XmlElement largeImgKeyElement = xmlDoc.CreateElement("largeImageKey");
                        largeImgKeyElement.InnerText = presence.AssetsData.LargeImageKey;
                        presenceElement.AppendChild(largeImgKeyElement);
                    }

                    if (!string.IsNullOrEmpty(presence.AssetsData?.LargeImageText))
                    {
                        XmlElement largeImgTextElement = xmlDoc.CreateElement("largeImageText");
                        largeImgTextElement.InnerText = presence.AssetsData.LargeImageText;
                        presenceElement.AppendChild(largeImgTextElement);
                    }

                    if (!string.IsNullOrEmpty(presence.AssetsData?.LargeImageKey))
                    {
                        XmlElement smallImgKeyElement = xmlDoc.CreateElement("smallImageKey");
                        smallImgKeyElement.InnerText = presence.AssetsData.SmallImageKey;
                        presenceElement.AppendChild(smallImgKeyElement);
                    }

                    if (!string.IsNullOrEmpty(presence.AssetsData?.LargeImageText))
                    {
                        XmlElement smallImgTextElement = xmlDoc.CreateElement("smallImageText");
                        smallImgTextElement.InnerText = presence.AssetsData.SmallImageText;
                        presenceElement.AppendChild(smallImgTextElement);
                    }

                    if (presence.Buttons != null)
                    {
                        for (int i = 0; i < presence.Buttons.Count; i++)
                        {
                            if (!presence.Buttons[i].IsValid()) continue;
                            XmlElement buttonElement = xmlDoc.CreateElement("button");
                            XmlElement textElement = xmlDoc.CreateElement("text");
                            XmlElement urlElement = xmlDoc.CreateElement("url");

                            textElement.InnerText = presence.Buttons[i].Label;
                            urlElement.InnerText = presence.Buttons[i].Url;
                            buttonElement.AppendChild(textElement);
                            buttonElement.AppendChild(urlElement);

                            presenceElement.AppendChild(buttonElement);
                        }
                    }
                    presenceListElement.AppendChild(presenceElement);
                }
                rootElement.AppendChild(presenceListElement);
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> nameEntry in presenceName)
            {
                XmlElement profileElement = xmlDoc.CreateElement("profile");
                string profileName = nameEntry.Key;

                profileElement.SetAttribute("name", profileName);
                foreach (KeyValuePair<string, string> contextEntry in nameEntry.Value)
                {
                    string context = contextEntry.Key;
                    string presenceID = contextEntry.Value;
                    XmlElement contextElement = xmlDoc.CreateElement(context);
                    contextElement.InnerText = presenceID;
                    profileElement.AppendChild(contextElement);

                    if (profileElement.ChildNodes.Count > 0)
                    {
                        rootElement.AppendChild(profileElement);
                    }
                }
            }

            try {
                if (!Directory.Exists(OptionsFolder)) {
                    Directory.CreateDirectory(OptionsFolder);
                }

                xmlDoc.AppendChild(rootElement);
                xmlDoc.Save(ConfigFile);
            } catch (Exception) {
                return false;
            }

            return true;
        }

        public static void SetApplicationId(string id) {
            AppId = id;
        }

        public static void SetProfiles(List<Profile> profileList) {
            Profiles.Clear();
            Profiles.AddRange(profileList);
        }

        public static void SetProfileCycleKey(HashSet<Key> keyCombo) {
            ProfileCycleKey.Clear();
            foreach(Key key in keyCombo) {
                ProfileCycleKey.Add(key);
            }
        }
    }
}