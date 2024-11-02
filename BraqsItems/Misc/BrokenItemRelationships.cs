using HarmonyLib;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace BraqsItems.Misc
{
    //for use with goobo sr.
    public class BrokenItemRelationships
    {
        public static ItemRelationshipType brokenItemRelationship;
        public static ItemDef.Pair[] brokenItemPairs;

        public static void Init()
        {
            brokenItemRelationship = new ItemRelationshipType();
            ItemCatalog.availability.CallWhenAvailable(() =>
            {

                ItemCatalog.itemRelationships.Add(brokenItemRelationship, Array.Empty<ItemDef.Pair>());

                addBrokenItemRelationship(RoR2Content.Items.ExtraLife, RoR2Content.Items.ExtraLifeConsumed);
                addBrokenItemRelationship(DLC1Content.Items.FragileDamageBonus, DLC1Content.Items.FragileDamageBonusConsumed);
                addBrokenItemRelationship(DLC1Content.Items.HealingPotion, DLC1Content.Items.HealingPotionConsumed);
                addBrokenItemRelationship(DLC1Content.Items.ExtraLifeVoid, DLC1Content.Items.ExtraLifeVoidConsumed);
            });
        }

        //Make sure the second item is the broken version
        public static void addBrokenItemRelationship(ItemDef normalitem, ItemDef brokenItem)
        {
            ItemDef.Pair pair = new ItemDef.Pair()
            {
                itemDef1 = normalitem,
                itemDef2 = brokenItem,
            };
            ItemCatalog.itemRelationships[brokenItemRelationship] = ItemCatalog.itemRelationships[brokenItemRelationship].AddToArray(pair);
        }
    }
}
