using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ProgressRenderer
{
    public class GameComponentProgressManager : GameComponent // for game wide ProgressRenderer settings to be saved per game
    {
        public static bool defaultEnabled = true;
        public static bool enabled = defaultEnabled;


        // variables related to automatic quality adjustment

        public static JPGQualityAdjustmentSetting defaultJPGQualityAdjustment = JPGQualityAdjustmentSetting.Manual;
        public static int defaultRenderSize = 20;
        public static int defaultJPGQuality_WORLD = 93;
        public static int defaultpixelsPerCell_WORLD = 32;

        public static JPGQualityAdjustmentSetting qualityAdjustment = defaultJPGQualityAdjustment;
        public static int renderSize = defaultRenderSize;
        public static int JPGQuality_WORLD = defaultJPGQuality_WORLD;
        public static int pixelsPerCell_WORLD = defaultpixelsPerCell_WORLD;

        public static bool JPGQualityGoingUp = false;
        public static bool JPGQualitySteady = false;
        public static int JPGQualityBottomMargin = -1;
        public static int JPGQualityTopMargin = -1;
        public static int JPGQualityLastTarget = defaultRenderSize;

        public GameComponentProgressManager(Game game)
        {
        }
        
        override public void StartedNewGame()
        {
            enabled = defaultEnabled;
            qualityAdjustment = defaultJPGQualityAdjustment;
            renderSize = defaultRenderSize;
            JPGQuality_WORLD = defaultJPGQuality_WORLD;
            pixelsPerCell_WORLD = defaultpixelsPerCell_WORLD;
            JPGQualityGoingUp = false;
            JPGQualitySteady = false;
            JPGQualityBottomMargin = -1;
            JPGQualityTopMargin = -1;
            JPGQualityLastTarget = defaultRenderSize;
            PRModSettings.JPGQualityInitialize = false;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enabled, "enabled", defaultEnabled);
            Scribe_Values.Look(ref qualityAdjustment, "JPGQualityAdjustment", defaultJPGQualityAdjustment);
            Scribe_Values.Look(ref renderSize, "renderSize", defaultRenderSize);
            Scribe_Values.Look(ref JPGQuality_WORLD, "JPGQuality", defaultJPGQuality_WORLD);
            Scribe_Values.Look(ref pixelsPerCell_WORLD, "pixelsPerCell", defaultpixelsPerCell_WORLD);
            Scribe_Values.Look(ref JPGQualityGoingUp, "JPGQualityGoingUp", false);
            Scribe_Values.Look(ref JPGQualitySteady, "JPGQualitySteady", false);
            Scribe_Values.Look(ref JPGQualityBottomMargin, "JPGQualityBottomMargin", -1);
            Scribe_Values.Look(ref JPGQualityTopMargin, "JPGQualityTopMargin", -1);
            Scribe_Values.Look(ref JPGQualityLastTarget, "JPGQualityLastTarget", defaultRenderSize);
        }
    }
}
