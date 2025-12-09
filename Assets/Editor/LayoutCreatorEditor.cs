// LayoutCreatorEditor.cs
using UnityEngine;
using UnityEditor;
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

        // 创建一个新的 LayoutData ScriptableObject 实例
        LayoutData newLayout = ScriptableObject.CreateInstance<LayoutData>();
        newLayout.layoutBuildings = buildingsData;

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
