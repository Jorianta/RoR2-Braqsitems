using RoR2;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using System;
using RoR2.UI;
using UnityEngine.UI;
using Rewired.Utils;
using MonoMod.Cil;
using On.RoR2.Projectile;

namespace BraqsItems
{
    public class multiProc
    {
        public static ItemDef itemDef;

        public static bool isEnabled = false;

        internal static void Init()
        {
            Log.Info("Initializing 7-Layer Dip Item");
            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "MULTIPROC";
            itemDef.nameToken = "ITEM_MULTIPROC_NAME";
            itemDef.pickupToken = "ITEM_MULTIPROC_PICKUP";
            itemDef.descriptionToken = "ITEM_MULTIPROC_DESC";
            itemDef.loreToken = "ITEM_MULTIPROC_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Lunar;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Utility,
                ItemTag.InteractableRelated,
                ItemTag.AIBlacklist,
                ItemTag.OnStageBeginEffect
            };

            itemDef.pickupIconSprite = BraqsItemsPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Items/impermanence/Icon.png");
            itemDef.pickupModelPrefab = BraqsItemsPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Items/impermanence/Model.prefab");
            
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

            Log.Info("7-Layer Dip Initialized");
        }

        public static void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
        }

        public static void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            GameObject attacker = damageInfo.attacker;
            if (!damageInfo.rejected && damageInfo.procCoefficient > 0f && attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.inventory)
            {
                DamageTypeCombo procs = 0; 

                var procChance = damageInfo.procCoefficient * 7f;
                var itemCount = attackerBody.inventory.GetItemCount(itemDef);
                for (int i = 0; i < itemCount; i++)
                {
                    if ((ulong)(damageInfo.damageType & DamageType.BleedOnHit) != 0 && RoR2.Util.CheckRoll(procChance, attackerBody.master))
                    {
                        procs |= DamageType.BleedOnHit;
                    }
                    if ((ulong)(damageInfo.damageType & DamageType.BleedOnHit) != 0 && RoR2.Util.CheckRoll(procChance, attackerBody.master))
                    {
                        procs |= DamageType.BleedOnHit;
                    }
                }
            }
        }

            public static void ProcChainMask_AddProc(On.RoR2.ProcChainMask.orig_AddProc orig, ref global::RoR2.ProcChainMask self, ProcType procType)
        {
                
        }
    }
}
