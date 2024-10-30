using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace BraqsItems.Items.equip.lunar
{
    public class ShareItems
    {
        public static EquipmentDef equipmentDef;
        public static int equipCooldown = 50;

        internal static void Init()
        {
            Log.Info("Initializing Imperfected Construct Equipment");
            //EQUIPMENT//

            equipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();

            equipmentDef.name = "SHAREITEMS";
            equipmentDef.AutoPopulateTokens();

            equipmentDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/DroneWeapons/texDroneWeaponsIcon.png").WaitForCompletion();
            equipmentDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DroneWeapons/PickupDroneWeapons.prefab").WaitForCompletion();

            equipmentDef.isLunar = true;
            equipmentDef.colorIndex = ColorCatalog.ColorIndex.LunarItem;

            equipmentDef.appearsInMultiPlayer = true;
            equipmentDef.appearsInSinglePlayer = true;
            equipmentDef.canBeRandomlyTriggered = false;
            equipmentDef.enigmaCompatible = true;
            equipmentDef.canDrop = true;

            equipmentDef.cooldown = equipCooldown;

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomEquipment(equipmentDef, displayRules));

            Hooks();

            Log.Info("Imperfected Construct Initialized"); 
        }

        private static void Hooks()
        {
            On.RoR2.Inventory.GetItemCount_ItemDef += Inventory_GetItemCount_ItemDef;
        }

        private static int Inventory_GetItemCount_ItemDef(On.RoR2.Inventory.orig_GetItemCount_ItemDef orig, Inventory self, ItemDef itemDef)
        {
            throw new NotImplementedException();
        }

        private void InitItemStealer()
        {
            if (NetworkServer.active)
            {
                ItemStealController itemStealController = new ItemStealController();
                itemStealController.itemLendFilter = ItemStealController.AIItemFilter;
                
            }
        }

        public class SharedInventory : Inventory
        {

        }
    }
}
