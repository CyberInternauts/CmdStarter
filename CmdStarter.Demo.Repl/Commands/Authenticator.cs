namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands
{
    public static class Authenticator
    {
        public enum LoginStatus
        {
            WrongUsername,
            WrongPassword,
            LoggedIn
        }

        public static bool IsCurrentlyLoggedIn { get; private set; }

        public static LoginStatus Authenticate(string username, string password)
        {
            if (!DemoDb.Accounts.ContainsKey(username)) return LoginStatus.WrongUsername;

            if (!DemoDb.Accounts[username].Equals(password)) return LoginStatus.WrongPassword;

            IsCurrentlyLoggedIn = true;

            return LoginStatus.LoggedIn;
        }

        public static void LogOut()
        {
            IsCurrentlyLoggedIn = false;
        }
    }
}
