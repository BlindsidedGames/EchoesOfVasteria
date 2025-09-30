using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class LocalizationManager : MonoBehaviour
{
    private const string SelectedLocaleKey = "Localization.SelectedLocale";

    [SerializeField] private string defaultLocaleCode = "en";

    public event Action<Locale> LocaleChanged;

    public Locale CurrentLocale => LocalizationSettings.SelectedLocale;

    public string CurrentLocaleCode => LocalizationSettings.SelectedLocale != null
        ? LocalizationSettings.SelectedLocale.Identifier.Code
        : string.Empty;

    private void Start()
    {
        InitializeLocalization();
    }

    private void InitializeLocalization()
    {
        var initializationOperation = LocalizationSettings.InitializationOperation;
        if (initializationOperation.IsDone)
        {
            ApplyStoredLocale();
        }
        else
        {
            initializationOperation.Completed += _ => ApplyStoredLocale();
        }
    }

    private void ApplyStoredLocale()
    {
        var savedLocaleCode = PlayerPrefs.GetString(SelectedLocaleKey, string.Empty);
        var targetLocale = !string.IsNullOrWhiteSpace(savedLocaleCode) ? FindLocale(savedLocaleCode) : null;

        if (targetLocale == null)
        {
            targetLocale = FindLocale(defaultLocaleCode);
        }

        if (targetLocale == null && LocalizationSettings.AvailableLocales.Locales.Count > 0)
        {
            targetLocale = LocalizationSettings.AvailableLocales.Locales[0];
        }

        if (targetLocale == null) return;

        SetLocaleInternal(targetLocale, true);
    }

    public void SelectLocale(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode))
        {
            Debug.LogWarning("LocalizationManager.SelectLocale received an empty locale code.");
            return;
        }

        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            StartCoroutine(SelectLocaleWhenInitialized(localeCode));
            return;
        }

        ApplyLocaleCode(localeCode);
    }

    public void SelectLocale(Locale locale)
    {
        if (locale == null)
        {
            Debug.LogWarning("LocalizationManager.SelectLocale received a null locale.");
            return;
        }

        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            StartCoroutine(SelectLocaleWhenInitialized(locale));
            return;
        }

        SetLocaleInternal(locale, true);
    }

    private IEnumerator SelectLocaleWhenInitialized(string localeCode)
    {
        yield return LocalizationSettings.InitializationOperation;
        ApplyLocaleCode(localeCode);
    }

    private IEnumerator SelectLocaleWhenInitialized(Locale locale)
    {
        yield return LocalizationSettings.InitializationOperation;
        SetLocaleInternal(locale, true);
    }

    private void ApplyLocaleCode(string localeCode)
    {
        var locale = FindLocale(localeCode);
        if (locale == null)
        {
            Debug.LogWarning($"LocalizationManager was not able to find a locale with code '{localeCode}'.");
            return;
        }

        SetLocaleInternal(locale, true);
    }

    private void SetLocaleInternal(Locale locale, bool persistSelection)
    {
        if (locale == null) return;

        if (LocalizationSettings.SelectedLocale == locale)
        {
            if (persistSelection)
            {
                PersistLocale(locale);
            }

            LocaleChanged?.Invoke(locale);
            return;
        }

        LocalizationSettings.SelectedLocale = locale;

        if (persistSelection)
        {
            PersistLocale(locale);
        }

        LocaleChanged?.Invoke(locale);
    }

    private void PersistLocale(Locale locale)
    {
        if (locale == null) return;

        PlayerPrefs.SetString(SelectedLocaleKey, locale.Identifier.Code);
        PlayerPrefs.Save();
    }

    private Locale FindLocale(string localeCode)
    {
        if (string.IsNullOrWhiteSpace(localeCode)) return null;

        var availableLocales = LocalizationSettings.AvailableLocales;
        var locale = availableLocales.GetLocale(localeCode);
        if (locale != null) return locale;

        var identifier = new LocaleIdentifier(localeCode);
        return availableLocales.GetLocale(identifier);
    }
}
