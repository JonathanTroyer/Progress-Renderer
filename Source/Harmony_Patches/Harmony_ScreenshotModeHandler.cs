using HarmonyLib;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(ScreenshotModeHandler))]
    [HarmonyPatch("ScreenshotModesOnGUI")]
    public class HarmonyScreenshotModeHandlerScreenshotModesOnGUI
    {
        public static void Postfix()
        {
            if (KeyBindingDefOf.LprManualRendering.KeyDownEvent)
            {
                MapComponentRenderManager.TriggerCurrentMapManualRendering();
                Event.current.Use();
            }
            else if (KeyBindingDefOf.LprManualRenderingForceFullMap.KeyDownEvent)
            {
                MapComponentRenderManager.TriggerCurrentMapManualRendering(true);
                Event.current.Use();
            }
        }
    }
}
