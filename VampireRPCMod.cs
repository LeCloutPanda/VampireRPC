using DiscordRPC;
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Resources;
using Il2CppVampireSurvivors;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework;
using Il2CppVampireSurvivors.Objects.Characters;
using Il2CppVampireSurvivors.Objects.Characters.Enemies;
using Il2CppVampireSurvivors.UI;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static MelonLoader.MelonLogger;

namespace VampireRPC
{
    public class ConfigData
    {
        public bool Enabled { get; set; }
    }

    public static class ModInfo
    {
        public const string Name = "Vampire RPC";
        public const string Description = "Adds in Discord Rich Presence support.";
        public const string Author = "LeCloutPanda";
        public const string Company = "Pandas Hell Hole";
        public const string Version = "1.0.0";
        public const string DownloadLink = "https://github.com/LeCloutPanda/VampireRPC";
    }

    public class VampireRPCMod : MelonMod
    {
        public enum RichPresenceState
        {
            Null,
            Menu,
            CharacterSelction,
            InGame,
            EndScreen
        }

        static readonly string configFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Configs");
        static readonly string filePath = Path.Combine(configFolder, "VampireRPC.json");

        static readonly string enabledKey = "Enabled";
        static bool enabled;
        static readonly string appId = "1094309317932482680";

        static void UpdateEnabled(bool value) => SetEnabled(value);
        static bool enabledSettingAdded = false;
        static System.Action<bool> enabledSettingChanged = UpdateEnabled;

        static GameManager manager;
        static MainGamePage mainGamePage;

        RichPresenceState state = RichPresenceState.Menu;

        private DiscordRpcClient client;

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("dev.panda.debugmode");
            harmony.PatchAll();

            ValidateConfig();

            if (enabled)
            {
                client = new DiscordRpcClient(appId);
                client.Initialize();
                client.SetPresence(new RichPresence()
                {
                    Details = "Main menu",
                    Assets = new Assets()
                    {
                        LargeImageKey = "logo",
                        LargeImageText = "Vampire Survivors"
                    }
                });
            }
        }

        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("Closing game");
            client.ClearPresence();
            client.Dispose();
        }

        string stage;
        string character;
        RichPresence presence = new RichPresence();

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            MelonLogger.Msg(sceneName);

            switch (sceneName.ToLower())
            {
                case "mainmenu":
                    state = RichPresenceState.Menu;
                    break;

                case "gameplay":
                    state = RichPresenceState.InGame;
                    break;

                default:
                    state = RichPresenceState.Null;
                    break;

            }
        }

        public override void OnFixedUpdate() 
        {
            base.OnFixedUpdate();

            if (manager != null && mainGamePage != null)
            {

                var currentStage = manager.Stage.ActiveStageData.stageName;
                var stageId = currentStage != null ? currentStage.ToLower().Replace(".", " ").Replace(" ", "_") : "logo";
                var stageName = currentStage != null ? currentStage : "null";

                var currentCharacter = manager.Player.CurrentCharacterData.charName;
                var characterId = currentCharacter.ToLower().Replace(".", " ").Replace("'", " ").Replace(" ", "_");

                var currentLevel = manager.Player.Level;

                if (stage != stageId) MelonLogger.Msg(stage = stageId);
                if (character !=  characterId) MelonLogger.Msg(character = characterId);

                switch(state)
                {
                    case RichPresenceState.Menu:
                        presence.Details = "Main Menu";
                        presence.Assets = new Assets()
                        {
                            LargeImageKey = "logo",
                            LargeImageText = "Vampire Survivors"
                        };
                        break;

                    case RichPresenceState.CharacterSelction:
                        presence.Details = "Selecting a character";
                        presence.Assets = new Assets()
                        {
                            LargeImageKey = "logo",
                            LargeImageText = "Vampire Survivors"
                        };                     
                        break;


                    case RichPresenceState.InGame:
                        presence.Details = $"💀 {mainGamePage._EnemiesText.text} 💰 {mainGamePage._CoinsText.text}";
                        presence.State = $"{mainGamePage._TimeText.text}";
                        presence.Assets = new Assets()
                        {
                            LargeImageKey = $"{stageId}",
                            LargeImageText = $"{stageName}",
                            SmallImageKey = $"{characterId}",
                            SmallImageText = $"{currentCharacter} lvl {currentLevel}"
                        };
                        break;

                    case RichPresenceState.EndScreen:
                        presence.Details = "Finishing game";
                        presence.Assets = new Assets()
                        {
                            LargeImageKey = "logo",
                            LargeImageText = "Vampire Survivors"
                        };
                        break;

                    default:
                    case RichPresenceState.Null:
                        presence.Details = "Playing Vampire Survivors";
                        presence.Assets = new Assets()
                        {
                            LargeImageKey = "logo",
                            LargeImageText = "Vampire Survivors"
                        };
                        break;
                }


                client.SetPresence(presence);
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
        static class PatchGameManager
        {
            static void Postfix(GameManager __instance)
            {
                manager = __instance;
            }
        }

        [HarmonyPatch(typeof(OptionsController), nameof(OptionsController.BuildGameplayPage))]
        static class PatchBuildGameplayPage
        {
            static void Postfix(OptionsController __instance)
            {
                if (!enabledSettingAdded) __instance.AddTickBox("Use Discord RPC", enabled, enabledSettingChanged, false);
                enabledSettingAdded = true;
            }
        }

        [HarmonyPatch(typeof(OptionsController), nameof(OptionsController.AddVisibleJoysticks))]
        static class PatchAddVisibleJoysticks { static void Postfix() => enabledSettingAdded = false; }


        [HarmonyPatch(typeof(MainGamePage), nameof(MainGamePage.Awake))]
        static class PatchMainGamePage { static void Postfix(MainGamePage __instance) => mainGamePage = __instance; }

        private static void SetEnabled(bool value)
        {
            ModifyConfigValue(enabledKey, value);
            enabled = value;
        }

        private static void ValidateConfig()
        {
            try
            {
                if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);
                if (!File.Exists(filePath)) File.WriteAllText(filePath, JsonConvert.SerializeObject(new ConfigData { }, Formatting.Indented));

                LoadConfig();
            }
            catch (System.Exception ex) { MelonLogger.Msg($"Error: {ex}"); }
        }

        private static void ModifyConfigValue<T>(string key, T value)
        {
            string file = File.ReadAllText(filePath);
            JObject json = JObject.Parse(file);

            if (!json.ContainsKey(key)) json.Add(key, JToken.FromObject(value));
            else
            {
                System.Type type = typeof(T);
                JToken newValue = JToken.FromObject(value);

                if (type == typeof(string)) json[key] = newValue.ToString();
                else if (type == typeof(int)) json[key] = newValue.ToObject<int>();
                else if (type == typeof(bool)) json[key] = newValue.ToObject<bool>();
                else { MelonLogger.Msg($"Unsupported type '{type.FullName}'"); return; }
            }

            string finalJson = JsonConvert.SerializeObject(json, Formatting.Indented);
            File.WriteAllText(filePath, finalJson);
        }

        private static void LoadConfig() => enabled = JObject.Parse(File.ReadAllText(filePath) ?? "{}").Value<bool>(enabledKey);
    }
}
