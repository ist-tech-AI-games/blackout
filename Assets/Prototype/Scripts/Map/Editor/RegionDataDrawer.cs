using UnityEngine;
using UnityEditor;

// Gemini 생성
[CustomPropertyDrawer(typeof(TilemapLoader.RegionData))]
public class RegionDataDrawer : PropertyDrawer
{
    // 한 줄의 높이 (기본 높이 + 여백)
    private float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. 프로퍼티 시작
        EditorGUI.BeginProperty(position, label, property);

        // 필드 찾기
        var nameProp = property.FindPropertyRelative("Name");
        var teamProp = property.FindPropertyRelative("Team");
        var areaProp = property.FindPropertyRelative("Area");
        var typeProp = property.FindPropertyRelative("RegionType");
        
        // 성소 전용 필드
        var inputTypeProp = property.FindPropertyRelative("InputUnitType");
        var outputTypeProp = property.FindPropertyRelative("OutputUnitType");
        var isLockedProp = property.FindPropertyRelative("IsLocked");

        // 그리기 시작 위치 초기화
        Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // 2. 헤더 (RegionData라는 라벨을 클릭해서 접을 수 있게 만듦)
        property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label);
        currentRect.y += lineHeight;

        if (property.isExpanded)
        {
            // 들여쓰기 시작
            EditorGUI.indentLevel++;

            // --- 공통 필드 그리기 ---
            EditorGUI.PropertyField(currentRect, nameProp);
            currentRect.y += lineHeight;

            EditorGUI.PropertyField(currentRect, teamProp);
            currentRect.y += EditorGUI.GetPropertyHeight(teamProp) + EditorGUIUtility.standardVerticalSpacing; // 커스텀 클래스 높이 대응

            EditorGUI.PropertyField(currentRect, areaProp, true); // 배열이므로 true(includeChildren)
            currentRect.y += EditorGUI.GetPropertyHeight(areaProp) + EditorGUIUtility.standardVerticalSpacing;

            // --- 타입 선택 (Enum) ---
            EditorGUI.PropertyField(currentRect, typeProp);
            currentRect.y += lineHeight;

            // --- 조건부 필드 그리기 ---
            // 선택된 Enum 값 가져오기
            TilemapLoader.RegionType currentType = (TilemapLoader.RegionType)typeProp.enumValueIndex;

            if (currentType == TilemapLoader.RegionType.Sanctuary)
            {
                // 박스 스타일로 성소 데이터임을 시각적으로 구분 (선택사항)
                // EditorGUI.HelpBox(currentRect, "Shrine Settings", MessageType.None); 
                
                EditorGUI.PropertyField(currentRect, inputTypeProp, new GUIContent("Input Class"));
                currentRect.y += lineHeight;

                EditorGUI.PropertyField(currentRect, outputTypeProp, new GUIContent("Output Class"));
                currentRect.y += lineHeight;

                EditorGUI.PropertyField(currentRect, isLockedProp);
                currentRect.y += lineHeight;
            }
            else if (currentType == TilemapLoader.RegionType.Storage)
            {
                // 창고는 추가 필드가 없지만, 필요하다면 여기에 작성
            }

            // 들여쓰기 원상복구
            EditorGUI.indentLevel--;
        }

        // 3. 프로퍼티 끝
        EditorGUI.EndProperty();
    }

    // Inspector에서 차지할 전체 높이를 계산하는 함수
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight; // 헤더 높이

        if (property.isExpanded)
        {
            totalHeight += EditorGUIUtility.standardVerticalSpacing;

            // 공통 필드 높이 계산
            totalHeight += lineHeight; // Name
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Team")) + EditorGUIUtility.standardVerticalSpacing;
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Area")) + EditorGUIUtility.standardVerticalSpacing;
            totalHeight += lineHeight; // Type Enum

            // 조건부 높이 계산
            var typeProp = property.FindPropertyRelative("RegionType");
            TilemapLoader.RegionType currentType = (TilemapLoader.RegionType)typeProp.enumValueIndex;

            if (currentType == TilemapLoader.RegionType.Sanctuary)
            {
                totalHeight += lineHeight * 3; // Input + Output + IsLocked
            }
        }

        return totalHeight;
    }
}