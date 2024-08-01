using UnityEngine;
using Runtime.PCG;

using UnityEditor;
using Editors.Shared;

namespace Editors.PCG
{
    [CustomEditor(typeof(PCGVolume))]
    internal sealed class PCGVolumeEditor : Editor
    {
    #region Fields
        private PCGVolume _volume;
        private SerializedObject _serializedObject;

        // Main
        private SerializedProperty _boundsSizeProperty;
        private SerializedProperty _seedProperty;
        private SerializedProperty _whatIsGroundProperty;

        // Generation Triggers
        private SerializedProperty _generateOnPropertyChangeProperty;
        private SerializedProperty _generateOnMoveProperty;

        // Point Generation
        private bool _pointSamplerSettingsOpen = true;
        private SerializedProperty _methodProperty;

        // Point Testing
        private SerializedProperty _thresholdNoiseProperty;

        // Entities
        private SerializedProperty _entitiesProperty;
        
        // Debug
        private bool _debugOpen = false;
    #endregion Fields

        private void OnEnable ()
        {
            _volume = (PCGVolume)target;
            _serializedObject = serializedObject;

        #region Cache Property Refs
            // Generation Triggers
            _generateOnPropertyChangeProperty = _serializedObject.FindProperty("_generateOnPropertyChange");
            _generateOnMoveProperty = _serializedObject.FindProperty("_generateOnMove");
            _generateOnPropertyChangeProperty = _serializedObject.FindProperty("_generateOnPropertyChange");

            // Main
            _boundsSizeProperty = _serializedObject.FindProperty("Bounds.Size");
            _seedProperty = _serializedObject.FindProperty("Seed");
            _whatIsGroundProperty = _serializedObject.FindProperty("WhatIsGround");

            // Point Generation
            _methodProperty = _serializedObject.FindProperty("Method");

            // Point Testing
            _thresholdNoiseProperty = _serializedObject.FindProperty("ThresholdNoise");

            // Entities
            _entitiesProperty = _serializedObject.FindProperty("Entities");
        #endregion Cache Property Refs
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            bool generationNotAllowed = _volume.IsSpawningObjects;

        #region Main
            EditorGUIUtils.PaintField(_boundsSizeProperty, "Bounds");
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUIUtils.PaintField(_seedProperty, "Seed");
                if (GUILayout.Button("Randomize")) _volume.Seed = UnityEngine.Random.Range(-10000, 10000);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUIUtils.PaintField(_whatIsGroundProperty, "What Is Ground");
        #endregion Main

        #region Generation
            // Generation Controls
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                EditorGUI.BeginDisabledGroup(_volume.GenerateOnPropertyChange);
                if (GUILayout.Button("Spawn")) _volume.Place(false);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("Reset")) _volume.ResetPlaced();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Generation Triggers");
            {
                EditorGUIUtils.PaintField(_generateOnPropertyChangeProperty, "When Property Changed");
                EditorGUIUtils.PaintField(_generateOnMoveProperty, "When Moved");
            }
            if (_volume.IsSpawningObjects) EditorGUILayout.HelpBox("Spawning objects in progress...", MessageType.Warning);
         #endregion Generation

            PaintSamplerSettings();
            PaintPointTesting();
            EditorGUILayout.PropertyField(_entitiesProperty);
            EditorGUIUtils.FoldoutHeaderGroup("Debug", ref _debugOpen, () => PCGEditorUtils.PaintDebugSection(_volume.DebugContainer, generationNotAllowed));

            serializedObject.ApplyModifiedProperties();
        }

        private void PaintSamplerSettings ()
        {
            EditorGUILayout.PropertyField(_methodProperty);
            switch (_volume.Method)
            {
                case PCGVolume.PointSamplingMethod.Grid:
                    EditorGUIUtils.FoldoutHeaderGroup
                    (
                        "Grid Settings",
                        ref _pointSamplerSettingsOpen,
                        () => PCGEditorUtils.PaintGridSettingsSection(_serializedObject)
                    );
                    break;

                default:
                case PCGVolume.PointSamplingMethod.Poisson:
                    EditorGUIUtils.FoldoutHeaderGroup
                    (
                        "Poisson Settings",
                        ref _pointSamplerSettingsOpen,
                        () => PCGEditorUtils.PaintPoissonDiskSettingsSection(_serializedObject, "PoissonSamplerSettings", ref _volume.PoissonSamplerSettings)
                    );
                    break;
            }
        }

        private void PaintPointTesting ()
        {
            EditorGUIUtils.PaintField(_thresholdNoiseProperty, "Threshold Noise");
        }
    }
}