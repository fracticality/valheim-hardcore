using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hardcore.Patches
{
    [HarmonyPatch(typeof(Player), "SetIntro")]
    public static class PlayerSetIntro
    {
        public static bool Prefix(Player __instance)
        {
            long profileID = __instance.GetPlayerID();              
            HardcoreData hardcoreData = Hardcore.GetHardcoreDataForProfileID(profileID);

            return !(hardcoreData != null && hardcoreData.skipIntro);
        }
    }    

    [HarmonyPatch(typeof(Player), "OnDeath")]
    public static class PlayerOnDeath
    {
        public static bool Prefix(Player __instance)
        {            
            long playerID = __instance.GetPlayerID();
            HardcoreData hardcoreProfile = Hardcore.GetHardcoreDataForProfileID(playerID);

            if (hardcoreProfile != null && hardcoreProfile.isHardcore)
            {
                hardcoreProfile.hasDied = true;      
                
                Traverse tPlayer = Traverse.Create(__instance);
                tPlayer.Field<bool>("m_firstSpawn").Value = true;                

                ZNetView nview = tPlayer.Field<ZNetView>("m_nview").Value;
                nview.GetZDO().Set("dead", true);
                nview.InvokeRPC(ZNetView.Everybody, "OnDeath", Array.Empty<object>());
                tPlayer.Method("CreateDeathEffects").GetValue(new object[] { });
                tPlayer.Field<GameObject>("m_visual").Value.SetActive(false);
                tPlayer.Field<List<Player.Food>>("m_foods").Value.Clear();

                __instance.UnequipAllItems();
                __instance.GetInventory().RemoveAll();

                if (Hardcore.Settings.clearCustomSpawn.Value)
                {
                    Game.instance.GetPlayerProfile().ClearCustomSpawnPoint();
                }
                Game.instance.RequestRespawn(10f);

                Gogan.LogEvent("Game", "Death", "biome: " + __instance.GetCurrentBiome().ToString(), 0L);

                return false;
            }

            return true;
        }        
    }
}
