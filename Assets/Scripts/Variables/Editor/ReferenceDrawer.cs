using TMPro;

namespace Variables.Editor
{
    using UnityEditor;
    using UnityEngine;

    public abstract class ReferenceDrawerBase<T> : PropertyDrawer
    {
        protected abstract T GetVariableValue(Object variable);

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
            
            // Draw toggle (as popup)
            modeProp.enumValueIndex = EditorGUILayout.Popup(modeProp.enumValueIndex, modeProp.enumDisplayNames);

            // Draw either constant or variable field
            if (modeProp.enumValueIndex == 0) // Constant
            {
                EditorGUILayout.PropertyField(constantProp, GUIContent.none);
            }
            else // Variable
            {
                EditorGUILayout.BeginVertical();

                EditorGUILayout.PropertyField(variableProp, GUIContent.none);

                // Draw preview label
                if (variableProp.objectReferenceValue != null)
                {
                    T value = GetVariableValue(variableProp.objectReferenceValue);

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
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
    

    [CustomPropertyDrawer(typeof(FloatReference))]
    public class FloatReferenceDrawer : ReferenceDrawerBase<float>
    {
        protected override float GetVariableValue(Object variable)
        {
            return ((FloatVariable)variable).Value;
        }
    }

    
    [CustomPropertyDrawer(typeof(SpriteReference))]
    public class SpriteReferenceDrawer : ReferenceDrawerBase<Sprite>
    {
        protected override Sprite GetVariableValue(Object variable)
        {
            return ((SpriteVariable)variable).Value;
        }
    }
    
    [CustomPropertyDrawer(typeof(ColorReference))]
    public class ColorReferenceDrawer : ReferenceDrawerBase<Color>
    {
        protected override Color GetVariableValue(Object variable)
        {
            return ((ColorVariable)variable).Value;
        }
    }
    
    [CustomPropertyDrawer(typeof(FontReference))]
    public class FontReferenceDrawer : ReferenceDrawerBase<TMP_FontAsset>
    {
        protected override TMP_FontAsset GetVariableValue(Object variable)
        {
            return ((FontVariable)variable).Value;
        }
    }
}