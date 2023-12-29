using HarmonyLib;
using Il2CppVampireSurvivors;
using Il2CppVampireSurvivors.Framework;
using MelonLoader;
using DiscordRPC;

namespace VampireRPC.src
{
    public static class ModInfo
    {
        public const string Name = "Vampire RPC";
        public const string Description = "Adds in Discord Rich Presence support.";
        public const string Author = "LeCloutPanda";
        public const string Company = "Pandas Hell Hole";
        public const string Version = "1.0.3";
        public const string DownloadLink = "https://github.com/LeCloutPanda/VampireRPC";
    }

    public class VampireRPC : MelonMod
    {
        public enum RichPresenceState
        {
            Null,
            Menu,
            CharacterSelction,
            InGame,
            EndScreen
        }

        private static GameManager manager;
        private static MainGamePage mainGamePage;

        private static readonly string appId = "1094309317932482680";
        private static RichPresenceState state = RichPresenceState.Menu;
        private static DiscordRpcClient client;
        private static string stage = "";
        private static string character = "";
        private static RichPresence presence = new RichPresence();

        private MelonPreferences_Category preferences;
        private static MelonPreferences_Entry<bool> enabled;

        public override void OnInitializeMelon()
        {
            preferences = MelonPreferences.CreateCategory("vampirerpc_preferences");
            enabled = preferences.CreateEntry("enabled", true);

            try
            {
                client = new DiscordRpcClient(appId);
                client.Initialize();
            }
            catch (Exception ex) { MelonLogger.Msg($"Exception thrown when creating RPC client: {ex}"); }

            if (enabled.Value)
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
            catch (Exception ex) { MelonLogger.Error($"Error occured when setting scene: {ex}"); }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (enabled.Value)
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
                catch (Exception ex) { MelonLogger.Error($"Error in FixedUpdate loop: {ex}"); }
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
    }
}
