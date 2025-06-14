﻿using HarmonyLib;
using Verse;

namespace ProgressRenderer
{

    [HarmonyPatch(typeof(DesignationManager))]
    [HarmonyPatch("DrawDesignations")]
    public class Harmony_DesignationManager_DrawDesignations
    {

        public static bool Prefix(DesignationManager instance)
        {
            if (!PrModSettings.RenderDesignations && instance.map.GetComponent<MapComponent_RenderManager>().Rendering)
            {
                return false;
            }
            return true;
        }

    }
    
}
