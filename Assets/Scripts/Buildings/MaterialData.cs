using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[InlineEditor, CreateAssetMenu(fileName = "Material Data", menuName = "Building/Material Data")]
public class MaterialData : ScriptableObject
{
    [Title("Materials")]
    public List<Material> Materials;

    private readonly Dictionary<int[], List<Material>> cachedMaterials = new Dictionary<int[], List<Material>>();

    public List<Material> GetMaterials(int[] indexs)
    {
        if (indexs == null)
        {
            return new List<Material>();
        }
        
        if (cachedMaterials.TryGetValue(indexs, out List<Material> value))
        {
            return value;
        }

        List<Material> result = new List<Material>();
        for (int i = 0; i < indexs.Length; i++)
        {
            result.Add(Materials[indexs[i]]);
        }

        cachedMaterials.Add(indexs, result);
        return result;
    }
}