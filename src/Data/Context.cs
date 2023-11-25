namespace obDRPC {
    public enum Context {
        None,
        Menu,
        InGame,
        Boarding
    }

    public static class ContextHelper {
        public static string ToString(Context context)
        {
            switch (context)
            {
                case Context.Menu:
                    return "menu";
                case Context.Boarding:
                    return "boarding";
                case Context.InGame:
                    return "game";
                default:
                    return "";
            }
        }

        public static Context FromString(string contextName)
        {
            switch (contextName)
            {
                case "menu":
                    return Context.Menu;
                case "game":
                    return Context.InGame;
                case "boarding":
                    return Context.Boarding;
                default:
                    return Context.None;
            }
        }
    }
}
