using System;
using System.Collections.Generic;
using System.Globalization;
using References.UI;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.Oracle;

namespace TimelessEchoes.UI.Cauldron
{
    /// <summary>Runtime adapter for the retained uGUI route; never invokes paired/full-stack mixing.</summary>
    public sealed class LegacyCauldronConversion
    {
        private readonly List<CauldronMixItemUIReferences> slots;
        private readonly Button add;
        private readonly TMP_Text preview;
        private readonly TMP_InputField amount;
        private readonly Button minus, plus, max;
        private List<Resource> foods;
        private Resource selected;
        private long sequence;
        private bool adding;
        private string conversionError;

        public LegacyCauldronConversion(List<CauldronMixItemUIReferences> slots,
            CauldronMixItemUIReferences first, CauldronMixItemUIReferences second,
            Button add, Button oldMixAll, TMP_Text preview, Image arrow)
        {
            this.slots = slots; this.add = add; this.preview = preview;
            if (first) first.gameObject.SetActive(false);
            if (second) second.gameObject.SetActive(false);
            if (arrow) arrow.gameObject.SetActive(false);
            if (oldMixAll) oldMixAll.gameObject.SetActive(false);
            if (!add) return;
            add.onClick.RemoveAllListeners(); add.onClick.AddListener(Add);
            var label = add.GetComponentInChildren<TMP_Text>(true);
            if (label) { label.text = "Add to Cauldron"; FitLabel(label); }
            var anchor = (RectTransform)add.transform;
            var column = anchor.parent.parent ? anchor.parent.parent : anchor.parent;
            var oldButtons = anchor.parent;
            var addLayout = add.GetComponent<LayoutElement>() ?? add.gameObject.AddComponent<LayoutElement>();
            addLayout.minWidth = 120; addLayout.preferredWidth = 130; addLayout.flexibleWidth = 1; addLayout.preferredHeight = 18;
            var buttonsLayout = oldButtons.GetComponent<HorizontalLayoutGroup>();
            if (buttonsLayout) { buttonsLayout.childControlWidth = true; buttonsLayout.childForceExpandWidth = true; }
            Transform DirectChild(Transform child) { while (child.parent && child.parent != column) child = child.parent; return child; }
            if (first) { var child=DirectChild(first.transform); if(child.parent==column&&child!=oldButtons)child.gameObject.SetActive(false); }
            if (second) { var child=DirectChild(second.transform); if(child.parent==column&&child!=oldButtons)child.gameObject.SetActive(false); }
            if (preview)
            {
                preview.transform.SetParent(column,false);preview.transform.SetSiblingIndex(oldButtons.GetSiblingIndex());
                var sizing=preview.GetComponent<LayoutElement>()??preview.gameObject.AddComponent<LayoutElement>();sizing.preferredHeight=22;sizing.minHeight=22;
                preview.fontSize=8;preview.enableAutoSizing=false;preview.alignment=TextAlignmentOptions.Left;
            }
            var row = new GameObject("Food amount", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)row.transform; rect.SetParent(column, false);
            rect.SetSiblingIndex(oldButtons.GetSiblingIndex());
            var rowSizing=row.AddComponent<LayoutElement>();rowSizing.preferredHeight=18;rowSizing.minHeight=18;
            rect.anchorMin = anchor.anchorMin; rect.anchorMax = anchor.anchorMax; rect.pivot = anchor.pivot;
            rect.sizeDelta = new Vector2(130, 18);
            rect.anchoredPosition = anchor.anchoredPosition + new Vector2(0, anchor.rect.height + 6);
            var layout = row.GetComponent<HorizontalLayoutGroup>(); layout.spacing = 3;
            layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlHeight = true;
            layout.childForceExpandHeight = true; layout.childControlWidth = true; layout.childForceExpandWidth = false;
            minus = CloneButton(add, row.transform, "−", 18, () => Step(-1));
            var input = new GameObject("Amount", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            input.transform.SetParent(row.transform, false);
            input.GetComponent<Image>().color = new Color32(56,46,49,255);
            input.GetComponent<LayoutElement>().flexibleWidth = 1; input.GetComponent<LayoutElement>().minWidth = 35;
            amount = input.GetComponent<TMP_InputField>();
            var textObject = new GameObject("Input text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(input.transform, false);
            var textRect = (RectTransform)textObject.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(3,2); textRect.offsetMax = new Vector2(-3,-2);
            var text = textObject.GetComponent<TextMeshProUGUI>(); text.font = label ? label.font : TMP_Settings.defaultFontAsset;
            text.fontSize = label ? label.fontSize : 8; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(245,228,214,255); text.raycastTarget = false;
            amount.textViewport = (RectTransform)input.transform; amount.textComponent = text;
            amount.text = "0"; amount.lineType = TMP_InputField.LineType.SingleLine;
            amount.onValueChanged.AddListener(_ => { conversionError = null; RefreshControls(); });
            plus = CloneButton(add, row.transform, "+", 18, () => Step(1));
            max = CloneButton(add, row.transform, "Max", 34, () => SetAmount(selected ? ResourceManager.Instance.GetAmount(selected) : 0));
            if (slots != null && slots.Count > 0 && slots[0])
            {
                var content = (RectTransform)slots[0].transform.parent;
                if (content.parent == column)
                {
                    var scrollObject = new GameObject("Food scroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect),typeof(LayoutElement));
                    scrollObject.transform.SetParent(column,false);scrollObject.transform.SetSiblingIndex(content.GetSiblingIndex());
                    scrollObject.GetComponent<Image>().color=Color.clear;
                    var sizing=scrollObject.GetComponent<LayoutElement>();sizing.minHeight=80;sizing.preferredHeight=100;sizing.flexibleHeight=1;
                    var viewport=(RectTransform)scrollObject.transform;content.SetParent(viewport,false);
                    content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
                    var fitter=content.GetComponent<ContentSizeFitter>()??content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                    var scroll=scrollObject.GetComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=20;
                }
            }
            foreach(var textLabel in column.GetComponentsInChildren<TMP_Text>(true))
                if(textLabel.text.Contains("Pick two resources") || textLabel.text.Contains("entire stack"))textLabel.text="Choose a food and amount. Add to Cauldron converts that amount.";
        }

        private static Button CloneButton(Button template, Transform parent, string title, float width, UnityEngine.Events.UnityAction action)
        {
            var button = UnityEngine.Object.Instantiate(template, parent);
            button.name = title; button.onClick.RemoveAllListeners(); button.onClick.AddListener(action);
            var label = button.GetComponentInChildren<TMP_Text>(true); if (label) { label.text = title; FitLabel(label); }
            var layout = button.gameObject.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width; layout.minWidth = width; layout.flexibleWidth = 0;
            return button;
        }
        private static void FitLabel(TMP_Text label)
        {
            label.enableAutoSizing=false;label.fontSize=8;label.alignment=TextAlignmentOptions.Center;label.margin=Vector4.zero;label.textWrappingMode=TextWrappingModes.NoWrap;
            var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(3,0);rect.offsetMax=new Vector2(-3,0);
        }
        private bool TryAmount(out double value)
        {
            value = 0;
            return amount && double.TryParse(amount.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && CauldronConversion.IsFinite(value);
        }
        private void SetAmount(double value) { if (amount) amount.text = value.ToString("R", CultureInfo.InvariantCulture); }
        private void Step(int delta)
        {
            if (!selected) return;
            if (!TryAmount(out var value)) value = 0;
            SetAmount(Math.Max(0, Math.Min(ResourceManager.Instance.GetAmount(selected), value + delta)));
        }
        private void Select(Resource resource)
        {
            if (adding) return;
            selected = resource;
            sequence = oracle?.saveData != null && oracle.saveData.CauldronConversionSequence < long.MaxValue ? oracle.saveData.CauldronConversionSequence + 1 : 0;
            SetAmount(Math.Min(1, ResourceManager.Instance.GetAmount(resource))); Refresh();
        }
        public void Add()
        {
            if (adding || !selected || !TryAmount(out var quantity)) return;
            adding = true; RefreshControls();
            try
            {
                if (CauldronManager.Instance.TryAddToCauldron(selected, quantity, sequence, out _, out var error))
                { selected = null; SetAmount(0); }
                else conversionError = error;
            }
            finally { adding = false; Refresh(); }
        }
        public void Clear() { selected = null; conversionError = null; SetAmount(0); foods = null; }
        public void Refresh()
        {
            var resources = ResourceManager.Instance; if (!resources || slots == null) return;
            if (selected && resources.GetAmount(selected) <= 0) selected = null;
            if (foods == null || !selected) foods = CauldronMixingPresentation.BuildDisplayFoods(resources);
            while (slots.Count > 0 && slots.Count < foods.Count)
            {
                var copy = UnityEngine.Object.Instantiate(slots[0], slots[0].transform.parent);
                if (!copy.GetComponentInParent<LayoutGroup>())
                {
                    var rect = (RectTransform)copy.transform; var start = (RectTransform)slots[0].transform;
                    var strideX = slots.Count > 1 ? ((RectTransform)slots[1].transform).anchoredPosition.x - start.anchoredPosition.x : start.rect.width;
                    var strideY = slots.Count > 4 ? ((RectTransform)slots[4].transform).anchoredPosition.y - start.anchoredPosition.y : -start.rect.height;
                    rect.anchoredPosition = start.anchoredPosition + new Vector2(slots.Count % 4 * strideX, slots.Count / 4 * strideY);
                }
                slots.Add(copy);
            }
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i]; var food = i < foods.Count ? foods[i] : null;
                if (!slot) continue; slot.gameObject.SetActive(food);
                if (!food) continue;
                var known = resources.IsUnlocked(food); var stock = known ? resources.GetAmount(food) : 0;
                if (slot.iconImage) { slot.iconImage.enabled = true; slot.iconImage.sprite = known ? food.icon : food.UnknownIcon; }
                if (slot.countText) slot.countText.text = known ? CalcUtils.FormatNumber(stock, true) : "???";
                if (slot.selectionImageGreen) slot.selectionImageGreen.enabled = food == selected;
                if (slot.selectionImageWhite) slot.selectionImageWhite.enabled = false;
                if (slot.selectButton)
                { slot.selectButton.onClick.RemoveAllListeners(); slot.selectButton.interactable = known && stock > 0 && !adding; var captured = food; slot.selectButton.onClick.AddListener(() => Select(captured)); }
            }
            RefreshControls();
        }
        private void RefreshControls()
        {
            if (!add || !amount) return;
            var resources = ResourceManager.Instance; var manager = CauldronManager.Instance;
            var stock = selected && resources ? resources.GetAmount(selected) : 0;
            var valid = TryAmount(out var quantity) && selected && manager && manager.CanAddToCauldron(selected, quantity);
            add.interactable = valid && !adding; amount.interactable = selected && !adding;
            if (minus) minus.interactable = selected && quantity > 0 && !adding;
            if (plus) plus.interactable = selected && quantity < stock && !adding;
            if (max) max.interactable = selected && stock > 0 && !adding;
            if (preview) preview.text = !string.IsNullOrEmpty(conversionError) ? conversionError : selected ? selected.name + " · +" + (valid ? (quantity * CauldronConversion.UnitValue(selected)).ToString("G15", CultureInfo.InvariantCulture) : "0") + " stew" : "Choose a food";
        }
    }
}
