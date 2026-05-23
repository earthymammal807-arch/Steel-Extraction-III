using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BulkLODSetup : EditorWindow
{
    


    [MenuItem("Tools/Bulk Setup Blender Terrain LODs")]
    public static void SetupLODs()
    {
        GameObject selectedParent = Selection.activeGameObject;


        string path = "Assets/Visual/Materials/Materials/VoronoiBrownianMask 1.mat";
        Material terrainMat = AssetDatabase.LoadAssetAtPath<Material>(path);


        if (selectedParent == null)
        {
            Debug.LogError("Please select the parent Terrain FBX object in the Hierarchy first!");
            return;
        }

        MeshRenderer[] allRenderers = selectedParent.GetComponentsInChildren<MeshRenderer>(true); //gets all objectrs that render a 3d mesh.

        // Key: The chunk number (e.g., "001"), Value: The 3 meshes belonging to it
        Dictionary<string, List<MeshRenderer>> chunkGroups = new Dictionary<string, List<MeshRenderer>>(); //just a hashmap

        // 1. Group the meshes by their dot suffix extension number
        foreach (MeshRenderer mr in allRenderers)
        {
            string name = mr.gameObject.name;
            int dotIdx = name.LastIndexOf(".");

            if (dotIdx == -1) continue; // Skip if it doesn't have a ".001" suffix

            // Extract the chunk number string (e.g., "001")
            string chunkNumber = name.Substring(dotIdx + 1);

            if (!chunkGroups.ContainsKey(chunkNumber))
            {
                chunkGroups[chunkNumber] = new List<MeshRenderer>();
            }
            chunkGroups[chunkNumber].Add(mr);
        }

        // 2. Create the LOD Group parents and assign the children
        int createdCount = 0;
        foreach (var pair in chunkGroups)
        {
            string chunkID = pair.Key;

            // Create a unique parent for this specific chunk number
            GameObject chunkParent = new GameObject("Terrain_Chunk_" + chunkID);

            // Match the parent transform position to the container asset
            chunkParent.transform.position = selectedParent.transform.position;
            chunkParent.transform.parent = selectedParent.transform.parent;

            LODGroup lodGroup = chunkParent.AddComponent<LODGroup>();

            // Sort the meshes so HighPoly goes first, Mid second, Low third
            List<MeshRenderer> chunkMeshes = pair.Value;
            chunkMeshes.Sort((a, b) => {
                int scoreA = GetLODOrderScore(a.name);
                int scoreB = GetLODOrderScore(b.name);
                return scoreA.CompareTo(scoreB);
            });

            List<LOD> unityLODs = new List<LOD>();

            // 3. Map sorted meshes into Unity LOD layers
            for (int i = 0; i < chunkMeshes.Count; i++)
            {
                MeshRenderer mr = chunkMeshes[i];
                mr.gameObject.transform.parent = chunkParent.transform; // Parent it


                float screenHeight = 0.7f;       // LOD 0 active from 100% down to 70% screen height
                if (i == 1) screenHeight = 0.45f; // LOD 1 active from 70% down to 45% screen height
                if (i == 2) screenHeight = 0.25f; // LOD 2 active from 45% down to 25% screen height
                if (i == 3) screenHeight = 0.10f; // SO ON 
                if (i == 4) screenHeight = 0.02f;

                LOD lod = new LOD(screenHeight, new Renderer[] { mr });
                unityLODs.Add(lod);
                if (terrainMat != null)
                {
                    mr.material = terrainMat;
                }
            }

            lodGroup.SetLODs(unityLODs.ToArray());

            // Critical step: Forces the purple bounding box to snap tightly to just this chunk!
            lodGroup.RecalculateBounds();

            createdCount++;
        }

        Debug.Log($"Successfully built {createdCount} chunk parents with isolated LOD Groups!");
    }

    // Helper method to prioritize detail levels
    private static int GetLODOrderScore(string name)
    {
        string lowerName = name.ToLower();
        if (lowerName.Contains("high")) return 0; // LOD 0
        if (lowerName.Contains("mid-high")) return 1;  // LOD 1
        if (lowerName.Contains("mid")) return 2;    // LOD 2
        if (lowerName.Contains("low")) return 3;    //SO ON
        if (lowerName.Contains("insane")) return 4;  
        return 5;                                  // Anything else fallback
    }
}
