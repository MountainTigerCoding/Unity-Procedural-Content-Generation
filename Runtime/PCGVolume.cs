using System.Collections.Generic;
using UnityEngine;
using Runtime.Shared;
using Runtime.ProceduralSampling;

namespace Runtime.PCG
{
    [AddComponentMenu("PCG/PCG Volume")]
    public sealed class PCGVolume : ObjectPlacer
    {
        public enum PointSamplingMethod : int
        {
            Poisson = 0,
            Grid    = 1,
        }

    #region Fields
        // Main
        public int Seed = 0;
        public ProcedualBounds Bounds = new(20, 20, 20);
        public LayerMask WhatIsGround;

#if UNITY_EDITOR
        // Generation Triggers
        [SerializeField] private bool _generateOnPropertyChange = false;
        [SerializeField] private bool _generateOnMove = false;
#endif

        // Point Generation
        [InspectorName("Sampling Method")] public PointSamplingMethod Method = PointSamplingMethod.Poisson;
        [InspectorName("Poisson Settings")] public PoissonDiscSampler.Settings PoissonSamplerSettings;
        [InspectorName("Grid Settings")] public GridSampler.Settings GridSamplerSettings = new();

        // Point Testing
        [InspectorName("Threshold Noise")] public NoiseSettings ThresholdNoise = new
        (
            Space.Self, NoiseSettings.SamplingAlgorithm.Perlin, new(0, 1), 0.2f, 1
        );

        // Spawning
        [Tooltip("Game Objects which will be generated")] public PCGEntity[] Entities;

        // Debug
        private bool _isSpawningObjects = false;
#if UNITY_EDITOR
        [SerializeField, InspectorName("Debug")] private PCGVolumeDebugContainer _debugContainer;
#endif
    #endregion Fields

    #region Properties
        public bool IsSpawningObjects { get => _isSpawningObjects; }
#if UNITY_EDITOR
        public bool GenerateOnPropertyChange { get => _generateOnPropertyChange; }
        public PCGVolumeDebugContainer DebugContainer { get => _debugContainer; }
#endif
    #endregion Properties

#if UNITY_EDITOR
        private void OnDrawGizmos ()
        {
            Bounds.DrawRegionGizmo(transform);
        }
#endif

        private void Start ()
        {
            TerminateActiveGeneration();
        }

        [ContextMenu("Terminate Generation")]
        public void TerminateActiveGeneration ()
        {
            StopAllCoroutines();
            _isSpawningObjects = false;
        }

        /// <summary>
        /// Destroys all children
        /// </summary>
        public override void ResetPlaced ()
        {
            TerminateActiveGeneration();
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++) DestroyImmediate(transform.GetChild(0).gameObject, true);
        }

#if UNITY_EDITOR
        private void OnUpdateEditor ()
        {
            if (transform == null) UnityAPICallDistributer.Instance.OnUpdateEditor.Unregister(OnUpdateEditor);
            if (UnityEditor.EditorUtility.IsPersistent(this)) return;

            if (transform.hasChanged) {
                transform.hasChanged = false;
                Place(false);
            }
        }
#endif

        public override List<PCGPlacementPoint> Place (bool justReturnPoints)
        {
            if (_isSpawningObjects) return null;
            if (Entities == null) return null;
            if (Entities.Length == 0) return null;

            // Pre Init
            transform.eulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
            _debugContainer = new();
            ResetPlaced();
            UnityEngine.Random.InitState(Seed);

            // Point Generation
            List<ProceduralPoint> rawPoints = GeneratePoints();
            if (rawPoints == null || rawPoints.Count == 0) return null;
            _debugContainer.OnPointsGenerated(rawPoints.Count);

            // Point Testing - Ground placement
            List<PCGPlacementPoint> points = PlacePointsOnGround(ref rawPoints, Bounds.GetOffset(transform.position));
            // Placement points are now in world space

            // Spawning
            if (!justReturnPoints) SpawnEntities(ref points);
            return points;
        }

    #region Point Generation
        private List<ProceduralPoint> GeneratePoints ()
        {
            switch (Method)
            {
                default:
                case PointSamplingMethod.Poisson:
                    PoissonSamplerSettings.OnValidate();
                    if (Mathf.Approximately(PoissonSamplerSettings.Radius, PoissonDiscSampler.Settings.MinRadius)) return null;
                    return PoissonDiscSampler.GeneratePoints(Seed, PoissonSamplerSettings, Bounds.GetOffset(transform), Bounds);

                case PointSamplingMethod.Grid:
                    GridSamplerSettings.OnValidate();
                    return GridSampler.GeneratePoints(GridSamplerSettings, Bounds);
            };
        }
    #endregion Point Generation

    #region Point Testing
        private List<PCGPlacementPoint> PlacePointsOnGround (ref List<ProceduralPoint> rawPoints, Vector3 offsetWS)
        {
            Vector2 offset = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value) * 2000;
            List<PCGPlacementPoint> points = new();
            foreach (ProceduralPoint rawPoint in rawPoints)
            {
                // Cutout/Threshold noise
                if (!ThresholdNoise.Sample2D(new Vector2(rawPoint.Position.x, rawPoint.Position.z) + offset, offsetWS, out float thresholdNoise)) continue;

                // Ground placement
                Ray ray = new(rawPoint.Position + offsetWS + (Vector3.up * Bounds.Size.y), Vector3.down);
                if (RaycastingUtils.RaycastNonAllocClosestDistance(5, ray, Bounds.Size.y, WhatIsGround, out RaycastHit result)) {
                    points.Add(new(result.point, result.normal, thresholdNoise));
                }
            }
            return points;
        }
    #endregion Point Testing

    #region Spawning At Points
        private void SpawnEntities (ref List<PCGPlacementPoint> points)
        {
            OnBeginSpawn();
            foreach (PCGPlacementPoint point in points) SpawnEntityAt(point);
            OnFinishSpawn();
        }

        public void OnBeginSpawn ()
        {
            _isSpawningObjects = true;
        }

        public void OnFinishSpawn ()
        {
            _isSpawningObjects = false;
            _debugContainer.OnAllSpawned();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void SpawnEntityAt (PCGPlacementPoint point)
        {
            // returning will discard the entity

        #region Spawning Terminators
            if (!Bounds.IsInsideVertical(transform.InverseTransformPoint(point.Position).y)) {
                // Is in height range of volume
                _debugContainer.OnEntityOutsideVolumeHeightRange();
                return;
            }

            // Find entities that are suitible for the terrain
            PCGEntity[] entitySelection = GetValidEntities(point);
            if (entitySelection.Length == 0) {
                return;
            }

            // Choose a random entity from the suitible selection
            PCGEntity selectedEntity = entitySelection[UnityEngine.Random.Range(0, entitySelection.Length)];
            if (selectedEntity == null) {
                Debug.LogError("Placement Entity is null in array!");
                return;
            }

#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(selectedEntity.GameObject)) {
                Debug.LogError("Entity '" + selectedEntity.GameObject.name + "' is not connected to a prefab and cannot be spawned!");
                return;
            }
#endif
        #endregion Spawning Terminators

        #region Object - Pre Setup
            Vector3 position = selectedEntity.GetPosition(point);
            Vector3 scale = selectedEntity.GetScale(out float scaleScalar) * point.ThresholdNoise;
            if(!selectedEntity.PositionValid(position, scaleScalar)) return;
        #endregion

        #region Object - Spawning
#if UNITY_EDITOR
            Transform spawnedObject = ((GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(selectedEntity.GameObject)).transform;
#else
            Transform spawnedObject = GameObject.Instantiate(selectedEntity.GameObject).transform;
#endif
            _debugContainer.OnObjectSpawned();
        #endregion

        #region Object - Transform Setup
            spawnedObject.SetParent(transform);
#pragma warning disable UNT0022 // Inefficient position/rotation assignment
            spawnedObject.position = position;
#pragma warning restore UNT0022 // Inefficient position/rotation assignment
            spawnedObject.rotation = selectedEntity.GetRotation(spawnedObject.transform, point.Normal, WhatIsGround);
            spawnedObject.localScale = scale;
        #endregion

        #region Object - Post Setup
            Vector3 eulerAngles = spawnedObject.localEulerAngles;
            eulerAngles += selectedEntity.RotationOffset;
            if (selectedEntity.RandomRotationX) eulerAngles.x += UnityEngine.Random.Range(0f, 360f);
            if (selectedEntity.RandomRotationZ) eulerAngles.z += UnityEngine.Random.Range(0f, 360f);
            spawnedObject.localRotation = Quaternion.Euler(eulerAngles);
        #endregion
        }

        private PCGEntity[] GetValidEntities (PCGPlacementPoint point)
        {
            if (Entities == null) return new PCGEntity[0];
            if (Entities.Length == 0) return new PCGEntity[0];

            List<PCGEntity> validPlacementEntities = new();
            foreach (PCGEntity entity in Entities)
            {
                if (entity == null) continue;
                if (entity.IsPointValid(point, ref _debugContainer)) validPlacementEntities.Add(entity);
            }

            return validPlacementEntities.ToArray();
        }
    #endregion Spawning At Points
    }
}