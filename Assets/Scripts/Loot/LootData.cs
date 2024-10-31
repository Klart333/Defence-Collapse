using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "New Loot", menuName = "Loot")]
public class LootData : SerializedScriptableObject
{
    [Title("Rarity")]
    public int Grade = 1;

    [Title("Loot")]
    public List<ILootEffect> LootEffects = new List<ILootEffect>();

    public void Perform(int grade)
    {
        for (int i = 0; i < LootEffects.Count; i++)
        {
            LootEffects[i].Perform(grade);
        }
    }

    [Button]
    public void AddModifierEditorOnly()
    {
        Perform(0);
    }
}