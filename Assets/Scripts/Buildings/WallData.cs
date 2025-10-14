using System;
using Effects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wall Data", menuName = "Building/State Data/Wall")]
public class WallData : SerializedScriptableObject
{
    [Title("Stats")]
    [OdinSerialize]
    public IStatGroup[] StatGroups = Array.Empty<IStatGroup>();
}

