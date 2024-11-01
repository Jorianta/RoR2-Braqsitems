using RoR2;
using RoR2.Projectile;
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
using TMPro;
using BraqsItems.Misc;

namespace BraqsItems
{
    public class BiggerExplosions
    {
        public static ItemDef itemDef;

        public static bool isEnabled = true;
        public static float percentPerStack = 0.05f;
        public static float basePercent = 0.1f;

        internal static void Init()
        {
            Log.Info("Initializing Accelerant Item");
            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "BIGGEREXPLOSIONS";
            itemDef.nameToken = "ITEM_BIGGEREXPLOSIONS_NAME";
            itemDef.pickupToken = "ITEM_BIGGEREXPLOSIONS_PICKUP";
            itemDef.descriptionToken = "ITEM_BIGGEREXPLOSIONS_DESC";
            itemDef.loreToken = "ITEM_BIGGEREXPLOSIONS_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier1;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
            };

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/StrengthenBurn/texGasTankIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/StrengthenBurn/PickupGasTank.prefab").WaitForCompletion();

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

            Log.Info("Acclerant Initialized");
        }

        public static void Hooks()
        {
            Stats.StatsCompEvent.StatsCompRecalc += StatsCompEvent_StatsCompRecalc;
        }

        public static void StatsCompEvent_StatsCompRecalc(object sender, Stats.StatsCompRecalcArgs args)
        {
            if (args.Stats && NetworkServer.active)
            {
                if (args.Stats.inventory)
                {
                    int stack = args.Stats.inventory.GetItemCount(itemDef);
                    if (stack > 0) {
                        args.Stats.blastRadiusBoostAdd *= 1 + (stack-1) * percentPerStack + basePercent;
                    }
                }
            }
        }
    }
}