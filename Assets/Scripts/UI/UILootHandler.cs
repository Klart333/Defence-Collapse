using TMPro;
using UnityEngine;

public class UILootHandler : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI description;

    [SerializeField]
    private GameObject panelParent;

    public void DisplayLoot(ILoot currentLoot)
    {
        description.text = currentLoot.Description.ToString();
        panelParent.SetActive(true);
    }

    public void Claim()
    {
        LootManager.Instance.ClaimLoot();
        panelParent.SetActive(false);
    }
}
