using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Voidex.DataLabs.Dashboard
{
    public class SearchableDropdown : PopupWindowContent
    {
        private string search = string.Empty;
        private Vector2 scroll;
        private DropdownField dropdownField;
        
        public Action<string> OnValueChanged;

        public SearchableDropdown(DropdownField dropdownField)
        {
            this.dropdownField = dropdownField;
            this.dropdownField.RegisterValueChangedCallback(evt =>
            {
                OnValueChanged?.Invoke(evt.newValue);
                editorWindow.Close();
            });
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(400, 300);
        }

        public override void OnGUI(Rect rect)
        {
            search = EditorGUILayout.TextField("", search, "SearchTextField");
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (!string.IsNullOrEmpty(search))
            {
                foreach (var choice in dropdownField.choices)
                {
                    if (choice.ToLower().Contains(search.ToLower()))
                    {
                        if (GUILayout.Button(choice, "PR Label"))
                        {
                            dropdownField.value = choice;
                            editorWindow.Close();
                        }
                    }
                }
            }
            else
            {
                foreach (var choice in dropdownField.choices)
                {
                    if (GUILayout.Button(choice, "PR Label"))
                    {
                        dropdownField.value = choice;
                        editorWindow.Close();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}