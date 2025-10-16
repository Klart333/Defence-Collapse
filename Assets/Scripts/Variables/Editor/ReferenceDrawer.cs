using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using UnityEditor;
using UnityEngine;
using System.IO;
using TMPro;

namespace Variables.Editor
{

    public abstract class ReferenceDrawerBase<T, T1> : PropertyDrawer where T1 : ScriptableObject
    {
        protected abstract T GetVariableValue(T1 variable);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Find the three sub-properties
            SerializedProperty modeProp    = property.FindPropertyRelative("Mode");
            SerializedProperty constantProp = property.FindPropertyRelative("ConstantValue");
            SerializedProperty variableProp = property.FindPropertyRelative("Variable");
            SerializedProperty previewProp = property.FindPropertyRelative("VariableValuePreview");

            // Draw label
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            // Draw toggle (as popup)
            modeProp.enumValueIndex = EditorGUILayout.Popup(modeProp.enumValueIndex, modeProp.enumDisplayNames);

            // Draw either constant or variable field
            if (modeProp.enumValueIndex == 0) // Constant
            {
                EditorGUILayout.PropertyField(constantProp, GUIContent.none);
                EditorGUILayout.EndHorizontal();

            }
            else // Variable
            {
                EditorGUILayout.PropertyField(variableProp, GUIContent.none);
                EditorGUILayout.EndHorizontal();

                // Draw preview label
                if (variableProp.objectReferenceValue != null)
                {
                    T value = GetVariableValue((T1)variableProp.objectReferenceValue);

                    switch (value)
                    {
                        case Object objectValue:
                            previewProp.objectReferenceValue = objectValue;
                            EditorGUILayout.ObjectField(previewProp, GUIContent.none);
                            break;
                        case Color colorValue:
                            EditorGUILayout.ColorField(colorValue);
                            break;
                        case float floatValue:
                            EditorGUILayout.FloatField(floatValue);
                            break;
                        case string stringValue:
                            EditorGUILayout.TextField(stringValue, EditorStyles.textField);
                            break;
                    }
                }
                else // Make button to create the ScriptableObject reference
                {
                    // Draw "Create" button when null
                    if (GUILayout.Button($"Create {typeof(T1).Name}", GUILayout.Height(20)))
                    {
                        CreateAndAssignAsset(variableProp);
                    }
                }

            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

        }

        /// <summary>
        /// Creates a ScriptableObject asset and assigns it to the given property.
        /// </summary>
        private void CreateAndAssignAsset(SerializedProperty variableProp)
        {
            System.Type targetType = typeof(T1);

            // Create instance
            ScriptableObject newAsset = ScriptableObject.CreateInstance(targetType);

            // Find parent asset path
            string assetPath = AssetDatabase.GetAssetPath(variableProp.serializedObject.targetObject);
            assetPath = string.IsNullOrEmpty(assetPath) ? "Assets" : Path.GetDirectoryName(assetPath);

            // Auto-name based on field name for clarity
            string safeName = ObjectNames.NicifyVariableName(variableProp.name);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(assetPath, $"{safeName}_{targetType.Name}.asset"));

            // Save asset
            AssetDatabase.CreateAsset(newAsset, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Assign reference
            variableProp.objectReferenceValue = newAsset;
            variableProp.serializedObject.ApplyModifiedProperties();

            // Highlight new asset
            EditorGUIUtility.PingObject(newAsset);
            Selection.activeObject = newAsset;
        }
    }
    

    [CustomPropertyDrawer(typeof(FloatReference))]
    public class FloatReferenceDrawer : ReferenceDrawerBase<float, FloatVariable>
    {
        protected override float GetVariableValue(FloatVariable variable)
        {
            return variable.Value;
        }
    }

    
    [CustomPropertyDrawer(typeof(SpriteReference))]
    public class SpriteReferenceDrawer : ReferenceDrawerBase<Sprite, SpriteVariable>
    {
        protected override Sprite GetVariableValue(SpriteVariable variable)
        {
            return variable.Value;
        }
    }
    
    [CustomPropertyDrawer(typeof(ColorReference))]
    public class ColorReferenceDrawer : ReferenceDrawerBase<Color, ColorVariable>
    {
        protected override Color GetVariableValue(ColorVariable variable)
        {
            return variable.Value;
        }
    }
    
    [CustomPropertyDrawer(typeof(FontReference))]
    public class FontReferenceDrawer : ReferenceDrawerBase<TMP_FontAsset, FontVariable>
    {
        protected override TMP_FontAsset GetVariableValue(FontVariable variable)
        {
            return variable.Value;
        }
    }
    
    [CustomPropertyDrawer(typeof(StringReference))]
    public class StringReferenceDrawer : ReferenceDrawerBase<string, StringVariable>
    {
        protected override string GetVariableValue(StringVariable variable)
        {
            LocalizedString localizedString = variable.LocalizedText;
            if (localizedString.IsEmpty) return "";

            LocalizedStringDatabase sd = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase;
            LocalizedDatabase<StringTable, StringTableEntry>.TableEntryResult entry = sd.GetTableEntry(localizedString.TableReference, localizedString.TableEntryReference);
            return entry.Entry.Value;
        }
    }
    
    [CustomPropertyDrawer(typeof(TextureReference))]
    public class TextureReferenceDrawer : ReferenceDrawerBase<Texture2D, TextureVariable>
    {
        protected override Texture2D GetVariableValue(TextureVariable variable)
        {
            return variable.Value;
        }
    }
}