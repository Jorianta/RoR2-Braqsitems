using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace BraqsItems
{
    internal class LightningDamageBoost
    {
        public static ItemDef itemDef;

        public static bool isEnabled = true;
        public static float percentPerStack = 0.3f;
        public static float basePercent = 0.5f;

        internal static void Init()
        {
            Log.Info("Initializing Induction Coil Item");
            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "LIGHTNINGDAMAGEBOOST";
            itemDef.nameToken = "ITEM_LIGHTNINGDAMAGEBOOST_NAME";
            itemDef.pickupToken = "ITEM_LIGHTNINGDAMAGEBOOST_PICKUP";
            itemDef.descriptionToken = "ITEM_LIGHTNINGDAMAGEBOOST_DESC";
            itemDef.loreToken = "ITEM_LIGHTNINGDAMAGEBOOST_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
            };

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Lightning/texCapacitorIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/PickupCapacitor.prefab").WaitForCompletion();

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

            Log.Info("Induction Coil Initialized");
        }

        public static void Hooks()
        {
            On.RoR2.Orbs.LightningOrb.Begin += LightningOrb_Begin;
        }

        private static void LightningOrb_Begin(On.RoR2.Orbs.LightningOrb.orig_Begin orig, RoR2.Orbs.LightningOrb self)
        {
            Log.Debug("LightningDamageBoost:LightningOrb_Begin");

            if (self.attacker && self.attacker.TryGetComponent(out CharacterBody body) && body.inventory)
            {
                int count = body.inventory.GetItemCount(itemDef);

                if (count > 0)
                {
                    self.damageValue *= 1 + (count-1) * percentPerStack + basePercent;
                    Log.Debug("Chain damage = " + self.damageValue);
                }
            }

            orig(self);
        }
    }
}
