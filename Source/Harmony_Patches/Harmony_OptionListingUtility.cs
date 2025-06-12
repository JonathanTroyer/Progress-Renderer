using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    [HarmonyPatch(typeof(OptionListingUtility), "DrawOptionListing"), StaticConstructorOnStartup]
    class MenuOption
    {
        static void Prefix(List<ListableOption> optList)
        {
            if (optList.Count == 0)
            {
                return;
            }
            if (optList[0].GetType() != typeof(ListableOption_WebLink))
            {
                return;
            }

            optList.Add(new ListableOption_WebLink("Share your renders", delegate
            {
                Process.Start("https://rimworld.gallery/m/rwpr");
            }, IconRwp));
        }

        public static readonly Texture2D IconRwp = ContentFinder<Texture2D>.Get("Icon RWP");
        
    }
}