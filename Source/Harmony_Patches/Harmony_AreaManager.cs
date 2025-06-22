using HarmonyLib;
using Verse;
using System.Reflection;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(AreaManager))]
    [HarmonyPatch("AreaManagerUpdate")]
    public class Harmony_AreaManager_AreaManagerUpdate
    {
        public static bool Prefix(AreaManager __instance)
        {
            try
            {
                Map map = __instance.map;
                if (map != null && map.GetComponent<MapComponent_RenderManager>()?.Rendering == true)
                    return false;
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[ProgressRenderer] AreaManagerUpdate patch error: {ex.Message}");
                return true;
            }
        }
    }
}