using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BraqsItems.Misc
{
    //A altered lightningOrb for our purposes
    class InfectionOrb : Orb
    {
        public int infectionStacks;
        //Character with the item
        public CharacterBody attacker;
        //Character spreading infection
        public CharacterBody infector;
        public int bouncesRemaining = 1;
        public List<HealthComponent> bouncedObjects;
        public TeamIndex teamIndex;
        public float range = 20f;
        public int targetsToFindPerBounce = 2;
        private BullseyeSearch search;




        public override void Begin()
        {

            Log.Debug("InfectionOrb:Begin()");
            string path = "Prefabs/Effects/OrbEffects/CrocoDiseaseOrbEffect";
            base.duration = 0.3f;

            teamIndex = attacker.teamComponent.teamIndex;

            EffectData effectData = new EffectData
            {
                origin = origin,
                genericFloat = base.duration,
                //color = new Color32()
            };
            effectData.SetHurtBoxReference(target);
            EffectManager.SpawnEffect(OrbStorageUtility.Get(path), effectData, transmit: true);
        }

        public override void OnArrival()
        {
            if (!target)
            {
                return;
            }
            GameObject targetObject = HurtBox.FindEntityObject(target);
            if ((bool)targetObject && targetObject.TryGetComponent(out CharacterBody body))
            {
                //SpreadDebuffs.inflictInfection(body, attacker, infectionStacks);

                addDebuffs(body,infector);
                

            }
            if (bouncesRemaining > 0)
            {
                for (int i = 0; i < targetsToFindPerBounce; i++)
                {
                    if (bouncedObjects != null)
                    {
                        bouncedObjects.Add(target.healthComponent);
                    }
                    HurtBox hurtBox = PickNextTarget(target.transform.position);
                    if ((bool)hurtBox)
                    {
                        InfectionOrb infectionOrb = new InfectionOrb();
                        infectionOrb.search = search;
                        infectionOrb.attacker = attacker;
                        infectionOrb.infector = infector;
                        infectionOrb.origin = target.transform.position;
                        infectionOrb.target = hurtBox;
                        infectionOrb.teamIndex = teamIndex;
                        infectionOrb.bouncesRemaining = bouncesRemaining - 1;
                        infectionOrb.bouncedObjects = bouncedObjects;
                        infectionOrb.range = range;
                        OrbManager.instance.AddOrb(infectionOrb);
                    }
                }
            }
        }

        public HurtBox PickNextTarget(Vector3 position)
        {
            if (search == null)
            {
                search = new BullseyeSearch();
            }
            search.searchOrigin = position;
            search.searchDirection = Vector3.zero;
            search.teamMaskFilter = TeamMask.allButNeutral;
            search.teamMaskFilter.RemoveTeam(teamIndex);
            search.filterByLoS = false;
            search.sortMode = BullseyeSearch.SortMode.Distance;
            search.maxDistanceFilter = range;
            search.RefreshCandidates();
            HurtBox hurtBox = (from v in search.GetResults()
                               where !bouncedObjects.Contains(v.healthComponent)
                               select v).FirstOrDefault();
            if ((bool)hurtBox)
            {
                bouncedObjects.Add(hurtBox.healthComponent);
            }
            return hurtBox;
        }

        public void addDebuffs(CharacterBody targetBody, CharacterBody infector)
        {
            Log.Debug("InfectionOrb:addDebuffs()");
            var buffList = infector.activeBuffsList;
            for (int i = 0; i < buffList.Count(); i++) 
            {
                BuffDef buff = BuffCatalog.GetBuffDef(buffList[i]);
                int buffCount = infector.GetBuffCount(buff);

                bool isDoT = false;
                for(int dotIndex = 0; dotIndex < DotController.dotDefs.Count(); dotIndex++)
                {
                    DotController.DotDef dot = DotController.dotDefs[dotIndex];
                    if (dot.associatedBuff.name == buff.name)
                    {
                        for (int j = 0; j < buffCount; j++)
                        {
                            InflictDotInfo inflictDotInfo = new InflictDotInfo
                            {
                                attackerObject = attacker.gameObject,
                                victimObject = targetBody.gameObject,
                                dotIndex = (DotController.DotIndex)dotIndex,
                                duration = 3f,
                                damageMultiplier = 1f
                            };
                            DotController.InflictDot(ref inflictDotInfo);
                        }
                        isDoT = true;
                    }
                }

                if (!buff.isDebuff) continue;
                Log.Debug("Adding " + buffCount + " stacks of " + buff.name);

                //If the DoT block ran, we've already handled this buff
                if (isDoT) continue;


                //In the  future, check for non-vanilla debuffs and raise an event so other people can do what they want.
                for (int j = 0; j < buffCount; j++)
                {
                    targetBody.AddTimedBuff(buff, 3f);
                }
            }
        }
    }
}
