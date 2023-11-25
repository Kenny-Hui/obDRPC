using System.Collections.Generic;

namespace obDRPC {
    public class Profile {
        public string Name;
        public Dictionary<Context, RPCLayout> Presence;

        public Profile(string name) {
            Name = name;
            Presence = new Dictionary<Context, RPCLayout> {
                { Context.Menu, new RPCLayout() },
                { Context.InGame, new RPCLayout() },
                { Context.Boarding, new RPCLayout() }
            };
        }

        public Profile(string name, RPCLayout menu, RPCLayout game, RPCLayout boarding) {
            Name = name;
            Presence = new Dictionary<Context, RPCLayout> {
                { Context.Menu, menu },
                { Context.InGame, game },
                { Context.Boarding, boarding }
            };
        }
    }
}
