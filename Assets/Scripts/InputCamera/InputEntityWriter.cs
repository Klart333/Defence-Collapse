using Cysharp.Threading.Tasks;
using Math = Utility.Math;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace InputCamera
{
    public class InputEntityWriter : MonoBehaviour
    {
        private static readonly int MousePos = Shader.PropertyToID("_MousePos");
        
        private EntityManager entityManager;
        private Entity mousePositionEntity;
        private InputManager inputManager;
        private Camera cam;

        private void OnEnable()
        {
            cam = Camera.main;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            mousePositionEntity = entityManager.CreateEntity(typeof(MousePositionComponent));
            
            GetInput().Forget();
        }

        private async UniTaskVoid GetInput()
        {
            inputManager = await InputManager.Get();
        }

        private void Update()
        {
            if (!inputManager) return;

            float2 screenPos = inputManager.Mouse.ReadValue<Vector2>();
            float3 worldPos = Math.GetGroundIntersectionPoint(cam, screenPos);
            entityManager.SetComponentData(mousePositionEntity, new MousePositionComponent
            {
                ScreenPosition = screenPos,
                WorldPosition = worldPos,
            });
            
            Shader.SetGlobalVector(MousePos, new Vector4(worldPos.x, worldPos.z, 0, 0));
        }
    }

    public struct MousePositionComponent : IComponentData
    {
        public float2 ScreenPosition;
        public float3 WorldPosition;
    }
}