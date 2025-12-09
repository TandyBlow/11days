// SaveManager.cs
using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static readonly string saveFileName = "savegame.json";
    private static readonly string completionFlagFile = "ngplus.unlocked";

    public static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private static string GetCompletionFlagPath()
    {
        return Path.Combine(Application.persistentDataPath, completionFlagFile);
    }
    // --- NEW: Method to set the completion flag ---
    public static void SetCompletionFlag()
    {
        File.Create(GetCompletionFlagPath()).Dispose();
        Debug.Log("Completion flag set. Next 'New Game' will be NG+.");
    }
    // --- NEW: Method to check for the completion flag ---
    public static bool HasCompletionFlag()
    {
        return File.Exists(GetCompletionFlagPath());
    }
    // --- NEW: Method to delete the completion flag (for True Ending) ---
    public static void DeleteCompletionFlag()
    {
        string path = GetCompletionFlagPath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Completion flag deleted. The cycle is broken.");
        }
    }


    public static void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true); // true for pretty print
        File.WriteAllText(GetSaveFilePath(), json);
        Debug.Log("游戏已保存至: " + GetSaveFilePath());
    }



    public static SaveData LoadGame()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("游戏存档已加载。");
            return data;
        }
        return null;
    }

    public static bool HasSaveData()
    {
        return File.Exists(GetSaveFilePath());
    }

    public static void DeleteSaveData()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("存档已删除。");
        }
    }
}
