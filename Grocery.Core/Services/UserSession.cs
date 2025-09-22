namespace Grocery.App.Services
{
    // Minimal, thread-safe session holder for the running app.
    // Other ViewModels can read UserSession.CurrentUsername to know who is logged in.
    public static class UserSession
    {
        private static readonly object _sync = new();
        private static string? _currentUsername;

        public static string? CurrentUsername
        {
            get
            {
                lock (_sync) { return _currentUsername; }
            }
            set
            {
                lock (_sync) { _currentUsername = value; }
            }
        }

        public static void Clear()
        {
            lock (_sync) { _currentUsername = null; }
        }
    }
}