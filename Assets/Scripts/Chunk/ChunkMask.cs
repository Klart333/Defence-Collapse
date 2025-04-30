using System;
using DG.Tweening;
using UnityEngine;

namespace Chunks
{
    public class ChunkMask : PooledMonoBehaviour
    {
        private static readonly int Color = Shader.PropertyToID("_Color");
        private static readonly int North = Shader.PropertyToID("_North");
        private static readonly int South = Shader.PropertyToID("_South");
        private static readonly int East = Shader.PropertyToID("_East");
        private static readonly int West = Shader.PropertyToID("_West");

        [SerializeField]
        private MeshRenderer meshRenderer;

        private MaterialPropertyBlock block;

        public Adjacencies Adjacencies { get; private set; }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            block.SetColor(Color, UnityEngine.Color.cyan);
            meshRenderer.GetPropertyBlock(block);
        }

        public void FadeIn(float duration)
        {
            Color color = new Color(0, 0, 0, 0);
            DOTween.To(x =>
            {
                color.a = x;
                block.SetColor(Color, color);
                meshRenderer.SetPropertyBlock(block);
            }, 0, 1, duration);
        }


        public void FadeOut(float duration)
        {
            Color color = new Color(0, 0, 0, 1);
            DOTween.To(x =>
            {
                color.a = x;
                block.SetColor(Color, color);
                meshRenderer.SetPropertyBlock(block);
            }, 1, 0, duration);
            
            gameObject.SetActive(false);
        }

        public void SetAdjacencies(Adjacencies adjacencies)
        {
            Adjacencies = adjacencies;
            block.SetFloat(North, (adjacencies & Adjacencies.North) > 0 ? 1 : 0);
            block.SetFloat(South, (adjacencies & Adjacencies.South) > 0 ? 1 : 0);
            block.SetFloat(East, (adjacencies & Adjacencies.East) > 0 ? 1 : 0);
            block.SetFloat(West, (adjacencies & Adjacencies.West) > 0 ? 1 : 0);
            meshRenderer.SetPropertyBlock(block);
        }
    }

    [Flags]
    public enum Adjacencies
    {
        None = 0,
        North = 1 << 0,
        South = 1 << 1,
        East = 1 << 2,
        West = 1 << 3,
    }
}