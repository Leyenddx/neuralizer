using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(TMP_Dropdown))]
[AddComponentMenu("Localization/Localize Dropdown")]
public class LocalizeDropdown : MonoBehaviour {

    public List<LocalizedString> options;

    private List<LocalizedString.ChangeHandler> handlers = new List<LocalizedString.ChangeHandler>();
    private TMP_Dropdown Dropdown;

    private void Awake() {
        Dropdown = GetComponent<TMP_Dropdown>();
        for(int i = 0; i < options.Count; i++) {
            if(i < Dropdown.options.Count) {
                Dropdown.options[i].text = "";
                Dropdown.options[i].image = null;
                Dropdown.options[i].color = Color.white;
            } else {
                Dropdown.options.Add(new TMP_Dropdown.OptionData("", null, Color.white));
            }
        }
        while(Dropdown.options.Count > options.Count) {
            Dropdown.options.RemoveAt(Dropdown.options.Count - 1);
        }
    }

    private void OnEnable() {
        handlers.Clear();
        for(int i = 0; i < options.Count; i++) {
            int j = i;
            LocalizedString.ChangeHandler handler = (value) => {
                Dropdown.options[j].text = value;
                if(Dropdown.value.Equals(j)) {
                    if(Dropdown.captionText) {
                        Dropdown.captionText.text = value;
                    }
                }
            };
            options[i].StringChanged += handler;
            handlers.Add(handler);
        }
    }

    private void OnDisable() {
        for(int i = 0; i < options.Count; i++) {
            try {
                options[i].StringChanged -= handlers[i];
            } finally { }
        }
        handlers.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if(!Dropdown) {
            Dropdown = GetComponent<TMP_Dropdown>();
        }
    }
#endif
}