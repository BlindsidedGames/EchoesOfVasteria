using QFSW.QC;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple password gate for developer console commands.
    /// </summary>
    public static class ConsoleAuth
    {
        private const string DevPassword = "MattsTheBest";
        private static bool _isAuthenticated;

        /// <summary>
        /// If true, dev commands are unlocked.
        /// </summary>
        public static bool IsAuthenticated => _isAuthenticated;

        [Command("login", "Authenticate to unlock developer commands")]
        public static void Login(string password)
        {
            if (password == DevPassword)
            {
                _isAuthenticated = true;
                if (QuantumConsole.Instance != null)
                {
                    QuantumConsole.Instance.LogToConsole("Developer commands unlocked.");
                }
            }
            else
            {
                if (QuantumConsole.Instance != null)
                {
                    QuantumConsole.Instance.LogToConsole("Invalid password.");
                }
                throw new System.Exception("Invalid password for developer commands.");
            }
        }

        [Command("logout", "Lock developer commands")]
        public static void Logout()
        {
            _isAuthenticated = false;
            if (QuantumConsole.Instance != null)
            {
                QuantumConsole.Instance.LogToConsole("Developer commands locked.");
            }
        }

        /// <summary>
        /// Throws if the user is not authenticated for dev commands.
        /// </summary>
        public static void EnsureAuthenticated()
        {
            if (!_isAuthenticated)
            {
                throw new System.Exception("Developer commands are locked. Use: login MattsTheBest");
            }
        }
    }
}


