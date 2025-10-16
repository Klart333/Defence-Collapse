using Sirenix.OdinInspector;
using UnityEngine;
using System;
using Loot;

namespace Effects.UI
{
    public class UIEffectContainer : MonoBehaviour
    {
        public event Action<EffectModifier, int> OnEffectRemoved;
        public event Action<EffectModifier, int> OnEffectAdded;

        [Title("Prefabs")]
        [SerializeField]
        private UIEffectDisplay effectDisplayPrefab;

        [Title("UI")]
        [SerializeField]
        private Transform containerContainer;
        
        [SerializeField]
        private UISingleDraggableContainer containerPrefab;
        
        private UISingleDraggableContainer[] spawnedContainers;
        private Action<IDraggable>[] containerAddActions;
        private Action<IDraggable>[] containerRemoveActions;

        public void Setup(int containerAmount)
        {
            spawnedContainers = new UISingleDraggableContainer[containerAmount];
            containerAddActions = new Action<IDraggable>[containerAmount];
            containerRemoveActions = new Action<IDraggable>[containerAmount];
            
            for (int i = 0; i < containerAmount; i++)
            {
                spawnedContainers[i] = containerPrefab.Get<UISingleDraggableContainer>();
                spawnedContainers[i].transform.SetParent(containerContainer, false);
                spawnedContainers[i].transform.SetSiblingIndex(i);

                int containerIndex = i;
                containerAddActions[i] = x => { OnDraggableAddedToContainer(x, containerIndex); };
                containerRemoveActions[i] = x => { OnDraggableRemovedFromContainer(x, containerIndex); };
                
                spawnedContainers[i].OnDraggableAdded += containerAddActions[i];
                spawnedContainers[i].OnDraggableRemoved += containerRemoveActions[i];
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < spawnedContainers.Length; i++)
            {
                spawnedContainers[i].gameObject.SetActive(false);
                
                spawnedContainers[i].OnDraggableAdded -= containerAddActions[i];
                spawnedContainers[i].OnDraggableRemoved -= containerRemoveActions[i];
            }
            
            spawnedContainers = null;
            containerAddActions = null;
            containerRemoveActions = null;
        }
        
        public void SetEffects(EffectModifier[] effects)
        {
            if (effects.Length != spawnedContainers.Length)
            {
                Debug.LogError("Mismatched number of effects");
                return;
            }

            for (int i = 0; i < spawnedContainers.Length; i++)
            {
                if (effects[i] == null)
                {
                    continue;
                }
                
                SpawnEffect(effects[i], spawnedContainers[i]);
            }
        }

        public void SpawnEffect(EffectModifier effectModifier, UISingleDraggableContainer container = null)
        {
            if (!container && !TryGetFirstEmptyContainer(out container))
            {
                Debug.LogError("Could not find empty container for effect");
                return;
            }
            
            UIEffectDisplay effect = effectDisplayPrefab.Get<UIEffectDisplay>();
            effect.SetParent(container.DraggableParent);
            effect.Display(effectModifier);
             
            container.AddDraggable(effect);
        }

        private bool TryGetFirstEmptyContainer(out UISingleDraggableContainer container)
        {
            for (int i = 0; i < spawnedContainers.Length; i++)
            {
                if (spawnedContainers[i].HasDraggable)
                {
                    continue;
                }
                
                container = spawnedContainers[i];
                return true;
            }

            container = null;
            return false;
        }

        
        private void OnDraggableAddedToContainer(IDraggable draggable, int containerIndex)
        {
            if (draggable is UIEffectDisplay effectDisplay)
            {
                OnEffectAdded?.Invoke(effectDisplay.EffectModifier, containerIndex);
            }
        }
        
        private void OnDraggableRemovedFromContainer(IDraggable draggable, int containerIndex)
        {
            if (draggable is UIEffectDisplay effectDisplay)
            {
                OnEffectRemoved?.Invoke(effectDisplay.EffectModifier, containerIndex);
            }
        }
    }

    public interface IDraggable
    {
        public IContainer Container { get; set; }
        public void SetParent(Transform parent);
        public void Disable();
    }

    public interface IContainer
    {
        public void AddDraggable(IDraggable draggable);
    }
}