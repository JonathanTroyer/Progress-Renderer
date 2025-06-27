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
        private static RenderFeedback _defaultRenderFeedback = RenderFeedback.Message;
        private static bool _defaultRenderNonPlayerHomes = false;
        private static bool _defaultRenderDesignations = false;
        private static bool _defaultRenderThingIcons = false;
        private static bool _defaultRenderGameConditions = true;
        private static bool _defaultRenderWeather = true;
        private static bool _defaultRenderZones = true;
        private static bool _defaultRenderOverlays = false;
        private static int _defaultSmoothRenderAreaSteps = 0;
        private static int _defaultInterval = 24;
        private static int _defaultTimeOfDay = 8;
        private static EncodingType _defaultEncoding = EncodingType.UnityJPG;
        
        private static int _defaultJPGQuality = 93;
        private static int _defaultpixelsPerCell = 32;
        private static bool _defaultScaleOutputImage = false;
        private static int _defaultOutputImageFixedHeight = 1024;
        private static bool _defaultCreateSubdirs = false;
        private static FileNamePattern _defaultFileNamePattern = FileNamePattern.DateTime;
        private static bool _defaultJPGQualityInitialize = false;
        public static RenderFeedback RenderFeedback = _defaultRenderFeedback;
        public static bool RenderDesignations = _defaultRenderDesignations;
        public static bool RenderThingIcons = _defaultRenderThingIcons;
        public static bool RenderGameConditions = _defaultRenderGameConditions;
        public static bool RenderWeather = _defaultRenderWeather;
        public static bool RenderZones = _defaultRenderZones;
        public static bool RenderOverlays = _defaultRenderOverlays;

        public static int SmoothRenderAreaSteps = _defaultSmoothRenderAreaSteps;
        private static int _whichInterval = RenderIntervalHelper.Intervals.IndexOf(_defaultInterval);
        public static int TimeOfDay = _defaultTimeOfDay;
        public static EncodingType Encoding = _defaultEncoding;
               
        public static int JPGQuality = _defaultJPGQuality;
        public static int PixelsPerCell = _defaultpixelsPerCell;
        public static bool JPGQualityInitialize = _defaultJPGQualityInitialize;
        public static bool ScaleOutputImage = _defaultScaleOutputImage;
        public static int OutputImageFixedHeight = _defaultOutputImageFixedHeight;
        public static string ExportPath;
        public static bool CreateSubdirs = _defaultCreateSubdirs;
        public static bool UseMapNameInstead; 
        public static FileNamePattern FileNamePattern = _defaultFileNamePattern;

        private static string _outputImageFixedHeightBuffer;

        public static bool DoMigrations { get; internal set; } = true;
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

        public void DoWindowContents(Rect settingsRect)
        {
            if (DoMigrations)
            {
                if (!MigratedOutputImageSettings)
                {
                    //Yes, I know for the people who used to use 1080 as scaling and have upgraded this will turn off scaling for them.
                    //Unfortunately I don't think there's a better way to handle this.
                    ScaleOutputImage = OutputImageFixedHeight > 0 && OutputImageFixedHeight != _defaultOutputImageFixedHeight;
                    if (!ScaleOutputImage) OutputImageFixedHeight = _defaultOutputImageFixedHeight;
                    MigratedOutputImageSettings = true;
                    Log.Warning("Migrated output image settings");
                }
                if (!MigratedInterval)
                {
                    _whichInterval = RenderIntervalHelper.Intervals.IndexOf(Interval);
                    if (_whichInterval < 0) _whichInterval = RenderIntervalHelper.Intervals.IndexOf(_defaultInterval);
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

            ls.Label((TaggedString)$"{"LPR_SettingsIntervalLabel".Translate()} {RenderIntervalHelper.GetLabel(Interval)}", -1, "LPR_SettingsIntervalDescription".Translate());
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
                    ls.Label("LPR_JPGQualityLabel".Translate() + JPGQuality.ToString(": ###0") + "%", -1, "LPR_JPGQualityDescription".Translate());
                    JPGQuality = (int)ls.Slider(JPGQuality, 1, 100);
                    ls.Label("LPR_SettingspixelsPerCellLabel".Translate() + PixelsPerCell.ToString(": ##0 ppc"), -1, "LPR_SettingspixelsPerCellDescription".Translate());
                    PixelsPerCell = (int)ls.Slider(PixelsPerCell, 1, 64);
                }
                else
                {
                    ls.Label("LPR_RenderSizeLabel".Translate() + GameComponentProgressManager.RenderSize.ToString(": ##0") + "MB (Current JPG quality" + GameComponentProgressManager.JPGQualityWorld.ToString(": ###0)"), -1, "LPR_RenderSizeDescription".Translate());
                    GameComponentProgressManager.RenderSize = (int)ls.Slider(GameComponentProgressManager.RenderSize, 5, 30);
                    ls.Label("LPR_SettingspixelsPerCell_WORLDLabel".Translate() + GameComponentProgressManager.PixelsPerCellWorld.ToString(": ##0 ppc"), -1, "LPR_SettingspixelsPerCell_WORLDDescription".Translate());
                    GameComponentProgressManager.PixelsPerCellWorld = (int)ls.Slider(GameComponentProgressManager.PixelsPerCellWorld, 1, 64);
                    ls.CheckboxLabeled("LPR_SettingsInitializeLabel".Translate(), ref JPGQualityInitialize, "LPR_SettingsInitializeDescription".Translate());
                    ls.Gap();
                }
            }
            else
            {
                ls.Label("LPR_SettingspixelsPerCellLabel".Translate() + PixelsPerCell.ToString(": ##0 ppc"), -1, "LPR_SettingspixelsPerCellDescription".Translate());
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
            if(ExportPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
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
            Scribe_Values.Look(ref RenderFeedback, "renderFeedback", _defaultRenderFeedback);
            Scribe_Values.Look(ref RenderDesignations, "renderDesignations", _defaultRenderDesignations);
            Scribe_Values.Look(ref RenderNonPlayerHomes, "renderNonPlayerHomes", _defaultRenderNonPlayerHomes);
            Scribe_Values.Look(ref RenderThingIcons, "renderThingIcons", _defaultRenderThingIcons);
            Scribe_Values.Look(ref RenderGameConditions, "renderGameConditions", _defaultRenderGameConditions);
            Scribe_Values.Look(ref RenderWeather, "renderWeather", _defaultRenderWeather);
            Scribe_Values.Look(ref RenderZones, "renderZones", _defaultRenderZones);
            Scribe_Values.Look(ref RenderOverlays, "renderOverlays", _defaultRenderOverlays);
            Scribe_Values.Look(ref SmoothRenderAreaSteps, "smoothRenderAreaSteps", _defaultSmoothRenderAreaSteps);
            Scribe_Values.Look(ref _whichInterval, "whichInterval", RenderIntervalHelper.Intervals.IndexOf(_defaultInterval));
            Scribe_Values.Look(ref TimeOfDay, "timeOfDay", _defaultTimeOfDay);
            Scribe_Values.Look(ref Encoding, "encodingFormat", _defaultEncoding);
            Scribe_Values.Look(ref JPGQuality, "JPGQuality", _defaultJPGQuality);
            Scribe_Values.Look(ref PixelsPerCell, "pixelsPerCell", _defaultpixelsPerCell);
            Scribe_Values.Look(ref ScaleOutputImage, "scaleOutputImage", _defaultScaleOutputImage);
            Scribe_Values.Look(ref OutputImageFixedHeight, "outputImageFixedHeight", _defaultOutputImageFixedHeight);
            Scribe_Values.Look(ref ExportPath, "exportPath", DesktopPath);
            Scribe_Values.Look(ref CreateSubdirs, "createSubdirs", _defaultCreateSubdirs);
            Scribe_Values.Look(ref UseMapNameInstead, "useMapNameInstead");
            Scribe_Values.Look(ref FileNamePattern, "fileNamePattern", _defaultFileNamePattern);
            Scribe_Values.Look(ref MigratedOutputImageSettings, "migratedOutputImageSettings", false, true);
            Scribe_Values.Look(ref MigratedInterval, "migratedInterval", false, true);
        }

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
                    labelIndex = Intervals.IndexOf(_defaultInterval);
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
