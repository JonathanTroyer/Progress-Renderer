using System.IO;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{

    public class PRMod : Mod
    {

        public PRModSettings settings;

        public PRMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<PRModSettings>();
            var modConfigFile = $"Mod_{Content.FolderName}_{GetType().Name}.xml";
            if (File.Exists(Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename(modConfigFile))))
            {
                PRModSettings.DoMigrations = false;
            }
        }

        public override string SettingsCategory()
        {
            return "LPR_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            settings.DoWindowContents(rect);
            base.DoSettingsWindowContents(rect);
        }

    }

}
