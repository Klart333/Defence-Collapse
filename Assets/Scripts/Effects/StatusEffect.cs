using Effects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Effects
{
    [InlineEditor, CreateAssetMenu(fileName = "New StatusEffect", menuName = "StatusEffect/StatusEffect")]
    public class StatusEffect : SerializedScriptableObject
    {
        [Title("Info")]
        public Sprite Icon;

        public string Description;

        [Title("Effects")]
        [OdinSerialize, NonSerialized]
        public List<IStatusEffect> Effects;

        public void TriggerEFfect(ref DamageInstance damageInstance)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].Perform(ref damageInstance);
            }
        }
    }
}


