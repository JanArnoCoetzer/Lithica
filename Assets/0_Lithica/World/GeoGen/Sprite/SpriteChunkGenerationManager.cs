using UnityEngine;

public class SpriteChunkGenerationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldManager world;

    private void Awake()
    {
        ResolveReferences();
    }

    public void GenerateNow()
    {
        ResolveReferences();

        if (world == null)
        {
            Debug.LogWarning("SpriteChunkGenerationManager: No WorldManager found.", this);
            return;
        }
    }

    private void ResolveReferences()
    {
        if (world == null)
            world = GetComponent<WorldManager>();
    }
}