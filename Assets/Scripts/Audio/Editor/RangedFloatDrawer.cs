using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomPropertyDrawer(typeof(RangedFloat), true)]
public class RangedFloatDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, label);

        SerializedProperty minProp = property.FindPropertyRelative("minValue");
        SerializedProperty maxProp = property.FindPropertyRelative("maxValue");

        float minValue = minProp.floatValue;
        float maxValue = maxProp.floatValue;

        float rangeMin = 0;
        float rangeMax = 1;

        MinMaxRangeAttribute[] ranges = (MinMaxRangeAttribute[])fieldInfo.GetCustomAttributes(typeof(MinMaxRangeAttribute), true);

        if (ranges.Length > 0)
        {
            rangeMin = ranges[0].Min;
            rangeMax = ranges[0].Max;
        }

        var minLabelRect = new Rect(position);
        minLabelRect.width = 40;
        GUI.Label(minLabelRect, new GUIContent(minValue.ToString("F2")));
        position.xMin += 40;

        var maxLabelRect = new Rect(position);
        maxLabelRect.xMin = maxLabelRect.xMax - 40;
        GUI.Label(maxLabelRect, new GUIContent(maxValue.ToString("F2")));
        position.xMax -= 40;

        EditorGUI.BeginChangeCheck(); // Begins the change check
        EditorGUI.MinMaxSlider(position, ref minValue, ref maxValue, rangeMin, rangeMax);

        if (EditorGUI.EndChangeCheck()) // Completes the change check and checks if anything changed
        {
            minProp.floatValue = minValue;
            maxProp.floatValue = maxValue;
        }

        EditorGUI.EndProperty();
    }
}
