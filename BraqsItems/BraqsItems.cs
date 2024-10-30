using BepInEx;
using BraqsItems.Util;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BraqsItems
{

    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]

    // Soft Dependencies
    //[BepInDependency(LookingGlass.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.DifferentModVersionsAreOk)]
    public class BraqsItemsPlugin : BaseUnityPlugin
    {

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Braquen";
        public const string PluginName = "BraqsItems";
        public const string PluginVersion = "0.0.1";


        public static PluginInfo pluginInfo;
        public static AssetBundle AssetBundle;

        public void Awake()
        {
            Log.Init(Logger);

            pluginInfo = Info;
            ExplosionEffectHelper.Init();
            // AssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pluginInfo.Location), "[X]assetbundle"));

            if (BiggerExplosions.isEnabled) 
            {
                BiggerExplosions.Init();
            }
            if (ExplodeAgain.isEnabled)
            {
                ExplodeAgain.Init();
            }
            if (ExplosionFrenzy.isEnabled)
            {
                ExplosionFrenzy.Init();
            }
            if (SpreadDebuffs.isEnabled)
            {
                SpreadDebuffs.Init();
            }

        }

        //TEST
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.

                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(ExplosionFrenzy.itemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}

