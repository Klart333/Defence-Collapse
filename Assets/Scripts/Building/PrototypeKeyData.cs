﻿using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[InlineEditor]
[CreateAssetMenu(fileName = "New data", menuName = "Building/Prototype Key Data")]
public class PrototypeKeyData : SerializedScriptableObject
{
    [Title("Key Info")]
    public HashSet<string> BuildingKeys;

    public HashSet<string> PathKeys;
}
