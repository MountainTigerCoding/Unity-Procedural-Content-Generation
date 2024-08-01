using System;
using UnityEngine;
using Unity.Mathematics;
using Runtime.Shared;

namespace Runtime.PCG
{
    [CreateAssetMenu(fileName = "PCG_Entity", menuName = "PCG/Entity")]
    public sealed class PCGEntity : ScriptableObject
    {
    #region Fields
        [SerializeField] private GameObject _prefab;
        [SerializeField, MinMaxSlider(0.05f, 5)] private Vector2 _scaleRange = new(0.05f, 5);
        [SerializeField] private Vector3 _rotationOffset = new(90, 0, 0);
        [SerializeField] private bool _randomRotationX = false;
        [SerializeField] private bool _randomRotationZ = false;
        [SerializeField, Range(-5, 5)] private float _surfaceOffset = 0;

        //[Header("Height Range")]
        [SerializeField, MinMaxSlider(-100, 1000), Tooltip("The minimium and maximium altitude in units")] private Vector2 _altitudeRange = new(-100, 1000);
        [SerializeField] private float _altitudeRangeJitter = 0;

        //[Header("Slope")]
        [SerializeField, MinMaxSlider(0, 90), Tooltip("The minimium and maximium slope in degree")] private Vector2 _slopeAngleRange = new(0, 90);
        [SerializeField] private float _maxSlopeAngleJitter = 0;
        [SerializeField, Range(0, 1)] private float _slopeAlign = 1;
        [SerializeField] private bool _slopeAlignSecondaryMultiSample = false;

        //[Header("Proximity Masking")]
        [SerializeField] private bool _useMask = false;
        [SerializeField] private LayerMask _nearMask;
        [SerializeField] private float _nearRadius = 0.1f;
        [SerializeField] private LayerMask _excludeMask;
        [SerializeField] private float _excludeRadius = 0.1f;
    #endregion Fields

    #region Properties
        public GameObject GameObject { get => _prefab; }
        public Vector3 Scale
        {
            get
            {
                float averageScale = (_prefab.transform.localScale.x + _prefab.transform.localScale.y + _prefab.transform.localScale.z) / 3 ;
                return new(averageScale, averageScale, averageScale);
            }
        }
        public Vector3 RotationOffset { get => _rotationOffset; }
        public bool RandomRotationX { get => _randomRotationX; }
        public bool RandomRotationZ { get => _randomRotationZ; }

        public bool UseMask { get => _useMask; }
    #endregion Properties

        public bool IsPointValid (PCGPlacementPoint point, ref PCGVolumeDebugContainer debugContainer)
        {
            // Altitude
            Vector3 altitudePosition = point.Position + Vector3.one * UnityEngine.Random.Range(-_altitudeRangeJitter, _altitudeRangeJitter);
            bool inAltitudeRange = (altitudePosition.y > _altitudeRange.x) && (altitudePosition.y < _altitudeRange.y);

            // Slope
            float surfaceAngle = 180f * math.remap
            (
                -1f, 1f,
                1f, 0f,
                math.dot(point.Normal, Vector3.up)
            );

            surfaceAngle += UnityEngine.Random.Range(-_maxSlopeAngleJitter, _maxSlopeAngleJitter);
            bool inSlopeRange = (surfaceAngle >= _slopeAngleRange.x) && (surfaceAngle < _slopeAngleRange.y);

            if (!inAltitudeRange) debugContainer.OnEntityOutAltitudeRange();
            if (!inSlopeRange) debugContainer.OnEntityOutOfSlopeRange();
            return inAltitudeRange && inSlopeRange;
        }

    #region Position
        public Vector3 GetPosition (PCGPlacementPoint point)
        {
            return point.Position + point.Normal * _surfaceOffset;
        }

        public bool PositionValid (Vector3 position, float scaleScalar)
        {
            if (!_useMask) return true;
            bool isNearIncluder = GetPointFilterResult(position, _nearRadius * scaleScalar, _nearMask);
            bool inNearExluder = GetPointFilterResult(position, _excludeRadius * scaleScalar, _excludeMask);
            return isNearIncluder && !inNearExluder;

            static bool GetPointFilterResult (Vector3 originWS, float radius, LayerMask layerMask)
            {
                return Physics.CheckSphere(originWS, radius, layerMask);
            }
        }
    #endregion Position

    #region Rotation
        public Quaternion GetRotation (Transform entity, Vector3 normal, LayerMask groundMask)
        {
            entity.rotation = NormalToRotation(normal);

            if (_slopeAlignSecondaryMultiSample) {
                normal = GenerateMultiSampleNormal(GetBounds(entity), groundMask);
                return NormalToRotation(normal);
            } else {
                return entity.rotation;
            }
        }

        private Quaternion NormalToRotation (Vector3 normal)
        {
            return Quaternion.Slerp
            (
                Quaternion.identity,
                Quaternion.Euler
                (
                    Quaternion.LookRotation(normal).eulerAngles + new Vector3(90f, 0f, 0f)) * Quaternion.Euler(Vector3.up * UnityEngine.Random.Range(0f, 360f)
                ),
                _slopeAlign
            );
        }

        private Vector3 GenerateMultiSampleNormal (Bounds bounds, LayerMask whatIsGround)
        {
            // Each element is a bottom corner of the bounding box
            Vector3[] samplePoints = new Vector3[4];

            Vector3 extents = bounds.extents * 0.5f;
            samplePoints[0] = bounds.center - new Vector3(extents.x, extents.y, extents.z);
            samplePoints[1] = bounds.center + new Vector3(-extents.x, extents.y, extents.z);
            samplePoints[2] = bounds.center + new Vector3(extents.x, extents.y, -extents.z);
            samplePoints[3] = bounds.center - new Vector3(-extents.x, extents.y, -extents.z);

            Vector3 averagePosition = Vector3.zero;
            Vector3 averageNormal = Vector3.zero;

            int i = 0;
            foreach (Vector3 origin in samplePoints)
            {
                Ray ray = new(origin + new Vector3(0f, 5f, 0f), Vector3.down);
                RaycastingUtils.RaycastNonAllocClosestDistance(5, ray, 50, whatIsGround, out RaycastHit result);

                //Debug.DrawLine(ray.origin, result.point, Color.green);
                averagePosition += result.point;
                averageNormal += result.normal;

                i++;
            }

            return averageNormal / samplePoints.Length;
        }
    #endregion Rotation

    #region Scale
        public Vector3 GetScale (out float scalar)
        {
            scalar = UnityEngine.Random.Range(_scaleRange.x, _scaleRange.y);
            return new Vector3(scalar, scalar, scalar);
        }
    #endregion Scale

        private Bounds GetBounds (Transform entity)
        {
            entity.TryGetComponent(out MeshRenderer meshRenderer);

            // Get MeshRenderer on the first child. There should be one here if its not on the base GameObject
            if (meshRenderer == null) entity.GetChild(0).TryGetComponent(out meshRenderer);
            if (meshRenderer == null) return new(Vector3.zero, Vector3.one);
            return meshRenderer.bounds;
        }
    }
}