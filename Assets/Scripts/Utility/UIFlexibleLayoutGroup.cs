using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine;

public class UIFlexibleLayoutGroup : MonoBehaviour
{
    [Header("0 Means Undefined")]
    public int rows = 1;
    public int coloumns = 1;

    [Header("Elastic Clamping")]
    [SerializeField]
    private bool useElasticClamping = true;

    [SerializeField]
    private bool clampingX = false;

    [Header("Restrictions")]
    [SerializeField]
    private bool restrictToSquare = false;

    [SerializeField]
    private bool CalculateOnStart = true;

    [SerializeField]
    private bool useParentRect;

    private RectTransform rect;

    void Start()
    {
        if (CalculateOnStart)
        {
            CalculateNewBounds();
        }
    }

    [Button]
    public void CalculateNewBounds()
    {
        float width = (transform as RectTransform).rect.width;
        float height = (transform as RectTransform).rect.height;
        if (useParentRect)
        {
            width = (transform.parent as RectTransform).rect.width;
            height = (transform.parent as RectTransform).rect.height;
        }

        GridLayoutGroup layoutGroup = GetComponent<GridLayoutGroup>();

        float x = coloumns == 0
            ? layoutGroup.cellSize.x - layoutGroup.spacing.x
            : (width - (layoutGroup.padding.left + layoutGroup.padding.right)) / coloumns - layoutGroup.spacing.x / Mathf.Max(rows, 1);

        float y = rows == 0 
            ? layoutGroup.cellSize.y - layoutGroup.spacing.y 
            : (height - (layoutGroup.padding.top + layoutGroup.padding.bottom)) / rows - layoutGroup.spacing.y  / Mathf.Max(coloumns, 1);

        layoutGroup.cellSize = new Vector2(x, y);

        if (useElasticClamping)
        {
            rect = transform as RectTransform;

            if (!clampingX)
            {
                rect.offsetMin = new Vector2(rect.offsetMin.x, Mathf.Min(0, transform.childCount * -(layoutGroup.cellSize.y + layoutGroup.spacing.y) - layoutGroup.padding.bottom + height));
            }
            else
            {
                rect.offsetMax = new Vector2(transform.childCount * (layoutGroup.cellSize.x + layoutGroup.spacing.x) - width + layoutGroup.padding.right, rect.offsetMin.y);
            }
        }

        if (restrictToSquare)
        {
            float min = Mathf.Min(layoutGroup.cellSize.x, layoutGroup.cellSize.y);
            layoutGroup.cellSize = new Vector2(min, min);
        }
    }
}
