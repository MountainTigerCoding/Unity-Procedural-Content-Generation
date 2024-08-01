using Runtime.PCG;

using UnityEditor;
using Editors.Shared;
using UnityEngine;

namespace Editors.PCG
{
    [CustomEditor(typeof(PCGEntity))]
    internal sealed class PCGEntityEditor : Editor
    {
    #region Fields
        private PCGEntity _target;
        private SerializedObject _serializedObject;

        private bool _paintProximityMaskSettings;
        private bool _paintSlopeSettings;

        private bool _forceRegeneration;
        private PCGVolume _associatedVolume;
    #endregion

        public override void OnInspectorGUI()
        {
            _target = (PCGEntity)target;
            _serializedObject = serializedObject;
            _forceRegeneration = false;

            PaintMainSettings();
            if (_target.GameObject == null) {
                EditorGUILayout.HelpBox("Choose an prefab to continue", MessageType.Info);
                OnTerminate();
                return;
            } 

            EditorGUIUtils.FoldoutHeaderGroup("Slope", ref _paintSlopeSettings, PaintSlopeSettings);
            EditorGUIUtils.FoldoutHeaderGroup("Proximity Mask", ref _paintProximityMaskSettings, PaintProximityMask);
            _associatedVolume = (PCGVolume)EditorGUILayout.ObjectField("Associated Volume", _associatedVolume, typeof(PCGVolume), true);
            OnTerminate();

            void OnTerminate ()
            {
                if (_serializedObject.ApplyModifiedProperties()) _forceRegeneration = true;
                if (_forceRegeneration && _associatedVolume != null) _associatedVolume.Place();
            }
        }

        private void PaintMainSettings ()
        {
            EditorGUIUtils.PaintField(_serializedObject, "_prefab", "Prefab");
            if (_target.GameObject == null) return;

            EditorGUIUtils.PaintField(_serializedObject, "_altitudeRange", "Altitude");
            EditorGUIUtils.PaintField(_serializedObject, "_scaleRange", "Scale");
            EditorGUIUtils.PaintField(_serializedObject, "_altitudeRangeJitter", "Altitude Jitter");
            EditorGUIUtils.PaintField(_serializedObject, "_rotationOffset", "Rotation Offset");

            // Random rotation
            //EditorGUILayout.BeginHorizontal();
            EditorGUIUtils.PaintField(_serializedObject, "_randomRotationX", "X");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Y", true);
            EditorGUI.EndDisabledGroup();
            EditorGUIUtils.PaintField(_serializedObject, "_randomRotationZ", "Z");
            //EditorGUILayout.EndHorizontal();

            EditorGUIUtils.PaintField(_serializedObject, "_surfaceOffset", "Surface Offset");
        }

        private void PaintSlopeSettings ()
        {
            EditorGUIUtils.PaintField(_serializedObject, "_slopeAngleRange", "Slope Range");
            EditorGUIUtils.PaintField(_serializedObject, "_maxSlopeAngleJitter", "Slope Range Jitter");
            EditorGUIUtils.PaintField(_serializedObject, "_slopeAlign", "Slope Align");
            EditorGUIUtils.PaintField(_serializedObject, "_slopeAlignSecondaryMultiSample", "Multi Sample Alignment Normal");
        }

        private void PaintProximityMask ()
        {
            EditorGUIUtils.PaintField(_serializedObject, "_useMask", "Use Mask");

            EditorGUI.BeginDisabledGroup(!_target.UseMask);
            EditorGUIUtils.PaintField(_serializedObject, "_nearMask", "Near Mask");
            EditorGUIUtils.PaintField(_serializedObject, "_nearRadius", "Near Radius");
            EditorGUIUtils.PaintField(_serializedObject, "_excludeMask", "Exclude Mask");
            EditorGUIUtils.PaintField(_serializedObject, "_excludeRadius", "Exclude Radius");
            EditorGUI.EndDisabledGroup();
        }
    }
}