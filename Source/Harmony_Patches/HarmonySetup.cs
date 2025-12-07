using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProgressRenderer
{
    [StaticConstructorOnStartup]
    static class HarmonySetup
    {
        static HarmonySetup()
        {
            var harmony = new Harmony("rimworld.neptimus7.progressrenderer");

            if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "scherub.planningextended"))
                harmony.PatchCategory("PlanningExtended");

            harmony.PatchAllUncategorized(Assembly.GetExecutingAssembly());
        }
    }
}
