using RoR2;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using System;

namespace BraqsItems
{
    internal class ExplodeAgain
    {
        public static ItemDef itemDef;

        public static bool isEnabled = true;
        public static float percentChance = 25f;
        public static float percentRadius = 50f;
        public static float damageCoefficient = 1f;

        public static GameObject bomblettePrefab;

        internal static void Init()
        {
            Log.Info("Initializing Bomblette Item");
            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "EXPLODEAGAIN";
            itemDef.nameToken = "ITEM_EXPLODEAGAIN_NAME";
            itemDef.pickupToken = "ITEM_EXPLODEAGAIN_PICKUP";
            itemDef.descriptionToken = "ITEM_EXPLODEAGAIN_DESC";
            itemDef.loreToken = "ITEM_EXPLODEAGAIN_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier2;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Damage,
            };

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoreMissile/texICBMIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MoreMissile/PickupICBM.prefab").WaitForCompletion();

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

            //EFFECTS//
            bomblettePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/CryoCanisterBombletsProjectile.prefab").WaitForCompletion();
            if(!bomblettePrefab.TryGetComponent(out ProjectileExplosion explosion)) Log.Debug("couldn't load bomblette explosion!!!!");
            explosion.blastProcCoefficient = 0;

            Hooks();

            Log.Info("Bomblette Initialized");
        }

        public static void Hooks()
        {
            On.RoR2.BlastAttack.Fire += BlastAttack_Fire;
        }

        public static BlastAttack.Result BlastAttack_Fire(On.RoR2.BlastAttack.orig_Fire orig, BlastAttack self)
        {

            if (NetworkServer.active && self.attacker && self.attacker.TryGetComponent(out CharacterBody body) && body.inventory)
            {
                var items = body.inventory.GetItemCount(itemDef);

                if(items > 0 && self.procCoefficient > 0) FireChildExplosions(self, body, items);
            }

            return orig(self);
        }

        //Much of this code was taken from the molten perf. If it looks weird, blame Hopoo. There were some strange things going on before I trimmed it down.
        protected static void FireChildExplosions(BlastAttack self, CharacterBody body, int items)
        {
            Log.Debug("ExplodeAgain:FireChildExplosions");
            Vector3 vector2 = self.position;
            Vector3 vector3 = Vector3.up;

            int maxbombs = items * 2 + 1;

            EffectData effectData = new EffectData
            {
                scale = 1f,
                origin = vector2
            };

            GameObject bomblette = bomblettePrefab;
            if(!bomblette.TryGetComponent(out ProjectileExplosion explosion)) Log.Error("couldnt change blast radius");
            explosion.blastRadius = self.radius * (percentRadius/100f);

            float damage = RoR2.Util.OnHitProcDamage(self.baseDamage, body.damage, damageCoefficient);

            for (int n = 0; n < maxbombs; n++)
            {
                if (!RoR2.Util.CheckRoll(percentChance * self.procCoefficient, body.master)) continue;

                float speedOverride = UnityEngine.Random.Range(0.5f, 1f) * self.radius * 3;

                float angle = (float)n * MathF.PI * 2f / (float)maxbombs;
                vector3.x += Mathf.Sin(angle);
                vector3.z += Mathf.Cos(angle);

                FireProjectileInfo fireProjectileInfo = default;
                fireProjectileInfo.projectilePrefab = bomblette;
                fireProjectileInfo.position = vector2 + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                fireProjectileInfo.rotation = RoR2.Util.QuaternionSafeLookRotation(vector3);
                fireProjectileInfo.procChainMask = self.procChainMask;
                fireProjectileInfo.owner = self.attacker;
                fireProjectileInfo.damage = damage;
                fireProjectileInfo.crit = self.crit;
                fireProjectileInfo.force = self.baseForce;
                fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
                fireProjectileInfo.speedOverride = speedOverride;
                fireProjectileInfo.useSpeedOverride = true;
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;


                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }
    }
}
