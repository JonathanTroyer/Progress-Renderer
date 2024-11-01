using System.Collections.Generic;
using System.Security.Policy;
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

            optList.Add(new ListableOption_WebLink("Share your renders", delegate () {
                System.Diagnostics.Process.Start("https://rimworld.gallery/m/rwpr");
            }, SculptureSmallAbstractC));
        }

        public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("Icon");
        public static readonly Texture2D IconMain = ContentFinder<Texture2D>.Get("Icon Main");
        public static readonly Texture2D PRInfo = ContentFinder<Texture2D>.Get("PRInfo");
        public static readonly Texture2D ImagesFolderInfo = ContentFinder<Texture2D>.Get("ImagesFolderInfo");
        public static readonly Texture2D SculptureSmallAbstractC = ContentFinder<Texture2D>.Get("Things/Building/Art/SculptureSmall/SculptureSmallAbstractC");
    }
}