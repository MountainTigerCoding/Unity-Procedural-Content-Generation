// Taken from: https://github.com/SebLague/Object-Placement-with-Physics/blob/master/Assets/PhysicsSimulation.cs
using UnityEngine;

namespace Runtime.PCG
{
    public readonly struct PhysicsSimulatedBody
    {
    #region Fields
        public readonly Rigidbody rigidbody;
        public readonly bool isChild;
        readonly Vector3 originalPosition;
        readonly Quaternion originalRotation;
        readonly Transform transform;
    #endregion

        public PhysicsSimulatedBody (Rigidbody rigidbody, bool isChild)
        {
            this.rigidbody = rigidbody;
            this.isChild = isChild;
            transform = rigidbody.transform;
            originalPosition = rigidbody.position;
            originalRotation = rigidbody.rotation;
        }

        public readonly void Reset ()
        {
            transform.SetPositionAndRotation(originalPosition, originalRotation);

            if (rigidbody != null && !rigidbody.isKinematic) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}