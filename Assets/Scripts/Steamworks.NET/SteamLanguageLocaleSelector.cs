using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Selects a locale based on the player's Steam language preference.
/// </summary>
/// <remarks>
/// Implements <see cref="IStartupLocaleSelector"/> so that Unity Localization
/// can automatically choose the appropriate locale on startup when the
/// <see cref="UseSteamLanguage"/> flag is enabled.
/// </remarks>
public class SteamLanguageLocaleSelector : IStartupLocaleSelector
{
    private const string UseSteamLanguageKey = "UseSteamLanguage";

    // Mapping between Steam's language codes and IETF locale codes used by Unity.
    private static readonly Dictionary<string, string> SteamToLocale = new()
    {
        { "arabic", "ar" },
        { "bulgarian", "bg" },
        { "czech", "cs" },
        { "danish", "da" },
        { "dutch", "nl" },
        { "english", "en" },
        { "finnish", "fi" },
        { "french", "fr" },
        { "german", "de" },
        { "greek", "el" },
        { "hungarian", "hu" },
        { "indonesian", "id" },
        { "italian", "it" },
        { "japanese", "ja" },
        { "koreana", "ko" },
        { "norwegian", "no" },
        { "polish", "pl" },
        { "portuguese", "pt" },
        { "brazilian", "pt-BR" },
        { "romanian", "ro" },
        { "russian", "ru" },
        { "schinese", "zh-CN" },
        { "spanish", "es" },
        { "swedish", "sv" },
        { "tchinese", "zh-TW" },
        { "thai", "th" },
        { "turkish", "tr" },
        { "ukrainian", "uk" },
        { "vietnamese", "vi" }
    };

    /// <summary>
    /// Flag stored in <see cref="PlayerPrefs"/> indicating whether Steam language
    /// should be used to select the locale.
    /// </summary>
    public static bool UseSteamLanguage
    {
        get => PlayerPrefs.GetInt(UseSteamLanguageKey, 0) == 1;
        set => PlayerPrefs.SetInt(UseSteamLanguageKey, value ? 1 : 0);
    }

    /// <inheritdoc />
    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        if (!UseSteamLanguage)
        {
            return null;
        }

#if !DISABLESTEAMWORKS
        if (!SteamManager.Initialized)
        {
            return null;
        }

        var steamLang = SteamApps.GetCurrentGameLanguage();
        if (string.IsNullOrEmpty(steamLang))
        {
            return null;
        }

        if (SteamToLocale.TryGetValue(steamLang, out var localeCode))
        {
            var identifier = new LocaleIdentifier(localeCode);
            return availableLocales.GetLocale(identifier);
        }
#endif
        return null;
    }
}

/// <summary>
/// Registers the <see cref="SteamLanguageLocaleSelector"/> with
/// <see cref="LocalizationSettings"/> before the first scene loads.
/// </summary>
public static class SteamLanguageLocaleSelectorInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSelector()
    {
        var selectors = LocalizationSettings.Instance.StartupLocaleSelectors;
        // Insert at the start so it runs before default selectors.
        selectors.Insert(0, new SteamLanguageLocaleSelector());
    }
}
