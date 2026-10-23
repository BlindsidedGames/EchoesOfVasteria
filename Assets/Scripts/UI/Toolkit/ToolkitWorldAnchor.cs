using System;
using System.Collections.Generic;
using System.Globalization;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    // Authored world UI data. No Canvas, Graphic, TMP renderer or runtime hierarchy conversion.
    [DisallowMultipleComponent]
    public sealed class ToolkitWorldAnchor : MonoBehaviour
    {
        public enum Value { Static, HealthFill, TaskFill, EchoFill, EnemyTitle, EnemyHealth }
        [Serializable] public sealed class Element
        {
            public RectTransform anchor, rowGroup;
            public Sprite sprite;
            public Color color = Color.white;
            public string text;
            public float fontSize;
            public bool sliced;
            // Canvas reference PPU / Image PPU multiplier, captured at authoring time.
            // Zero preserves the existing presentation of world UI not migrated yet.
            public float sliceReferencePixelsPerUnit;
            public Value value;
            public bool enabled = true;
        }
        public Element[] elements = Array.Empty<Element>();
        public HealthBase health;
        public Enemy enemy;
        public ContinuousTask task;
        public EchoController echo;
        private string healthText, healthNegativeSign;
        private int displayedHealth, displayedMaxHealth;
        public static readonly HashSet<ToolkitWorldAnchor> Active = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();
        private void OnEnable()
        {
            if (!health) health = GetComponentInParent<HealthBase>();
            if (!enemy) enemy = GetComponentInParent<Enemy>();
            if (!task) task = GetComponentInParent<ContinuousTask>();
            if (!echo) echo = GetComponentInParent<EchoController>();
            Active.Add(this);
        }
        private void OnDisable() => Active.Remove(this);
        public float Fill(Value value) => value switch
        {
            Value.HealthFill => health && health.MaxHealth > 0 ? Mathf.Max(.05f, health.CurrentHealth / health.MaxHealth) : 1,
            Value.TaskFill => task ? task.ProgressRemaining : 1,
            Value.EchoFill => echo ? echo.LifetimeRemainingFraction : 1,
            _ => 1
        };
        public string Text(Element e)
        {
            if (e.value == Value.EnemyTitle && enemy) return enemy.DisplayTitle;
            if (e.value == Value.EnemyHealth && health)
            {
                var current = health.CurrentHealth > 0 ? Mathf.Max(1, Mathf.FloorToInt(health.CurrentHealth)) : 0;
                var maximum = Mathf.FloorToInt(health.MaxHealth);
                // Visible anchors refresh every frame. Only format when the displayed integers
                // change, including after pooled enemies are healed or initialized again.
                var negativeSign = Blindsided.Utilities.LocaleNumbers.Code;
                if (healthText == null || displayedHealth != current || displayedMaxHealth != maximum || healthNegativeSign != negativeSign)
                {
                    displayedHealth = current; displayedMaxHealth = maximum; healthNegativeSign = negativeSign;
                    healthText = $"{Blindsided.Utilities.LocaleNumbers.Integer(current)} / {Blindsided.Utilities.LocaleNumbers.Integer(maximum)}";
                }
                return healthText;
            }
            return e.text;
        }
    }
}
