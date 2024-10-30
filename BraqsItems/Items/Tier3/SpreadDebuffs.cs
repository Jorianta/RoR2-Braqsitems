using RoR2;
using RoR2.Orbs;
using R2API;
using UnityEngine;
using BraqsItems.Misc;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;


namespace BraqsItems
{
    public class SpreadDebuffs
    {
        public static ItemDef itemDef;
        public static ItemDef hiddenItemDef;
        public static BuffDef buffDef;

        public static bool isEnabled = false;

        internal static void Init()
        {
            Log.Info("Initializing SPREADDEBUFFS Item");
            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "SPREADDEBUFFS";
            itemDef.nameToken = "ITEM_SPREADDEBUFFS_NAME";
            itemDef.pickupToken = "ITEM_SPREADDEBUFFS_PICKUP";
            itemDef.descriptionToken = "ITEM_SPREADDEBUFFS_DESC";
            itemDef.loreToken = "ITEM_SPREADDEBUFFS_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Utility,
                ItemTag.InteractableRelated,
                ItemTag.AIBlacklist,
                ItemTag.OnStageBeginEffect
            };

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Infusion/texInfusionIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Infusion/PickupInfusion.prefab").WaitForCompletion();

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


            //DEBUFF//
            buffDef = ScriptableObject.CreateInstance<BuffDef>();

            buffDef.name = "Infected";
            buffDef.canStack = false;
            buffDef.isHidden = false;
            buffDef.isDebuff = true;
            buffDef.isCooldown = false;

            buffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Infusion/texInfusionIcon.png").WaitForCompletion();


            Hooks();

            Log.Info("SPREADDEBUFFS Initialized");
        }

        public static void Hooks()
        {
            On.RoR2.CharacterBody.RemoveBuff_BuffDef += CharacterBody_RemoveBuff_BuffDef;
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy ;
        }

        public static void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && attackerBody.inventory && attackerBody.inventory.GetItemCount(itemDef) > 0 && damageInfo.procCoefficient > 0)
            {
                if (victim.TryGetComponent(out CharacterBody victimBody))
                {
                    //inflictInfection(victimBody, damageInfo.attacker, attackerBody.inventory.GetItemCount(itemDef));
                    spreadInfection(attackerBody,victimBody);
                }
            }
            orig(self, damageInfo, victim);
        }

        public static void CharacterBody_RemoveBuff_BuffDef(On.RoR2.CharacterBody.orig_RemoveBuff_BuffDef orig, CharacterBody self, BuffDef buff)
        {
            orig(self, buff);

            if(buff == buffDef)
            {
                removeInfection(self);
            }
        }

        public static void inflictInfection(CharacterBody body, CharacterBody attacker, int stacks)
        {
            //If the body already has a more severe infection, return
            if (body.TryGetComponent(out BraqsItemsInfectedBehaviour component) && component.stack >= stacks) return;

            component = body.AddItemBehavior<BraqsItemsInfectedBehaviour>(stacks);
            component.attacker = attacker;
            body.AddBuff(buffDef);
        }

        //NOTE: does not remove debuff, but is called when the debuff is removed.
        private static void removeInfection(CharacterBody body)
        {
            if (body.TryGetComponent(out BraqsItemsInfectedBehaviour component))
            {
                UnityEngine.Object.Destroy(component);
            }
        }

        public static void spreadInfection(CharacterBody attacker, CharacterBody body)
        {
            Log.Debug("BraqsItemsInfectedBehaviour:SpreadInfection()");

            //Don't infect self
            List<HealthComponent> previousInfectedList = new List<HealthComponent>() { body.healthComponent };
            //Maybe make bouncesremaining a ref? would make stacking infections easier.

            InfectionOrb infectionOrb = new InfectionOrb()
            {
                attacker = attacker,
                origin = body.transform.position,
                bouncesRemaining = 1,
                infector = body,
                //infectionStacks
                bouncedObjects = previousInfectedList,
                teamIndex = attacker.teamComponent.teamIndex
            };
            //Three initial tendrils
            for (int i = 0; i < 3; i++)
            {
                HurtBox hurtBox = infectionOrb.PickNextTarget(body.transform.position);
                if ((bool)hurtBox)
                {
                    previousInfectedList.Add(hurtBox.healthComponent);
                    infectionOrb.target = hurtBox;
                    OrbManager.instance.AddOrb(infectionOrb);
                }
            }
        }

        //NOTE: this "ItemBehavior" is not associated with any actual item
        public class BraqsItemsInfectedBehaviour : CharacterBody.ItemBehavior
        {
            public CharacterBody attacker { get; set; }
            public float infectThreshold;
            public float nextThreshold;


            public void Start()
            {
                if (!body.healthComponent) return;


                Log.Debug("BraqsItemsInfectedBehaviour:Start()");

                //What percent health to spread infection. 1 stack is at 50%, 2 is at 33/66. 3 at 25/50/75, etc.
                infectThreshold = 1 / (stack+1);
                nextThreshold = getNextThreshold();
            }

            public void Update()
            {   
                if(nextThreshold <= 0)
                {
                    return;
                }

                if (body.healthComponent.health / body.healthComponent.fullHealth <= nextThreshold)
                {
                    spreadInfection();
                    nextThreshold = getNextThreshold();
                }
            }

            public void OnDestroy()
            {

            }

            public float getNextThreshold()
            {
                var next= (body.healthComponent.health / (body.healthComponent.fullHealth % infectThreshold)) * infectThreshold;
                Log.Debug("Next Threshold = " + next);
                return next;
            }

            private void spreadInfection()
            {
                Log.Debug("BraqsItemsInfectedBehaviour:SpreadInfection()");

                //Don't infect self
                List<HealthComponent> previousInfectedList = new List<HealthComponent>(){ body.healthComponent };
                //Maybe make bouncesremaining a ref? would make stacking infections easier.

                InfectionOrb infectionOrb = new InfectionOrb() {
                    attacker = attacker,
                    origin = transform.position,
                    bouncesRemaining = 1,
                    bouncedObjects = previousInfectedList,
                };
                //Three initial tendrils
                for (int i = 0; i < 3; i++)
                {
                    HurtBox hurtBox = infectionOrb.PickNextTarget(base.transform.position);
                    if ((bool)hurtBox)
                    {
                        previousInfectedList.Add(hurtBox.healthComponent);
                        infectionOrb.target = hurtBox;
                        OrbManager.instance.AddOrb(infectionOrb);
                    }
                }
            }
        }
    }
}
