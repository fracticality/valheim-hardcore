using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Hardcore
{
    [BepInPlugin(Hardcore.UMID, Hardcore.ModName, Hardcore.Version)]
    [BepInDependency(ValheimLib.ValheimLib.ModGuid)]
    public class Hardcore : BaseUnityPlugin
    {
        public const string UMID = "fracticality.valheim.hardcore";
        public const string Version = "1.2.0";
        public const string ModName = "Hardcore";
        Harmony _Harmony;
        public static ManualLogSource Log;        

        public static HardcoreData newProfileData;
        public static List<HardcoreData> hardcoreProfiles = new List<HardcoreData>();
        public static bool clearCustomSpawn = true;
        public static HitData lastHitData;

        public static GameObject uiPanel;
        public static GameObject hardcoreLabel;        

        private const int numDeathStrings = 7;
        private static string[] deathStrings;

        private static Dictionary<string, List<string>> damageTypeStrings;

        private void Awake()
        {

			Log = Logger;            

            _Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);            
            
        }        

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                Log.LogInfo(Localization.instance.Localize(GetRandomDeathString()));
            }
        }

        private void OnDestroy()
        {
            if (_Harmony != null) _Harmony.UnpatchSelf();
            if (uiPanel != null) Destroy(uiPanel);
            if (hardcoreLabel != null) Destroy(hardcoreLabel);
        }

        private static void PopulateDeathStrings()
        {
            deathStrings = new string[numDeathStrings];
            for (int i = 0; i < numDeathStrings; i++)
            {
                deathStrings[i] = $"$hardcore_death_msg{i+1}";
            }
        }

        private static void PopulateDamageTypeStrings()
        {
            damageTypeStrings = new Dictionary<string, List<string>>();

            Traverse tDamageTypes = Traverse.Create(typeof(HitData.DamageTypes));
            List<string> damageTypeNames = tDamageTypes.Fields();
            
            foreach (string fieldName in damageTypeNames)
            {
                PopulateDamageTypeStrings(fieldName);
            }
        }

        private static void PopulateDamageTypeStrings(string damageType)
        {
            Log.LogInfo($"Populating strings for damage type: [{damageType}]...");
            Dictionary<string, string> englishTokens = Traverse.Create(typeof(ValheimLib.Language))
                                                               .Method("GetLanguageDict", new Type[] { typeof(string)})
                                                               .GetValue<Dictionary<string, string>>(new object[]
                                                               { 
                                                                   "English" 
                                                               });            

            List<string> damageTypeTokens = new List<string>();
            foreach (string token in englishTokens.Keys)
            {
                if (token.StartsWith($"hardcore_deathby_{damageType.Substring(2)}"))
                {
                    damageTypeTokens.Add(token);
                }
            }

            Log.LogInfo($"...{damageTypeTokens.Count} strings added.");
            damageTypeStrings.Add(damageType, damageTypeTokens);
        }

        public static string GetRandomDamageTypeString(string damageType)
        {
            if (damageTypeStrings == null)
            {
                Log.LogInfo("damageTypeStrings is null. Creating...");
                PopulateDamageTypeStrings();
            }

            Log.LogInfo($"Attempting to retrieve strings for: [{damageType}]...");
            if (damageTypeStrings.TryGetValue(damageType, out List<string> deathStrings))
            {
                Log.LogInfo($"...{deathStrings.Count} found.");
                if (deathStrings.Count > 0)
                {
                    int random = UnityEngine.Random.Range(0, deathStrings.Count);
                    return deathStrings[random];
                }
            }

            return string.Empty;
        }

        public static string GetRandomDeathString()
        {
            if (deathStrings == null)
            {
                PopulateDeathStrings();
            }

            int random = UnityEngine.Random.Range(0, deathStrings.Length);
            return deathStrings[random];
        }

        public static void ResetHardcorePlayer(PlayerProfile playerProfile)
        {
            Player.m_localPlayer.ResetCharacter();

            playerProfile.ClearCustomSpawnPoint();

            Traverse tMinimap = Traverse.Create(Minimap.instance);

            List<Minimap.PinData> pins = tMinimap.Field<List<Minimap.PinData>>("m_pins").Value;
            List<Minimap.PinData> pinsToRemove = new List<Minimap.PinData>();
            foreach (Minimap.PinData pin in pins)
            {
                if (pin.m_save)
                    pinsToRemove.Add(pin);
            }
            foreach (Minimap.PinData pinToRemove in pinsToRemove)
            {
                Minimap.instance.RemovePin(pinToRemove);
            }

            Minimap.instance.Reset();
            Minimap.instance.SaveMapData();
        }

        public static bool SaveDataToDisk()
        {
            Directory.CreateDirectory(Utils.GetSaveDataPath() + "/mod_data");
            string filename = Utils.GetSaveDataPath() + "/mod_data/hardcore_characters.fch";
            string fileOld = string.Copy(filename) + ".old";
            string fileNew = string.Copy(filename) + ".new";

            FileStream fStream = File.Create(fileNew);
            var bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bFormatter.Serialize(fStream, hardcoreProfiles);

            fStream.Flush(true);
            fStream.Close();
            fStream.Dispose();            

            if (File.Exists(filename))
            {
                if (File.Exists(fileOld))
                {
                    File.Delete(fileOld);
                }
                File.Move(filename, fileOld);
            }
            File.Move(fileNew, filename);
            return true;
        }

        public static bool LoadDataFromDisk()
        {            
            string filename = Utils.GetSaveDataPath() + "/mod_data/hardcore_characters.fch";
            string fileOld = string.Copy(filename) + ".old";            
            FileStream fStream;                  

            Log.LogInfo($"Loading hardcore character list...");
            try
            {
                if (!File.Exists(filename))
                {
                    Log.LogWarning("- Data file missing! Searching for backup...");
                    if (File.Exists(fileOld))
                    {
                        Log.LogInfo("- Backup found! Restoring...");
                        File.Move(fileOld, filename);
                    }
                    else
                    {
                        Log.LogWarning("- Backup missing!");
                        return false;
                    }
                }
                fStream = File.OpenRead(filename);
            }
            catch
            {
                Log.LogError("Failed to open " + filename);
                return false;
            }            

            try
            {
                var bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();                
                hardcoreProfiles = (List<HardcoreData>)bFormatter.Deserialize(fStream);                

                List<PlayerProfile> playerProfiles = PlayerProfile.GetAllPlayerProfiles();
                foreach (HardcoreData profileData in hardcoreProfiles)
                {
                    if (!playerProfiles.Exists((PlayerProfile profile) => { return profile.GetPlayerID() == profileData.profileID; }))
                    {
                        Log.LogInfo($"- Player ID {profileData.profileID} no longer exists! Removing...");
                        hardcoreProfiles.Remove(profileData);
                    }
                }

                Log.LogInfo($"Loaded {hardcoreProfiles.Count} Hardcore Profile(s)");
            }
            catch (Exception e)
            {                
                Log.LogError("Failed to read from " + filename + ": " + e.Message);
                return false;
            }
            finally
            {                
                fStream.Dispose();
            }

            return true;
        }
    }

    [Serializable()]
    public class HardcoreData
    {
        public long profileID = 0L;

        public bool isHardcore = false;
        public bool skipIntro = false;
        public bool disableTutorials = false;
        public bool hasDied = false;

        public HardcoreData() { }
    }    

    [HarmonyPatch(typeof(Raven), "Spawn")]
    public static class RavenSpawn
    {
        public static bool Prefix()
        {
            long profileID = Game.instance.GetPlayerProfile().GetPlayerID();
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData hardProfile) => { return hardProfile.profileID == profileID; });

            return !(hardcoreProfile != null && hardcoreProfile.disableTutorials);
        }
    }   

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

    [HarmonyPatch(typeof(Player), "SetIntro")]
    public static class PlayerSetIntro
    {
        public static bool Prefix(Player __instance)
        {
            long profileID = __instance.GetPlayerID();
            HardcoreData hardcoreData = Hardcore.hardcoreProfiles.Find((HardcoreData data) => { return data.profileID == profileID; });            

            return !hardcoreData.skipIntro;
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "OnNewCharacterDone")]
    public static class FejdStartupOnNewCharacterDone
    {
        public static void Prefix()
        {

            Toggle hardcoreToggle = Hardcore.uiPanel.transform.Find("Hardcore Toggle").GetComponent<Toggle>();
            Toggle skipIntroToggle = Hardcore.uiPanel.transform.Find("Skip Intro Toggle").GetComponent<Toggle>();
            Toggle disableTutorialsToggle = Hardcore.uiPanel.transform.Find("Disable Tutorials Toggle").GetComponent<Toggle>();
            
            Hardcore.newProfileData = new HardcoreData()
            {
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
                foreach (Toggle toggle in toggles)
                {
                    toggle.isOn = false;
                }

                return;
            }


            //TODO: Create GO for each portion (panel, toggle, text, checkbox, checkmark, background)
            //      Add RectTransform component to each GO
            //      Build panel→toggle→GO hierarchy
            //      Add components and configure

            FejdStartup instance = FejdStartup.instance;
            GameObject panel = instance.m_newCharacterPanel;            
            RectTransform hairPanel = panel.transform.Find("CusomizationPanel").Find("HairPanel").GetComponent<RectTransform>();
            float width = hairPanel.rect.width;

            GameObject hardcorePanel = new GameObject("HardcorePanel");
            RectTransform hardcorePanelRect = hardcorePanel.AddComponent<RectTransform>();
            hardcorePanelRect.SetParent(panel.transform);
            hardcorePanelRect.anchorMin = new Vector2(1f, 0.5f);
            hardcorePanelRect.anchorMax = new Vector2(1f, 0.5f);
            hardcorePanelRect.anchoredPosition = new Vector2(-width / 2, 0f);
            hardcorePanelRect.sizeDelta = new Vector2(width, 150);

            Color fadedWhite = new Color(0.784f, 0.784f, 0.784f);
            Image hardcorePanelBG = hardcorePanel.AddComponent<Image>();
            hardcorePanelBG.sprite = panel.transform.Find("CusomizationPanel").Find("bkg").GetComponent<Image>().sprite;            
            hardcorePanelBG.color = fadedWhite * 0.8f;

            Toggle maletoggle = panel.GetComponent<PlayerCustomizaton>().m_maleToggle;
            Sprite checkboxSprite = maletoggle.image.sprite;            
            Sprite checkmarkSprite = (maletoggle.graphic as Image).sprite;            
            CanvasGroup canvas = panel.GetComponent<CanvasGroup>();                       

            string[] toggleNames = new string[] { "Hardcore", "Skip Intro", "Disable Tutorials" };
            int count = toggleNames.Length;

            for (int i = 0; i < count; i++)
            {
                string toggleName = toggleNames[i] + " Toggle";
                float vOffset = (i * 50) + 25;

                GameObject toggleGO = new GameObject(toggleName);                
                RectTransform toggleRect = toggleGO.AddComponent<RectTransform>();
                toggleRect.SetParent(hardcorePanelRect);
                toggleRect.anchorMin = new Vector2(0f, 1f);
                toggleRect.anchorMax = new Vector2(0f, 1f);
                toggleRect.anchoredPosition = new Vector2((width / 2) + 28, -vOffset);
                toggleRect.sizeDelta = new Vector2(width, 150);

                GameObject checkboxGO = new GameObject(toggleName + " Checkbox");
                RectTransform checkboxRect = checkboxGO.AddComponent<RectTransform>();
                checkboxRect.SetParent(toggleRect);
                checkboxRect.anchorMin = new Vector2(0f, 0.5f);
                checkboxRect.anchorMax = new Vector2(0f, 0.5f);
                checkboxRect.anchoredPosition = new Vector2(0f, 0f);
                checkboxRect.sizeDelta = new Vector2(28f, 28f);

                Image checkbox = checkboxGO.AddComponent<Image>();
                checkbox.color = Color.white;
                checkbox.sprite = checkboxSprite;

                GameObject checkmarkGO = new GameObject(toggleName + " Checkmark");
                RectTransform checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
                checkmarkRect.SetParent(toggleRect);
                checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
                checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
                checkmarkRect.anchoredPosition = new Vector2(0f, 0f);
                checkmarkRect.sizeDelta = new Vector2(28f, 28f);

                Image checkmark = checkmarkGO.AddComponent<Image>();
                checkmark.sprite = checkmarkSprite;
                checkmark.color = new Color(1f, 0.641f, 0f);

                Toggle toggle = toggleGO.AddComponent<Toggle>();
                toggle.image = checkbox;
                toggle.graphic = checkmark;
                toggle.isOn = false;
                toggle.transition = Selectable.Transition.ColorTint;
                toggle.colors = maletoggle.colors;

                GameObject textGO = new GameObject(toggleName + " Label");
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.SetParent(checkboxRect);
                textRect.anchorMin = new Vector2(1f, 0.5f);
                textRect.anchorMax = new Vector2(1f, 0.5f);
                textRect.sizeDelta = new Vector2(width - 28, 50);

                Outline outline = textGO.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1f, -1f);

                Text text = textGO.AddComponent<Text>();
                text.font = canvas.GetComponentInChildren<Text>().font;
                text.fontSize = maletoggle.GetComponentInChildren<Text>().fontSize;
                text.color = maletoggle.GetComponentInChildren<Text>().color;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;

                string localizationToken = "$hardcore_" + toggleNames[i].ToLower().Replace(' ', '_');

                text.text = Localization.instance.Localize(localizationToken);                

                textRect.anchoredPosition = new Vector2((text.preferredWidth / 2) + 5, 0f);                

                ButtonSfx sfx = toggleGO.AddComponent<ButtonSfx>();
                sfx.m_sfxPrefab = maletoggle.GetComponentInChildren<ButtonSfx>().m_sfxPrefab;                
            }                                                                       

            Hardcore.uiPanel = hardcorePanel;            
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "ShowCharacterSelection")]
    public static class FejdStartupShowCharacterSelection
    {
        public static void Postfix()
        {
            Traverse tInstance = Traverse.Create(FejdStartup.instance);
            int profileIndex = tInstance.Field<int>("m_profileIndex").Value;
            List<PlayerProfile> profiles = tInstance.Field<List<PlayerProfile>>("m_profiles").Value;

            if (profiles.Count > 0)
            {
                PlayerProfile profile = profiles[profileIndex];
                long profileID = profile.GetPlayerID();
                bool isHardcore = Hardcore.hardcoreProfiles.Exists((HardcoreData data) => { return data.profileID == profileID && data.isHardcore; });
                
                Hardcore.hardcoreLabel.SetActive(isHardcore);
            }
            
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "OnButtonRemoveCharacterYes")]
    public static class FejdStartupOnButtonRemoveCharacterYes
    {
        public static void Prefix(FejdStartup __instance)
        {
            Traverse tInstance = Traverse.Create(__instance);

            List<PlayerProfile> profiles = tInstance.Field<List<PlayerProfile>>("m_profiles").Value;
            int profileIndex = tInstance.Field<int>("m_profileIndex").Value;
            PlayerProfile profile = profiles[profileIndex];

            Hardcore.hardcoreProfiles.RemoveAll((HardcoreData data) => { return data.profileID == profile.GetPlayerID(); });         

            Hardcore.SaveDataToDisk();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Start")]
    public static class FejdStartupStart
    {        
        public static void Prefix()
        {            
            bool isFirstStartup = Traverse.Create(typeof(FejdStartup)).Field<bool>("m_firstStartup").Value;
            if (isFirstStartup)
            {
                Hardcore.LoadDataFromDisk();
            }
        }               
    }

    [HarmonyPatch(typeof(FejdStartup), "UpdateCharacterList")]
    public static class FejdStartupUpdateCharacterList
    {
        public static void Postfix()
        {
            if (!Hardcore.hardcoreLabel)
            {
                GameObject selectScreen = Traverse.Create(FejdStartup.instance).Field<GameObject>("m_characterSelectScreen").Value;

                Text characterName = selectScreen.transform.Find("SelectCharacter").Find("CharacterName").GetComponentInChildren<Text>();

                GameObject hardcoreLabel = new GameObject("HardcoreLabel");
                RectTransform rect = hardcoreLabel.AddComponent<RectTransform>();                
                rect.position = (characterName.transform as RectTransform).position + new Vector3(0, 60);
                rect.sizeDelta = new Vector2((characterName.transform as RectTransform).rect.width, 40);

                Text text = hardcoreLabel.AddComponent<Text>();
                text.font = characterName.font;
                text.color = Color.red;
                text.fontSize = characterName.fontSize - 12;
                text.alignment = TextAnchor.MiddleCenter;
                text.text = Localization.instance.Localize("($hardcore_hardcore)");

                Outline outline = hardcoreLabel.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(2, -2);

                hardcoreLabel.transform.SetParent(characterName.transform);

                Hardcore.hardcoreLabel = hardcoreLabel;
            }

            Traverse traverse = Traverse.Create(FejdStartup.instance);
            int profileIndex = traverse.Field<int>("m_profileIndex").Value;
            List<PlayerProfile> profiles = traverse.Field<List<PlayerProfile>>("m_profiles").Value;

            if (profiles.Count > 0)
            {
                long selectedProfileID = traverse.Field<List<PlayerProfile>>("m_profiles").Value[profileIndex].GetPlayerID();

                if (Hardcore.newProfileData != null)
                {                    
                    Hardcore.newProfileData.profileID = selectedProfileID;
                    Hardcore.hardcoreProfiles.Add(Hardcore.newProfileData);

                    Hardcore.SaveDataToDisk();
                    Hardcore.newProfileData = null;                                        
                }

                bool isHardcore = Hardcore.hardcoreProfiles.Exists((HardcoreData data) => { return data.profileID == selectedProfileID && data.isHardcore; });
                Hardcore.hardcoreLabel.SetActive(isHardcore);
            }            
        }
    }
    
    [HarmonyPatch(typeof(Player), "OnDamaged")]
    public static class PlayerOnDamaged
    {
        public static void Prefix(HitData hit)
        {
            Hardcore.lastHitData = hit;
            //if (hit != null && hit.HaveAttacker())
            //{
            //    Character attacker = hit.GetAttacker();                                
            //    Hardcore.lastAttackerName = attacker.GetHoverName();
            //    Hardcore.Log.LogInfo($"Attacker Name: {Hardcore.lastAttackerName}");                                             
            //}
        }
    }

    [HarmonyPatch(typeof(Player), "OnDeath")]
    public static class PlayerOnDeath
    {        
        public static bool Prefix(Player __instance)
        {
            long playerID = __instance.GetPlayerID();
            HardcoreData hardcoreProfile = Hardcore.hardcoreProfiles.Find((HardcoreData profile) => { return profile.profileID == playerID; });
            if (hardcoreProfile.isHardcore)
            {
                Traverse tPlayer = Traverse.Create(__instance);
                tPlayer.Field<bool>("m_firstSpawn").Value = true;
                
                hardcoreProfile.hasDied = true;

                string text = Localization.instance.Localize(Hardcore.GetRandomDeathString());
                Chat.instance.SendText(Talker.Type.Shout, text);

                HitData hit = Hardcore.lastHitData;
                string lastAttackerName = "themself";
                Character attacker = hit.GetAttacker();
                if (attacker)
                {
                    lastAttackerName = attacker.GetHoverName();
                }                

                Traverse tDamages = Traverse.Create(hit.m_damage);
                List<string> damageFieldNames = tDamages.Fields();

                float max = 0.0f;
                string highestDamageType = "m_damage";
                foreach (string fieldName in damageFieldNames)
                {
                    float value = tDamages.Field<float>(fieldName).Value;
                    if (value > max)
                    {
                        max = value;
                        highestDamageType = fieldName;
                    }
                }                                

                string damageTypeString = Localization.instance.Localize("$" + Hardcore.GetRandomDamageTypeString(highestDamageType));                                               

                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ShowMessage", new object[]
                {
                    (int)MessageHud.MessageType.Center,                    
                    Localization.instance.Localize("$hardcore_killed_by_msg_peers", __instance.GetPlayerName(), lastAttackerName, damageTypeString)
                });

                ZNetView nview = tPlayer.Field<ZNetView>("m_nview").Value;
                nview.GetZDO().Set("dead", true);
                nview.InvokeRPC(ZNetView.Everybody, "OnDeath", Array.Empty<object>());                
                tPlayer.Method("CreateDeathEffects").GetValue(new object[] { });
                tPlayer.Field<GameObject>("m_visual").Value.SetActive(false);
                tPlayer.Field<List<Player.Food>>("m_foods").Value.Clear();

                __instance.UnequipAllItems();
                __instance.GetInventory().RemoveAll();

                if (Hardcore.clearCustomSpawn)
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
