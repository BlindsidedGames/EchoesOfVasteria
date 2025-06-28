using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Controls a group of buttons and their corresponding text objects.
    ///     When a button is clicked, it becomes non-interactable and shows its
    ///     disabled text while the others become interactable and show their
    ///     enabled text.
    /// </summary>
    public class ButtonToggleGroup : MonoBehaviour
    {
        [SerializeField] private List<Button> buttons = new();
        [SerializeField] private List<GameObject> enabledTexts = new();
        [SerializeField] private List<GameObject> disabledTexts = new();

        private readonly List<UnityAction> listeners = new();

        private void Awake()
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                int index = i;
                UnityAction action = () => OnButtonClicked(index);
                listeners.Add(action);
                if (buttons[i] != null)
                    buttons[i].onClick.AddListener(action);
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < buttons.Count && i < listeners.Count; i++)
                if (buttons[i] != null)
                    buttons[i].onClick.RemoveListener(listeners[i]);
        }

        private void OnButtonClicked(int index)
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                bool selected = i == index;
                if (buttons[i] != null)
                    buttons[i].interactable = !selected;

                if (i < enabledTexts.Count && enabledTexts[i] != null)
                    enabledTexts[i].SetActive(!selected);

                if (i < disabledTexts.Count && disabledTexts[i] != null)
                    disabledTexts[i].SetActive(selected);
            }
        }
    }
}
