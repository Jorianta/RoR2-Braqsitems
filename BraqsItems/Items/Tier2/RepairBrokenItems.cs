using BraqsItems.Misc;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;
using HarmonyLib;

namespace BraqsItems.Items.Tier2
{
    public class RepairBrokenItems
    {
        public static ItemDef itemDef;

        public static bool isEnabled = true;
        public static int baseRepairs = 2;
        public static int repairsPerStack = 2;
        public static float whiteRepairChance = 100f;
        public static float greenRepairChance = 75f;
        public static float redRepairChance = 50f;

        public static Dictionary<ItemTier, int> tierWeights;

        internal static void Init()
        {
            Log.Info("Initializing Goobo Sr. Item");

            //ITEM//
            itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "REPAIRBROKENITEMS";
            itemDef.nameToken = "ITEM_REPAIRBROKENITEMS_NAME";
            itemDef.pickupToken = "ITEM_REPAIRBROKENITEMS_PICKUP";
            itemDef.descriptionToken = "ITEM_REPAIRBROKENITEMS_DESC";
            itemDef.loreToken = "ITEM_REPAIRBROKENITEMS_LORE";

            itemDef.AutoPopulateTokens();

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier2;
            });

            itemDef.tags = new ItemTag[]
            {
                ItemTag.Utility,
            };

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/GummyClone/texGummyCloneIcon.png").WaitForCompletion();
            itemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GummyClone/PickupGummyClone.prefab").WaitForCompletion();

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

            //RepairBrokenItems_CONSUMED.Init();


            Hooks();

            Log.Info("Goobo Sr. Initialized");
        }

        public static void Hooks()
        {
            On.RoR2.CharacterMaster.OnServerStageBegin += CharacterMaster_OnServerStageBegin;
        }

        private static void CharacterMaster_OnServerStageBegin(On.RoR2.CharacterMaster.orig_OnServerStageBegin orig, CharacterMaster self, Stage stage)
        {
            orig(self, stage);

            int totalRepairAttempts = self.inventory.GetItemCount(itemDef);
            if (totalRepairAttempts <= 0 || !self ||!stage) return;

            totalRepairAttempts = (totalRepairAttempts - 1) * repairsPerStack + baseRepairs;

            Log.Debug("RepairBrokenItems: Attempting " + totalRepairAttempts + " repairs");

            int relationships = ItemCatalog.itemRelationships[BrokenItemRelationships.brokenItemRelationship].Length;
            float[] weights = new float[relationships];
            float[] chances = new float[relationships];
            float totalWeight = 0;

            for(int i = 0; i < relationships; i++)
            {
                ItemDef.Pair pair = ItemCatalog.itemRelationships[BrokenItemRelationships.brokenItemRelationship][i];
                int temp = self.inventory.GetItemCount(pair.itemDef2);

                if (pair.itemDef1.tier == ItemTier.Tier1 || pair.itemDef1.tier == ItemTier.VoidTier1) chances[i] = whiteRepairChance;
                else if (pair.itemDef1.tier == ItemTier.Tier3 || pair.itemDef1.tier == ItemTier.VoidTier3) chances[i] = redRepairChance;
                else chances[i] = greenRepairChance;

                weights[i] = temp;
                totalWeight += temp;
            }

            if(totalWeight <= 0) return;

            int[] repairs = new int[relationships];


            for (int i = 0; i < totalRepairAttempts; i++)
            {
                float cursor = 0;
                float random = UnityEngine.Random.Range(0f, totalWeight);

                //Find a random broken item to repair
                for (int j = 0; j < relationships; j++)
                {
                    cursor += weights[j];
                    if (cursor >= random)
                    {   
                        //Try to repair
                        if(RoR2.Util.CheckRoll(chances[i], self)) repairs[j]++;
                        weights[j] -= 1;
                        break; 
                    }
                }
            }


            for (int i = 0; i < repairs.Length; i++)
            {
                tryRepairItems(self, i, repairs[i]);
            }
        }

        public static void tryRepairItems(CharacterMaster master, int brokenItemRelationshipIndex, int count)
        {
            if (master == null || brokenItemRelationshipIndex < 0 || brokenItemRelationshipIndex >= ItemCatalog.itemRelationships[BrokenItemRelationships.brokenItemRelationship].Length || count <= 0)
            {
                return;
            }
            ItemDef.Pair pair = ItemCatalog.itemRelationships[BrokenItemRelationships.brokenItemRelationship][brokenItemRelationshipIndex];
            Log.Debug("RepairBrokenItems: Repairing " + count + " " + pair.itemDef2.name);

            count = Math.Min(count, master.inventory.GetItemCount(pair.itemDef2));

            master.inventory.RemoveItem(pair.itemDef2, count);
            master.inventory.GiveItem(pair.itemDef1, count);

            CharacterMasterNotificationQueue.SendTransformNotification(master, pair.itemDef2.itemIndex, pair.itemDef1.itemIndex, CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);

            return;
        }


        //private static void CharacterMasterNotificationQueue_SendTransformNotification(On.RoR2.CharacterMasterNotificationQueue.orig_SendTransformNotification_CharacterMaster_ItemIndex_ItemIndex_TransformationType orig, CharacterMaster characterMaster, ItemIndex oldIndex, ItemIndex newIndex, CharacterMasterNotificationQueue.TransformationType transformationType)
        //{

        //    ItemDef oldItem = ItemCatalog.GetItemDef(oldIndex);
        //    ItemDef newItem = ItemCatalog.GetItemDef(newIndex);

        //    int oldItemStacks = characterMaster.inventory.GetItemCount(oldIndex);
        //    int gooboStacks = characterMaster.inventory.GetItemCount(itemDef);

        //    orig(characterMaster, oldIndex, newIndex, transformationType);

        //    //TODO: Add a timer on this so goobos dont become bottles and then get consumed immediately (Could be an issue).

        //    //Is the new item broken?
        //    if (transformationType == 0 && newItem.tier == ItemTier.NoTier)
        //    {
        //        //How many broke?
        //        int brokenStacks = oldItemStacks - characterMaster.inventory.GetItemCount(oldIndex);

        //        int goobosUsed = 0;
        //        for (int i = 0; i < brokenStacks; i++)
        //        {
        //            if (RoR2.Util.CheckRoll(0.5f, 0f, characterMaster)) goobosUsed++;
        //        }

        //        characterMaster.inventory.GiveItem(oldItem, goobosUsed);
        //        characterMaster.inventory.GiveItem(RepairBrokenItems_CONSUMED.itemDef, goobosUsed);
        //        characterMaster.inventory.RemoveItem(itemDef, goobosUsed);
        //        CharacterMasterNotificationQueue.SendTransformNotification(characterMaster, itemDef.itemIndex, oldItem.itemIndex, CharacterMasterNotificationQueue.TransformationType.Suppressed);
        //        //EffectData effectData3 = new EffectData
        //    }
        //}

        //public class RepairBrokenItemsBehavior : CharacterBody.ItemBehavior
        //{
        //    public ItemDef.Pair[] itemsBroken = Array.Empty<ItemDef.Pair>();

        //    public void OnStart()
        //    {
        //        On.RoR2.Inventory.GiveItem_ItemDef_int += Inventory_GiveItem_ItemDef_int;
        //    }

        //    public void OnDisable()
        //    {
        //        On.RoR2.Inventory.GiveItem_ItemDef_int -= Inventory_GiveItem_ItemDef_int;
        //    }

        //    public void FixedUpdate()
        //    {

        //    }

        //    private void Inventory_GiveItem_ItemDef_int(On.RoR2.Inventory.orig_GiveItem_ItemDef_int orig, Inventory self, ItemDef itemDef, int count)
        //    {
        //        if (body.inventory == self)
        //        {
        //            ItemDef fixedItem = null;
        //            foreach(ItemDef.Pair pair in ItemCatalog.itemRelationships[BrokenItemRelationships.brokenItemRelationship])
        //            {
        //                if (pair.itemDef2 == itemDef)
        //                {

        //                    for (int i = 0; i < count; i++) {
        //                        //Add to our list of items broken this stage
        //                        itemsBroken = itemsBroken.AddToArray(pair);
        //                    }
        //                }
        //            }

        //            if (fixedItem != null) {
        //                int num = Math.Max(count, stack);

        //                int goobosUsed = 0;
        //                for (int i = 0; i < num; i++)
        //                {
        //                    if (RoR2.Util.CheckRoll(0.5f, 0f, )) goobosUsed++;
        //                }

        //                characterMaster.inventory.GiveItem(oldItem, goobosUsed);
        //                characterMaster.inventory.GiveItem(RepairBrokenItems_CONSUMED.itemDef, goobosUsed);
        //                characterMaster.inventory.RemoveItem(itemDef, goobosUsed);
        //                CharacterMasterNotificationQueue.SendTransformNotification(characterMaster, itemDef.itemIndex, oldItem.itemIndex, CharacterMasterNotificationQueue.TransformationType.Suppressed);
        //                //EffectData effectData3 = new EffectData
        //            }
        //        }

        //        orig(self,itemDef,count);
        //    }
        //}
    }
}
