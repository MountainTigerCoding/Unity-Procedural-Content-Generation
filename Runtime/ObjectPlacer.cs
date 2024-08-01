using System.Collections.Generic;
using UnityEngine;

namespace Runtime.PCG
{
    [ExecuteInEditMode()]
    [DisallowMultipleComponent()]
    [AddComponentMenu("PCG/Object Placer")]
    public class ObjectPlacer : MonoBehaviour
    {
        public virtual void Place () => Place(false);
        public virtual List<PCGPlacementPoint> Place (bool justReturnPoints) => null;
        public virtual void ResetPlaced () { }
    }
}