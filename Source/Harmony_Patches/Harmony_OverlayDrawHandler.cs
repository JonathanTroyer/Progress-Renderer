﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(OverlayDrawHandler))]
    [HarmonyPatch("ShouldDrawPowerGrid", MethodType.Getter)]
    public class Harmony_OverlayDrawHandler_ShouldDrawPowerGrid
    {

        public static bool Prefix()
        {
            if (Find.CurrentMap.GetComponent<MapComponent_RenderManager>().Rendering)
            {
                return false;
            }
            return true;
        }

    }

}
