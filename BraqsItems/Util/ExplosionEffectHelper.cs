using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BraqsItems.Util
{
    internal class ExplosionEffectHelper
    {
        public static GameObject ExplosionEffect;
        public static void Init()
        {
            ExplosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/StickyBomb/BehemothVFX.prefab").WaitForCompletion();
        }
        public static void doExtraExplosionEffect(Vector3 position, float scale)
        {
            EffectManager.SpawnEffect(GlobalEventManager.CommonAssets.igniteOnKillExplosionEffectPrefab, new EffectData
            {
                origin = position,
                scale = scale,
            }, transmit: true);
            EffectManager.SpawnEffect(ExplosionEffect, new EffectData
            {
                origin = position,
                scale = scale,
            }, transmit: true);
        }

    }
}
