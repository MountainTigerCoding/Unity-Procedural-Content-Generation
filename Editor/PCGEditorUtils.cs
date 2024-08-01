using UnityEngine;
using Runtime.PCG;
using Runtime.ProceduralSampling;

using UnityEditor;
using Editors.Shared;
using Editors.ProceduralSampling;

namespace Editors.PCG
{
    internal static class PCGEditorUtils
    {
        public static void PaintPoissonDiskSettingsSection (SerializedObject serializedObject, string path, ref PoissonDiscSampler.Settings sampler)
        {
            EditorGUI.indentLevel++;
            EditorGUIUtils.PaintField(serializedObject, path + ".Radius", "Radius");
            EditorGUIUtils.PaintField(serializedObject, path + ".SampleNumBeforeRejection", "Samples Before Rejection");
            EditorGUIUtils.PaintField(serializedObject, path + ".LimitPoints", "Limit Points");
            EditorGUI.indentLevel--;
            ProcedualSamplingEditorUtils.PaintNoiseSettings(serializedObject, path + ".RadiiInfluencingNoise", "Radii Influencing Noise");
        }

        public static void PaintGridSettingsSection (SerializedObject serializedObject)
        {
            EditorGUIUtils.PaintField(serializedObject, "GridSamplerSettings.CellSize", "Cell Size");
        }

        public static void PaintDebugSection (PCGVolumeDebugContainer debugContainer, bool generationNotAllowed)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUIUtils.Header("Point Generation");
            EditorGUILayout.FloatField("Points Generated", debugContainer.PointsGenerated);

            EditorGUIUtils.Header("Entity Validation");
            EditorGUILayout.FloatField("Beyond Altitude Range", debugContainer.EntitiesOutAltitudeRange);
            EditorGUILayout.FloatField("Beyond Slope Range", debugContainer.EntitiesOutOfSlopeRange);
            EditorGUILayout.FloatField("Beyond Volume Height", debugContainer.EntitiesOutsideVolumeHeightRange);

            EditorGUIUtils.Header("Spawning");
            EditorGUILayout.FloatField("Spawned", debugContainer.ObjectsSpawned);
            EditorGUILayout.TextField(new GUIContent("Points Used", "The percentage of points generated and used to spawn objects"), System.Convert.ToString(debugContainer.PointsUsedPercentage) + '%');
            EditorGUI.EndDisabledGroup();

            // Informtion Boxes
            if (generationNotAllowed) EditorGUILayout.HelpBox("Generation is not allowed as another process is running", MessageType.Info);
            PaintWarnings();

            void PaintWarnings ()
            {
                if (debugContainer.Warnings == null) return;
                if (debugContainer.Warnings.Length == 0) return;
                foreach (string message in debugContainer.Warnings) EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }
    }
}