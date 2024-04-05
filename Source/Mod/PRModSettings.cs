using System;
using System.Collections.Generic;
using System.IO;
using ProgressRenderer.Source.Enum;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    public class PrModSettings : ModSettings
    {
        private const RenderFeedback DefaultRenderFeedback = RenderFeedback.Window;
        private const bool DefaultRenderNonPlayerHomes = false;
        private const bool DefaultRenderDesignations = false;
        private const bool DefaultRenderThingIcons = false;
        private const bool DefaultRenderGameConditions = true;
        private const bool DefaultRenderWeather = true;
        private const bool DefaultRenderZones = true;
        private const bool DefaultRenderOverlays = false;
        private const int DefaultSmoothRenderAreaSteps = 0;
        private const int DefaultInterval = 24;
        private const int DefaultTimeOfDay = 8;
        private const EncodingType DefaultEncoding = EncodingType.UnityJPG;
        private const int DefaultJPGQuality = 93;
        private const int DefaultPixelsPerCell = 32;
        private const bool DefaultScaleOutputImage = false;
        private const int DefaultOutputImageFixedHeight = 1080;
        private const bool DefaultCreateSubdirs = false;
        private const FileNamePattern DefaultFileNamePattern = FileNamePattern.DateTime;
        private const bool DefaultJPGQualityInitialize = false;
        
        public static RenderFeedback RenderFeedback = DefaultRenderFeedback;
        public static bool RenderDesignations = DefaultRenderDesignations;
        public static bool RenderThingIcons = DefaultRenderThingIcons;
        public static bool RenderGameConditions = DefaultRenderGameConditions;
        public static bool RenderWeather = DefaultRenderWeather;
        public static bool RenderZones = DefaultRenderZones;
        public static bool RenderOverlays = DefaultRenderOverlays;

        public static int SmoothRenderAreaSteps = DefaultSmoothRenderAreaSteps;
        private static int _whichInterval = RenderIntervalHelper.Intervals.IndexOf(DefaultInterval);
        public static int TimeOfDay = DefaultTimeOfDay;
        public static EncodingType Encoding = DefaultEncoding;

        public static int JPGQuality = DefaultJPGQuality;
        public static int PixelsPerCell = DefaultPixelsPerCell;
        public static bool JPGQualityInitialize = DefaultJPGQualityInitialize;
        public static bool ScaleOutputImage = DefaultScaleOutputImage;
        public static int OutputImageFixedHeight = DefaultOutputImageFixedHeight;
        public static string ExportPath;
        public static bool CreateSubdirs = DefaultCreateSubdirs;
        public static bool UseMapNameInstead;
        public static FileNamePattern FileNamePattern = DefaultFileNamePattern;

        private static string _outputImageFixedHeightBuffer;
        public static bool MigratedOutputImageSettings;
        public static bool MigratedInterval;

        public static bool RenderNonPlayerHomes;

        public PrModSettings()
        {
            if (ExportPath.NullOrEmpty())
            {
                ExportPath = DesktopPath;
            }
        }

        public static bool DoMigrations { get; internal set; } = true;

        public static int Interval
        {
            get
            {
                return RenderIntervalHelper.Intervals[_whichInterval];
            }
        }

        private static string DesktopPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }

        public void DoWindowContents(Rect settingsRect)
        {
            if (DoMigrations)
            {
                if (!MigratedOutputImageSettings)
                {
                    //Yes, I know for the people who used to use 1080 as scaling and have upgraded this will turn off scaling for them.
                    //Unfortunately I don't think there's a better way to handle this.
                    ScaleOutputImage = OutputImageFixedHeight > 0 && OutputImageFixedHeight != DefaultOutputImageFixedHeight;
                    if (!ScaleOutputImage) OutputImageFixedHeight = DefaultOutputImageFixedHeight;
                    MigratedOutputImageSettings = true;
                    Log.Warning("Migrated output image settings");
                }
                if (!MigratedInterval)
                {
                    _whichInterval = RenderIntervalHelper.Intervals.IndexOf(Interval);
                    if (_whichInterval < 0) _whichInterval = RenderIntervalHelper.Intervals.IndexOf(DefaultInterval);
                    MigratedInterval = true;
                    Log.Warning("Migrated interval settings");
                }
            }

            var ls = new Listing_Standard();
            var leftHalf = new Rect(settingsRect.x, settingsRect.y, settingsRect.width / 2 - 12f, settingsRect.height);
            var rightHalf = new Rect(settingsRect.x + settingsRect.width / 2 + 12f, settingsRect.y, settingsRect.width / 2 - 12f, settingsRect.height);

            ls.Begin(leftHalf);

            // Left half (general settings)
            ls.CheckboxLabeled("LPR_SettingsEnabledLabel".Translate(), ref GameComponentProgressManager.Enabled, "LPR_SettingsEnabledDescription".Translate());
            var backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsRenderFeedbackLabel".Translate(), ("LPR_RenderFeedback_" + RenderFeedback).Translate()))
            {
                var menuEntries = new List<FloatMenuOption>();
                var feedbackTypes = (RenderFeedback[])Enum.GetValues(typeof(RenderFeedback));
                foreach (var type in feedbackTypes)
                {
                    menuEntries.Add(new FloatMenuOption(("LPR_RenderFeedback_" + EnumUtils.ToFriendlyString(type)).Translate(), delegate
                    {
                        RenderFeedback = type;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            Text.Anchor = backupAnchor;

            ls.Gap();
            ls.Label("LPR_SettingsRenderSettingsLabel".Translate(), -1, "LPR_SettingsRenderSettingsDescription".Translate());
            ls.GapLine();
            ls.CheckboxLabeled("LPR_SettingsRenderDesignationsLabel".Translate(), ref RenderDesignations, "LPR_SettingsRenderDesignationsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderThingIconsLabel".Translate(), ref RenderThingIcons, "LPR_SettingsRenderThingIconsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderGameConditionsLabel".Translate(), ref RenderGameConditions, "LPR_SettingsRenderGameConditionsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderWeatherLabel".Translate(), ref RenderWeather, "LPR_SettingsRenderWeatherDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderZonesLabel".Translate(), ref RenderZones, "LPR_SettingsRenderZonesDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderOverlaysLabel".Translate(), ref RenderOverlays, "LPR_SettingsRenderOverlaysDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsRenderNonPlayerHomes".Translate(), ref RenderNonPlayerHomes, "LPR_SettingsRenderNonPlayerHomesDescription".Translate());
            ls.GapLine();

            ls.Gap();
            ls.Label("LPR_SettingsSmoothRenderAreaStepsLabel".Translate() + SmoothRenderAreaSteps.ToString(": #0"), -1, "LPR_SettingsSmoothRenderAreaStepsDescription".Translate());
            SmoothRenderAreaSteps = (int)ls.Slider(SmoothRenderAreaSteps, 0, 30);

            ls.Label($"{"LPR_SettingsIntervalLabel".Translate()} {RenderIntervalHelper.GetLabel(Interval)}", -1, "LPR_SettingsIntervalDescription".Translate());
            _whichInterval = (int)ls.Slider(_whichInterval, 0, RenderIntervalHelper.Intervals.Count - 1);
            ls.Label("LPR_SettingsTimeOfDayLabel".Translate() + TimeOfDay.ToString(" 00H"), -1, "LPR_SettingsTimeOfDayDescription".Translate());
            TimeOfDay = (int)ls.Slider(TimeOfDay, 0, 23);

            ls.End();

            // Right half (file settings)
            ls.Begin(rightHalf);

            backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;


            if (ls.ButtonTextLabeled("LPR_SettingsEncodingLabel".Translate(), ("LPR_ImgEncoding_" + EnumUtils.ToFriendlyString(Encoding)).Translate()))
            {
                var menuEntries = new List<FloatMenuOption>();
                var encodingTypes = (EncodingType[])Enum.GetValues(typeof(EncodingType));
                foreach (var encodingType in encodingTypes)
                {
                    menuEntries.Add(new FloatMenuOption(("LPR_ImgEncoding_" + EnumUtils.ToFriendlyString(encodingType)).Translate(), delegate
                    {
                        Encoding = encodingType;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(menuEntries));
            }
            Text.Anchor = backupAnchor;

            if (Encoding == EncodingType.UnityJPG)
            {
                if (ls.ButtonTextLabeled("LPR_SettingsJPGQualityAdjustment".Translate(), ("LPR_JPGQualityAdjustment_" + EnumUtils.ToFriendlyString(GameComponentProgressManager.QualityAdjustment)).Translate()))
                {
                    var menuEntries = new List<FloatMenuOption>();
                    var jpgQualityAdjustmentSettings = (JPGQualityAdjustmentSetting[])Enum.GetValues(typeof(JPGQualityAdjustmentSetting));
                    foreach (var jpgQualityAdjustmentSetting in jpgQualityAdjustmentSettings)
                    {
                        menuEntries.Add(new FloatMenuOption(("LPR_JPGQualityAdjustment_" + EnumUtils.ToFriendlyString(jpgQualityAdjustmentSetting)).Translate(), delegate
                        {
                            GameComponentProgressManager.QualityAdjustment = jpgQualityAdjustmentSetting;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(menuEntries));
                }
                Text.Anchor = backupAnchor;

                if (GameComponentProgressManager.QualityAdjustment == JPGQualityAdjustmentSetting.Manual)
                {
                    ls.Label("LPR_JPGQualityLabel".Translate() + JPGQuality.ToString(": ##0") + "%", -1, "LPR_JPGQualityDescription".Translate());
                    JPGQuality = (int)ls.Slider(JPGQuality, 1, 100);
                    ls.Label("LPR_SettingsPixelsPerCellLabel".Translate() + PixelsPerCell.ToString(": ##0 ppc"), -1, "LPR_SettingsPixelsPerCellDescription".Translate());
                    PixelsPerCell = (int)ls.Slider(PixelsPerCell, 1, 64);
                }
                else
                {
                    ls.Label("LPR_RenderSizeLabel".Translate() + GameComponentProgressManager.RenderSize.ToString(": ##0") + "MB (Current JPG quality" + GameComponentProgressManager.JPGQualityWorld.ToString(": ##0)"), -1, "LPR_RenderSizeDescription".Translate());
                    GameComponentProgressManager.RenderSize = (int)ls.Slider(GameComponentProgressManager.RenderSize, 5, 30);
                    ls.Label("LPR_SettingsPixelsPerCell_WORLDLabel".Translate() + GameComponentProgressManager.PixelsPerCellWorld.ToString(": ##0 ppc"), -1, "LPR_SettingsPixelsPerCell_WORLDDescription".Translate());
                    GameComponentProgressManager.PixelsPerCellWorld = (int)ls.Slider(GameComponentProgressManager.PixelsPerCellWorld, 1, 64);
                    ls.CheckboxLabeled("LPR_SettingsInitializeLabel".Translate(), ref JPGQualityInitialize, "LPR_SettingsInitializeDescription".Translate());
                    ls.Gap();
                }
            }
            else
            {
                ls.Label("LPR_SettingsPixelsPerCellLabel".Translate() + PixelsPerCell.ToString(": ##0 ppc"), -1, "LPR_SettingspixelsPerCellDescription".Translate());
                PixelsPerCell = (int)ls.Slider(PixelsPerCell, 1, 64);
            }

            ls.Gap();
            ls.CheckboxLabeled("LPR_SettingsScaleOutputImageLabel".Translate(), ref ScaleOutputImage, "LPR_SettingsScaleOutputImageDescription".Translate());
            if (ScaleOutputImage)
            {
                ls.Label("LPR_SettingsOutputImageFixedHeightLabel".Translate());
                ls.TextFieldNumeric(ref OutputImageFixedHeight, ref _outputImageFixedHeightBuffer, 1);
                ls.Gap();
            }

            ls.GapLine();
            if (ScaleOutputImage)
            {
                ls.Gap(); // All about that visual balance
            }
            ls.Label("LPR_SettingsExportPathLabel".Translate(), -1, "LPR_SettingsExportPathDescription".Translate());
            ExportPath = ls.TextEntry($"{ExportPath}");
            if (ExportPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                ls.Label("LPR_SettingsExportPathInvalid".Translate());
            }

            ls.Gap();
            ls.CheckboxLabeled("LPR_SettingsCreateSubdirsLabel".Translate(), ref CreateSubdirs, "LPR_SettingsCreateSubdirsDescription".Translate());
            ls.CheckboxLabeled("LPR_SettingsUseMapNameInstead".Translate(), ref UseMapNameInstead, "LPR_SettingsUseMapNameInsteadDescription".Translate());
            backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (ls.ButtonTextLabeled("LPR_SettingsFileNamePatternLabel".Translate(), ("LPR_FileNamePattern_" + FileNamePattern).Translate()))
            {
                var menuEntries = new List<FloatMenuOption>();
                var patterns = (FileNamePattern[])Enum.GetValues(typeof(FileNamePattern));
                foreach (var pattern in patterns)
                {
                    menuEntries.Add(new FloatMenuOption(("LPR_FileNamePattern_" + EnumUtils.ToFriendlyString(pattern)).Translate(), delegate
                    {
                        FileNamePattern = pattern;
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
            Scribe_Values.Look(ref RenderFeedback, "renderFeedback", DefaultRenderFeedback);
            Scribe_Values.Look(ref RenderDesignations, "renderDesignations", DefaultRenderDesignations);
            Scribe_Values.Look(ref RenderNonPlayerHomes, "renderNonPlayerHomes", DefaultRenderNonPlayerHomes);
            Scribe_Values.Look(ref RenderThingIcons, "renderThingIcons", DefaultRenderThingIcons);
            Scribe_Values.Look(ref RenderGameConditions, "renderGameConditions", DefaultRenderGameConditions);
            Scribe_Values.Look(ref RenderWeather, "renderWeather", DefaultRenderWeather);
            Scribe_Values.Look(ref RenderZones, "renderZones", DefaultRenderZones);
            Scribe_Values.Look(ref RenderOverlays, "renderOverlays", DefaultRenderOverlays);
            Scribe_Values.Look(ref SmoothRenderAreaSteps, "smoothRenderAreaSteps", DefaultSmoothRenderAreaSteps);
            Scribe_Values.Look(ref _whichInterval, "whichInterval", RenderIntervalHelper.Intervals.IndexOf(DefaultInterval));
            Scribe_Values.Look(ref TimeOfDay, "timeOfDay", DefaultTimeOfDay);
            Scribe_Values.Look(ref Encoding, "encodingFormat", DefaultEncoding);
            Scribe_Values.Look(ref JPGQuality, "JPGQuality", DefaultJPGQuality);
            Scribe_Values.Look(ref PixelsPerCell, "pixelsPerCell", DefaultPixelsPerCell);
            Scribe_Values.Look(ref ScaleOutputImage, "scaleOutputImage", DefaultScaleOutputImage);
            Scribe_Values.Look(ref OutputImageFixedHeight, "outputImageFixedHeight", DefaultOutputImageFixedHeight);
            Scribe_Values.Look(ref ExportPath, "exportPath", DesktopPath);
            Scribe_Values.Look(ref CreateSubdirs, "createSubdirs", DefaultCreateSubdirs);
            Scribe_Values.Look(ref UseMapNameInstead, "useMapNameInstead");
            Scribe_Values.Look(ref FileNamePattern, "fileNamePattern", DefaultFileNamePattern);
            Scribe_Values.Look(ref MigratedOutputImageSettings, "migratedOutputImageSettings", false, true);
            Scribe_Values.Look(ref MigratedInterval, "migratedInterval", false, true);
        }

        private static class RenderIntervalHelper
        {
            public static readonly List<int> Intervals = new List<int> { 15 * 24, 10 * 24, 6 * 24, 5 * 24, 4 * 24, 3 * 24, 2 * 24, 24, 12, 8, 6, 4, 3, 2, 1 };
            public static readonly List<int> WhichLabelsForInterval = new List<int> { 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 2, 2, 2, 2, 3 };
            public static readonly List<string> Labels = new List<string> { "LPR_RenderEveryDays", "LPR_RenderEveryDay", "LPR_RenderEveryHours", "LPR_RenderEveryHour" };

            public static string GetLabel(int interval)
            {
                var labelIndex = Intervals.IndexOf(interval);
                if (labelIndex < 0)
                {
                    Log.Error("Wrong configuration found for ProgressRenderer.PRModSettings.interval. Using default value.");
                    labelIndex = Intervals.IndexOf(DefaultInterval);
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
    }
}
