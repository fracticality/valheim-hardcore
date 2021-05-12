using HarmonyLib;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hardcore.Patches
{
    [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterDone")]
    public static class FejdStartupOnNewCharacterDone
    {
        public static void Prefix()
        {
            Transform transform = Hardcore.uiPanel.transform;
            Toggle hardcoreToggle = transform.Find("Hardcore Toggle").GetComponent<Toggle>();
            Toggle skipIntroToggle = transform.Find("Skip Intro Toggle").GetComponent<Toggle>();
            Toggle disableTutorialsToggle = transform.Find("Disable Tutorials Toggle").GetComponent<Toggle>();

            Hardcore.newProfileData = new HardcoreData()
            {
                profileID = 0L,
                isHardcore = hardcoreToggle.isOn,
                skipIntro = skipIntroToggle.isOn,
                disableTutorials = disableTutorialsToggle.isOn
            };
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "OnCharacterNew")]
    public static class FejdStartupOnCharacterNew
    {
        public static void Postfix()
        {
            if (Hardcore.uiPanel != null)
            {
                Toggle[] toggles = Hardcore.uiPanel.GetComponentsInChildren<Toggle>();
                toggles?.Do(toggle =>
                {
                    bool forceHardcore = Hardcore.Settings.HardcoreOnly.Value && toggle.name == "Hardcore Toggle";                    
                    toggle.isOn = forceHardcore;
                    toggle.interactable = !forceHardcore;
                });

                return;
            }            
        }
    }    

    [HarmonyPatch(typeof(FejdStartup), "OnButtonRemoveCharacterYes")]
    public static class FejdStartupOnButtonRemoveCharacterYes
    {
        public static void Prefix(List<PlayerProfile> ___m_profiles, int ___m_profileIndex)
        {            
            PlayerProfile profile = ___m_profiles[___m_profileIndex];

            Hardcore.hardcoreProfiles.RemoveAll((HardcoreData data) => { return data.profileID == profile.GetPlayerID(); });

            Hardcore.SaveDataToDisk();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Start")]
    public static class FejdStartupStart
    {
        public static void Prefix(bool ___m_firstStartup)
        {            
            if (___m_firstStartup)
            {
                Hardcore.LoadDataFromDisk();
            }
        }
    }    
}
