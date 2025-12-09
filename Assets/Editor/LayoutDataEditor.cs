// LayoutDataEditor.cs (放在 Editor 文件夹)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LayoutData))]
public class LayoutDataEditor : Editor
{
    private SerializedProperty layoutEntriesProp;

    private void OnEnable()
    {
        // 找到我們要編輯的列表屬性
        layoutEntriesProp = serializedObject.FindProperty("layoutEntries");
    }

    public override void OnInspectorGUI()
    {
        // 更新序列化對象
        serializedObject.Update();

        EditorGUILayout.LabelField("Layout Configuration", EditorStyles.boldLabel);
        
        // 繪製列表
        for (int i = 0; i < layoutEntriesProp.arraySize; i++)
        {
            SerializedProperty entryProp = layoutEntriesProp.GetArrayElementAtIndex(i);
            
            EditorGUILayout.BeginHorizontal();
            
            // 绘制 BuildingData 字段和 GridPosition 字段
            EditorGUILayout.PropertyField(entryProp, GUIContent.none);

            // 绘制一个红色的 "-" 按钮用于删除
            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                // 从列表中移除该元素
                layoutEntriesProp.DeleteArrayElementAtIndex(i);
                break; // 退出循环，因为列表已修改
            }

            EditorGUILayout.EndHorizontal();
        }

        // 绘制一个 "+" 按钮用于添加新条目
        if (GUILayout.Button("Add New Building Entry", GUILayout.Height(25)))
        {
            layoutEntriesProp.InsertArrayElementAtIndex(layoutEntriesProp.arraySize);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // 验证按钮，检查是否有重叠
        if (GUILayout.Button("Validate Layout (Check for Overlaps)", GUILayout.Height(30)))
        {
            ValidateLayout((LayoutData)target);
        }

        // 应用所有修改
        serializedObject.ApplyModifiedProperties();
    }

    private void ValidateLayout(LayoutData layoutData)
    {
        if (layoutData.layoutEntries.Count == 0)
        {
            Debug.Log("Layout is empty. Nothing to validate.");
            return;
        }

        // 使用一个字典来模拟网格，检查重叠
        Dictionary<Vector2Int, BuildingData> gridOccupation = new Dictionary<Vector2Int, BuildingData>();
        bool hasOverlap = false;

        foreach (var entry in layoutData.layoutEntries)
        {
            if (entry.buildingData == null) continue;

            for (int x = 0; x < entry.buildingData.width; x++)
            {
                for (int y = 0; y < entry.buildingData.height; y++)
                {
                    Vector2Int cellPos = new Vector2Int(entry.gridPosition.x + x, entry.gridPosition.y + y);
                    if (gridOccupation.TryGetValue(cellPos, out BuildingData occupyingBuilding))
                    {
                        Debug.LogError($"Validation Error: Overlap detected! Cell ({cellPos.x}, {cellPos.y}) is occupied by '{occupyingBuilding.buildingName}' and is also being claimed by '{entry.buildingData.buildingName}'.");
                        hasOverlap = true;
                    }
                    else
                    {
                        gridOccupation[cellPos] = entry.buildingData;
                    }
                }
            }
        }

        if (hasOverlap)
        {
            EditorUtility.DisplayDialog("Validation Failed", "Layout has overlapping buildings! Check the Console for details.", "OK");
        }
        else
        {
            Debug.Log("Validation Successful: No overlaps detected in the layout.");
            EditorUtility.DisplayDialog("Validation Successful", "No overlaps detected in the layout.", "OK");
        }
    }
}
