using UnityEngine;
using Runtime.Shared;

namespace Runtime.PCG
{
    [ExecuteAlways]
    [AddComponentMenu("PCG/PCG Placement Test")]
    public sealed class PCGPlacementTest : MonoBehaviour
    {
    #region Fields
        public Transform[] Entities;
        [SerializeField] private bool _multiSampleSurface = false;
        [SerializeField] private LayerMask _whatIsGroundMask;
        [SerializeField, Range(0f, 1f)] private float _slopeAlign = 0.5f;
        [SerializeField] private float _extentsMultiplier = 0.7f;
    #endregion

        private void Update ()
        {
            if (Entities == null) return;

            UnityEngine.Random.InitState(0);

            foreach (Transform entity in Entities)
            {
                if (entity == null) continue;
                entity.rotation = GetRotation(entity);
            }
        }

        [ContextMenu("Use Children")]
        private void UseChildren ()
        {
            Entities = Utils.GetChildren(transform);
        }

        public Quaternion GetRotation (Transform entity)
        {
            Ray ray = new(entity.position + new Vector3(0f, 20f, 0f), Vector3.down);
            RaycastingUtils.RaycastNonAllocClosestDistance(10, ray, 100f, _whatIsGroundMask, out RaycastHit result);
            entity.SetPositionAndRotation(result.point, NormalToRotation(result.normal));

            if (_multiSampleSurface) {
                Vector3 normal = GenerateMultiSampleNormal(GetBounds(entity), _whatIsGroundMask);
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

        private Bounds GetBounds (Transform entity)
        {
            entity.TryGetComponent(out MeshRenderer meshRenderer);

            // Get MeshRenderer on the first child. There should be one here if its not on the base GameObject
            if (meshRenderer == null) entity.GetChild(0).TryGetComponent(out meshRenderer);

            if (meshRenderer == null) return new(Vector3.zero, Vector3.one);
            return meshRenderer.bounds;
        }

        private Vector3 GenerateMultiSampleNormal (Bounds bounds, LayerMask whatIsGround)
        {
            // Each element is a bottom corner of the bounding box
            Vector3[] samplePoints = new Vector3[4];

            Vector3 extents = bounds.extents * _extentsMultiplier;
            samplePoints[0] = bounds.center - new Vector3(extents.x, extents.y, extents.z);
            samplePoints[1] = bounds.center + new Vector3(-extents.x, extents.y, extents.z);
            samplePoints[2] = bounds.center + new Vector3(extents.x, extents.y, -extents.z);
            samplePoints[3] = bounds.center - new Vector3(-extents.x, extents.y, -extents.z);

            Vector3 averageNormal = Vector3.zero;
            Vector3[] points = new Vector3[samplePoints.Length];
            int i = 0;
            foreach (Vector3 origin in samplePoints)
            {
                Ray ray = new(origin + new Vector3(0f, 5f, 0f), Vector3.down);
                RaycastingUtils.RaycastNonAllocClosestDistance(5, ray, 50, whatIsGround, out RaycastHit result);
                Debug.DrawLine(ray.origin, result.point, Color.green);
                averageNormal += result.normal;

                points[i] = result.point;

                i++;
            }

            return averageNormal / samplePoints.Length;
        }
    }
}