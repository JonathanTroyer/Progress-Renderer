﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProgressRenderer
{

	public abstract class Designator_CornerMarker : Designator
	{

        private DesignateMode mode;

        public Designator_CornerMarker(DesignateMode mode)
		{
			this.mode = mode;
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			useMouseIcon = true;
		}

		protected override DesignationDef Designation
		{
			get
			{
				return DesignationDefOf.CornerMarker;
			}
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
			AcceptanceReport result;
			if (!c.InBounds(Map))
			{
				result = false;
			}
			else
			{
				if (mode == DesignateMode.Add)
				{
					if (Map.designationManager.DesignationAt(c, Designation) != null)
					{
						return false;
					}
				}
				else if (mode == DesignateMode.Remove)
				{
					if (Map.designationManager.DesignationAt(c, Designation) == null)
					{
						return false;
					}
				}
				result = true;
			}
			return result;
		}

		public override void DesignateSingleCell(IntVec3 c)
        {
			if (mode == DesignateMode.Add)
			{
                Map.designationManager.AddDesignation(new Designation(c, Designation));
			}
			else if (mode == DesignateMode.Remove)
			{
                Map.designationManager.DesignationAt(c, Designation).Delete();
            }
            // Give feedback for the new amount of markers on the map
            DesignateSingleCellFeedback();
		}

        private void DesignateSingleCellFeedback()
        {
            var cornerMarkers = Map.designationManager.AllDesignations.FindAll(des => des.def == DesignationDefOf.CornerMarker);
            // Message for the amount of markers on the map
            var markerCount = cornerMarkers.Count;
            string message = "LPR_MessageCornerMarkerAmount".Translate(markerCount) + " ";
            if (markerCount < 2)
            {
                message += "LPR_MessageCornerMarkerTooLess".Translate();
            }
            else if (markerCount > 2)
            {
                message += "LPR_MessageCornerMarkerTooMany".Translate();
            }
            else
            {
                message += "LPR_MessageCornerMarkerCorrect".Translate();
            }
            Messages.Message(message, MessageTypeDefOf.CautionInput, false);
            // Message for the created area (if enough markers)
            if (markerCount > 1)
            {
                var startX = Map.Size.x;
                var startZ = Map.Size.z;
                var endX = 0;
                var endZ = 0;
                foreach (var des in cornerMarkers)
                {
                    var cell = des.target.Cell;
                    if (cell.x < startX) { startX = cell.x; }
                    if (cell.z < startZ) { startZ = cell.z; }
                    if (cell.x > endX) { endX = cell.x; }
                    if (cell.z > endZ) { endZ = cell.z; }
                }
                endX += 1;
                endZ += 1;
                var distX = endX - startX;
                var distZ = endZ - startZ;
                var ratio = ((float)distX / distZ).ToString("0.###");
                string messageRect = "LPR_MessageCornerMarkersRect".Translate(distX, distZ);
                if (distX * 3 == distZ * 4)
                {
                    messageRect += " " + "LPR_MessageCornerMarkersRectRatioDefined".Translate(ratio, "4:3");
                }
                else if (distX * 2 == distZ * 3)
                {
                    messageRect += " " + "LPR_MessageCornerMarkersRectRatioDefined".Translate(ratio, "3:2");
                }
                else if (distX * 10 == distZ * 16)
                {
                    messageRect += " " + "LPR_MessageCornerMarkersRectRatioDefined".Translate(ratio, "16:10" );
                }
                else if (distX * 9 == distZ * 16)
                {
                    messageRect += " " + "LPR_MessageCornerMarkersRectRatioDefined".Translate(ratio, "16:9" );
                }
                else
                {
                    messageRect += " " + "LPR_MessageCornerMarkersRectRatio".Translate(ratio);
                }
                if (distZ <= 20)
                {
                    messageRect += " " + "LPR_MessageCornerMarkesRectHeightTooLow".Translate();
                }
                Messages.Message(messageRect, MessageTypeDefOf.CautionInput, false);
            }
        }

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}

		public override void RenderHighlight(List<IntVec3> dragCells)
		{
			DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
		}

	}

}
