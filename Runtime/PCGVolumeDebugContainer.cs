using UnityEngine;
using System.Collections.Generic;

namespace Runtime.PCG
{
    [System.Serializable]
    public class PCGVolumeDebugContainer
    {
    #region Fields
        private string[] _warnings;

        //[Header("Point Generation")]
        [SerializeField] private int _pointsGenerated;

        //[Header("Entity Validation")]
        private int _entitiesOutAltitudeRange;
        private int _entitiesOutOfSlopeRange;
        private int _entitiesOutsideVolumeHeightRange;

        //[Header("Spawning")]
        private int _objectsSpawned;
        [Tooltip("The the percentage of generated points used to spawn objects")] private float _pointsUsedPercentage;
    #endregion Fields

    #region Properties
        public string[] Warnings { get => _warnings; }
        public int PointsGenerated { get => _pointsGenerated; }

        public int EntitiesOutAltitudeRange { get => _entitiesOutAltitudeRange; }
        public int EntitiesOutOfSlopeRange { get => _entitiesOutOfSlopeRange; }
        public int EntitiesOutsideVolumeHeightRange { get => _entitiesOutsideVolumeHeightRange; }

        public int ObjectsSpawned { get => _objectsSpawned; }
        public float PointsUsedPercentage { get => _pointsUsedPercentage; }
    #endregion Properties

        public PCGVolumeDebugContainer()
        {
            _pointsGenerated = 0;

            _entitiesOutOfSlopeRange = 0;
            _entitiesOutAltitudeRange = 0;
            _entitiesOutsideVolumeHeightRange = 0;

            _objectsSpawned = 0;
        }

        // Point Generation
        public void OnPointsGenerated (int amount) => _pointsGenerated = amount;

        // Entity Validation
        public void OnEntityOutOfSlopeRange ()
        {
            _entitiesOutOfSlopeRange++;
        }

        public void OnEntityOutAltitudeRange ()
        {
            _entitiesOutAltitudeRange++;
        }
        public void OnEntityOutsideVolumeHeightRange ()
        {
            _entitiesOutsideVolumeHeightRange++;
        }

        // Spawning
        public void OnObjectSpawned ()
        {
            _objectsSpawned++;
        }

        public void OnAllSpawned ()
        {
            // float casts are not redundant and is required for the division to work
            _pointsUsedPercentage = Mathf.Round((float)_objectsSpawned / (float)_pointsGenerated * 100f);

            List<string> warnings = new();
            if (PointsUsedPercentage < 10f) warnings.Add("It is recommended not to use a PCG volume with only " + PointsUsedPercentage + "% of points being used to spawn objects.");
            _warnings = warnings.ToArray();
        }
    }
}