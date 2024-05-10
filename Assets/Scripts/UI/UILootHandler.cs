using TMPro;
using UnityEngine;

public class UILootHandler : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI description;

    [SerializeField]
    private GameObject panelParent;

    public void DisplayLoot(LootData currentLoot, int grade)
    {
        description.text = currentLoot.LootEffects[0].GetDescription(grade);
        panelParent.SetActive(true);
    }

    public void Claim()
    {
        LootManager.Instance.ClaimLoot();
        panelParent.SetActive(false);
    }
}
