using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;


namespace BraqsItems.Misc
{
    public class Stats
    {
        public static void Init()
        {
            CharacterBody.onBodyStartGlobal += CharacterBody_Start;
            On.RoR2.BlastAttack.Fire += BlastAttack_Fire;
        }

        private static void CharacterBody_Start(CharacterBody body)
        {

            if (body.masterObject && !body.masterObject.GetComponent<BraqsItems_StatsComponent>())
            {
                body.masterObject.AddComponent<BraqsItems_StatsComponent>();
            }
        }

        public class BraqsItems_StatsComponent : MonoBehaviour
        {
            // this monobehavior is attached to every charactermaster when they spawn
            // Thanks GOTCE

            // generic player stuff
            public CharacterMaster master;

            public Inventory inventory;
            public CharacterBody body;

            // blastattack radius coefficient
            public float blastRadiusBoost;

            //recalc
            public float blastRadiusBoostAdd;

            private void Start()
            {

                if (gameObject.GetComponent<CharacterMaster>())
                {
                    master = gameObject.GetComponent<CharacterMaster>();
                    body = master.GetBody();
                    inventory = master.inventory;
                }
                RecalculateStatsAPI.GetStatCoefficients += UpdateStats;

            }

            private void OnDestroy()
            {
                RecalculateStatsAPI.GetStatCoefficients -= UpdateStats;
            }

            private void UpdateStats(CharacterBody cbody, RecalculateStatsAPI.StatHookEventArgs args)
            {

                if (cbody && cbody.master == master && cbody.inventory && cbody.masterObject.GetComponent<BraqsItems_StatsComponent>())
                {
                    inventory = cbody.inventory;
                    body = cbody;
                    //Coefficient, starts at 1
                    blastRadiusBoostAdd = 1;

                    StatsCompEvent.StatsCompRecalc?.Invoke(this, new(cbody.masterObject.GetComponent<BraqsItems_StatsComponent>()));

                    blastRadiusBoost = blastRadiusBoostAdd; 

                }
            }

            public void FixedUpdate()
            {

            }
        }

        public static class StatsCompEvent
        {
            public static EventHandler<StatsCompRecalcArgs> StatsCompRecalc;
        }

        public class StatsCompRecalcArgs
        {
            public BraqsItems_StatsComponent Stats;

            public StatsCompRecalcArgs(BraqsItems_StatsComponent stats)
            {
                Stats = stats;
            }
        }

        //Implementation of blastRadiusBoost stat
        public static BlastAttack.Result BlastAttack_Fire(On.RoR2.BlastAttack.orig_Fire orig, BlastAttack self)
        {
            if (NetworkServer.active && self.attacker)
            {
                CharacterBody body = self.attacker.GetComponent<CharacterBody>();
                if (body && body.master && body.master.TryGetComponent(out Stats.BraqsItems_StatsComponent stats))
                {
                    self.radius *= stats.blastRadiusBoost;

                    if (stats.blastRadiusBoost > 1)
                    {
                        Util.ExplosionEffectHelper.doExtraExplosionEffect(self.position, self.radius);
                    }
                    Log.Debug("Blast radius boost: " + stats.blastRadiusBoost);
                }
            }
            else Log.Debug("NO BONUS FOR YOU"); 

            return orig(self);
        }
    }
}
