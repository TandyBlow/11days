// SuggestionDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SuggestionDatabase", menuName = "Game/Suggestion Database")]
public class SuggestionDatabase : ScriptableObject
{
    public List<SuggestionData> allSuggestions = new List<SuggestionData>();
}
