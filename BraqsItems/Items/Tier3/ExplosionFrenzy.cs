using BraqsItems.Misc;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace BraqsItems
{
    internal class ExplosionFrenzy
    {
        public static ItemDef itemDef;

        public static bool isEnabled = true;
        public static float baseBurn = 0.5f;
        public static float burnPerStack = 0.5f;
        public static float explosionBoost = 0.1f;
        public static int baseMaxBonus = 10;
        public static int maxBonusPerStack = 10;


        internal static void Init()
        {
            Log.Info("Initializing My Manifesto Item");
            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "EXPLOSIONFRENZY";
            itemDef.nameToken = "ITEM_EXPLOSIONFRENZY_NAME";
            itemDef.pickupToken = "ITEM_EXPLOSIONFRENZY_PICKUP";
            itemDef.descriptionToken = "ITEM_EXPLOSIONFRENZY_DESC";
            itemDef.loreToken = "ITEM_EXPLOSIONFRENZY_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
            };

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/BonusGoldPackOnKill/texTomeIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BonusGoldPackOnKill/PickupTome.prefab").WaitForCompletion();

            ModelPanelParameters ModelParams = itemDef.pickupModelPrefab.AddComponent<ModelPanelParameters>();

            ModelParams.minDistance = 5;
            ModelParams.maxDistance = 10;
            // itemDef.pickupModelPrefab.GetComponent<ModelPanelParameters>().cameraPositionTransform.localPosition = new Vector3(1, 1, -0.3f); 
            // itemDef.pickupModelPrefab.GetComponent<ModelPanelParameters>().focusPointTransform.localPosition = new Vector3(0, 1, -0.3f);
            // itemDef.pickupModelPrefab.GetComponent<ModelPanelParameters>().focusPointTransform.localEulerAngles = new Vector3(0, 0, 0);



            itemDef.canRemove = true;
            itemDef.hidden = false;

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));


            Hooks();

            Log.Info("My Manifesto Initialized");
        }

        public static void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;
            On.RoR2.BlastAttack.Fire += BlastAttack_Fire;
        }

        private static void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            self.AddItemBehavior<ExplosionFrenzyBehavior>(self.inventory.GetItemCount(itemDef));
            orig(self);
        }

        private static BlastAttack.Result BlastAttack_Fire(On.RoR2.BlastAttack.orig_Fire orig, BlastAttack self)
        {

            //Apply burn
            BlastAttack.Result result = orig(self);

            if (self.attacker && self.attacker.TryGetComponent(out CharacterBody body) && body.inventory)
            {
                int stacks = body.inventory.GetItemCount(itemDef);
                if (stacks > 0 && result.hitCount > 0)
                {
                    float damage = ((stacks - 1) * burnPerStack + baseBurn) * self.baseDamage;

                    for (int i = 0; i < result.hitCount; i++)
                    {
                        HurtBox hurtBox = result.hitPoints[i].hurtBox;

                        if ((bool)hurtBox.healthComponent)
                        {
                            InflictDotInfo inflictDotInfo = default(InflictDotInfo);
                            inflictDotInfo.victimObject = hurtBox.healthComponent.gameObject;
                            inflictDotInfo.attackerObject = self.attacker;
                            inflictDotInfo.totalDamage = damage;
                            inflictDotInfo.dotIndex = DotController.DotIndex.Burn;
                            inflictDotInfo.damageMultiplier = 1f;
                            InflictDotInfo dotInfo = inflictDotInfo;

                            StrengthenBurnUtils.CheckDotForUpgrade(body.inventory, ref dotInfo);
                            
                            DotController.InflictDot(ref dotInfo);
                        }
                    }
                }
            }
            
            return result;
        }

        public class ExplosionFrenzyBehavior : CharacterBody.ItemBehavior
        {
            bool wasActive = false;
            int maxBonus;
            Dictionary<HealthComponent, int> victims = new Dictionary<HealthComponent, int>();

            private void Start()
            {
                Log.Debug("ExplosionFrenzyBehavior:Start()");
                maxBonus = maxBonus * (stack-1) + baseMaxBonus;
                On.RoR2.DotController.OnDotStackAddedServer += DotController_OnDotStackAddedServer;
                On.RoR2.DotController.OnDotStackRemovedServer += DotController_OnDotStackRemovedServer;
                On.RoR2.HealthComponent.OnDestroy += HealthComponent_OnDestroy;
                Stats.StatsCompEvent.StatsCompRecalc += StatsCompEvent_StatsCompRecalc;
            }

            private void OnDestroy()
            {
                Log.Debug("ExplosionFrenzyBehavior:OnDestroy()");
                On.RoR2.DotController.OnDotStackAddedServer -= DotController_OnDotStackAddedServer;
                On.RoR2.DotController.OnDotStackRemovedServer -= DotController_OnDotStackRemovedServer;
                On.RoR2.HealthComponent.OnDestroy -= HealthComponent_OnDestroy;
                Stats.StatsCompEvent.StatsCompRecalc -= StatsCompEvent_StatsCompRecalc;
            }

            private void DotController_OnDotStackAddedServer(On.RoR2.DotController.orig_OnDotStackAddedServer orig, DotController self, object dotStack)
            {
                DotController.DotStack stack = (DotController.DotStack)dotStack;
                Debug.Log(UnityEngine.Object.ReferenceEquals(stack.attackerObject, body.gameObject));

                if (stack.attackerObject && UnityEngine.Object.ReferenceEquals(stack.attackerObject, body.gameObject) && self.victimHealthComponent 
                    && (stack.dotIndex == DotController.DotIndex.Burn || stack.dotIndex == DotController.DotIndex.StrongerBurn))
                {
                    HealthComponent key = self.victimHealthComponent;
                    if (!victims.ContainsKey(key))
                    {
                        victims.Add(key, 1);
                        body.RecalculateStats();
                        Log.Debug(victims.Count + " burning enemies");
                    }
                    else
                    {
                        victims[key]++;
                    }
                }

                orig(self, dotStack);

            }

            private void DotController_OnDotStackRemovedServer(On.RoR2.DotController.orig_OnDotStackRemovedServer orig, DotController self, object dotStack)
            {
                DotController.DotStack stack = (DotController.DotStack)dotStack;

                if (stack.attackerObject && UnityEngine.Object.ReferenceEquals(stack.attackerObject, body.gameObject) && self.victimHealthComponent 
                    && (stack.dotIndex == DotController.DotIndex.Burn || stack.dotIndex == DotController.DotIndex.StrongerBurn))
                {
                    HealthComponent key = self.victimHealthComponent;
                    if (victims.ContainsKey(key))
                    {
                        victims[key]--;

                        if (victims[key] <= 0)
                        {
                            victims.Remove(key);
                            body.RecalculateStats();
                            Log.Debug(victims.Count + " burning enemies");
                        }
                    }
                }

                orig(self, dotStack);
            }

            private void HealthComponent_OnDestroy(On.RoR2.HealthComponent.orig_OnDestroy orig, HealthComponent self)
            {
                if (victims.ContainsKey(self))
                {
                    victims.Remove(self);
                    body.RecalculateStats();
                    Log.Debug(victims.Count + " burning enemies");
                }
            }


            public void StatsCompEvent_StatsCompRecalc(object sender, Stats.StatsCompRecalcArgs args)
            {
                if (args.Stats && NetworkServer.active)
                {
                    if (args.Stats.inventory)
                    {
                        int bonus = Math.Min(victims.Count,maxBonus);
                        if (bonus > 0)
                        {
                            args.Stats.blastRadiusBoostAdd *= (bonus) * explosionBoost + 1;
                        }
                    }
                }
            }
        }
    }
}
