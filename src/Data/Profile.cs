﻿using System.Collections.Generic;

namespace obDRPC {
    public class Profile {
        public string Name;
        public Dictionary<string, RPCData> PresenceList;

        public Profile(string name) {
            Name = name;
            PresenceList = new Dictionary<string, RPCData> {
                { "menu", new RPCData() },
                { "game", new RPCData() },
                { "boarding", new RPCData() }
            };
        }

        public Profile(string name, RPCData menu, RPCData game, RPCData boarding) {
            Name = name;
            PresenceList = new Dictionary<string, RPCData> {
                { "menu", menu },
                { "game", game },
                { "boarding", boarding }
            };
        }
    }
}
