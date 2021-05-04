using HarmonyLib;

namespace DeathAlert.Patches
{
    [HarmonyPatch(typeof(Player), "OnDamaged")]
    public static class PlayerOnDamaged
    {
        public static void Prefix(HitData hit)
        {
            if (DeathAlert.Settings.Enabled.Value)
            {
                DeathAlert.lastHitData = hit;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnDeath")]
    public static class PlayerOnDeath
    {
        public static void Prefix(Player __instance)
        {
            if (DeathAlert.Settings.Enabled.Value)
            {
                DeathAlert.ShowDeathAlert(__instance);
            }
        }
    }
}
