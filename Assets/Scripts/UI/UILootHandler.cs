using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILootHandler : MonoBehaviour
{
    [Title("References")]
    [SerializeField]
    private GameObject panelParent;

    [SerializeField]
    private TextMeshProUGUI description;

    [SerializeField]
    private TextMeshProUGUI title;

    [SerializeField]
    private Image icon;

    public void DisplayEffect(EffectModifier effect)
    {
        panelParent.SetActive(true);

        description.text = effect.Description;
        title.text = effect.Title;
        icon.sprite = effect.Icon;
    }

    public void Claim()
    {
        //LootManager.Instance.ClaimLoot();
        panelParent.SetActive(false);
    }
}
