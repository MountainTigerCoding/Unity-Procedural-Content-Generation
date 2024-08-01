using System.Collections.Generic;
using UnityEngine;
using Runtime.Shared;
using Runtime.ProceduralSampling;

namespace Runtime.PCG
{
    [AddComponentMenu("PCG/PCG Biome")]
    public sealed class PCGBiome : ObjectPlacer
    {
    #region Fields
        public ProcedualBounds Bounds = new(50, 50, 50);
        public int Seed = 0;
        public PCGVolume[] Volumes { private set; get; }
    #endregion

    #region Properties
        /// <summary>
        /// Returns true if any volumes have been found
        /// </summary>
        public bool HasVolumes 
        {
            get {
                if (Volumes == null) return false;
                return Volumes.Length > 0;
            }
        }
    #endregion Properties

#if UNITY_EDITOR
        private void OnValidate ()
        {
            if (UnityEditor.EditorUtility.IsPersistent(this)) return;
            UpdateVolumeSettings();
        }
#endif

        public void UpdateVolumeSettings ()
        {
            if (!HasVolumes) return;
            int i = 0;
            foreach (PCGVolume volume in Volumes)
            {
                volume.Bounds = Bounds;
                volume.Seed = Seed + i;

                volume.transform.localPosition = Vector3.zero;
                volume.transform.localEulerAngles = Vector3.zero;
                volume.transform.localScale = Vector3.one;
                i++;
            }
        }

        [ContextMenu("Find")]
        public void FindVolumes ()
        {
            Volumes = Utils.TryGetMassComponents<PCGVolume>(Utils.GetChildren(transform));
            UpdateVolumeSettings();
        }

        [ContextMenu("Place")]
        public override List<PCGPlacementPoint> Place (bool justReturnPoints)
        {
            transform.eulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
            if (!HasVolumes) return null;

            UpdateVolumeSettings();
            foreach (PCGVolume volume in Volumes) volume.Place(false);
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Reset")]
        public override void ResetPlaced ()
        {
            if (!HasVolumes) return;
            foreach (PCGVolume volume in Volumes) volume.ResetPlaced();  
        }
#endif
    }
}