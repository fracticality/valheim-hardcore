using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hardcore.Patches
{
    [HarmonyPatch(typeof(PlayerProfile), "Save")]
    public static class PlayerProfileSave
    {
        public static void Postfix(ref PlayerProfile __instance)
        {
            if (Hardcore.newProfileData != null)
            {
                Hardcore.Log.LogInfo($"Registering Hardcore Profile ID [{__instance.GetPlayerID()}]");
                Hardcore.newProfileData.profileID = __instance.GetPlayerID();
                Hardcore.hardcoreProfiles.Add(Hardcore.newProfileData);

                Hardcore.SaveDataToDisk();
                Hardcore.newProfileData = null;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerProfile), "GetAllPlayerProfiles")]
    public static class PlayerProfileGetAllPlayerProfiles
    {
        public static void Postfix(ref List<PlayerProfile> __result)
        {
            if (Hardcore.Settings.HardcoreOnly.Value)
            {
                __result.RemoveAll(profile =>
                {
                    HardcoreData data = Hardcore.GetHardcoreDataForProfileID(profile.GetPlayerID());
                    return (data == null || !data.isHardcore);
                });
            }
        }
    }
}
