using System;

namespace TimelessEchoes.MapGeneration
{
    public static class RunDistanceRules
    {
        public static double EffectiveCap(double cap, double multiplier, double flat) => Math.Max(0, cap * multiplier + flat);
        public static double ReapGain(double cap, double multiplier, double flat) => EffectiveCap(cap, multiplier, flat) * .01;
    }
}
