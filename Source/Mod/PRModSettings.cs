using ProgressRenderer.Source.Enum;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{

       
    public class PRModSettings : ModSettings
    {
        private static RenderFeedback defaultRenderFeedback = RenderFeedback.Window;
        private static bool defaultCreateSubdirs = false;
        private static FileNamePattern defaultFileNamePattern = FileNamePattern.DateTime;

        public static int defaultInterval = 24; // also exists in MapComponent_RenderManager

        public static RenderFeedback renderFeedback = defaultRenderFeedback;
        public static string exportPath;
        public static bool createSubdirs = defaultCreateSubdirs;
        public static FileNamePattern fileNamePattern = defaultFileNamePattern;

        private static string outputImageFixedHeightBuffer;

        public static bool DoMigrations { get; internal set; } = true;
        public static bool migratedOutputImageSettings = false;
        public static bool migratedInterval = false;

        public PRModSettings() : base()
        {
            if (exportPath.NullOrEmpty())
            {
                exportPath = DesktopPath;
            }
        }
        
        private static class RenderIntervalHelper
        {
            public static readonly List<int> Intervals = new List<int>() { 15 * 24, 10 * 24, 6 * 24, 5 * 24, 4 * 24, 3 * 24, 2 * 24, 24, 12, 8, 6, 4, 3, 2, 1 };
            public static readonly List<int> WhichLabelsForInterval = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 2, 2, 2, 2, 3 };
            public static readonly List<string> Labels = new List<string>() { "LPR_RenderEveryDays", "LPR_RenderEveryDay", "LPR_RenderEveryHours", "LPR_RenderEveryHour" };

            public static string GetLabel(int interval)
            {
                var labelIndex = Intervals.IndexOf(interval);
                if (labelIndex < 0)
                {
                    Log.Error("Wrong configuration found for ProgressRenderer.PRModSettings.interval. Using default value.");
                    labelIndex = Intervals.IndexOf(defaultInterval);
                }

                var whichLabel = WhichLabelsForInterval[labelIndex];
                float labelVal = interval;
                if (whichLabel == 0)
                {
                    labelVal /= 24f;
                }

                return Labels[whichLabel].Translate(labelVal.ToString("#0"));
            }
        }

        public void DoWindowContents(Rect settingsRect)
        {
            if (DoMigrations)
            {
                if (!migratedOutputImageSettings)
                {
                    //Yes, I know for the people who used to use 1080 as scaling and have upgraded this will turn off scaling for them.
                    //Unfortunately I don't think there's a better way to handle this.
                    MapComponent_RenderManager.scaleOutputImage = MapComponent_RenderManager.outputImageFixedHeight > 0 && MapComponent_RenderManager.outputImageFixedHeight != MapComponent_RenderManager.defaultOutputImageFixedHeight;
                    if (!MapComponent_RenderManager.scaleOutputImage) MapComponent_RenderManager.outputImageFixedHeight = MapComponent_RenderManager.defaultOutputImageFixedHeight;
                    migratedOutputImageSettings = true;
                    Log.Warning("Migrated output image settings");
                }
                if (!migratedInterval)
                {
                    MapComponent_RenderManager.whichInterval = RenderIntervalHelper.Intervals.IndexOf(interval);
                    if (MapComponent_RenderManager.whichInterval < 0) MapComponent_RenderManager.whichInterval = RenderIntervalHelper.Intervals.IndexOf(defaultInterval);
                    migratedInterval = true;
                    Log.Warning("Migrated interval settings");
                }
            }

            var ls = new Listing_Standard();
            var leftHalf = new Rect(settingsRect.x, settingsRect.y, settingsRect.width / 2 - 12f, settingsRect.height);
            var rightHalf = new Rect(settingsRect.x + settingsRect.width / 2 + 12f, settingsRect.y, settingsRect.width / 2 - 12f, settingsRect.height);

            ls.Begin(leftHalf);

            // Left half (general settings)
            ls.CheckboxLabeled("LPR_SettingsEnabledLabel".Translate(), ref MapComponent_RenderManager.renderingEnabled, "LPR_SettingsEnabledDescription".Translate());
            var backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsRenderFeedbackLabel".Translate(), ("LPR_RenderFeedback_" + renderFeedback).Translate()))
            {
                var menuEntries = new List<FloatMenuOption>();
                var feedbackTypes = (RenderFeedback[])Enum.GetValues(typeof(RenderFeedback));
                foreach (var type in feedbackTypes)
                {
                    menuEntries.Add(new FloatMenuOption(("LPR_RenderFeedback_" + EnumUtils.ToFriendlyString(type)).Translate(), delegate
                    {
                        renderFeedback = type;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            Text.Anchor = backupAnchor;

            ls.Gap();
            ls.Label("LPR_SettingsRenderSettingsLabel".Translate(), -1, "LPR_SettingsRenderSettingsDescription".Translate());
            ls.GapLine();
            ls.CheckboxLabeled("LPR_SettingsRenderDesignationsLabel".Translate(), ref MapComponent_RenderManager.renderDesignations, "LPR_SettingsRenderDesignationsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderThingIconsLabel".Translate(), ref MapComponent_RenderManager.renderThingIcons, "LPR_SettingsRenderThingIconsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderGameConditionsLabel".Translate(), ref MapComponent_RenderManager.renderGameConditions, "LPR_SettingsRenderGameConditionsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderWeatherLabel".Translate(), ref MapComponent_RenderManager.renderWeather, "LPR_SettingsRenderWeatherDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderZonesLabel".Translate(), ref MapComponent_RenderManager.renderZones, "LPR_SettingsRenderZonesDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderOverlaysLabel".Translate(), ref MapComponent_RenderManager.renderOverlays, "LPR_SettingsRenderOverlaysDescription".Translate());
            ls.GapLine();

            ls.Gap();
            ls.Label("LPR_SettingsSmoothRenderAreaStepsLabel".Translate() + MapComponent_RenderManager.smoothRenderAreaSteps.ToString(": #0"), -1, "LPR_SettingsSmoothRenderAreaStepsDescription".Translate());
            MapComponent_RenderManager.smoothRenderAreaSteps = (int)ls.Slider(MapComponent_RenderManager.smoothRenderAreaSteps, 0, 30);

            ls.Label($"{"LPR_SettingsIntervalLabel".Translate()} {RenderIntervalHelper.GetLabel(interval)}", -1, "LPR_SettingsIntervalDescription".Translate());
            MapComponent_RenderManager.whichInterval = (int)ls.Slider(MapComponent_RenderManager.whichInterval, 0, RenderIntervalHelper.Intervals.Count - 1);
            ls.Label("LPR_SettingsTimeOfDayLabel".Translate() + MapComponent_RenderManager.timeOfDay.ToString(" 00H"), -1, "LPR_SettingsTimeOfDayDescription".Translate());
            MapComponent_RenderManager.timeOfDay = (int)ls.Slider(MapComponent_RenderManager.timeOfDay, 0, 23);

            ls.End();

            // Right half (file settings)
            ls.Begin(rightHalf);

            backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;


            if (ls.ButtonTextLabeled("LPR_SettingsEncodingLabel".Translate(), ("LPR_ImgEncoding_" + EnumUtils.ToFriendlyString(MapComponent_RenderManager.encoding)).Translate()))
            {
                var menuEntries = new List<FloatMenuOption>();
                var encodingTypes = (EncodingType[])Enum.GetValues(typeof(EncodingType));
                foreach (var encodingType in encodingTypes)
                {
                    menuEntries.Add(new FloatMenuOption(("LPR_ImgEncoding_" + EnumUtils.ToFriendlyString(encodingType)).Translate(), delegate
                    {
                        MapComponent_RenderManager.encoding = encodingType;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            Text.Anchor = backupAnchor;

            if (MapComponent_RenderManager.encoding == EncodingType.UnityJPG)
            {
                if (ls.ButtonTextLabeled("LPR_SettingsJPGQualityAdjustment".Translate(), ("LPR_JPGQualityAdjustment_" + EnumUtils.ToFriendlyString(MapComponent_RenderManager.qualityAdjustment)).Translate()))
                {
                    var menuEntries = new List<FloatMenuOption>();
                    var JPGQualityAdjustmentSettings = (JPGQualityAdjustmentSetting[])Enum.GetValues(typeof(JPGQualityAdjustmentSetting));
                    foreach (var JPGQualityAdjustmentSetting in JPGQualityAdjustmentSettings)
                    {
                        menuEntries.Add(new FloatMenuOption(("LPR_JPGQualityAdjustment_" + EnumUtils.ToFriendlyString(JPGQualityAdjustmentSetting)).Translate(), delegate
                        {
                            MapComponent_RenderManager.qualityAdjustment = JPGQualityAdjustmentSetting;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(menuEntries));
                }
                Text.Anchor = backupAnchor;

                if (MapComponent_RenderManager.qualityAdjustment == JPGQualityAdjustmentSetting.Manual)
                {
                    ls.Label("LPR_JPGQualityLabel".Translate() + MapComponent_RenderManager.JPGQuality.ToString(": ##0") + "%", -1, "LPR_JPGQualityDescription".Translate());
                    MapComponent_RenderManager.JPGQuality = (int)ls.Slider(MapComponent_RenderManager.JPGQuality, 1, 100);
                }
                else
                {
                    ls.Label("LPR_RenderSizeLabel".Translate() + MapComponent_RenderManager.renderSize.ToString(": ##0")+"MB (Current JPG quality"+ MapComponent_RenderManager.JPGQuality.ToString(": ##0)"), -1, "LPR_RenderSizeDescription".Translate());
                    MapComponent_RenderManager.renderSize = (int)ls.Slider(MapComponent_RenderManager.renderSize, 5, 30);
                }
            }

            ls.Label("LPR_SettingspixelsPerCellLabel".Translate() + MapComponent_RenderManager.pixelsPerCell.ToString(": ##0 ppc"), -1, "LPR_SettingspixelsPerCellDescription".Translate());
            MapComponent_RenderManager.pixelsPerCell = (int)ls.Slider(MapComponent_RenderManager.pixelsPerCell, 1, 64);

            ls.Gap();
            ls.CheckboxLabeled("LPR_SettingsScaleOutputImageLabel".Translate(), ref MapComponent_RenderManager.scaleOutputImage, "LPR_SettingsScaleOutputImageDescription".Translate());
            if (MapComponent_RenderManager.scaleOutputImage)
            {
                ls.Label("LPR_SettingsOutputImageFixedHeightLabel".Translate());
                ls.TextFieldNumeric(ref MapComponent_RenderManager.outputImageFixedHeight, ref outputImageFixedHeightBuffer, 1);
                ls.Gap();
            }

            ls.GapLine();
            if (MapComponent_RenderManager.scaleOutputImage)
            {
                ls.Gap(); // All about that visual balance
            }
            ls.Label("LPR_SettingsExportPathLabel".Translate(), -1, "LPR_SettingsExportPathDescription".Translate());
            exportPath = ls.TextEntry(exportPath);

            ls.Gap();
            ls.CheckboxLabeled("LPR_SettingsCreateSubdirsLabel".Translate(), ref createSubdirs, "LPR_SettingsCreateSubdirsDescription".Translate());
            backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsFileNamePatternLabel".Translate(), ("LPR_FileNamePattern_" + fileNamePattern).Translate()))
            {
                var menuEntries = new List<FloatMenuOption>();
                var patterns = (FileNamePattern[])Enum.GetValues(typeof(FileNamePattern));
                foreach (var pattern in patterns)
                {
                    menuEntries.Add(new FloatMenuOption(("LPR_FileNamePattern_" + EnumUtils.ToFriendlyString(pattern)).Translate(), delegate
                    {
                        fileNamePattern = pattern;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            Text.Anchor = backupAnchor;

            ls.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Scribe_Values.Look(ref enabled, "enabled", defaultEnabled);
            Scribe_Values.Look(ref renderFeedback, "renderFeedback", defaultRenderFeedback);
            // Scribe_Values.Look(ref renderDesignations, "renderDesignations", defaultRenderDesignations);
            // Scribe_Values.Look(ref renderThingIcons, "renderThingIcons", defaultRenderThingIcons);
            // Scribe_Values.Look(ref renderGameConditions, "renderGameConditions", defaultRenderGameConditions);
            // Scribe_Values.Look(ref renderWeather, "renderWeather", defaultRenderWeather);
            // Scribe_Values.Look(ref renderZones, "renderZones", defaultRenderZones);
            // Scribe_Values.Look(ref renderOverlays, "renderOverlays", defaultRenderOverlays);
            // Scribe_Values.Look(ref smoothRenderAreaSteps, "smoothRenderAreaSteps", defaultSmoothRenderAreaSteps);
            // Scribe_Values.Look(ref whichInterval, "whichInterval", RenderIntervalHelper.Intervals.IndexOf(defaultInterval));
            // Scribe_Values.Look(ref timeOfDay, "timeOfDay", defaultTimeOfDay);
            // Scribe_Values.Look(ref encoding, "encoding", defaultEncoding);
            // Scribe_Values.Look(ref qualityAdjustment, "JPGQualityAdjustment", defaultJPGQualityAdjustment);
            // Scribe_Values.Look(ref renderSize, "renderSize", defaultRenderSize);
            // Scribe_Values.Look(ref JPGQuality, "JPGQuality", defaultJPGQuality);
            // Scribe_Values.Look(ref pixelsPerCell, "pixelsPerCell", defaultpixelsPerCell);
            // Scribe_Values.Look(ref scaleOutputImage, "scaleOutputImage", defaultScaleOutputImage);
            // Scribe_Values.Look(ref outputImageFixedHeight, "outputImageFixedHeight", defaultOutputImageFixedHeight);
            Scribe_Values.Look(ref exportPath, "exportPath", DesktopPath);
            Scribe_Values.Look(ref createSubdirs, "createSubdirs", defaultCreateSubdirs);
            Scribe_Values.Look(ref fileNamePattern, "fileNamePattern", defaultFileNamePattern);
            Scribe_Values.Look(ref migratedOutputImageSettings, "migratedOutputImageSettings", false, true);
            Scribe_Values.Look(ref migratedInterval, "migratedInterval", false, true);
        }

        public static int interval
        {
            get
            {
                return RenderIntervalHelper.Intervals[MapComponent_RenderManager.whichInterval];
            }
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
