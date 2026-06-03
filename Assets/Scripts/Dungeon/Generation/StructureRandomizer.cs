using UnityEngine;

public class StructureRandomizer : MonoBehaviour
{
    [Header("Structures")]
    public GameObject[] structurePrefab;

    public GameObject GetRandomStructure()
    {
        if (structurePrefab == null || structurePrefab.Length == 0)
            return null;

        return structurePrefab[Random.Range(0, structurePrefab.Length)];
    }
}
