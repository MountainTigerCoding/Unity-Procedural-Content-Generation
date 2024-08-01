using UnityEngine;
using Runtime.Shared;

namespace Runtime.PCG
{
    [ExecuteAlways]
    [AddComponentMenu("PCG/Raycast Object Placer")]
    public sealed class RaycastObjectPlacer : ObjectPlacer
    {
    #region Fields
        public bool AlwaysUpdate = false;
        public bool AutoUpdate = false;
        public LayerMask LayerMask;

        [Header("Rotation")]
        [Range(0, 1)] public float MatchTerrainNormal = 0f;
        public bool RandomRotate = false;
        public Vector3 RotationOffset = new(90f, 0f, 0f);
    #endregion

        public override void Place ()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Ray ray = new(child.position + Vector3.up * 80f, Vector3.down);

                if (Physics.Raycast(ray, out RaycastHit impact, 500f, LayerMask, QueryTriggerInteraction.Collide)) {
                    child.position = impact.point;

                    if (MatchTerrainNormal > 0f) {
                        child.rotation = Quaternion.Slerp
                        (
                            Quaternion.Euler(0f, child.rotation.y, 0f),
                            Quaternion.Euler(Quaternion.LookRotation(impact.normal).eulerAngles + RotationOffset),
                            MatchTerrainNormal
                        );
                    }
                }
            }
        }
    }
}