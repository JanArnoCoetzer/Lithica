using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryGridController))]
public class InventoryGridControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Inventory Preview"))
        {
            var controller = (InventoryGridController)target;
            controller.GenerateInventoryPreview();

            // Mark scene dirty so changes are saved
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(controller);
                if (controller.gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }
    }
}