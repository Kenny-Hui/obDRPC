using System.Collections.Generic;

namespace obDRPC {
    public class Profile {
        public string Name { get; set; }
        public Dictionary<string, RPCData> PresenceList;

        public Profile(string name) {
            Name = name;
            PresenceList = new Dictionary<string, RPCData>();
            PresenceList.Add("menu", new RPCData());
            PresenceList.Add("game", new RPCData());
            PresenceList.Add("boarding", new RPCData());
        }

        public Profile(string name, RPCData menu, RPCData game, RPCData boarding) {
            Name = name;
            PresenceList = new Dictionary<string, RPCData>();
            PresenceList.Add("menu", menu);
            PresenceList.Add("game", game);
            PresenceList.Add("boarding", boarding);
        }
    }
}
