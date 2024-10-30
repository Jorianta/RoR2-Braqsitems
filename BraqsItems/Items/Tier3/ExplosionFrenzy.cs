using R2API;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.Networking;

namespace BraqsItems
{
    internal class ExplosionFrenzy
    {
        public static ItemDef itemDef;
        public static BuffDef buffDef;
        public static BuffDef coolDownBuffDef;
        public static BuffDef primedBuffDef;

        public static GameObject ExplosionEffect;

        public static bool isEnabled = true;
        public static float baseDuration = 6f;
        public static float durationPerStack = 4f;


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

            //BUFFS//
            buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = "Manifestation";
            buffDef.canStack = false;
            buffDef.isHidden = false;
            buffDef.isDebuff = false;
            buffDef.isCooldown = false;
            buffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/EliteFire/texAffixRedIcon.png").WaitForCompletion();

            coolDownBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            coolDownBuffDef.name = "Manifesto Cooldown";
            coolDownBuffDef.canStack = true;
            coolDownBuffDef.isHidden = false;
            coolDownBuffDef.isDebuff = true;
            coolDownBuffDef.isCooldown = true;
            coolDownBuffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/ElementalRingVoid/texBuffElementalRingVoidCooldownIcon.tif").WaitForCompletion();

            primedBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            primedBuffDef.name = "Manifesto Ready";
            primedBuffDef.canStack = false;
            primedBuffDef.isHidden = false;
            primedBuffDef.isDebuff = false;
            primedBuffDef.isCooldown = false;
            primedBuffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/ElementalRingVoid/texBuffElementalRingVoidReadyIcon.tif").WaitForCompletion();


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
            bool flag = self.attacker.TryGetComponent(out CharacterBody body);
            //Do bonuses
            if (NetworkServer.active && flag && body.HasBuff(buffDef))
            {
                self.radius *= 2;
                self.damageType |= DamageType.IgniteOnHit;
            }

            Util.ExplosionEffectHelper.doExtraExplosionEffect(self.position, self.radius);

            //Apply buff
            BlastAttack.Result result = orig(self);

            if (self.attacker && flag)
            {            
                if (body.HasBuff(primedBuffDef) && result.hitCount >= 5)
                {
                    addAnarchyBuff(body);
                }
            }
            
            return result;
        }

        private static void addAnarchyBuff(CharacterBody body)
        {
            Log.Debug("Adding buff");
            if (!body.inventory) return;
            int stack = body.inventory.GetItemCount(itemDef);
            float duration = (stack - 1) * durationPerStack + baseDuration;
            body.AddTimedBuff(buffDef, duration);
        }

        public class ExplosionFrenzyBehavior : CharacterBody.ItemBehavior
        {
            bool wasActive = false;

            private void OnStart()
            {
                Log.Debug("ExplosionFrenzyBehavior:Start()");
                wasActive = false;
            }

            private void OnDestroy()
            {
                if ((bool)body)
                {
                    if (body.HasBuff(primedBuffDef))
                    {
                        body.RemoveBuff(primedBuffDef);
                    }
                    if (body.HasBuff(coolDownBuffDef))
                    {
                        body.RemoveBuff(coolDownBuffDef);
                    }
                }
            }

            private void FixedUpdate()
            {
                bool active = body.HasBuff(buffDef);
                bool cooldown = body.HasBuff(coolDownBuffDef);
                bool ready = body.HasBuff(primedBuffDef);
                //If the buff just ran out, add cooldown
                if (wasActive && !active)
                {
                    Log.Debug("Adding cooldown");
                    AddCooldown();
                }
                if (!cooldown && !ready && !active)
                {
                    Log.Debug("Adding ready buff");
                    body.AddBuff(primedBuffDef);
                }
                if (ready && active)
                {
                    Log.Debug("removing ready buff");
                    body.RemoveBuff(primedBuffDef);
                }

                wasActive = active;
            }

            private void AddCooldown()
            {
                for (int k = 1; (float)k <= 10f; k++)
                {
                    body.AddTimedBuff(coolDownBuffDef, k);
                }
            }
        }
    }
}
