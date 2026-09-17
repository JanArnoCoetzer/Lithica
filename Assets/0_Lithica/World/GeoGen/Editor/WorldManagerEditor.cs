using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);

        WorldManager world = (WorldManager)target;

        if (GUILayout.Button("Generate Debug Mesh"))
        {
            Undo.RecordObject(world, "Generate Debug Mesh");
            world.GenerateDebugMesh();
        }

        if (GUILayout.Button("Clear Debug Mesh"))
        {
            Undo.RecordObject(world, "Clear Debug Mesh");
            world.ClearDebugMesh();
        }

        if (GUILayout.Button("Generate Sprites"))
        {
            Undo.RecordObject(world, "Generate Sprites");
            world.GenerateSprites();
        }

        if (GUILayout.Button("Clear Sprites"))
        {
            Undo.RecordObject(world, "Clear Sprites");
            world.ClearSprites();
        }

        if (GUILayout.Button("Clear All Generated"))
        {
            Undo.RecordObject(world, "Clear All Generated");
            world.ClearAllGenerated();
        }
    }
}