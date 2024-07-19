using UnityEngine;
using UnityEditor;

namespace UltimateReplay
{
    [CustomPropertyDrawer(typeof(ReplayIdentity))]
    public class ReplayIdentityPropertyDrawer : PropertyDrawer
    {
        // Private
        private bool isEditingMode = false;
        private string editText = "";

        // Public
        public const int buttonWidth = 24;

        // Methods
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Display the main label
            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            
            // Find the ID property
            SerializedProperty idProperty = property.FindPropertyRelative("id");
            
            // Check for error
            if(idProperty == null)
            {
                GUI.TextField(contentRect, "<error: property 'id' not found>");
                return;
            }

            // Check if show edit button is visible
            bool showEditButton = property.serializedObject.isEditingMultipleObjects == false;
            int editButtonWidth = (showEditButton == true) ? buttonWidth : 0;

            // Update edit text
            if (isEditingMode == false)
                editText = idProperty.intValue.ToString();

            Rect fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width - editButtonWidth, contentRect.height);

            if (property.serializedObject.isEditingMultipleObjects == false)
            {
                GUI.SetNextControlName("id_property");

                // Display the id field
                editText = GUI.TextField(fieldRect, editText, isEditingMode == true ? EditorStyles.textField : EditorStyles.helpBox);
            }
            else
            {
                GUI.TextField(fieldRect, char.ConvertFromUtf32(0x00002015), EditorStyles.helpBox);
            }

            // Get the icon
            GUIContent editContent = EditorGUIUtility.IconContent("d_CustomTool");

            if(GUI.Button(new Rect(contentRect.xMax - editButtonWidth, contentRect.y, editButtonWidth, contentRect.height), editContent) == true)
            {               
                // Check for complete - save changes
                if(isEditingMode == true)
                {
                    ApplyEditedIdentity(idProperty);
                }
                else
                {
                    GUI.FocusControl("id_property");
                    isEditingMode = true;
                }
            }

            // Check for confirm enter
            if(Event.current.isKey == true && Event.current.keyCode == KeyCode.Return)
            {
                if(isEditingMode == true)
                {
                    ApplyEditedIdentity(idProperty);
                }
            }
        }

        private void ApplyEditedIdentity(SerializedProperty idProperty)
        {
            // Try to parse
            uint newId;
            if (uint.TryParse(editText, out newId) == true && newId != 0)
            {
                // Check for too large
                if (newId > ReplayIdentity.maxValue)
                {
                    Debug.LogWarning("The provided id value `" + newId + "` exceeds the maximum allowed value: " + ReplayIdentity.maxValue);
                    editText = idProperty.intValue.ToString();
                }
                else
                {
                    idProperty.intValue = (int)newId;
                }
            }
            else if (newId == 0)
            {
                Debug.LogWarning("Invalid id value `0` is not allowed!");
                editText = idProperty.intValue.ToString();
            }
            else
            {
                Debug.LogWarning("Failed to parse id value: " + editText + ", Must be a positive integer value!");
                editText = idProperty.intValue.ToString();
            }

            isEditingMode = false;
            GUI.FocusControl(null);
        }
    }
}
