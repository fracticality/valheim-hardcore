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
}
