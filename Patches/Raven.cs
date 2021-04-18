using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hardcore.Patches
{
    [HarmonyPatch(typeof(Raven), "Spawn")]
    public static class RavenSpawn
    {
        public static bool Prefix()
        {
            long profileID = Game.instance.GetPlayerProfile().GetPlayerID();
            HardcoreData hardcoreProfile = Hardcore.GetHardcoreDataForProfileID(profileID);

            return !(hardcoreProfile != null && hardcoreProfile.disableTutorials);
        }
    }
}
