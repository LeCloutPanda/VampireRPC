using DiscordRPC;
using HarmonyLib;
using Il2CppSystem;
using Il2CppVampireSurvivors.Framework;
using Il2CppVampireSurvivors.Objects.Characters;
using Il2CppVampireSurvivors.UI;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

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
        static readonly string configFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Configs");
        static readonly string filePath = Path.Combine(configFolder, "VampireRPC.json");

        static readonly string enabledKey = "Enabled";
        static bool enabled;
        static readonly string appId = "1094309317932482680";

        static void UpdateEnabled(bool value) => SetEnabled(value);
        static bool enabledSettingAdded = false;
        static System.Action<bool> enabledSettingChanged = UpdateEnabled;

        System.DateTime start = System.DateTime.Now;

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
                    Details = "Sitting in Main menu",
                    Assets = new Assets()
                    {
                        LargeImageKey = "vampire_survivors_vamp",
                        LargeImageText = "Vampire Survivors",
                        SmallImageKey = "vampire_survivors_vamp",
                        SmallImageText = "Vampire Survivors"
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

        public override void OnLateUpdate()
        {
            base.OnUpdate();

            // Set details to gold count
            // Set state to kill count
            // Set time to be current playthrough time
            // Set large icon to current map and text to map name
            // Set small icon to current hero and text to hero name

            client.SetPresence(new RichPresence()
            {
                Details = $"💰: {69}",
                State = $"💀: {420}",
                Timestamps = new Timestamps()
                {
                    Start = start
                },
                Assets = new Assets()
                {
                    LargeImageKey = "vampire_survivors_vamp",
                    LargeImageText = "Map name",
                    SmallImageKey = "vampire_survivors_vamp",
                    SmallImageText = "Character Name"
                }
            });
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
