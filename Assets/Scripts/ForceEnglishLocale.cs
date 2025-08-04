using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
///     Forces the game to use the English locale on startup.
/// </summary>
public static class ForceEnglishLocale
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetEnglishLocale()
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("en"));
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
    }
}
