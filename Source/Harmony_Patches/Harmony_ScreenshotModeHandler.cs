using HarmonyLib;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(ScreenshotModeHandler))]
    [HarmonyPatch("ScreenshotModesOnGUI")]
    public class Harmony_ScreenshotModeHandler_ScreenshotModesOnGUI
    {

        public static void Postfix()
        {
            if (KeyBindingDefOf.LprManualRendering.KeyDownEvent)
            {
                MapComponent_RenderManager.TriggerCurrentMapManualRendering();
                Event.current.Use();
            }
            else if (KeyBindingDefOf.LprManualRenderingForceFullMap.KeyDownEvent)
            {
                MapComponent_RenderManager.TriggerCurrentMapManualRendering(true);
                Event.current.Use();
            }
        }

    }
    
}
