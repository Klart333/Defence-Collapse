using Sirenix.OdinInspector;
using UnityEngine;

public class Portal : PooledMonoBehaviour
{
    [Title("Portal")]
    [SerializeField]
    private LayerMask regularMask;

    [SerializeField]
    private LayerMask hoverMask;

    [SerializeField]
    private MeshRenderer[] renderers;

    [SerializeField]
    private GameObject unlockCanvas;

    private bool hovered = false;

    public bool Locked { get; private set; } = true;

    private void Update()
    {
        if (unlockCanvas.activeSelf && !hovered &&  InputManager.Instance.GetFire && !InputManager.Instance.MouseOverUI())
        {
            unlockCanvas.SetActive(false);
        }
    }

    private void OnMouseEnter()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].gameObject.layer = (int)Mathf.Log(hoverMask.value, 2);
        }
        hovered = true;
    }

    private void OnMouseExit()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].gameObject.layer = (int)Mathf.Log(regularMask.value, 2);
        }
        hovered = false;
    }

    private void OnMouseDown()
    {
        unlockCanvas.SetActive(true);
    }

    public void Unlock()
    {
        Locked = false;
    }
}
