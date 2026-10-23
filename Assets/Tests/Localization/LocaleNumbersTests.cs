#if UNITY_INCLUDE_TESTS
using System;
using Blindsided.Utilities;
using NUnit.Framework;

namespace TimelessEchoes.Tests.Localization
{
    public class LocaleNumbersTests
    {
        [TestCase("en", "12,345.5", "12.5%")]
        [TestCase("de", "12.345,5", "12,5\u00a0%")]
        [TestCase("es-419", "12,345.5", "12.5\u00a0%")]
        [TestCase("fr", "12\u202f345,5", "12,5\u00a0%")]
        [TestCase("pt-BR", "12.345,5", "12,5%")]
        [TestCase("ru", "12\u00a0345,5", "12,5\u00a0%")]
        [TestCase("ja", "12,345.5", "12.5%")]
        [TestCase("zh-CN", "12,345.5", "12.5%")]
        public void NumbersAndPercentagesFollowSelectedCulture(string code, string number, string percent)
        {
            Assert.That(LocaleNumbers.Format(12345.5, "N1", code), Is.EqualTo(number));
            Assert.That(LocaleNumbers.PercentPoints(12.5, code: code), Is.EqualTo(percent));
        }

        [TestCase("de", "12,5", 12.5)]
        [TestCase("fr", "12,5", 12.5)]
        [TestCase("en", "12.5", 12.5)]
        [TestCase("es-419", "12.5", 12.5)]
        public void QuantityInputUsesDisplayedDecimalSeparator(string code, string text, double expected)
        {
            Assert.That(LocaleNumbers.TryInput(text, out var value, code), Is.True);
            Assert.That(value, Is.EqualTo(expected));
        }

        [TestCase("de", "1.000")]
        [TestCase("en", "1,000")]
        [TestCase("ru", "1\u00a0000")]
        [TestCase("en", "1e300")]
        [TestCase("en", "Infinity")]
        [TestCase("en", "NaN")]
        public void QuantityInputRejectsAmbiguousOrNonFiniteValues(string code, string text)
            => Assert.That(LocaleNumbers.TryInput(text, out _, code), Is.False);

        [TestCase("de", 1000d, "1.000", "")]
        [TestCase("ja", 10000d, "1.00", "万")]
        [TestCase("zh-CN", 100000000d, "1.00", "亿")]
        [TestCase("es-419", 1000000000d, "1000", "\u00a0M")]
        public void CompactNotationRespectsLocalMagnitude(string code, double value, string number, string suffix)
        {
            Assert.That(LocaleNumbers.CompactParts(value, false, out var actual, out var unit, code), Is.True);
            Assert.That(actual, Is.EqualTo(number));
            Assert.That(unit, Is.EqualTo(suffix));
        }

        [Test]
        public void UnsupportedCompactMagnitudeRequestsScientificFallback()
        {
            Assert.That(LocaleNumbers.CompactParts(1e30, false, out _, out _, "zh-CN"), Is.False);
            Assert.That(LocaleNumbers.Format(double.NaN), Is.EqualTo("—"));
            Assert.That(LocaleNumbers.Format(double.PositiveInfinity), Is.EqualTo("∞"));
        }

        [Test]
        public void DurationColourMarkupOnlyClosesExplicitOverrides()
        {
            var plain = CalcUtils.FormatTime(61, mspace: false);
            Assert.That(plain, Does.Not.Contain("<color"));
            Assert.That(plain, Does.Not.Contain("</color>"));
            var coloured = CalcUtils.FormatTime(61, mspace: false, colourOverride: "<color=#abcdef>");
            Assert.That(System.Text.RegularExpressions.Regex.Matches(coloured, "<color=").Count, Is.EqualTo(2));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(coloured, "</color>").Count, Is.EqualTo(2));
        }

        [TestCase(1, " секунда")]
        [TestCase(2, " секунды")]
        [TestCase(5, " секунд")]
        [TestCase(11, " секунд")]
        [TestCase(21, " секунда")]
        [TestCase(1.5, " секунды")]
        public void RussianDurationUsesCorrectPlural(double count, string expected)
            => Assert.That(LocaleNumbers.TimeUnit(3, count, false, "ru"), Is.EqualTo(expected));
    }
}
#endif
