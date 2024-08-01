using UnityEngine;

namespace Runtime.PCG
{
    [System.Serializable]
    public struct PCGPlacementPoint
    {
    #region Properties
        public Vector3 Position { private set; get; }
        public Vector3 Normal { private set; get; }
        public float ThresholdNoise { private set; get; }
        public readonly Quaternion NormalRotation { get => Quaternion.LookRotation(Normal); }
    #endregion Properties

        public PCGPlacementPoint (Vector3 position, Vector3 normal, float thresholdNoise)
        {
            Position = position;
            Normal = normal;
            ThresholdNoise = thresholdNoise;
        }

        public void SetPosition (Vector3 value)
        {
            Position = value;
        }

        public void SetNormal (Vector3 eulerAnglesNormalised)
        {
            Normal = eulerAnglesNormalised;
        }
    }
}