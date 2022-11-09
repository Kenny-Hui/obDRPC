using System.Collections.Generic;

namespace obDRPC {
    public class Profile {
        public Dictionary<string, RPCData> PresenceList;

        public Profile() {
            PresenceList = new Dictionary<string, RPCData>();
            PresenceList.Add("menu", new RPCData());
            PresenceList.Add("game", new RPCData());
            PresenceList.Add("boarding", new RPCData());
        }

        public Profile(RPCData menu, RPCData game, RPCData boarding) {
            PresenceList = new Dictionary<string, RPCData>();
            PresenceList.Add("menu", menu);
            PresenceList.Add("game", game);
            PresenceList.Add("boarding", boarding);
        }
    }
}
