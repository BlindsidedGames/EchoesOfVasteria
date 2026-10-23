using System;
using System.Globalization;
using UnityEngine.Localization.Settings;

namespace Blindsided.Utilities
{
    /// <summary>Player-facing formatting only. Never use this for saves, identifiers or wire formats.</summary>
    public static class LocaleNumbers
    {
        public static string Code
        {
            get
            {
                var selected = LocalizationSettings.SelectedLocaleAsync;
                return selected.IsDone && selected.Result != null ? Normalize(selected.Result.Identifier.Code) : "en";
            }
        }

        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code)) return "en";
            code = code.ToLowerInvariant();
            if (code.StartsWith("de")) return "de";
            if (code.StartsWith("es")) return "es-419";
            if (code.StartsWith("fr")) return "fr";
            if (code.StartsWith("pt")) return "pt-BR";
            if (code.StartsWith("ru")) return "ru";
            if (code.StartsWith("ja")) return "ja";
            if (code.StartsWith("zh")) return "zh-CN";
            return "en";
        }

        // Explicit formats avoid OS-dependent neutral-culture resolution and user overrides.
        private static readonly CultureInfo English = Create("en-US", ".", ",", 1);
        private static readonly CultureInfo German = Create("de-DE", ",", ".", 0);
        private static readonly CultureInfo Spanish = Create("es-MX", ".", ",", 0);
        private static readonly CultureInfo French = Create("fr-FR", ",", "\u202f", 0);
        private static readonly CultureInfo Portuguese = Create("pt-BR", ",", ".", 1);
        private static readonly CultureInfo Russian = Create("ru-RU", ",", "\u00a0", 0);
        private static readonly CultureInfo Japanese = Create("ja-JP", ".", ",", 1);
        private static readonly CultureInfo Chinese = Create("zh-CN", ".", ",", 1);
        private static CultureInfo Create(string name, string dec, string group, int percentPattern)
        {
            var culture = new CultureInfo(name, false);
            culture.NumberFormat.NumberDecimalSeparator = dec;
            culture.NumberFormat.NumberGroupSeparator = group;
            culture.NumberFormat.PercentDecimalSeparator = dec;
            culture.NumberFormat.PercentGroupSeparator = group;
            culture.NumberFormat.PercentPositivePattern = percentPattern;
            return CultureInfo.ReadOnly(culture);
        }
        public static CultureInfo Culture => GetCulture(Code);
        public static string Format(FormattableString value) => value.ToString(Culture);
        public static string PercentSuffix => (Culture.NumberFormat.PercentPositivePattern == 0 ? "\u00a0" : "") + "%";
        public static CultureInfo GetCulture(string code) => Normalize(code) switch
        {
            "de" => German, "es-419" => Spanish, "fr" => French, "pt-BR" => Portuguese,
            "ru" => Russian, "ja" => Japanese, "zh-CN" => Chinese, _ => English
        };
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static string Format(double value, string format = "0.##", string code = null)
            => double.IsNaN(value) ? "—" : double.IsPositiveInfinity(value) ? "∞" : double.IsNegativeInfinity(value) ? "−∞" : value.ToString(format, GetCulture(code ?? Code));
        public static string Integer(double value, bool grouped = true, string code = null)
            => Format(value, grouped ? "N0" : "0", code);
        public static string PercentPoints(double value, string format = "0.#", string code = null)
        {
            var culture = GetCulture(code ?? Code);
            return Format(value, format, code) + (culture.NumberFormat.PercentPositivePattern == 0 ? "\u00a0" : "") + "%";
        }
        public static string Input(double value, string code = null) => IsFinite(value) ? Format(Math.Floor(value), "0", code) : "0";

        /// <summary>Read quantity input with selected-language decimals. No grouping, suffixes or fallback parser:
        /// ambiguous text such as German "1.000" is rejected instead of silently becoming one or one thousand.</summary>
        public static bool TryInput(string text, out double value, string code = null)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var culture = GetCulture(code ?? Code);
            text = text.Trim();
            var separator = culture.NumberFormat.NumberDecimalSeparator;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] >= '0' && text[i] <= '9') continue;
                if (i == 0 && (text[i] == '+' || text[i] == '-')) continue;
                if (separator.Length == 1 && text[i] == separator[0]) continue;
                return false;
            }
            return double.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture, out value) && IsFinite(value);
        }

        private static readonly string[] EnglishUnits = { "", "K", "M", "B", "T" };
        private static readonly string[] GermanUnits = { "", "", "Mio.", "Mrd.", "Bio." };
        private static readonly string[] SpanishUnits = { "", "K", "M", "M", "B" };
        private static readonly string[] FrenchUnits = { "", "k", "M", "Md", "Bn" };
        private static readonly string[] PortugueseUnits = { "", "mil", "mi", "bi", "tri" };
        private static readonly string[] RussianUnits = { "", "тыс.", "млн", "млрд", "трлн" };
        private static readonly string[] JapaneseUnits = { "", "万", "億", "兆", "京" };
        private static readonly string[] ChineseUnits = { "", "万", "亿", "万亿" };
        // CLDR customary compact names have a finite useful range. Beyond it use scientific notation;
        // never extend English short-scale names into a language with different magnitudes.
        public static bool CompactParts(double value, bool hideDecimal, out string number, out string suffix, string code = null)
        {
            code = Normalize(code ?? Code);
            suffix = "";
            if (!IsFinite(value)) { number = Format(value, code: code); return true; }
            var absolute = Math.Abs(value);
            var eastAsian = code == "ja" || code == "zh-CN";
            var step = eastAsian ? 4 : 3;
            var group = absolute < 1 ? 0 : Math.Max(0, (int)Math.Floor(Math.Log10(absolute) / step));
            var units = code switch
            {
                "de" => GermanUnits, "es-419" => SpanishUnits, "fr" => FrenchUnits,
                "pt-BR" => PortugueseUnits, "ru" => RussianUnits, "ja" => JapaneseUnits,
                "zh-CN" => ChineseUnits, _ => EnglishUnits
            };
            if (group >= units.Length) { number = null; return false; }
            if (code == "de" && group == 1) { number = Format(Math.Truncate(value), "N0", code); return true; }
            var divisor = Math.Pow(10, group * step);
            if (code == "es-419" && group == 3) divisor = 1e6; // CLDR: 1000 M, not English billion B.
            var mantissa = value / divisor;
            var integerDigits = Math.Abs(mantissa) < 1 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(mantissa))) + 1;
            var decimals = hideDecimal && group == 0 ? 0 : Math.Max(0, 3 - integerDigits);
            var factor = Math.Pow(10, decimals);
            mantissa = Math.Truncate(mantissa * factor) / factor;
            number = Format(mantissa, "F" + decimals, code);
            if (group > 0) suffix = (eastAsian ? "" : "\u00a0") + units[group];
            return true;
        }

        public static string TimeUnit(int unit, double count, bool shortForm, string code = null)
        {
            code = Normalize(code ?? Code);
            string[] units;
            if (shortForm)
            {
                units = code switch
                {
                    "de" => new[] { "T", "Std.", "Min.", "Sek." },
                    "es-419" => new[] { "d", "h", "min", "s" },
                    "fr" => new[] { "j", "h", "min", "s" },
                    "pt-BR" => new[] { "d", "h", "min", "s" },
                    "ru" => new[] { "д", "ч", "мин", "с" },
                    "ja" => new[] { "日", "時間", "分", "秒" },
                    "zh-CN" => new[] { "天", "小时", "分钟", "秒" },
                    _ => new[] { "d", "h", "m", "s" }
                };
            }
            else if (code == "ru")
            {
                var mod100 = Math.Abs(count) % 100; var mod10 = Math.Abs(count) % 10;
                var plural = count != Math.Truncate(count) ? 1 : mod100 >= 11 && mod100 <= 14 ? 2 : mod10 == 1 ? 0 : mod10 >= 2 && mod10 <= 4 ? 1 : 2;
                var forms = new[] { new[] { "день", "дня", "дней" }, new[] { "час", "часа", "часов" }, new[] { "минута", "минуты", "минут" }, new[] { "секунда", "секунды", "секунд" } };
                return " " + forms[unit][plural];
            }
            else
            {
                var singular = count == 1;
                units = code switch
                {
                    "de" => singular ? new[] { "Tag", "Stunde", "Minute", "Sekunde" } : new[] { "Tage", "Stunden", "Minuten", "Sekunden" },
                    "es-419" => singular ? new[] { "día", "hora", "minuto", "segundo" } : new[] { "días", "horas", "minutos", "segundos" },
                    "fr" => count >= 0 && count < 2 ? new[] { "jour", "heure", "minute", "seconde" } : new[] { "jours", "heures", "minutes", "secondes" },
                    "pt-BR" => singular ? new[] { "dia", "hora", "minuto", "segundo" } : new[] { "dias", "horas", "minutos", "segundos" },
                    "ja" => new[] { "日", "時間", "分", "秒" }, "zh-CN" => new[] { "天", "小时", "分钟", "秒" },
                    _ => singular ? new[] { "Day", "Hour", "Minute", "Second" } : new[] { "Days", "Hours", "Minutes", "Seconds" }
                };
            }
            return (code == "ja" || code == "zh-CN" || shortForm && code == "en" ? "" : " ") + units[unit];
        }
        public static string Unavailable => Code switch
        {
            "de" => "n. v.", "es-419" => "N/D", "fr" => "N/D", "pt-BR" => "N/D", "ru" => "н/д", "ja" => "該当なし", "zh-CN" => "不适用", _ => "N/A"
        };
    }
}
