using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGenerationManager))]
public class TerrainGenerationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerationManager manager = (TerrainGenerationManager)target;

        serializedObject.Update();

        SerializedProperty isoLevelProp = serializedObject.FindProperty("isoLevel");
        SerializedProperty clampProp = serializedObject.FindProperty("clampFinal01");
        SerializedProperty filtersProp = serializedObject.FindProperty("filters");

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(isoLevelProp);
        EditorGUILayout.PropertyField(clampProp);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);

        for (int i = 0; i < filtersProp.arraySize; i++)
        {
            SerializedProperty element = filtersProp.GetArrayElementAtIndex(i);
            TerrainFilter filter = element.managedReferenceValue as TerrainFilter;

            EditorGUILayout.BeginVertical("box");

            string title = filter != null ? filter.displayName : $"Filter {i}";
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, title, true);

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(element, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Up") && i > 0)
            {
                filtersProp.MoveArrayElement(i, i - 1);
                break;
            }

            if (GUILayout.Button("Down") && i < filtersProp.arraySize - 1)
            {
                filtersProp.MoveArrayElement(i, i + 1);
                break;
            }

            if (GUILayout.Button("Remove"))
            {
                filtersProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Add Filter", EditorStyles.boldLabel);

        if (GUILayout.Button("Base Terrain"))
            AddFilter(filtersProp, new BaseTerrainFilter());

        if (GUILayout.Button("Cave"))
            AddFilter(filtersProp, new CaveFilter());

        if (GUILayout.Button("Add Noise"))
            AddFilter(filtersProp, new AddNoiseFilter());

        if (GUILayout.Button("Smooth"))
            AddFilter(filtersProp, new SmoothFilter());

        if (GUILayout.Button("Multiply"))
            AddFilter(filtersProp, new MultiplyFilter());

        if (GUILayout.Button("Clamp"))
            AddFilter(filtersProp, new ClampFilter());

        if (GUILayout.Button("Terrace"))
            AddFilter(filtersProp, new TerraceFilter());

        if (GUILayout.Button("Remap Curve"))
            AddFilter(filtersProp, new RemapCurveFilter());

        if (GUILayout.Button("Height Mask"))
            AddFilter(filtersProp, new HeightMaskFilter());

        if (GUILayout.Button("Clear All Filters"))
            filtersProp.ClearArray();

        bool changed = EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();

        if (changed)
        {
            EditorUtility.SetDirty(manager);
            manager.NotifyChangedFromEditor();
        }
    }

    private void AddFilter(SerializedProperty filtersProp, TerrainFilter filter)
    {
        int index = filtersProp.arraySize;
        filtersProp.InsertArrayElementAtIndex(index);

        SerializedProperty element = filtersProp.GetArrayElementAtIndex(index);
        element.managedReferenceValue = filter;
    }
}