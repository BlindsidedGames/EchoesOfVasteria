using System;
using System.Collections.Generic;
using TimelessEchoes.Tilemaps;
using UnityEditor;
using UnityEngine;
// Make sure this namespace is correct

[CustomEditor(typeof(WeightedRuleTile))]
public class WeightedRuleTileEditor : RuleTileEditor
{
    private readonly Dictionary<object, bool> _foldoutStates = new();

    public override void OnInspectorGUI()
    {
        var weightedTile = (WeightedRuleTile)target;

        // --- UPGRADE BUTTON ---
        EditorGUILayout.HelpBox(
            "If weight fields are not showing up for 'Random' rules, try clicking this button to upgrade them.",
            MessageType.Info);
        if (GUILayout.Button("Attempt to Upgrade Rules"))
            // When the button is clicked, we manually convert the rules.
            UpgradeRules(weightedTile);
        EditorGUILayout.Space();

        // Draw the default Rule Tile editor
        base.OnInspectorGUI();

        EditorGUILayout.Space(15);

        var foundRandomRule = false;

        for (var i = 0; i < weightedTile.m_TilingRules.Count; i++)
        {
            var rule = weightedTile.m_TilingRules[i];

            if (rule is WeightedTilingRule wRule && wRule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Random)
            {
                if (!foundRandomRule)
                {
                    EditorGUILayout.LabelField("Sprite Weight Configuration", EditorStyles.boldLabel);
                    foundRandomRule = true;
                }

                if (wRule.m_SpriteWeights == null || wRule.m_SpriteWeights.Length != wRule.m_Sprites.Length)
                    Array.Resize(ref wRule.m_SpriteWeights, wRule.m_Sprites.Length);

                if (!_foldoutStates.ContainsKey(rule)) _foldoutStates.Add(rule, false);

                _foldoutStates[rule] = EditorGUILayout.Foldout(_foldoutStates[rule], $"Weights for Rule {i + 1}", true);

                if (_foldoutStates[rule])
                {
                    EditorGUI.indentLevel++;
                    if (wRule.m_Sprites.Length > 0)
                        for (var j = 0; j < wRule.m_SpriteWeights.Length; j++)
                        {
                            if (wRule.m_SpriteWeights[j] == 0) wRule.m_SpriteWeights[j] = 1.0f;
                            wRule.m_SpriteWeights[j] =
                                EditorGUILayout.FloatField($"Sprite {j + 1} Weight", wRule.m_SpriteWeights[j]);
                        }
                    else
                        EditorGUILayout.LabelField("Add sprites to this rule to set their weights.");

                    EditorGUI.indentLevel--;
                }
            }
        }

        if (GUI.changed) EditorUtility.SetDirty(target);
    }

    private void UpgradeRules(WeightedRuleTile tile)
    {
        var rulesUpgraded = false;
        for (var i = 0; i < tile.m_TilingRules.Count; i++)
        {
            var currentRule = tile.m_TilingRules[i];

            // Check if the rule is a standard one that needs upgrading
            if (currentRule.GetType() == typeof(RuleTile.TilingRule))
            {
                // Create a new WeightedTilingRule
                var newRule = new WeightedTilingRule();

                // Manually copy all the properties from the old rule to the new one
                newRule.m_Sprites = currentRule.m_Sprites;
                newRule.m_GameObject = currentRule.m_GameObject;
                newRule.m_ColliderType = currentRule.m_ColliderType;
                newRule.m_Output = currentRule.m_Output;
                newRule.m_Neighbors = currentRule.m_Neighbors;
                newRule.m_RuleTransform = currentRule.m_RuleTransform;

                // Initialize the weights array
                newRule.m_SpriteWeights = new float[newRule.m_Sprites.Length];

                // Replace the old rule with the new one in the list
                tile.m_TilingRules[i] = newRule;
                rulesUpgraded = true;

                Debug.Log($"Rule #{i} has been successfully upgraded to a WeightedTilingRule.");
            }
        }

        if (rulesUpgraded)
        {
            EditorUtility.SetDirty(tile); // Mark the asset as changed so it saves
            Debug.Log("Upgrade complete. Please re-select the asset if the UI hasn't updated.");
        }
        else
        {
            Debug.Log("No rules needed upgrading.");
        }
    }
}