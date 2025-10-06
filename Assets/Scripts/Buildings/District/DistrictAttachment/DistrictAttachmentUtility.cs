using Effects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Unity.Entities;
using UnityEngine;

namespace Buildings.District.DistrictAttachment
{
    [CreateAssetMenu(fileName = "District Attachment Utility", menuName = "Utility/District Attachment Utility", order = 0)]
    public class DistrictAttachmentUtility : SerializedScriptableObject
    {
        [OdinSerialize]
        public IAuthoringComponent[] DistrictAttachments;
    }

    public interface IAuthoringComponent
    {
        public MonoBehaviour AuthoringComponent { get; }
    }

    public struct AttachmentAttackValue : IComponentData
    {
        public float Value;
    }
}