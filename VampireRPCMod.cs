using DiscordRPC;
using HarmonyLib;
using Il2CppVampireSurvivors;
using Il2CppVampireSurvivors.Framework;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace VampireRPC
{
    public class ConfigData
    {
        public string Name = ModInfo.Name;
        public bool UseDiscordRPC = false;
    }

    public static class ModInfo
    {
        public const string Name = "Vampire RPC";
        public const string Description = "Adds in Discord Rich Presence support.";
        public const string Author = "LeCloutPanda";
        public const string Company = "Pandas Hell Hole";
        public const string Version = "1.0.2.0";
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

        public static ConfigData config = new ConfigData();

        static readonly string appId = "1094309317932482680";

        static GameManager manager;
        static MainGamePage mainGamePage;

        
        RichPresenceState state = RichPresenceState.Menu;

        private DiscordRpcClient client;

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            ValidateConfig();

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("dev.panda.debugmode");
            harmony.PatchAll();

            try
            {
                client = new DiscordRpcClient(appId);
                client.Initialize();
            }
            catch (System.Exception ex) { MelonLogger.Msg($"Exception thrown when creating RPC client: {ex}"); }

            if (config.UseDiscordRPC)
            {

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
            client.ClearPresence();
            client.Dispose();
        }

        string stage = "";
        string character = "";
        RichPresence presence = new RichPresence();

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);

            try
            {
                MelonLogger.Msg($"Loaded scene: {sceneName.ToLower()}");

                switch (sceneName.ToLower())
                {
                    case "preloader":
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
            catch(System.Exception ex) { MelonLogger.Error($"Error occured when setting scene: {ex}"); }
        }

        DateTime lastModified;

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();

            DateTime lastWriteTime = File.GetLastWriteTime(filePath);

            if (lastModified != lastWriteTime)
            {
                lastModified = lastWriteTime);
                LoadConfig();
                MelonLogger.Msg($"[{lastModified.ToString("HH:mm:ss")}] Reloading Config for {ModInfo.Name}");
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (config.UseDiscordRPC == true)
            {
                try
                {
                    switch (state)
                    {
                        case RichPresenceState.Menu:
                            presence.Details = "Main Menu";
                            presence.Assets = new Assets()
                            {
                                LargeImageKey = "logo",
                                LargeImageText = "Vampire Survivors"
                            };
                            break;

                        case RichPresenceState.InGame:
                            if (client == null) MelonLogger.Error("Client is null");
                            if (manager == null) MelonLogger.Error("Manager is null");
                            if (mainGamePage == null) MelonLogger.Error("MainGamePage is null");

                            if (client != null && manager != null && mainGamePage != null)
                            {

                                var currentStage = manager.Stage.ActiveStageData.stageName;
                                var stageId = currentStage != null ? currentStage.ToLower().Replace(".", " ").Replace(" ", "_") : "logo";
                                var stageName = currentStage != null ? currentStage : "null";

                                var currentCharacter = manager.Player.CurrentCharacterData.charName;
                                var characterId = currentCharacter.ToLower().Replace(".", " ").Replace("'", " ").Replace(" ", "_");

                                var currentLevel = manager.Player.Level;

                                if (stage != stageId) MelonLogger.Msg($"Current Stage: {stage = stageId}");
                                if (stage == "logo" && state != RichPresenceState.EndScreen) state = RichPresenceState.EndScreen;
                                if (character != characterId) MelonLogger.Msg($"Current Character: {character = characterId}");

                                presence.Details = $"💀 {mainGamePage._EnemiesText.text} 💰 {mainGamePage._CoinsText.text}";
                                presence.State = $"{mainGamePage._TimeText.text}";
                                presence.Assets = new Assets()
                                {
                                    LargeImageKey = $"{stageId}",
                                    LargeImageText = $"{stageName}",
                                    SmallImageKey = $"{characterId}",
                                    SmallImageText = $"{currentCharacter} lvl {currentLevel}"
                                };
                            }
                            break;

                        case RichPresenceState.EndScreen:
                            presence.Details = "Finishing game";
                            presence.State = "";
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
                catch (System.Exception ex) { MelonLogger.Error($"Error in FixedUpdate loop: {ex}"); }
            }
            else
            {
                client.ClearPresence();
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnUpdate))]
        static class PatchGameManager { static void Postfix(GameManager __instance) { if (manager == null) MelonLogger.Msg($"Setting GameManager variable: {manager = __instance}"); } }

        [HarmonyPatch(typeof(MainGamePage), nameof(MainGamePage.Awake))]
        static class PatchMainGamePage { static void Postfix(MainGamePage __instance) => MelonLogger.Msg($"Setting MainGamePage variable: {mainGamePage = __instance}"); }

        private static void ValidateConfig()
        {
            try
            {
                if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);
                if (!File.Exists(filePath)) File.WriteAllText(filePath, JsonConvert.SerializeObject(new ConfigData { }, Formatting.Indented));

                LoadConfig();
            }
            catch (System.Exception ex) { MelonLogger.Error($"Error validating Config: {ex}"); }
        }

        private static void LoadConfig()
        {
            JObject json = JObject.Parse(File.ReadAllText(filePath) ?? "{}");

            config.Name = (string)json.GetValue("Name");
            config.UseDiscordRPC = (bool)json.GetValue("UseDiscordRPC");
        }
    }
}
