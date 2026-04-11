using System.Windows;

namespace Subtitle_draft_GMTPC
{
    /// <summary>
    /// Application settings management (C# replacement for My.Settings)
    /// </summary>
    public static class AppSettings
    {
        private static Properties.Settings Settings => Properties.Settings.Default;

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
