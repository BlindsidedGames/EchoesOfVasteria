using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Blindsided.Utilities
{
    public class MathMaker9000 : MonoBehaviour
    {
        public double nanites;
        public double envelopers;
        public double divisor = 1;

        public double exponent;
        public double result;

        private void Update()
        {
            exponent = Math.Log10(1 + envelopers) / divisor;
            result = nanites * (1 + exponent);
        }
    }
}