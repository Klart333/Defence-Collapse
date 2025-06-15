using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Effects.ECS;
using System;

namespace Effects
{
    public class DamageCallbackHandler : MonoBehaviour
    {
        public static readonly Dictionary<int, Action<DamageCallbackComponent>> DamageDoneEvent = new Dictionary<int, Action<DamageCallbackComponent>>();
        
        private EntityManager entityManager;
        private Entity damageCallbackEntity;
        
        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            damageCallbackEntity = entityManager.CreateSingletonBuffer<DamageCallbackComponent>();
            entityManager.AddComponent<DamageCallbackSingletonTag>(damageCallbackEntity);
        }

        private void Update()
        {
            DynamicBuffer<DamageCallbackComponent> damageCallbackBuffer = entityManager.GetBuffer<DamageCallbackComponent>(damageCallbackEntity);
            if (damageCallbackBuffer.IsEmpty) return;
                
            foreach (DamageCallbackComponent callbackComponent in damageCallbackBuffer)
            {
                if (DamageDoneEvent.TryGetValue(callbackComponent.Key, out Action<DamageCallbackComponent> callback))
                {
                    callback.Invoke(callbackComponent);
                }
            }
                
            damageCallbackBuffer.Clear();
        }
    }
}