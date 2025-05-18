using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{

    public class PRMod : Mod
    {

        public PRModSettings settings;

        public static AccessTools.FieldRef<object, bool> SkipCustomRenderingRef = null;

        public PRMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<PRModSettings>();
            var modConfigFile = $"Mod_{Content.FolderName}_{GetType().Name}.xml";
            if (File.Exists(Path.Combine(GenFilePaths.ConfigFolderPath, GenText.SanitizeFilename(modConfigFile))))
            {
                PRModSettings.DoMigrations = false;
            }

            //Only test for Camera+ when mods are loaded, otherwise this can end up never setting the type properly
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (Type.GetType("CameraPlus.CameraPlusMain") != null)
                {
                    SkipCustomRenderingRef = AccessTools.FieldRefAccess<bool>("CameraPlus.CameraPlusMain:skipCustomRendering");
                }
            });
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
