using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModUtils;
using UnityEngine;
using UnityEngine.UI;
using fastJSON;

namespace Hardcore
{
    [BepInPlugin(Hardcore.UMID, Hardcore.ModName, Hardcore.Version)]    
    public class Hardcore : BaseUnityPlugin
    {
        public const string UMID = "fracticality.valheim.hardcore";
        public const string Version = "1.3.1";
        public const string ModName = "Hardcore";
        public static readonly string ModPath = Path.GetDirectoryName(typeof(Hardcore).Assembly.Location);

        Harmony _Harmony;
        public static ManualLogSource Log;        

        public static HardcoreData newProfileData;
        public static List<HardcoreData> hardcoreProfiles = new List<HardcoreData>();        
        public static HitData lastHitData;

        public static GameObject uiPanel;
        public static GameObject hardcoreLabel;        

        public struct Settings
        {
            public static ConfigEntry<bool> Enabled;
            public static ConfigEntry<bool> ClearMapOnDeath;
            public static ConfigEntry<bool> ClearCustomSpawn;
            public static ConfigEntry<bool> HardcoreOnly;
        }
               
        private void Awake()
        {

			Log = Logger;                        

            Settings.Enabled = Config.Bind("General", "Enabled", true, $"Enable/disable {ModName}'s functionality.");
            Settings.ClearMapOnDeath = Config.Bind("General", "ClearMapOnDeath", true, "Whether or not to clear map data on death. Disable if map syncing is in play.");
            Settings.ClearCustomSpawn = Config.Bind("General", "ClearCustomSpawn", true, "Whether or not to clear the player's bed spawn point on death.");
            Settings.HardcoreOnly = Config.Bind("General", "HardcoreOnly", false, "If enabled, will force all new characters to be Hardcore and filters non-Hardcore characters from the character list.");

            Settings.Enabled.SettingChanged += Enabled_SettingChanged;            

            if (Settings.Enabled.Value)
            {
                Init();
                Settings.HardcoreOnly.SettingChanged += HardcoreOnly_SettingChanged;
            }
        }

        private void Init()
        {
            _Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            TranslationUtils.InsertTranslations(ModName, ModPath);
        }

        private void Enabled_SettingChanged(object sender, EventArgs e)
        {
            if (Settings.Enabled.Value)
            {
                Init();
                Settings.HardcoreOnly.SettingChanged += HardcoreOnly_SettingChanged;                
            }
            else
            {
                OnDestroy();
                Settings.HardcoreOnly.SettingChanged -= HardcoreOnly_SettingChanged;
            }

            HardcoreOnly_SettingChanged(null, null);
        }

        private void HardcoreOnly_SettingChanged(object sender, EventArgs e)
        {
            Traverse.Create(FejdStartup.instance).Field("m_profiles").SetValue(null);
            Traverse.Create(FejdStartup.instance).Field("m_profileIndex").SetValue(0);
            Traverse.Create(FejdStartup.instance).Method("UpdateCharacterList").GetValue();
        }

        private void Update()
        {
            if (!Settings.Enabled.Value)
            {
                return;
            }

            FejdStartup fejdStartup = FejdStartup.instance;
            if (!fejdStartup)
            {
                return;
            }

            if (!hardcoreLabel)
            {
                InitHardcoreLabel();
            }

            if (!uiPanel)
            {
                InitUIPanel();
            }

            if (fejdStartup.m_characterSelectScreen.activeInHierarchy)
            {                
                Traverse tInstance = Traverse.Create(fejdStartup);
                int profileIndex = tInstance.Field<int>("m_profileIndex").Value;

                List<PlayerProfile> profiles = tInstance.Field<List<PlayerProfile>>("m_profiles").Value;
                if (profiles?.Count > 0)
                {
                    PlayerProfile profile = profiles[profileIndex];
                    long profileID = profile.GetPlayerID();
                    bool isHardcore = hardcoreProfiles.Exists((HardcoreData data) => { return data.profileID == profileID && data.isHardcore; });

                    hardcoreLabel.SetActive(isHardcore);
                }
            }            

            if (Input.GetKeyDown(KeyCode.F4))
            {
                
            }
        }

        private void OnDestroy()
        {
            if (_Harmony != null) _Harmony.UnpatchSelf();
            if (uiPanel != null) Destroy(uiPanel);
            if (hardcoreLabel != null) Destroy(hardcoreLabel);
        }

        private static void InitGameObjects()
        {
            InitHardcoreLabel();
            InitUIPanel();
        }

        private static void InitHardcoreLabel()
        {
            GameObject selectScreen = FejdStartup.instance.m_characterSelectScreen;

            Text characterName = selectScreen.transform.Find("SelectCharacter").Find("CharacterName").GetComponentInChildren<Text>();

            GameObject hardcoreLabelGO = new GameObject("HardcoreLabel");
            RectTransform rect = hardcoreLabelGO.AddComponent<RectTransform>();
            rect.position = (characterName.transform as RectTransform).position + new Vector3(0, 60);
            rect.sizeDelta = new Vector2((characterName.transform as RectTransform).rect.width, 40);

            Text text = hardcoreLabelGO.AddComponent<Text>();
            text.font = characterName.font;
            text.color = Color.red;
            text.fontSize = characterName.fontSize - 12;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = Localization.instance.Localize("($hardcore_hardcore)");

            Outline outline = hardcoreLabelGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            hardcoreLabelGO.transform.SetParent(characterName.transform);

            hardcoreLabel = hardcoreLabelGO;

            Log.LogInfo($"{hardcoreLabel} Initialized.");            
        }

        private static void InitUIPanel()
        {
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

            uiPanel = hardcorePanel;
            Log.LogInfo($"{uiPanel} Initialized.");
        }

        public static HardcoreData GetHardcoreDataForProfileID(long profileID)
        {
            return hardcoreProfiles.Find((HardcoreData profile) => { return profile.profileID == profileID; });
        }

        public static void ResetHardcorePlayer(PlayerProfile playerProfile)
        {            
            Traverse.Create(Player.m_localPlayer)
                    .Field<Skills>("m_skills").Value
                    .Clear();                        
            
            Player.m_localPlayer.GetKnownTexts().RemoveAll((KeyValuePair<string, string> pair) => { return pair.Key.StartsWith("<|>"); });

            // Clear out custom EquipmentSlotInventory and QuickSlotInventory, if applicable
            AppDomain currentDomain = AppDomain.CurrentDomain;
            List<Assembly> assemblies = new List<Assembly>(currentDomain.GetAssemblies());            
            if (assemblies.Find((Assembly assembly) => { return assembly.GetName().Name == "EquipmentAndQuickSlots"; }) != null)
            {                
                Component extendedPlayerData = Player.m_localPlayer.GetComponent("ExtendedPlayerData");                
                if (extendedPlayerData)
                {                    
                    Traverse tExtendedPlayerData = Traverse.Create(extendedPlayerData);
                    tExtendedPlayerData.Field<Inventory>("EquipmentSlotInventory").Value.RemoveAll();
                    tExtendedPlayerData.Field<Inventory>("QuickSlotInventory").Value.RemoveAll();
                }
            }            
            // End clear custom inventories

            if (Settings.ClearMapOnDeath.Value)
            {
                // Reset sync data for MapSharingMadeEasy to prevent removal of all shared pins on next sync
                ZNetView nview = Traverse.Create(Player.m_localPlayer).Field<ZNetView>("m_nview").Value;
                if (nview != null)
                {
                    string syncData = nview.GetZDO().GetString("playerSyncData", string.Empty);
                    if (!string.IsNullOrEmpty(syncData))
                    {
                        nview.GetZDO().Set("playerSyncData", string.Empty);
                    }
                }
                // End reset sync data

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
        }
        
        public static bool SaveDataToDisk()
        {            
            string profilesPath = Path.Combine(ModPath, "Profiles");
            Directory.CreateDirectory(profilesPath);
            string filename = Path.Combine(profilesPath, "hardcore_profiles.json");
            string fileOld = string.Copy(filename) + ".old";
            string fileNew = string.Copy(filename) + ".new";

            string json = JSON.ToNiceJSON(hardcoreProfiles, new JSONParameters() { UseExtensions = false });
            File.WriteAllText(fileNew, json);

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

            //Directory.CreateDirectory(Utils.GetSaveDataPath() + "/mod_data");
            //string filename = Utils.GetSaveDataPath() + "/mod_data/hardcore_characters.fch";
            //string fileOld = string.Copy(filename) + ".old";
            //string fileNew = string.Copy(filename) + ".new";

            //FileStream fStream = File.Create(fileNew);
            //var bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //bFormatter.Serialize(fStream, hardcoreProfiles);

            //fStream.Flush(true);
            //fStream.Close();
            //fStream.Dispose();            

            //if (File.Exists(filename))
            //{
            //    if (File.Exists(fileOld))
            //    {
            //        File.Delete(fileOld);
            //    }
            //    File.Move(filename, fileOld);
            //}
            //File.Move(fileNew, filename);
            //return true;
        }        

        // TODO: Change savefile to use json rather than arbitrary binary deserialization
        public static bool LoadDataFromDisk()
        {
            string profilesPath = Path.Combine(ModPath, "Profiles");
            Directory.CreateDirectory(profilesPath);
            string filename = Path.Combine(profilesPath, "hardcore_profiles.json");
            string fileOld = string.Copy(filename) + ".old";

            Log.LogInfo($"Loading hardcore profiles...");
            if (!File.Exists(filename))
            {
                Log.LogWarning("  Data file missing! Searching for backup...");
                if (File.Exists(fileOld))
                {
                    Log.LogInfo("  Backup found! Restoring...");
                    File.Move(fileOld, filename);
                }
                else
                {
                    Log.LogWarning("  No backup found");
                    return false;
                }
            }

            string json = File.ReadAllText(filename);
            hardcoreProfiles = JSON.ToObject<List<HardcoreData>>(json);            

            List<PlayerProfile> playerProfiles = PlayerProfile.GetAllPlayerProfiles();

            // Remove extraneous hardcore profiles
            hardcoreProfiles.RemoveAll(hp =>
            {
                return !playerProfiles.Exists(pp =>
                {
                    return pp.GetPlayerID() == hp.profileID;
                });
            });

            Log.LogInfo($"  Loaded {hardcoreProfiles.Count} Hardcore Profile(s)");

            return true;

            //string filename = Utils.GetSaveDataPath() + "/mod_data/hardcore_characters.fch";
            //string fileOld = string.Copy(filename) + ".old";            
            //FileStream fStream;                  

            //Log.LogInfo($"Loading hardcore character list...");
            //try
            //{
            //    if (!File.Exists(filename))
            //    {
            //        Log.LogWarning("- Data file missing! Searching for backup...");
            //        if (File.Exists(fileOld))
            //        {
            //            Log.LogInfo("- Backup found! Restoring...");
            //            File.Move(fileOld, filename);
            //        }
            //        else
            //        {
            //            Log.LogWarning("- Backup missing!");
            //            return false;
            //        }
            //    }
            //    fStream = File.OpenRead(filename);
            //}
            //catch
            //{
            //    Log.LogError("Failed to open " + filename);
            //    return false;
            //}            

            //try
            //{
            //    var bFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();                
            //    hardcoreProfiles = (List<HardcoreData>)bFormatter.Deserialize(fStream);                

            //    List<PlayerProfile> playerProfiles = PlayerProfile.GetAllPlayerProfiles();
            //    foreach (HardcoreData profileData in hardcoreProfiles)
            //    {
            //        if (!playerProfiles.Exists((PlayerProfile profile) => { return profile.GetPlayerID() == profileData.profileID; }))
            //        {
            //            Log.LogInfo($"- Player ID {profileData.profileID} no longer exists! Removing...");
            //            hardcoreProfiles.Remove(profileData);
            //        }
            //    }

            //    Log.LogInfo($"Loaded {hardcoreProfiles.Count} Hardcore Profile(s)");
            //}
            //catch (Exception e)
            //{                
            //    Log.LogError("Failed to read from " + filename + ": " + e.Message);
            //    return false;
            //}
            //finally
            //{                
            //    fStream.Dispose();
            //}

            //return true;
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
}
