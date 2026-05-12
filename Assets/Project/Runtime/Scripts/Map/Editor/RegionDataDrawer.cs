using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(RegionData))]
public class RegionDataDrawer : PropertyDrawer
{
    private readonly float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var nameProp = property.FindPropertyRelative("Name");
        var teamProp = property.FindPropertyRelative("Team");
        var areaProp = property.FindPropertyRelative("Area");
        
        var typeProp = property.FindPropertyRelative("Type");
        
        var inputTypeProp = property.FindPropertyRelative("InputUnitType");
        var outputTypeProp = property.FindPropertyRelative("OutputUnitType");
        var isLockedProp = property.FindPropertyRelative("IsLocked");

        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label);
        currentRect.y += lineHeight;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            EditorGUI.PropertyField(currentRect, nameProp);
            currentRect.y += lineHeight;

            EditorGUI.PropertyField(currentRect, teamProp);
            currentRect.y += EditorGUI.GetPropertyHeight(teamProp) + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(currentRect, areaProp, true);
            currentRect.y += EditorGUI.GetPropertyHeight(areaProp) + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(currentRect, typeProp);
            currentRect.y += lineHeight;

            // Enum 값 가져오기
            RegionType currentType = (RegionType)typeProp.enumValueIndex;

            if (currentType == RegionType.Sanctuary)
            {
                EditorGUI.PropertyField(currentRect, inputTypeProp, new GUIContent("Input Unit"));
                currentRect.y += lineHeight;

                EditorGUI.PropertyField(currentRect, outputTypeProp, new GUIContent("Output Unit"));
                currentRect.y += lineHeight;

                EditorGUI.PropertyField(currentRect, isLockedProp);
                currentRect.y += lineHeight;
            }
            // 창고나 기본은 추가 필드 없음

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded)
        {
            totalHeight += EditorGUIUtility.standardVerticalSpacing;
            totalHeight += lineHeight; // Name
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Team")) + EditorGUIUtility.standardVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Area")) + EditorGUIUtility.standardVerticalSpacing;
            totalHeight += lineHeight; // Type

            var typeProp = property.FindPropertyRelative("Type");
            RegionType currentType = (RegionType)typeProp.enumValueIndex;

            if (currentType == RegionType.Sanctuary)
            {
                totalHeight += lineHeight * 3;
            }
        }

        return totalHeight;
    }
}