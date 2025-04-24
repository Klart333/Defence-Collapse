using DG.Tweening;
using System;
using UnityEngine;

namespace Loot
{
    public class LootOrb : PooledMonoBehaviour
    {
        [SerializeField]
        private LayerMask normalLayer;

        [SerializeField]
        private LayerMask hoverLayer;

        public int Grade { get; set; }

        private float scale;
        private bool hovered;

        private void OnEnable()
        {
            scale = transform.localScale.x;
        }

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

            transform.DORewind();
        }

        private void OnMouseEnter()
        {
            gameObject.layer = (int)Mathf.Log(hoverLayer.value, 2);
            hovered = true;

            transform.DORewind();
            transform.DOScale(scale * 1.1f, 0.2f);
        }

        private void OnMouseExit()
        {
            gameObject.layer = (int)Mathf.Log(normalLayer.value, 2);
            hovered = false;

            transform.DOKill();
            transform.DOScale(scale, 0.2f);
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
            int grade = Grade;
            LootData loot = LootManager.Instance.GetLootData(ref grade);

            loot.Perform(grade);

            gameObject.SetActive(false);
        }
    }
}
