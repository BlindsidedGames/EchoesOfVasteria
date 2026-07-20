using System.Collections.Generic;
using VinTools.BetterRuleTiles.Internal;

namespace VinTools.BetterRuleTiles
{
    public partial class BetterRuleTileBase
    {
        public List<CustomTileProperty> customProperties = new List<CustomTileProperty>();

        /// <summary>
        /// Gets the integer with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (GetProperty(key, out var p)) return p.GetInt();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the float with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public float GetFloat(string key, float defaultValue = 0)
        {
            if (GetProperty(key, out var p)) return p.GetFloat();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the double with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public double GetDouble(string key, double defaultValue = 0)
        {
            if (GetProperty(key, out var p)) return p.GetDouble();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the character with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public char GetChar(string key, char defaultValue = '\0')
        {
            if (GetProperty(key, out var p)) return p.GetChar();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the string with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public string GetString(string key, string defaultValue = "")
        {
            if (GetProperty(key, out var p)) return p.GetString();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the boolean with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (GetProperty(key, out var p)) return p.GetBool();
            else return defaultValue;
        }

        private bool GetProperty(string key, out CustomTileProperty prop)
        {
            prop = customProperties.Find(t => t._key == key);
            if (prop == null) return false;
            else return true;
        }
    }
    
    public partial class BetterHexagonalRuleTileBase
    {
        public List<CustomTileProperty> customProperties = new List<CustomTileProperty>();

        /// <summary>
        /// Gets the integer with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (GetProperty(key, out var p)) return p.GetInt();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the float with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public float GetFloat(string key, float defaultValue = 0)
        {
            if (GetProperty(key, out var p)) return p.GetFloat();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the double with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public double GetDouble(string key, double defaultValue = 0)
        {
            if (GetProperty(key, out var p)) return p.GetDouble();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the character with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public char GetChar(string key, char defaultValue = '\0')
        {
            if (GetProperty(key, out var p)) return p.GetChar();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the string with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public string GetString(string key, string defaultValue = "")
        {
            if (GetProperty(key, out var p)) return p.GetString();
            else return defaultValue;
        }

        /// <summary>
        /// Gets the boolean with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key was not found</param>
        /// <returns></returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (GetProperty(key, out var p)) return p.GetBool();
            else return defaultValue;
        }

        private bool GetProperty(string key, out CustomTileProperty prop)
        {
            prop = customProperties.Find(t => t._key == key);
            if (prop == null) return false;
            else return true;
        }
    }
}