using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.Utilities.TextStrings;
using static Blindsided.SaveData.GameData;

namespace Blindsided.Utilities
{
    public static class CalcUtils
    {
        public static readonly string[] Prefix =
        {
            "", // 0: 1e0
            " K", // 1: 1e3
            " M", // 2: 1e6
            " B", // 3: 1e9
            " T", // 4: 1e12
            " Qa", // 5: 1e15   (Quadrillion)
            " Qi", // 6: 1e18   (Quintillion)
            " Sx", // 7: 1e21   (Sextillion)
            " Sp", // 8: 1e24   (Septillion)
            " Oc", // 9: 1e27   (Octillion)
            " No", // 10: 1e30  (Nonillion)
            " Dc", // 11: 1e33  (Decillion)
            " UDc", // 12: 1e36  (Undecillion)
            " DDc", // 13: 1e39  (Duodecillion)
            " TDc", // 14: 1e42  (Tredecillion)
            " QaDc", // 15: 1e45  (Quattuordecillion)
            " QiDc", // 16: 1e48  (Quindecillion)
            " SxDc", // 17: 1e51  (Sexdecillion)
            " SpDc", // 18: 1e54  (Septendecillion)
            " OcDc", // 19: 1e57  (Octodecillion)
            " NoDc", // 20: 1e60  (Novemdecillion)
            " Vg", // 21: 1e63  (Vigintillion)
            " UVg", // 22: 1e66  (Unvigintillion)
            " DVg", // 23: 1e69  (Duovigintillion)
            " TVg", // 24: 1e72  (Tresvigintillion)
            " QaVg", // 25: 1e75  (Quattuorvigintillion)
            " QiVg", // 26: 1e78  (Quinvigintillion)
            " SxVg", // 27: 1e81  (Sesvigintillion)
            " SpVg", // 28: 1e84  (Septemvigintillion)
            " OcVg", // 29: 1e87  (Octovigintillion)
            " NoVg", // 30: 1e90  (Novemvigintillion)
            " Tg", // 31: 1e93  (Trigintillion)
            " UTg", // 32: 1e96  (Untrigintillion)
            " DTg", // 33: 1e99  (Duotrigintillion)
            " TTg", // 34: 1e102 (Trestrigintillion)
            " QaTg", // 35: 1e105 (Quattuortrigintillion)
            " QiTg", // 36: 1e108 (Quintrigintillion)
            " SxTg", // 37: 1e111 (Sestrigintillion)
            " SpTg", // 38: 1e114 (Septentrigintillion)
            " OcTg", // 39: 1e117 (Octotrigintillion)
            " NoTg", // 40: 1e120 (Novemtrigintillion)
            " Qag", // 41: 1e123 (Quadragintillion)
            " UQag", // 42: 1e126 (Unquadragintillion)
            " DQag", // 43: 1e129 (Duoquadragintillion)
            " TQag", // 44: 1e132 (Tresquadragintillion)
            " QaQag", // 45: 1e135 (Quattuorquadragintillion)
            " QiQag", // 46: 1e138 (Quinquadragintillion)
            " SxQag", // 47: 1e141 (Sexquadragintillion)
            " SpQag", // 48: 1e144 (Septenquadragintillion)
            " OcQag", // 49: 1e147 (Octoquadragintillion)
            " NoQag", // 50: 1e150 (Novemquadragintillion)
            " Qig", // 51: 1e153 (Quinquagintillion)
            " UQig", // 52: 1e156 (Unquinquagintillion)
            " DQig", // 53: 1e159 (Duoquinquagintillion)
            " TQig", // 54: 1e162 (Tresquinquagintillion)
            " QaQig", // 55: 1e165 (Quattuorquinquagintillion)
            " QiQig", // 56: 1e168 (Quinquinquagintillion)
            " SxQig", // 57: 1e171 (Sexquinquagintillion)
            " SpQig", // 58: 1e174 (Septenquinquagintillion)
            " OcQig", // 59: 1e177 (Octoquinquagintillion)
            " NoQig", // 60: 1e180 (Novemquinquagintillion)
            " Sxg", // 61: 1e183 (Sexagintillion)
            " USxg", // 62: 1e186 (Unsexagintillion)
            " DSxg", // 63: 1e189 (Duosexagintillion)
            " TSxg", // 64: 1e192 (Tresexagintillion)
            " QaSxg", // 65: 1e195 (Quattuorsexagintillion)
            " QiSxg", // 66: 1e198 (Quinsexagintillion)
            " SxSxg", // 67: 1e201 (Sexsexagintillion)
            " SpSxg", // 68: 1e204 (Septensexagintillion)
            " OcSxg", // 69: 1e207 (Octosexagintillion)
            " NoSxg", // 70: 1e210 (Novemsexagintillion)
            " Spg", // 71: 1e213 (Septuagintillion)
            " USpg", // 72: 1e216 (Unseptuagintillion)
            " DSpg", // 73: 1e219 (Duoseptuagintillion)
            " TSpg", // 74: 1e222 (Treseptuagintillion)
            " QaSpg", // 75: 1e225 (Quattuorseptuagintillion)
            " QiSpg", // 76: 1e228 (Quinseptuagintillion)
            " SxSpg", // 77: 1e231 (Sexseptuagintillion)
            " SpSpg", // 78: 1e234 (Septenseptuagintillion)
            " OcSpg", // 79: 1e237 (Octoseptuagintillion)
            " NoSpg", // 80: 1e240 (Novemseptuagintillion)
            " Ocg", // 81: 1e243 (Octogintillion)
            " UOcg", // 82: 1e246 (Unoctogintillion)
            " DOcg", // 83: 1e249 (Duooctogintillion)
            " TOcg", // 84: 1e252 (Tresoctogintillion)
            " QaOcg", // 85: 1e255 (Quattuoroctogintillion)
            " QiOcg", // 86: 1e258 (Quinoctogintillion)
            " SxOcg", // 87: 1e261 (Sexoctogintillion)
            " SpOcg", // 88: 1e264 (Septenoctogintillion)
            " OcOcg", // 89: 1e267 (Octooctogintillion)
            " NoOcg", // 90: 1e270 (Novemoctogintillion)
            " Nog", // 91: 1e273 (Nonagintillion)
            " UNog", // 92: 1e276 (Unnonagintillion)
            " DNog", // 93: 1e279 (Duononagintillion)
            " TNog", // 94: 1e282 (Trenonagintillion)
            " QaNog", // 95: 1e285 (Quattuornonagintillion)
            " QiNog", // 96: 1e288 (Quinnonagintillion)
            " SxNog", // 97: 1e291 (Sexnonagintillion)
            " SpNog", // 98: 1e294 (Septennonagintillion)
            " OcNog", // 99: 1e297 (Octononagintillion)
            " NoNog", // 100: 1e300 (Novemnonagintillion)
            " Ce", // 101: 1e303 (Centillion)
            " UCe", // 102: 1e306 (Uncentillion) - Covers up to (but not including) 1e309
            " DCe" // 103: 1e309 (Duocentillion) - Next prefix if needed
        };


//true = Joules
//false = Watts
        public static readonly string[] EnergyPrefixJ = { "J", "KJ", "MJ", "GJ", "TJ", "PJ", "EJ", "ZJ", "YJ" };
        public static readonly string[] EnergyPrefixW = { "W", "KW", "MW", "GW", "TW", "PW", "EW", "ZW", "YW" };

        private static readonly IReadOnlyDictionary<string, string> replacements =
            new Dictionary<string, string>
        {
            { "{colorHighlight}", ColourHighlight }
        };

        // Numeric values stay culture-independent; only their final presentation uses LocaleNumbers.
        public static string FormatNumber(double x, bool hideDecimal = false, float fontWeight = 400f,
            bool useMspace = false, float mspaceSize = 0.5f)
        {
            if (!LocaleNumbers.IsFinite(x)) return LocaleNumbers.Format(x);
            var absX = Math.Abs(x);
            string number;
            string suffix = "";
            if (Notation == NumberTypes.Scientific && absX >= 1000)
                number = ScientificTruncated(x);
            else if (Notation == NumberTypes.Engineering && absX >= 1000)
            {
                var group = Math.Max(0, (int)Math.Floor(Math.Log10(absX) / 3));
                var mantissa = x / Math.Pow(10, group * 3);
                var integerDigits = Math.Abs(mantissa) < 1 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(mantissa))) + 1;
            var digits = Math.Max(0, 3 - integerDigits);
                number = LocaleNumbers.Format(Truncate(mantissa, digits), "F" + digits) + "e" +
                    (group * 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (Notation != NumberTypes.Scientific && Notation != NumberTypes.Engineering)
            {
                if (!LocaleNumbers.CompactParts(x, hideDecimal, out number, out suffix))
                    number = ScientificTruncated(x);
            }
            else
            {
                var digits = hideDecimal ? 0 : absX < 10 ? 2 : absX < 100 ? 1 : 0;
                number = LocaleNumbers.Format(Truncate(x, digits), "F" + digits);
            }
            var formatted = Monospace(number, useMspace, mspaceSize);
            return suffix.Length == 0 ? formatted : formatted + "<font-weight=" +
                fontWeight.ToString(System.Globalization.CultureInfo.InvariantCulture) + ">" + suffix + "</font-weight>";
        }

        private static double Truncate(double value, int decimalPlaces)
        {
            var factor = Math.Pow(10, decimalPlaces);
            return Math.Truncate(value * factor) / factor;
        }
        private static string ScientificTruncated(double value)
        {
            if (value == 0) return LocaleNumbers.Format(0, "F2");
            var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            // Division by 10^exponent can overflow/underflow for subnormal values. The framework's
            // scientific conversion remains safe across the entire finite double range.
            var invariant = value.ToString("E16", System.Globalization.CultureInfo.InvariantCulture);
            var split = invariant.IndexOf('E');
            var mantissa = double.Parse(invariant.Substring(0, split), System.Globalization.CultureInfo.InvariantCulture);
            exponent = int.Parse(invariant.Substring(split + 1), System.Globalization.CultureInfo.InvariantCulture);
            return LocaleNumbers.Format(Truncate(mantissa, 2), "F2") + "e" +
                exponent.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        private static string Monospace(string value, bool enabled, float size)
        {
            if (!enabled) return value;
            var start = "<mspace=" + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "em>";
            const string end = "</mspace>";
            var decimalSeparator = LocaleNumbers.Culture.NumberFormat.NumberDecimalSeparator;
            return start + value.Replace(decimalSeparator, end + decimalSeparator + start) + end;
        }

        public static string FormatEnergy(double x, bool type)
        {
            if (!LocaleNumbers.IsFinite(x)) return LocaleNumbers.Format(x);
            if (x == 0) return LocaleNumbers.Format(0, "F2") + (type ? "J" : "W");
            var group = Math.Max(0, (int)Math.Floor(Math.Log10(Math.Abs(x)) / 3));
            var prefixes = type ? EnergyPrefixJ : EnergyPrefixW;
            if (group >= prefixes.Length) return ScientificTruncated(x) + (type ? "J" : "W");
            var mantissa = x / Math.Pow(10, group * 3);
            var integerDigits = Math.Abs(mantissa) < 1 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(mantissa))) + 1;
            var digits = Math.Max(0, 3 - integerDigits);
            return LocaleNumbers.Format(Truncate(mantissa, digits), "F" + digits) + prefixes[group];
        }

        public static string FormatUnits(int units, bool mspace = true, float mspaceSize = 0.6f, bool isMs = false)
            => Monospace(units.ToString(isMs ? "D2" : "0", LocaleNumbers.Culture), mspace, mspaceSize);

        public static string FormatTime(double time, bool showDecimal = false,
            bool mspace = true, float mspaceSize = 0.6f, bool shortForm = true,
            bool absolutevalue = true, string colourOverride = "")
        {
            if (!LocaleNumbers.IsFinite(time)) return $"{ColourRed}{LocaleNumbers.Format(time)}{EndColour}";
            if (time >= TimeSpan.MaxValue.TotalSeconds || time <= -TimeSpan.MaxValue.TotalSeconds)
                return $"{ColourRed}∞{EndColour}";
            var span = TimeSpan.FromSeconds(Math.Abs(time));
            var builder = new System.Text.StringBuilder();
            if (!absolutevalue && time < 0) builder.Append('-');
            void AppendUnit(int value, int unit, bool required = false)
            {
                if (value == 0 && !required) return;
                if (builder.Length > 0 && builder[builder.Length - 1] != '-') builder.Append(' ');
                string numeric = LocaleNumbers.Integer(value, false);
                if (unit == 3 && showDecimal && span.Milliseconds != 0)
                    numeric += LocaleNumbers.Culture.NumberFormat.NumberDecimalSeparator +
                        (span.Milliseconds / 10).ToString("D2", LocaleNumbers.Culture);
                builder.Append(Monospace(numeric, mspace, mspaceSize));
                builder.Append(colourOverride).Append(LocaleNumbers.TimeUnit(unit, unit == 3 && showDecimal ? value + (span.Milliseconds / 10) / 100.0 : value, shortForm));
                if (!string.IsNullOrEmpty(colourOverride)) builder.Append(EndColour);
            }
            AppendUnit(span.Days, 0);
            AppendUnit(span.Hours, 1);
            AppendUnit(span.Minutes, 2);
            AppendUnit(span.Seconds, 3, true);
            return builder.ToString();
        }

        public static string FormatProduction(double production, bool invert = false)
        {
            if (TimeScale == 0) return $"{ColourRed}{LocaleNumbers.Unavailable}{EndColour}";
            return FormatNumber(invert ? (UseScaledTimeForValues ? production : production / TimeScale) :
                (UseScaledTimeForValues ? production : production * TimeScale));
        }

        public static string FormatTimeRemaining(double timeRemaining, bool showDecimal = false, bool shortForm = true,
            bool na = true)
        {
            if (TimeScale == 0) return $"{ColourRed}{(na ? LocaleNumbers.Unavailable : "∞")}{EndColour}";
            return FormatTime(UseScaledTimeForValues ? timeRemaining : timeRemaining / TimeScale, showDecimal,
                shortForm: shortForm);
        }

        public static string Plural(this string str, double num)
        {
            return str + (num == 1 ? "" : "s");
        }


        public static string Scramble(this string s)
        {
            return new string(s.ToCharArray().OrderBy(x => Guid.NewGuid()).ToArray());
        }

        public static string ReplacePlaceholders(string input)
        {
            foreach (var pair in replacements) input = input.Replace(pair.Key, pair.Value);
            return input;
        }

        #region BuyCalcs

        public static double BuyXCost(
            double numberToBuy,
            double baseCost,
            double exponent,
            double currentLevel,
            double costMultiplier = 1f
        )
        {
            // Geometric series *plus* the multiplier
            var cost = costMultiplier
                       * baseCost
                       * Math.Pow(exponent, currentLevel)
                       * ((Math.Pow(exponent, numberToBuy) - 1) / (exponent - 1));

            return cost;
        }


        public static double BuyMaxCost(double currencyOwned, double baseCost, double exponent, double currentLevel)
        {
            var n = Math.Floor(Math.Log(
                currencyOwned * (exponent - 1f) / (baseCost * Math.Pow(exponent, currentLevel)) + 1,
                exponent));
            return BuyXCost(n, baseCost, exponent, currentLevel);
        }

        public static int MaxAffordable(
            double currencyOwned,
            double baseCost,
            double exponent,
            double currentLevel,
            double costMultiplier = 1
        )
        {
            // The same formula, except we multiply baseCost by `costMultiplier`.
            // currencyOwned >= costMultiplier * baseCost * exponent^currentLevel * [ (exponent^n - 1) / (exponent - 1) ]
            // Solve for n:
            var n = Math.Floor(Math.Log(
                currencyOwned * (exponent - 1)
                / (costMultiplier * baseCost * Math.Pow(exponent, currentLevel))
                + 1,
                exponent
            ));

            // Because we used Floor, n can be negative if the user canâ€™t afford even 1.
            // Typically, we clamp it at 0 or return 0 if the value is negative.
            if (n < 0) n = 0;

            return (int)n;
        }


        public static double TriangleNumber(double n)
        {
            return n * (n + 1) / 2;
        }

        public static (int, double) RecalculateLevel(double xpForFirstLevel, double exponent, double totalXp)
        {
            if (xpForFirstLevel == 0 || exponent == 0)
            {
                Debug.Log("lolNope");
                return (1, 0);
            }

            var lvl = 1;

            var xpToLevel = CostToLevel(xpForFirstLevel, lvl, exponent);
            while (totalXp > xpToLevel)
            {
                if (totalXp >= xpToLevel)
                {
                    totalXp -= xpToLevel;
                    lvl++;
                }

                xpToLevel = CostToLevel(xpForFirstLevel, lvl, exponent);
            }

            if (totalXp >= xpToLevel)
            {
                totalXp -= xpToLevel;
                lvl++;
            }

            return (lvl, totalXp);
        }

        private static double CostToLevel(double xpForFirstlevel, double lvl, double exponent)
        {
            return Math.Floor(BuyXCost(1, xpForFirstlevel * lvl, exponent, lvl - 1));
        }

        #endregion
    }
}
