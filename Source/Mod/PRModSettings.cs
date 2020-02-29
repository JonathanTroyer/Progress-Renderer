﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{

    public class PRModSettings : ModSettings
    {

        public const float GapHeight = 10f;
        public static string[] SupportedEncodings = { "png_unity", "jpg_unity" };
        
        private static bool DefaultEnabled = true;
        private static RenderFeedback DefaultRenderFeedback = RenderFeedback.Window;
        private static bool DefaultRenderDesignations = false;
        private static bool DefaultRenderThingIcons = false;
        private static bool DefaultRenderGameConditions = true;
        private static bool DefaultRenderWeather = true;
        private static int DefaultSmoothRenderAreaSteps = 0;
        private static int DefaultInterval = 24;
        private static int DefaultTimeOfDay = 8;
        private static string DefaultEncoding = SupportedEncodings[1];
        private static int DefaultPixelPerCell = 32;
        private static int DefaultOutputImageFixedHeight = 0;
        private static bool DefaultCreateSubdirs = false;
        private static FileNamePattern DefaultFileNamePattern = FileNamePattern.DateTime;

        public static bool enabled = DefaultEnabled;
        public static RenderFeedback renderFeedback = DefaultRenderFeedback;
        public static bool renderDesignations = DefaultRenderDesignations;
        public static bool renderThingIcons = DefaultRenderThingIcons;
        public static bool renderGameConditions = DefaultRenderGameConditions;
        public static bool renderWeather = DefaultRenderWeather;
        public static int smoothRenderAreaSteps = DefaultSmoothRenderAreaSteps;
        public static int interval = DefaultInterval;
        public static int timeOfDay = DefaultTimeOfDay;
        public static string encoding = DefaultEncoding;
        public static int pixelPerCell = DefaultPixelPerCell;
        public static int outputImageFixedHeight = DefaultOutputImageFixedHeight;
        public static string exportPath;
        public static bool createSubdirs = DefaultCreateSubdirs;
        public static FileNamePattern fileNamePattern = DefaultFileNamePattern;

        private static string outputImageFixedHeightBuffer;

        public PRModSettings() : base()
        {
            if (exportPath.NullOrEmpty())
            {
                exportPath = DesktopPath;
            }
        }

        public void DoWindowContents(Rect rect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(rect);
            ls.Gap(GapHeight);

            ls.CheckboxLabeled("LPR_SettingsEnabledLabel".Translate(), ref enabled, "LPR_SettingsEnabledDescription".Translate());
            ls.Gap(GapHeight);

            // Backup original values
            TextAnchor backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsRenderFeedbackLabel".Translate(), ("LPR_RenderFeedback_" + renderFeedback).Translate()))
            {
                List<FloatMenuOption> menuEntries = new List<FloatMenuOption>();
                menuEntries.Add(new FloatMenuOption(("LPR_RenderFeedback_" + RenderFeedback.None).Translate(), delegate
                {
                    renderFeedback = RenderFeedback.None;
                }));
                menuEntries.Add(new FloatMenuOption(("LPR_RenderFeedback_" + RenderFeedback.Message).Translate(), delegate
                {
                    renderFeedback = RenderFeedback.Message;
                }));
                menuEntries.Add(new FloatMenuOption(("LPR_RenderFeedback_" + RenderFeedback.Window).Translate(), delegate
                {
                    renderFeedback = RenderFeedback.Window;
                }));
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            // Restore original values
            Text.Anchor = backupAnchor;

            ls.GapGapLine(GapHeight);

            ls.CheckboxGroupLabeled("LPR_SettingsRenderSettingsLabel".Translate(), "LPR_SettingsRenderSettingsDescription".Translate(), "LPR_SettingsRenderDesignationsLabel".Translate(), ref renderDesignations, "LPR_SettingsRenderDesignationsDescription".Translate(), "LPR_SettingsRenderThingIconsLabel".Translate(), ref renderThingIcons, "LPR_SettingsRenderThingIconsDescription".Translate());
            ls.CheckboxGroupLabeled(null, "LPR_SettingsRenderSettingsDescription".Translate(), "LPR_SettingsRenderGameConditionsLabel".Translate(), ref renderGameConditions, "LPR_SettingsRenderGameConditionsDescription".Translate(), "LPR_SettingsRenderWeatherLabel".Translate(), ref renderWeather, "LPR_SettingsRenderWeatherDescription".Translate());
            ls.Gap(GapHeight);
            ls.SliderLabeled("LPR_SettingsSmoothRenderAreaStepsLabel".Translate(), ref smoothRenderAreaSteps, 0, 30, null, "LPR_SettingsSmoothRenderAreaStepsDescription".Translate());
            ls.GapGapLine(GapHeight);

            ls.FixedFrequencySliderLabeled("LPR_SettingsIntervalLabel".Translate(), ref interval, "LPR_SettingsIntervalDescription".Translate());
            ls.Gap(GapHeight);
            ls.SliderLabeled("LPR_SettingsTimeOfDayLabel".Translate(), ref timeOfDay, 0, 23, "00 h", "LPR_SettingsTimeOfDayDescription".Translate());
            ls.GapGapLine(GapHeight);

            // Backup original values
            backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsEncodingLabel".Translate(), ("LPR_ImgEncoding_" + encoding).Translate()))
            {
                List<FloatMenuOption> menuEntries = new List<FloatMenuOption>();
                menuEntries.Add(new FloatMenuOption(("LPR_ImgEncoding_" + SupportedEncodings[0]).Translate(), delegate
                {
                    encoding = SupportedEncodings[0];
                }));
                menuEntries.Add(new FloatMenuOption(("LPR_ImgEncoding_" + SupportedEncodings[1]).Translate(), delegate
                {
                    encoding = SupportedEncodings[1];
                }));
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            // Restore original values
            Text.Anchor = backupAnchor;

            ls.Gap(GapHeight);
            ls.SliderLabeled("LPR_SettingsPixelPerCellLabel".Translate(), ref pixelPerCell, 1, 64, "##0 ppc", "LPR_SettingsPixelPerCellDescription".Translate());
            ls.Gap(GapHeight);
            ls.IntegerFieldLabeled("LPR_SettingsOutputImageFixedHeightLabel".Translate(), ref outputImageFixedHeight, ref outputImageFixedHeightBuffer, "LPR_SettingsOutputImageFixedHeightAdditionalInfo".Translate(), "LPR_SettingsOutputImageFixedHeightDescription".Translate());
            ls.Gap(GapHeight);
            ls.TextFieldLabeled("LPR_SettingsExportPathLabel".Translate(), ref exportPath, "LPR_SettingsExportPathDescription".Translate());
            ls.Gap(GapHeight);
            ls.CheckboxLabeled("LPR_SettingsCreateSubdirsLabel".Translate(), ref createSubdirs, "LPR_SettingsCreateSubdirsDescription".Translate());
            ls.Gap(GapHeight);
            
            // Backup original values
            backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsFileNamePatternLabel".Translate(), ("LPR_FileNamePattern_" + fileNamePattern).Translate()))
            {
                List<FloatMenuOption> menuEntries = new List<FloatMenuOption>();
                menuEntries.Add(new FloatMenuOption(("LPR_FileNamePattern_" + FileNamePattern.DateTime).Translate(), delegate
                {
                    fileNamePattern = FileNamePattern.DateTime;
                }));
                menuEntries.Add(new FloatMenuOption(("LPR_FileNamePattern_" + FileNamePattern.Numbered).Translate(), delegate
                {
                    fileNamePattern = FileNamePattern.Numbered;
                }));
                menuEntries.Add(new FloatMenuOption(("LPR_FileNamePattern_" + FileNamePattern.BothTmpCopy).Translate(), delegate
                {
                    fileNamePattern = FileNamePattern.BothTmpCopy;
                }));
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            // Restore original values
            Text.Anchor = backupAnchor;

            ls.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enabled, "enabled", DefaultEnabled);
            Scribe_Values.Look(ref renderFeedback, "renderFeedback", DefaultRenderFeedback);
            Scribe_Values.Look(ref renderDesignations, "renderDesignations", DefaultRenderDesignations);
            Scribe_Values.Look(ref renderThingIcons, "renderThingIcons", DefaultRenderThingIcons);
            Scribe_Values.Look(ref renderGameConditions, "renderGameConditions", DefaultRenderGameConditions);
            Scribe_Values.Look(ref renderWeather, "renderWeather", DefaultRenderWeather);
            Scribe_Values.Look(ref smoothRenderAreaSteps, "smoothRenderAreaSteps", DefaultSmoothRenderAreaSteps);
            Scribe_Values.Look(ref interval, "interval", DefaultInterval);
            Scribe_Values.Look(ref timeOfDay, "timeOfDay", DefaultTimeOfDay);
            Scribe_Values.Look(ref encoding, "encoding", DefaultEncoding);
            Scribe_Values.Look(ref pixelPerCell, "pixelPerCell", DefaultPixelPerCell);
            Scribe_Values.Look(ref outputImageFixedHeight, "outputImageFixedHeight", DefaultOutputImageFixedHeight);
            Scribe_Values.Look(ref exportPath, "exportPath", DesktopPath);
            Scribe_Values.Look(ref createSubdirs, "createSubdirs", DefaultCreateSubdirs);
            Scribe_Values.Look(ref fileNamePattern, "fileNamePattern", DefaultFileNamePattern);
        }

        private static string DesktopPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

    }

}
