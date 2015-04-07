using Newtonsoft.Json;
using Windows.Storage;

namespace YubiCrypt.Mobile.WindowsPhone
{
    internal static class SettingsHelper
    {
        private static readonly ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        internal static void SaveSetting(string key, object value)
        {
            var jsonSerializedObject = JsonConvert.SerializeObject(value);
            localSettings.Values[key] = jsonSerializedObject;
        }

        internal static T LoadSetting<T>(string key)
        {
            string storedJsonObject = (string) localSettings.Values[key];

            if (string.IsNullOrWhiteSpace(storedJsonObject))
                return default(T);

            T storedObject = JsonConvert.DeserializeObject<T>(storedJsonObject);

            return storedObject;
        }

        internal static bool SettingsExists(string key)
        {
            return localSettings.Values.ContainsKey(key);
        }

        internal static bool DeleteSetting(string key)
        {
            if (localSettings.Values.ContainsKey(key))
            {
                return localSettings.Values.Remove(key);
            }
            return false;
        }
    }
}
