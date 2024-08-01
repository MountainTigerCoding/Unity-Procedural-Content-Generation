using UnityEngine;
using Runtime.PCG;

using UnityEditor;
using Editors.Shared;

namespace Editors.PCG
{
    [CustomEditor(typeof(PCGBiome))]
    public sealed class PCGBiomeEditor : Editor
    {
    #region Field
        private SerializedObject _serializedObject;
        private PCGBiome _biome;

        private SerializedProperty _boundsSizeProperty;
        private SerializedProperty _seedProperty;
        private Vector2 _volumesScrollPosition;
    #endregion Field

        private void OnEnable ()
        {
            _biome = (PCGBiome)target;
            _serializedObject = serializedObject;

            _boundsSizeProperty = _serializedObject.FindProperty("Bounds.Size");
            _seedProperty = _serializedObject.FindProperty("Seed");
            _volumesScrollPosition = Vector2.zero;
        }

        public override void OnInspectorGUI ()
        {
            EditorGUIUtils.PaintField(_boundsSizeProperty, "Bounds");
            EditorGUIUtils.PaintField(_seedProperty, "Seed");
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Find")) _biome.FindVolumes();
                if (_biome.HasVolumes) {
                    if (GUILayout.Button("Place Instant")) _biome.Place(false);
                    if (GUILayout.Button("Reset")) _biome.ResetPlaced();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (_biome.HasVolumes) EditorGUIUtils.DrawDisabledObjectArray(_biome.Volumes, ref _volumesScrollPosition);

            _serializedObject.ApplyModifiedProperties();
        }
    }
}