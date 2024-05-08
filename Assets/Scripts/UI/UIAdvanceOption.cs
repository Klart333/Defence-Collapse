using Sirenix.OdinInspector;
using UnityEngine;

public class UIAdvanceOption : MonoBehaviour
{
    [Title("Tower Type")]
    [SerializeField]
    private TowerType towerType;

    private Animator animator;
    private StupidButton button;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        button = GetComponent<StupidButton>();
    }

    private void Update()
    {
        if (!animator.gameObject.activeSelf && button.Hovered)
        {
            animator.gameObject.SetActive(true);
        }
        else if (animator.gameObject.activeSelf && !button.Hovered)
        {
            animator.gameObject.SetActive(false);
        }
    }

    public void SelectOption()
    {
        BuildingUpgradeManager.Instance.SelectAdvancementOption(towerType);
    }
}
