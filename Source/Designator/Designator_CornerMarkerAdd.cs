using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
	public class Designator_CornerMarkerAdd : Designator_CornerMarker
	{

        public Designator_CornerMarkerAdd() : base(DesignateMode.Add)
		{
			defaultLabel = "DesignatorCornerMarker".Translate();
			defaultDesc = "DesignatorCornerMarkerDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Designators/CornerMarkerOn");
			soundSucceeded = SoundDefOf.Designate_PlanAdd;
		}

	}

}
