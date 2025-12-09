// BuildingPrefabGenerator.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class BuildingPrefabGenerator
{
    // --- 配置项 ---

    // 泡泡图片的资源路径 (相对于 Assets/)
    // ↓↓↓ !!! 请务必修改这里，指向你的“泡泡”图片资源 !!! ↓↓↓
    private const string BubbleSpritePath = "Assets/Sprites/bubble.png";
    // ↑↑↑ !!! 例如 "Assets/MyArt/Icons/InteractionBubble.png" ↑↑↑

    // Prefab的保存路径
    private const string PrefabSavePath = "Assets/Prefabs/Buildings";

    // 【新】要设置的Sorting Layer的名称
    private const string TargetSortingLayerName = "Building";

    // 【新】要设置的默认缩放值
    private static readonly Vector3 DefaultScale = new Vector3(0.2f, 0.2f, 1f);


    [MenuItem("游戏工具 (Game Tools)/从图片创建建筑 Prefab (Create Building Prefab from Image)", false, 1)]
    private static void CreateBuildingPrefab()
    {
        // 0. 【新】前置检查：确认目标Sorting Layer是否存在
        if (!DoesSortingLayerExist(TargetSortingLayerName))
        {
            EditorUtility.DisplayDialog("错误 (Error)",
                $"Sorting Layer '{TargetSortingLayerName}' 不存在。\n\n请先在 'Edit -> Project Settings -> Tags and Layers' 中创建它。",
                "好的 (OK)");
            return; // 停止执行
        }

        // 1. 获取在Project窗口中选中的对象
        var selectedObject = Selection.activeObject;
        if (selectedObject == null || !(selectedObject is Texture2D))
        {
            EditorUtility.DisplayDialog("错误 (Error)", "请先在项目窗口中选择一张图片 (Texture2D)！", "好的 (OK)");
            return;
        }

        // 2. 处理选中的图片资源：修改Pivot为底部中心
        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (textureImporter == null)
        {
            EditorUtility.DisplayDialog("错误 (Error)", "无法获取图片导入设置，请确保选择的是一张有效的图片。", "好的 (OK)");
            return;
        }

        bool settingsChanged = false;
        if (textureImporter.textureType != TextureImporterType.Sprite)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            settingsChanged = true;
        }
        if (textureImporter.spritePivot != new Vector2(0.5f, 0f))
        {
            textureImporter.spritePivot = new Vector2(0.5f, 0f); // Bottom Center
            settingsChanged = true;
        }

        if (settingsChanged)
        {
            textureImporter.SaveAndReimport();
            Debug.Log($"已自动修改图片 '{selectedObject.name}' 的Pivot为底部中心。");
        }

        Sprite buildingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (buildingSprite == null)
        {
            EditorUtility.DisplayDialog("错误 (Error)", "无法从路径加载Sprite，请检查图片设置。", "好的 (OK)");
            return;
        }

        // 3. 创建并配置GameObject
        GameObject buildingGO = new GameObject(buildingSprite.name);

        // 【新】设置GameObject的缩放
        buildingGO.transform.localScale = DefaultScale;

        // 添加并设置SpriteRenderer
        SpriteRenderer sr = buildingGO.AddComponent<SpriteRenderer>();
        sr.sprite = buildingSprite;
        // 【新】设置Sorting Layer
        sr.sortingLayerName = TargetSortingLayerName;

        // 添加并设置BoxCollider
        BoxCollider boxCollider = buildingGO.AddComponent<BoxCollider>();
        boxCollider.size = sr.sprite.bounds.size;
        boxCollider.center = sr.sprite.bounds.center;

        // 添加并配置IsometricSorter
        IsometricSorter sorter = buildingGO.AddComponent<IsometricSorter>();
        SerializedObject sorterSO = new SerializedObject(sorter);
        sorterSO.FindProperty("isStatic").boolValue = true;
        sorterSO.FindProperty("sortChildRenderers").boolValue = true;
        sorterSO.FindProperty("externalOffset").intValue = 10;
        sorterSO.ApplyModifiedProperties();

        // 添加BuildingInstance脚本
        BuildingInstance instance = buildingGO.AddComponent<BuildingInstance>();

        // 4. 创建并配置交互泡泡子对象
GameObject bubbleGO = new GameObject("InteractionBubble");
bubbleGO.transform.SetParent(buildingGO.transform);

// 为泡泡添加SpriteRenderer并提前加载Sprite以获取其高度
SpriteRenderer bubbleSr = bubbleGO.AddComponent<SpriteRenderer>();
Sprite bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BubbleSpritePath);

if (bubbleSprite != null)
{
    bubbleSr.sprite = bubbleSprite;
}
else
{
    Debug.LogWarning($"未在路径 '{BubbleSpritePath}' 找到泡泡图片。请手动为 '{bubbleGO.name}' 添加Sprite。");
}

// 【修改】设置泡泡的位置：在建筑顶部再减去泡泡自身的高度
// 我们需要在这里检查bubbleSprite是否为null，以防图片加载失败
float bubbleHeight = (bubbleSprite != null) ? bubbleSprite.bounds.size.y : 0f;
bubbleGO.transform.localPosition = new Vector3(0, sr.sprite.bounds.size.y - bubbleHeight, 0);

// 为泡泡也设置相同的Sorting Layer
bubbleSr.sortingLayerName = TargetSortingLayerName;
bubbleGO.SetActive(false);


        // 5. 将泡泡对象关联到BuildingInstance脚本
        SerializedObject instanceSO = new SerializedObject(instance);
        instanceSO.FindProperty("interactionBubble").objectReferenceValue = bubbleGO;
        instanceSO.ApplyModifiedProperties();

        // 6. 创建Prefab
        if (!Directory.Exists(PrefabSavePath))
        {
            Directory.CreateDirectory(PrefabSavePath);
        }
        string prefabPath = Path.Combine(PrefabSavePath, buildingGO.name + ".prefab");
        string uniquePrefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(buildingGO, uniquePrefabPath, InteractionMode.UserAction);

        // 7. 清理临时创建在场景中的GameObject
        GameObject.DestroyImmediate(buildingGO);

        // 完成提示
        EditorUtility.DisplayDialog("成功 (Success)", $"已成功创建建筑Prefab！\n保存在: {uniquePrefabPath}", "太棒了！ (Great!)");
        
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(uniquePrefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
    
    /// <summary>
    /// 检查指定的Sorting Layer是否存在于项目中
    /// </summary>
    private static bool DoesSortingLayerExist(string layerName)
    {
        foreach (var layer in SortingLayer.layers)
        {
            if (layer.name == layerName)
            {
                return true;
            }
        }
        return false;
    }
}
