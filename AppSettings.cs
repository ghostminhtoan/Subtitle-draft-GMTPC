using System.Configuration;
using System.Diagnostics;
using System.Windows;

namespace Subtitle_draft_GMTPC
{
    /// <summary>
    /// Application settings management (C# replacement for My.Settings)
    /// </summary>
    public static class AppSettings
    {
        private static Properties.Settings Settings => Properties.Settings.Default;

        static AppSettings()
        {
            RegisterTranslateTabSettings();
        }

        private static void RegisterTranslateTabSettings()
        {
            RegisterStringSetting("SentencePromptName1", "Prompt 1");
            RegisterStringSetting("SentencePromptName2", "Prompt 2");
            RegisterStringSetting("SentencePromptName3", "Prompt 3");
            RegisterStringSetting("SentencePromptName4", "Prompt 4");
            RegisterStringSetting("SentencePromptName5", "Prompt 5");
            RegisterStringSetting("SentencePromptContent1", "");
            RegisterStringSetting("SentencePromptContent2", "");
            RegisterStringSetting("SentencePromptContent3", "");
            RegisterStringSetting("SentencePromptContent4", "");
            RegisterStringSetting("SentencePromptContent5", "");

            RegisterStringSetting("OneWordPromptName1", "Prompt 1");
            RegisterStringSetting("OneWordPromptName2", "Prompt 2");
            RegisterStringSetting("OneWordPromptName3", "Prompt 3");
            RegisterStringSetting("OneWordPromptName4", "Prompt 4");
            RegisterStringSetting("OneWordPromptName5", "Prompt 5");
            RegisterStringSetting("OneWordPromptContent1", "");
            RegisterStringSetting("OneWordPromptContent2", "");
            RegisterStringSetting("OneWordPromptContent3", "");
            RegisterStringSetting("OneWordPromptContent4", "");
            RegisterStringSetting("OneWordPromptContent5", "");
        }

        private static void RegisterStringSetting(string key, string defaultValue)
        {
            if (Settings.Properties[key] != null)
            {
                return;
            }

            var property = new SettingsProperty(key)
            {
                PropertyType = typeof(string),
                Provider = Settings.Providers["LocalFileSettingsProvider"],
                DefaultValue = defaultValue,
                IsReadOnly = false,
                SerializeAs = SettingsSerializeAs.String
            };
            property.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
            property.Attributes.Add(typeof(DefaultSettingValueAttribute), new DefaultSettingValueAttribute(defaultValue));
            property.Attributes.Add(typeof(DebuggerNonUserCodeAttribute), new DebuggerNonUserCodeAttribute());
            Settings.Properties.Add(property);
        }

        public static string GetString(string key, string fallback = "")
        {
            var value = Settings[key] as string;
            return value ?? fallback;
        }

        public static void SetString(string key, string value)
        {
            Settings[key] = value ?? string.Empty;
        }

        public static void MigrateLegacyTranslatePrompts()
        {
            MigrateStringValue("SentencePromptName1", PromptName1);
            MigrateStringValue("SentencePromptName2", PromptName2);
            MigrateStringValue("SentencePromptName3", PromptName4);
            MigrateStringValue("SentencePromptName4", PromptName5);
            MigrateStringValue("OneWordPromptName1", PromptName3);

            MigrateStringValue("SentencePromptContent1", PromptContent1);
            MigrateStringValue("SentencePromptContent2", PromptContent2);
            MigrateStringValue("SentencePromptContent3", PromptContent4);
            MigrateStringValue("SentencePromptContent4", PromptContent5);
            MigrateStringValue("OneWordPromptContent1", PromptContent3);
        }

        private static void MigrateStringValue(string key, string legacyValue)
        {
            if (string.IsNullOrWhiteSpace(legacyValue))
            {
                return;
            }

            var currentValue = GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(currentValue) || currentValue.StartsWith("Prompt "))
            {
                SetString(key, legacyValue);
            }
        }

        public static string TranslatePrompt
        {
            get => Settings.TranslatePrompt;
            set { Settings.TranslatePrompt = value; }
        }

        public static string PromptName1
        {
            get => Settings.PromptName1;
            set { Settings.PromptName1 = value; }
        }

        public static string PromptName2
        {
            get => Settings.PromptName2;
            set { Settings.PromptName2 = value; }
        }

        public static string PromptName3
        {
            get => Settings.PromptName3;
            set { Settings.PromptName3 = value; }
        }

        public static string PromptName4
        {
            get => Settings.PromptName4;
            set { Settings.PromptName4 = value; }
        }

        public static string PromptName5
        {
            get => Settings.PromptName5;
            set { Settings.PromptName5 = value; }
        }

        public static string PromptContent1
        {
            get => Settings.PromptContent1;
            set { Settings.PromptContent1 = value; }
        }

        public static string PromptContent2
        {
            get => Settings.PromptContent2;
            set { Settings.PromptContent2 = value; }
        }

        public static string PromptContent3
        {
            get => Settings.PromptContent3;
            set { Settings.PromptContent3 = value; }
        }

        public static string PromptContent4
        {
            get => Settings.PromptContent4;
            set { Settings.PromptContent4 = value; }
        }

        public static string PromptContent5
        {
            get => Settings.PromptContent5;
            set { Settings.PromptContent5 = value; }
        }

        public static int TextToSubMaxChars
        {
            get => Settings.TextToSubMaxChars;
            set { Settings.TextToSubMaxChars = value; }
        }

        public static double TextToSubCps
        {
            get => Settings.TextToSubCps;
            set { Settings.TextToSubCps = value; }
        }

        public static int TextToSubGap
        {
            get => Settings.TextToSubGap;
            set { Settings.TextToSubGap = value; }
        }

        public static void Save()
        {
            try
            {
                Settings.Save();
            }
            catch { }
        }
    }
}
