using System.IO;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    public class PrMod : Mod
    {
        private readonly PrModSettings _settings;

        public PrMod(ModContentPack content) : base(content)
        {
            _settings = GetSettings<PrModSettings>();
            var modConfigFile = $"Mod_{Content.FolderName}_{GetType().Name}.xml";
            if (File.Exists(Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename(modConfigFile))))
            {
                PrModSettings.DoMigrations = false;
            }
        }

        public override string SettingsCategory()
        {
            return "LPR_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            _settings.DoWindowContents(rect);
            base.DoSettingsWindowContents(rect);
        }
    }
}
