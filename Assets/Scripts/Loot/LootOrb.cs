using System;
using UnityEngine;

public class LootOrb : PooledMonoBehaviour
{
    [SerializeField]
    private LayerMask normalLayer;

    [SerializeField]
    private LayerMask hoverLayer;

    public int Grade {  get; set; }

    private bool hovered;

    protected override void OnDisable()
    {
        base.OnDisable();

        Reset();
    }

    private void Reset()
    {
        Grade = -1;
        hovered = false;
        gameObject.layer = (int)Mathf.Log(normalLayer.value, 2);
    }

    private void OnMouseEnter()
    {
        gameObject.layer = (int)Mathf.Log(hoverLayer.value, 2);

        hovered = true;
    }

    private void OnMouseExit() 
    {
        gameObject.layer = (int)Mathf.Log(normalLayer.value, 2);

        hovered = false;
    }

    private void Update()
    {
        if (hovered && InputManager.Instance.Fire.WasReleasedThisFrame())
        {
            Collect();
        }
    }

    private void Collect()
    {
        LootManager.Instance.CollectLoot(Grade);

        gameObject.SetActive(false);
    }
}
