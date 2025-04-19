using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class LootManager : Singleton<LootManager> // Make a collect all loot button
{
    [Title("Loot Data")]
    [SerializeField]
    private List<LootData> lootDatas = new List<LootData>();

    [Title("Prefabs")]
    [SerializeField]
    private LootOrb normalLoot;

    [Title("UI")]
    [SerializeField]
    private UILootHandler lootHandler;

    private readonly Dictionary<int, List<LootData>> GradedLootData = new Dictionary<int, List<LootData>>();

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < lootDatas.Count; i++)
        {
            if (GradedLootData.TryGetValue(lootDatas[i].Grade, out List<LootData> value))
            {
                value.Add(lootDatas[i]);
            }
            else
            {
                GradedLootData.Add(lootDatas[i].Grade, new List<LootData> { lootDatas[i] });
            }
        }
    }

    public LootOrb SpawnLoot(Vector3 pos, float scale, int grade)
    {
        LootOrb loot = normalLoot.GetAtPosAndRot<LootOrb>(pos, Quaternion.identity);
        loot.transform.localScale *= scale;
        loot.Grade = grade;

        return loot;
    }

    public int GetGrade(List<float> probabilites)
    {
        for (int i = probabilites.Count - 1; i >= 0; i--)
        {
            float value = Random.value;
            if (value <= probabilites[i])
            {
                return i;
            }
        }

        return -1;
    }

    public LootData GetLootData(ref int grade)
    {
        int lootGrade = 0;
        for (int i = grade; i > 0; i--)
        {
            float odds = Mathf.Pow((grade - i + 1) / 3.0f, i / 2.0f);

            if (Random.value < odds)
            {
                grade -= i;
                lootGrade = i;
                break;
            }
        }

        return GradedLootData[lootGrade][Random.Range(0, GradedLootData[lootGrade].Count)];
    }

    public void DisplayEffectGained(EffectModifier effect)
    {
        lootHandler.DisplayEffect(effect);
    }
}
