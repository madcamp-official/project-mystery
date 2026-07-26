using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wake.UI
{
    public sealed class DialogueChoiceButtonSet
    {
        public DialogueChoiceButtonSet(Button[] buttons, TMP_Text[] labels)
        {
            Buttons = buttons;
            Labels = labels;
        }

        public Button[] Buttons { get; }
        public TMP_Text[] Labels { get; }
    }

    public static class DialogueChoiceButtonPool
    {
        public static DialogueChoiceButtonSet EnsureCapacity(
            Transform container,
            IReadOnlyList<string> existingNames,
            int capacity)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            if (existingNames == null || existingNames.Count == 0)
                throw new ArgumentException(
                    "At least one existing button name is required.",
                    nameof(existingNames));
            if (capacity < existingNames.Count)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be smaller than the authored button count.");

            var buttons = new List<Button>(capacity);
            foreach (string objectName in existingNames)
            {
                Transform child = container.Find(objectName);
                Button button = child != null
                    ? child.GetComponent<Button>()
                    : null;
                if (button == null)
                    throw new InvalidOperationException(
                        $"Choice button '{objectName}' is missing.");
                buttons.Add(button);
            }

            Button template = buttons[buttons.Count - 1];
            while (buttons.Count < capacity)
            {
                int index = buttons.Count;
                Button clone = UnityEngine.Object.Instantiate(
                    template,
                    container,
                    false);
                clone.name = $"Choice ({index})";
                clone.onClick.RemoveAllListeners();
                clone.gameObject.SetActive(false);
                clone.transform.SetSiblingIndex(index);
                buttons.Add(clone);
            }

            TMP_Text[] labels = buttons
                .Select(button => button.GetComponentInChildren<TMP_Text>(true))
                .ToArray();
            if (labels.Any(label => label == null))
                throw new InvalidOperationException(
                    "Every choice button must contain a TMP_Text label.");
            return new DialogueChoiceButtonSet(buttons.ToArray(), labels);
        }
    }
}
