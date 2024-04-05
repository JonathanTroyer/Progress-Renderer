using Verse;

namespace ProgressRenderer
{
    public class GameComponentProgressManager : GameComponent // for game wide ProgressRenderer settings to be saved per game
    {
        private const bool DefaultEnabled = true;


        // variables related to automatic quality adjustment

        private const JPGQualityAdjustmentSetting DefaultJPGQualityAdjustment = JPGQualityAdjustmentSetting.Manual;
        private const int DefaultRenderSize = 20;
        private const int DefaultJPGQualityWorld = 93;
        private const int DefaultPixelsPerCellWorld = 32;
        public static bool Enabled = DefaultEnabled;

        public static JPGQualityAdjustmentSetting QualityAdjustment = DefaultJPGQualityAdjustment;
        public static int RenderSize = DefaultRenderSize;
        public static int JPGQualityWorld = DefaultJPGQualityWorld;
        public static int PixelsPerCellWorld = DefaultPixelsPerCellWorld;

        public static bool JPGQualityGoingUp;
        public static bool JPGQualitySteady;
        public static int JPGQualityBottomMargin = -1;
        public static int JPGQualityTopMargin = -1;
        public static int JPGQualityLastTarget = DefaultRenderSize;

        public GameComponentProgressManager(Game game)
        {
        }

        public override void StartedNewGame()
        {
            Enabled = DefaultEnabled;
            QualityAdjustment = DefaultJPGQualityAdjustment;
            RenderSize = DefaultRenderSize;
            JPGQualityWorld = DefaultJPGQualityWorld;
            PixelsPerCellWorld = DefaultPixelsPerCellWorld;
            JPGQualityGoingUp = false;
            JPGQualitySteady = false;
            JPGQualityBottomMargin = -1;
            JPGQualityTopMargin = -1;
            JPGQualityLastTarget = DefaultRenderSize;
            PrModSettings.JPGQualityInitialize = false;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Enabled, "enabled", DefaultEnabled);
            Scribe_Values.Look(ref QualityAdjustment, "JPGQualityAdjustment");
            Scribe_Values.Look(ref RenderSize, "renderSize", DefaultRenderSize);
            Scribe_Values.Look(ref JPGQualityWorld, "JPGQuality", DefaultJPGQualityWorld);
            Scribe_Values.Look(ref PixelsPerCellWorld, "pixelsPerCell", DefaultPixelsPerCellWorld);
            Scribe_Values.Look(ref JPGQualityGoingUp, "JPGQualityGoingUp");
            Scribe_Values.Look(ref JPGQualitySteady, "JPGQualitySteady");
            Scribe_Values.Look(ref JPGQualityBottomMargin, "JPGQualityBottomMargin", -1);
            Scribe_Values.Look(ref JPGQualityTopMargin, "JPGQualityTopMargin", -1);
            Scribe_Values.Look(ref JPGQualityLastTarget, "JPGQualityLastTarget", DefaultRenderSize);
        }
    }
}
