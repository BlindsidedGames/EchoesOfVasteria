using System;
using System.Collections.Generic;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.UI.Toolkit
{
    // Authored world UI data. No Canvas, Graphic, TMP renderer or runtime hierarchy conversion.
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
            public Value value;
            public bool enabled = true;
        }
        public Element[] elements = Array.Empty<Element>();
        public HealthBase health;
        public Enemy enemy;
        public ContinuousTask task;
        public EchoController echo;
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
                return $"{(health.CurrentHealth > 0 ? Mathf.Max(1, Mathf.FloorToInt(health.CurrentHealth)) : 0)} / {Mathf.FloorToInt(health.MaxHealth)}";
            return e.text;
        }
    }
}
