using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BraqsItems.Util
{
    internal class GameEvents
    {
        public delegate void ExplosionEventHandler(BlastAttack blastAttack, GameObject explosionEffect);

        public static event ExplosionEventHandler OnExplosion;
    }
}
