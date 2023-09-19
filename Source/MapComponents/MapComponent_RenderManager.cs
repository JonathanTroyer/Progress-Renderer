using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HarmonyLib;
using ProgressRenderer.Source.Enum;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    public class MapComponent_RenderManager : MapComponent
    {
        public static int nextGlobalRenderManagerTickOffset = 0;
        public bool Rendering { get; private set; }

        private int tickOffset = -1;
        private int lastRenderedHour = -999;
        private int lastRenderedCounter = 0;
        private float rsOldStartX = -1f;
        private float rsOldStartZ = -1f;
        private float rsOldEndX = -1f;
        private float rsOldEndZ = -1f;
        private float rsTargetStartX = -1f;
        private float rsTargetStartZ = -1f;
        private float rsTargetEndX = -1f;
        private float rsTargetEndZ = -1f;
        private float rsCurrentPosition = 1f;

        private int imageWidth, imageHeight;
        private Texture2D imageTexture;

        private Task EncodingTask;

        private bool manuallyTriggered = false;
        private bool encoding = false;
        private bool ctrlEncodingPost = false;
        private SmallMessageBox messageBox;

        private struct VisibilitySettings
        {
            public bool showZones;
            public bool showRoofOverlay;
            public bool showFertilityOverlay;
            public bool showTerrainAffordanceOverlay;
            public bool showPollutionOverlay;
            public bool showTemperatureOverlay;
        }

        public MapComponent_RenderManager(Map map) : base(map)
        {
        }

        public override void FinalizeInit() // New map and after loading
        {
            if (tickOffset < 0)
            {
                tickOffset = nextGlobalRenderManagerTickOffset;
                nextGlobalRenderManagerTickOffset = (nextGlobalRenderManagerTickOffset + 5) % GenTicks.TickRareInterval;
            }
        }

        public override void MapComponentUpdate()
        {
            // Watch for finished encoding to clean up and free memory
            if (ctrlEncodingPost)
            {
                DoEncodingPost();
                ctrlEncodingPost = false;
            }
        }

        public override void MapComponentTick()
        {
            // TickRare
            if (Find.TickManager.TicksGame % 250 != tickOffset)
            {
                return;
            }

            // Check for rendering
            // Only render player home maps
            if (!map.IsPlayerHome)
            {
                return;
            }

            // Check for correct time to render
            var longLat = Find.WorldGrid.LongLatOf(map.Tile);
            var currHour = MoreGenDate.HoursPassedInteger(Find.TickManager.TicksAbs, longLat.x);
            if (currHour <= lastRenderedHour)
            {
                return;
            }

            if (currHour % PRModSettings.interval != PRModSettings.timeOfDay % PRModSettings.interval)
            {
                return;
            }

            // Update timing variables
            lastRenderedHour = currHour;
            lastRenderedCounter++;
            // Check if rendering is enabled
            if (!PRModSettings.enabled)
            {
                return;
            }

            // Show message window or print message
            ShowCurrentRenderMessage();
            // Start rendering
            Find.CameraDriver.StartCoroutine(DoRendering());
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastRenderedHour, "lastRenderedHour", -999);
            Scribe_Values.Look(ref lastRenderedCounter, "lastRenderedCounter", 0);
            Scribe_Values.Look(ref rsOldStartX, "rsOldStartX", -1f);
            Scribe_Values.Look(ref rsOldStartZ, "rsOldStartZ", -1f);
            Scribe_Values.Look(ref rsOldEndX, "rsOldEndX", -1f);
            Scribe_Values.Look(ref rsOldEndZ, "rsOldEndZ", -1f);
            Scribe_Values.Look(ref rsTargetStartX, "rsTargetStartX", -1f);
            Scribe_Values.Look(ref rsTargetStartZ, "rsTargetStartZ", -1f);
            Scribe_Values.Look(ref rsTargetEndX, "rsTargetEndX", -1f);
            Scribe_Values.Look(ref rsTargetEndZ, "rsTargetEndZ", -1f);
            Scribe_Values.Look(ref rsCurrentPosition, "rsCurrentPosition", 1f);
        }

        public static void TriggerCurrentMapManualRendering(bool forceRenderFullMap = false)
        {
            Find.CurrentMap.GetComponent<MapComponent_RenderManager>().DoManualRendering(forceRenderFullMap);
        }

        public void DoManualRendering(bool forceRenderFullMap = false)
        {
            ShowCurrentRenderMessage();
            manuallyTriggered = true;
            Find.CameraDriver.StartCoroutine(DoRendering(forceRenderFullMap));
        }

        private void ShowCurrentRenderMessage()
        {
            if (PRModSettings.renderFeedback == RenderFeedback.Window)
            {
                messageBox = new SmallMessageBox("LPR_Rendering".Translate());
                Find.WindowStack.Add(messageBox);
            }
            else if (PRModSettings.renderFeedback == RenderFeedback.Message)
            {
                Messages.Message("LPR_Rendering".Translate(), MessageTypeDefOf.CautionInput, false);
            }
        }

        private IEnumerator DoRendering(bool forceRenderFullMap = false)
        {
            yield return new WaitForFixedUpdate();
            if (Rendering)
            {
                Log.Error("Progress Renderer is still rendering an image while a new rendering was requested. This can lead to missing or wrong data. (This can also happen in rare situations when you trigger manual rendering the exact same time as an automatic rendering happens. If you did that, just check your export folder if both renderings were done corrently and ignore this error.)");
            }

            Rendering = true;

            // Temporary switch to this map for rendering
            var switchedMap = false;
            var rememberedMap = Find.CurrentMap;
            if (map != rememberedMap)
            {
                switchedMap = true;
                Current.Game.CurrentMap = map;
            }

            // Close world view if needed
            var rememberedWorldRendered = WorldRendererUtility.WorldRenderedNow;
            if (rememberedWorldRendered)
            {
                CameraJumper.TryHideWorld();
            }

            #region Hide overlays

            var settings = Find.PlaySettings;
            var oldVisibilities = new VisibilitySettings
            {
                showZones = settings.showZones,
                showRoofOverlay = settings.showRoofOverlay,
                showFertilityOverlay = settings.showFertilityOverlay,
                showTerrainAffordanceOverlay = settings.showTerrainAffordanceOverlay,
                showPollutionOverlay = settings.showPollutionOverlay,
                showTemperatureOverlay = settings.showTemperatureOverlay
            };

            if (!PRModSettings.renderZones)
                Find.PlaySettings.showZones = false;
            if (!PRModSettings.renderOverlays)
            {
                Find.PlaySettings.showRoofOverlay = false;
                Find.PlaySettings.showFertilityOverlay = false;
                Find.PlaySettings.showTerrainAffordanceOverlay = false;
                Find.PlaySettings.showPollutionOverlay = false;
                Find.PlaySettings.showTemperatureOverlay = false;
            }

            //TODO: Hide plans
            //TODO: Hide blueprints
            //TODO: Hide fog of war (stretch) 

            #endregion

            #region Calculate rendered area

            float startX = 0;
            float startZ = 0;
            float endX = map.Size.x;
            float endZ = map.Size.z;
            if (!forceRenderFullMap)
            {
                var cornerMarkers = map.designationManager.AllDesignations.FindAll(des => des.def == DesignationDefOf.CornerMarker);
                if (cornerMarkers.Count > 1)
                {
                    startX = endX;
                    startZ = endZ;
                    endX = 0;
                    endZ = 0;
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
                }
            }

            // Only use smoothing when rendering was not triggered manually
            if (!manuallyTriggered)
            {
                // Test if target render area changed to reset smoothing
                if (rsTargetStartX != startX || rsTargetStartZ != startZ || rsTargetEndX != endX || rsTargetEndZ != endZ)
                {
                    // Check if area was manually reset or uninitialized (-1) to not smooth
                    if (rsTargetStartX == -1f && rsTargetStartZ == -1f && rsTargetEndX == -1f && rsTargetEndZ == -1f)
                    {
                        rsCurrentPosition = 1f;
                    }
                    else
                    {
                        rsCurrentPosition = 1f / (PRModSettings.smoothRenderAreaSteps + 1);
                    }

                    rsOldStartX = rsTargetStartX;
                    rsOldStartZ = rsTargetStartZ;
                    rsOldEndX = rsTargetEndX;
                    rsOldEndZ = rsTargetEndZ;
                    rsTargetStartX = startX;
                    rsTargetStartZ = startZ;
                    rsTargetEndX = endX;
                    rsTargetEndZ = endZ;
                }

                // Apply smoothing to render area
                if (rsCurrentPosition < 1f)
                {
                    startX = rsOldStartX + (rsTargetStartX - rsOldStartX) * rsCurrentPosition;
                    startZ = rsOldStartZ + (rsTargetStartZ - rsOldStartZ) * rsCurrentPosition;
                    endX = rsOldEndX + (rsTargetEndX - rsOldEndX) * rsCurrentPosition;
                    endZ = rsOldEndZ + (rsTargetEndZ - rsOldEndZ) * rsCurrentPosition;
                    rsCurrentPosition += 1f / (PRModSettings.smoothRenderAreaSteps + 1);
                }
            }

            var distX = endX - startX;
            var distZ = endZ - startZ;

            #endregion

            #region Calculate texture size

            // Calculate basic values that are used for rendering
            int newImageWidth;
            int newImageHeight;
            if (PRModSettings.scaleOutputImage)
            {
                newImageWidth = (int) (PRModSettings.outputImageFixedHeight / distZ * distX);
                newImageHeight = PRModSettings.outputImageFixedHeight;
            }
            else
            {
                newImageWidth = (int) (distX * PRModSettings.pixelPerCell);
                newImageHeight = (int) (distZ * PRModSettings.pixelPerCell);
            }

            var mustUpdateTexture = false;
            if (newImageWidth != imageWidth || newImageHeight != imageHeight)
            {
                mustUpdateTexture = true;
                imageWidth = newImageWidth;
                imageHeight = newImageHeight;
            }

            #endregion

            #region Initialize camera and textures

            var cameraPosX = distX / 2;
            var cameraPosZ = distZ / 2;
            var orthographicSize = cameraPosZ;
            var cameraBasePos = new Vector3(cameraPosX, 15f + (orthographicSize - 11f) / 49f * 50f, cameraPosZ);

            // Initialize cameras and textures
            var renderTexture = RenderTexture.GetTemporary(imageWidth, imageHeight, 24);
            if (imageTexture == null || mustUpdateTexture)
            {
                imageTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
            }

            var camera = Find.Camera;
            var camDriver = camera.GetComponent<CameraDriver>();
            camDriver.enabled = false;

            // Store current camera data
            var rememberedRootPos = map.rememberedCameraPos.rootPos;
            var rememberedRootSize = map.rememberedCameraPos.rootSize;
            var rememberedFarClipPlane = camera.farClipPlane;

            // Overwrite current view rect in the camera driver
            var camViewRect = camDriver.CurrentViewRect;
            var camRectMinX = Math.Min((int) startX, camViewRect.minX);
            var camRectMinZ = Math.Min((int) startZ, camViewRect.minZ);
            var camRectMaxX = Math.Max((int) Math.Ceiling(endX), camViewRect.maxX);
            var camRectMaxZ = Math.Max((int) Math.Ceiling(endZ), camViewRect.maxZ);
            var camDriverTraverse = Traverse.Create(camDriver);
            camDriverTraverse.Field("lastViewRect").SetValue(CellRect.FromLimits(camRectMinX, camRectMinZ, camRectMaxX, camRectMaxZ));
            camDriverTraverse.Field("lastViewRectGetFrame").SetValue(Time.frameCount);

            #endregion

            yield return new WaitForEndOfFrame();

            // Set camera values needed for rendering
            camera.orthographicSize = orthographicSize;
            camera.farClipPlane = cameraBasePos.y + 6.5f;

            #region Render

            // Set render textures
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;

            // Render the image texture
            try
            {
                if (PRModSettings.renderWeather)
                {
                    map.weatherManager.DrawAllWeather();
                }
                camera.transform.position = new Vector3(startX + cameraBasePos.x, cameraBasePos.y, startZ + cameraBasePos.z);
                camera.Render();
                imageTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            #endregion

            #region Restore pre-render state

            // Restore camera and viewport
            RenderTexture.active = null;
            //tmpCam.targetTexture = null;
            camera.targetTexture = null;
            camera.farClipPlane = rememberedFarClipPlane;
            camDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);
            camDriver.enabled = true;

            RenderTexture.ReleaseTemporary(renderTexture);

            // Enable overlays
            Find.PlaySettings.showZones = oldVisibilities.showZones;
            Find.PlaySettings.showRoofOverlay = oldVisibilities.showRoofOverlay;
            Find.PlaySettings.showFertilityOverlay = oldVisibilities.showFertilityOverlay;
            Find.PlaySettings.showTerrainAffordanceOverlay = oldVisibilities.showTerrainAffordanceOverlay;
            Find.PlaySettings.showPollutionOverlay = oldVisibilities.showPollutionOverlay;
            Find.PlaySettings.showTemperatureOverlay = oldVisibilities.showTemperatureOverlay;

            // Switch back to world view if needed
            if (rememberedWorldRendered)
            {
                CameraJumper.TryShowWorld();
            }

            // Switch back to remembered map if needed
            if (switchedMap)
            {
                Current.Game.CurrentMap = rememberedMap;
            }

            #endregion

            // Signal finished rendering
            Rendering = false;
            // Hide message box
            if (messageBox != null)
            {
                messageBox.Close();
                messageBox = null;
            }

            yield return null;

            // Start encoding
            if (EncodingTask != null && !EncodingTask.IsCompleted)
                EncodingTask.Wait();
            EncodingTask = Task.Run(DoEncoding);
        }

        private void DoEncoding()
        {
            if (encoding)
            {
                Log.Error("Progress Renderer is still encoding an image while the encoder was called again. This can lead to missing or wrong data.");
            }

            switch (PRModSettings.encoding)
            {
                case EncodingType.UnityJPG:
                    EncodeUnityJpg();
                    break;
                case EncodingType.UnityPNG:
                    EncodeUnityPng();
                    break;
                default:
                    Log.Error("Progress Renderer encoding setting is wrong or missing. Using default for now. Go to the settings and set a new value.");
                    EncodeUnityJpg();
                    break;
            }
        }

        private void DoEncodingPost()
        {
            // Clean up unused objects
            //UnityEngine.Object.Destroy(imageTexture);

            // Signal finished encoding
            manuallyTriggered = false;
            encoding = false;
        }

        private void EncodeUnityPng()
        {
            var encodedImage = imageTexture.EncodeToPNG();
            SaveUnityEncoding(encodedImage);
        }

        private void EncodeUnityJpg()
        {
            var encodedImage = imageTexture.EncodeToJPG(PRModSettings.jpgQuality);
            SaveUnityEncoding(encodedImage);
        }

        private void SaveUnityEncoding(byte[] encodedImage)
        {
            // Create file and save encoded image
            var filePath = CreateCurrentFilePath();

            File.WriteAllBytes(filePath, encodedImage);
            // Create tmp copy to file if needed
            if (!manuallyTriggered && PRModSettings.fileNamePattern == FileNamePattern.BothTmpCopy)
            {
                File.Copy(filePath, CreateFilePath(FileNamePattern.Numbered, true));
            }

            DoEncodingPost();
        }

        private string CreateCurrentFilePath()
        {
            return CreateFilePath(manuallyTriggered ? FileNamePattern.DateTime : PRModSettings.fileNamePattern);
        }

        private string CreateFilePath(FileNamePattern fileNamePattern, bool addTmpSubdir = false)
        {
            // Build image name
            string imageName;
            if (fileNamePattern == FileNamePattern.Numbered)
            {
                imageName = CreateImageNameNumbered();
            }
            else
            {
                imageName = CreateImageNameDateTime();
            }

            // Create path and subdirectory
            var path = PRModSettings.exportPath;
            if (PRModSettings.createSubdirs)
            {
                path = Path.Combine(path, Find.World.info.seedString);
            }

            Directory.CreateDirectory(path);
            // Add subdir for manually triggered renderings
            if (manuallyTriggered)
            {
                path = Path.Combine(path, "manually");
                Directory.CreateDirectory(path);
            }

            // Create additional subdir for numbered symlinks
            if (addTmpSubdir)
            {
                path = Path.Combine(path, "tmp");
                Directory.CreateDirectory(path);
            }

            // Get correct file and location
            var fileExt = EnumUtils.GetFileExtension(PRModSettings.encoding);
            var filePath = Path.Combine(path, imageName + "." + fileExt);
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var i = 1;
            filePath = Path.Combine(path, imageName);
            string newPath;
            do
            {
                newPath = filePath + "-alt" + i + "." + fileExt;
                i++;
            } while (File.Exists(newPath));

            return newPath;
        }

        private string CreateImageNameDateTime()
        {
            var tick = Find.TickManager.TicksAbs;
            var longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
            var year = GenDate.Year(tick, longitude);
            var quadrum = MoreGenDate.QuadrumInteger(tick, longitude);
            var day = GenDate.DayOfQuadrum(tick, longitude) + 1;
            var hour = GenDate.HourInteger(tick, longitude);
            return "rimworld-" + Find.World.info.seedString + "-" + map.Tile + "-" + year + "-" + quadrum + "-" + ((day < 10) ? "0" : "") + day + "-" + ((hour < 10) ? "0" : "") + hour;
        }

        private string CreateImageNameNumbered()
        {
            return "rimworld-" + Find.World.info.seedString + "-" + map.Tile + "-" + lastRenderedCounter.ToString("000000");
        }
    }
}