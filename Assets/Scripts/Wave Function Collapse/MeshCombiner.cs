using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    private MeshFilter meshFilter;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public Mesh CombineMeshes()
    {
        // All our children (and us)
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);

        // All the meshes in our children (just a big list)
        List<Material> materials = new List<Material>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false); // <-- you can optimize this
        foreach (MeshRenderer meshRenderer in renderers)
        {
            if (meshRenderer.transform == transform)
                continue;

            Material[] localMats = meshRenderer.sharedMaterials;
            foreach (Material localMat in localMats)
                if (!materials.Contains(localMat))
                    materials.Add(localMat);
        }

        // Each material will have a mesh for it.
        List<Mesh> submeshes = new List<Mesh>();
        for (int i = 0; i < materials.Count; i++)
        {
            // Make a combiner for each (sub)mesh that is mapped to the right material.
            List<CombineInstance> combiners = new List<CombineInstance>();
            for (int j = 0; j < filters.Length; j++)
            {
                MeshFilter filter = filters[j];

                // The filter doesn't know what materials are involved, get the renderer.
                if (renderers[j] == null || filter.sharedMesh == null)
                {
                    //Debug.LogError(filter.name + " has no MeshRenderer");
                    continue;
                }

                // Let's see if their materials are the one we want right now.
                Material[] localMaterials = renderers[j].sharedMaterials;
                for (int g = 0; g < localMaterials.Length; g++)
                {
                    if (localMaterials[g] != materials[i])
                        continue;
                    // This submesh is the material we're looking for right now.
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        subMeshIndex = g,
                        transform = filter.transform.localToWorldMatrix
                    };
                    combiners.Add(ci);
                }
            }
            // Flatten into a single mesh.
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combiners.ToArray(), true);
            submeshes.Add(mesh);
        }

        // The final mesh: combine all the material-specific meshes as independent submeshes.
        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        foreach (Mesh mesh in submeshes)
        {
            CombineInstance ci = new CombineInstance
            {
                mesh = mesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
            finalCombiners.Add(ci);
        }
        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(finalCombiners.ToArray(), false);
        meshFilter.sharedMesh = finalMesh;
        //Debug.Log("Final mesh has " + submeshes.Count + " materials.");

        GetComponent<MeshCollider>().sharedMesh = finalMesh;
        GetComponent<MeshRenderer>().materials = materials.ToArray();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].transform == transform)
            {
                continue;
            }
            Destroy(renderers[i].gameObject);
        }

        return finalMesh;
    }
}
