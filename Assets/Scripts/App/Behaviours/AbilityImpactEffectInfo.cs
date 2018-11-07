using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class AbilityImpactEffectInfo : MonoBehaviour
    {
        [Range(0, 20)]
        public float delayAfterImpactVFX;
        [Range(0, 20)]
        public float delayBeforeDestroyImpactVFX;

        public AbilityImpactEffectInfo()
        {
            delayAfterImpactVFX = 0;
            delayBeforeDestroyImpactVFX = 0;
        }
    }
}
