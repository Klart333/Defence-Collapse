using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using DG.Tweening;
using Pathfinding;
using Gameplay;

namespace InputCamera
{
    public class InputEntityWriter : MonoBehaviour
    {
        private static readonly int UseMousePos = Shader.PropertyToID("_UseMousePos");
        private static readonly int MousePos = Shader.PropertyToID("_MousePos");
        
        [Title("Animation")]
        [SerializeField]
        private float blendDuration = 0.3f;
        
        [SerializeField]
        private Ease blendEase = Ease.InOutSine;
        
        private InputManager inputManager;
        private GameManager gameManager;

        private Vector3 overrideMousePosition;
        private EntityManager entityManager;
        private Entity mousePositionEntity;
        
        private bool isOverridingMousePosition;

        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            mousePositionEntity = entityManager.CreateEntity(typeof(MousePositionComponent));
            
            GetInput().Forget();
            GetGameManager().Forget();
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
        }

        private async UniTaskVoid GetGameManager()
        {
            gameManager = await GameManager.Get();
        }

        private void Update()
        {
            if (inputManager == null || gameManager.IsGameOver) return;

            entityManager.SetComponentData(mousePositionEntity, new MousePositionComponent
            {
                ScreenPosition = inputManager.CurrentMouseScreenPosition,
                WorldPosition = inputManager.CurrentMouseWorldPosition,
                PathIndex = inputManager.CurrentMousePathIndex,
            });

            Shader.SetGlobalInteger(UseMousePos, isOverridingMousePosition ? 1 : 0);
            if (!isOverridingMousePosition) return;

            Vector4 pos = new Vector4(overrideMousePosition.x, overrideMousePosition.z, 0, 0);
            Shader.SetGlobalVector(MousePos, pos);
        }

        public void OverrideShaderMousePosition(Vector3 mousePosition, bool blend = true)
        {
            isOverridingMousePosition = true;

            if (blend)
            {
                DOTween.Kill(0);

                Vector3 lastPos = overrideMousePosition;
                DOTween.To(() => lastPos, x => overrideMousePosition = x, mousePosition, blendDuration).SetEase(blendEase).SetId(0);
            }
            else
            {
                overrideMousePosition = mousePosition;
            }
        }

        public void DisableOverride()
        {
            isOverridingMousePosition = false;
        }
    }

    public struct MousePositionComponent : IComponentData
    {
        public float2 ScreenPosition;
        public float3 WorldPosition;
        public PathIndex PathIndex;
    }
}