using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hardcore.Patches
{
    [HarmonyPatch(typeof(Valkyrie), "Awake")]
    public static class ValkyrieAwake
    {
        public static void Prefix(ref Vector3 __state, ref Valkyrie __instance)
        {
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData hard) => { return hard.profileID == Game.instance.GetPlayerProfile().GetPlayerID(); });

            if (hardcoreProfile.skipIntro)
            {
                Vector3 position = Player.m_localPlayer.transform.position;
                __state = new Vector3(position.x, position.y, position.z);
            }
            else if (hardcoreProfile.hasDied)
            {
                __instance.m_startPause = 0f;
            }
        }

        public static void Postfix(Vector3 __state)
        {
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData hard) => { return hard.profileID == playerProfile.GetPlayerID(); });
            if (hardcoreProfile.skipIntro)
            {
                Player.m_localPlayer.transform.position = new Vector3(__state.x, __state.y, __state.z);
                if (hardcoreProfile.isHardcore)
                {
                    Hardcore.ResetHardcorePlayer(playerProfile);
                    hardcoreProfile.hasDied = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Valkyrie), "ShowText")]
    public static class ValkyrieShowText
    {
        public static bool Prefix()
        {
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData hard) => { return hard.profileID == Game.instance.GetPlayerProfile().GetPlayerID(); });
            return !(hardcoreProfile.skipIntro || hardcoreProfile.hasDied);
        }
    }

    [HarmonyPatch(typeof(Valkyrie), "SyncPlayer")]
    public static class ValkyrieSyncPlayer
    {
        public static bool Prefix()
        {
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData hard) => { return hard.profileID == Game.instance.GetPlayerProfile().GetPlayerID(); });
            return !(hardcoreProfile.skipIntro);
        }
    }

    [HarmonyPatch(typeof(Valkyrie), "DropPlayer")]
    public static class ValkyrieDropPlayer
    {
        public static void Postfix()
        {
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData profile) => { return profile.profileID == playerProfile.GetPlayerID(); });
            if (hardcoreProfile.isHardcore && hardcoreProfile.hasDied)
            {
                Hardcore.ResetHardcorePlayer(playerProfile);
                hardcoreProfile.hasDied = false;
            }
        }
    }
}
