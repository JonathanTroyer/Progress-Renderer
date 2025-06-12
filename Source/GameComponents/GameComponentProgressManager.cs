using Verse;

namespace ProgressRenderer
{
    public class GameComponentProgressManager : GameComponent // for game wide ProgressRenderer settings to be saved per game
    {
        
        public static bool Enabled = true;
        public static bool TileFoldersEnabled;

        // variables related to automatic quality adjustment

        private const JPGQualityAdjustmentSetting DefaultJPGQualityAdjustment = JPGQualityAdjustmentSetting.Manual;
        private const int DefaultRenderSize = 20;
        private const int DefaultJPGQualityWorld = 93;
        private const int DefaultPixelsPerCellWorld = 32;

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
            Enabled = true; //When a new game is created or Progress Renderer is added as a mod mid game, rendering is automatically enabled
            TileFoldersEnabled = true; //Only when a new game is created, automatic tile folder creation is automatically enabled. If 1.6 is ever released, remove all this logic as a new game will have to be started anyway, likely.
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
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref TileFoldersEnabled, "tileFoldersEnabled");
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
