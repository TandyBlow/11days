// LayoutCreatorEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class LayoutCreatorEditor
{
    [MenuItem("Tools/Game/Save Current Layout as Asset")]
    private static void SaveCurrentLayout()
    {
        // 确保只在播放模式下，且BuildManager存在时运行
        if (!Application.isPlaying || BuildManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "Please run the game and place buildings before saving a layout.", "OK");
            return;
        }

        // 获取当前所有建筑的数据
        var buildingsData = BuildManager.Instance.GetPlacedBuildingsSaveData();

        if (buildingsData == null || buildingsData.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "No buildings found on the grid to save.", "OK");
            return;
        }

        // 转换为 LayoutData 真正使用的 layoutEntries。
        // 以前写的是没人读的 layoutBuildings，结果“保存成功”的布局加载出来是 0 栋建筑。
        List<EditorLayoutEntry> layoutEntries = new List<EditorLayoutEntry>();
        foreach (var building in buildingsData)
        {
            BuildingData data = BuildManager.Instance.FindBuildingData(building.buildingName);
            if (data == null)
            {
                Debug.LogWarning($"导出布局时跳过 '{building.buildingName}'：找不到对应的 BuildingData。");
                continue;
            }
            layoutEntries.Add(new EditorLayoutEntry
            {
                buildingData = data,
                gridPosition = new Vector2Int(building.gridX, building.gridY)
            });
        }

        if (layoutEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "没有可导出的建筑数据。", "OK");
            return;
        }

        // 创建一个新的 LayoutData ScriptableObject 实例
        LayoutData newLayout = ScriptableObject.CreateInstance<LayoutData>();
        newLayout.layoutEntries = layoutEntries;

        // 弹出保存文件对话框
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Layout Asset",
            "NewLayout.asset",
            "asset",
            "Please enter a file name to save the layout to."
        );

        if (string.IsNullOrEmpty(path)) return; // 用户取消了

        // 在指定路径创建并保存资产
        AssetDatabase.CreateAsset(newLayout, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newLayout;

        Debug.Log($"Layout saved successfully at: {path}");
    }
}
