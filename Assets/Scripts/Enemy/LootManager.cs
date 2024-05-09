using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class LootManager : Singleton<LootManager>
{
    [Title("Loots")]
    [SerializeField]
    private LootOrb normalLoot;

    [Title("UI")]
    [SerializeField]
    private UILootHandler lootHandler;

    private ILoot currentLoot;

    public LootOrb SpawnLoot(Vector3 pos, float scale, int grade)
    {
        LootOrb loot = normalLoot.GetAtPosAndRot<LootOrb>(pos, Quaternion.identity);
        loot.Grade = grade;
        loot.transform.localScale *= scale;

        return loot;
    }

    public int GetGrade(List<float> probabilites)
    {
        for (int i = probabilites.Count - 1; i >= 0; i--)
        {
            float value = UnityEngine.Random.value;
            if (value <= probabilites[i])
            {
                return i;
            }
        }

        return -1;
    }

    public void CollectLoot(int grade)
    {
        print("Grade: " + grade);
        currentLoot = new GoldLoot();

        lootHandler.DisplayLoot(currentLoot);
    }

    public void ClaimLoot()
    {
        currentLoot.Perform();
    }
}
