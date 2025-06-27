using System;
using System.Collections;
using System.IO;
using System.Threading;
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
        public static int NextGlobalRenderManagerTickOffset;
        public bool Rendering { get; private set; }

        private int tickOffset = -1;
        private int lastRenderedHour = -999;
        private int lastRenderedCounter;
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

        private Task encodingTask;

        private bool manuallyTriggered;
        private bool encoding;
        private bool ctrlEncodingPost;
        private SmallMessageBox messageBox;


        private struct VisibilitySettings
        {
            public bool ShowZones;
            public bool ShowRoofOverlay;
            public bool ShowFertilityOverlay;
            public bool ShowTerrainAffordanceOverlay;
            public bool ShowPollutionOverlay;
            public bool ShowTemperatureOverlay;
        }

        public MapComponent_RenderManager(Map map) : base(map)
        {
        }

        public override void FinalizeInit() // New map and after loading
        {
            if (tickOffset < 0)
            {
                tickOffset = NextGlobalRenderManagerTickOffset;
                NextGlobalRenderManagerTickOffset = (NextGlobalRenderManagerTickOffset + 5) % GenTicks.TickRareInterval;
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

        // TODO: 1.6, use VTR
        public override void MapComponentTick()
        {
            // TickRare
            if (Find.TickManager.TicksGame % 250 != tickOffset)
            {
                return;
            }

            // Check for rendering
            // Only render player home maps
            // TODO: IsPlayerHome returns true for landed tiles
            if (!map.IsPlayerHome && !PrModSettings.RenderNonPlayerHomes)
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

            if (currHour % PrModSettings.Interval != PrModSettings.TimeOfDay % PrModSettings.Interval)
            {
                return;
            }

            // Update timing variables
            lastRenderedHour = currHour;
            lastRenderedCounter++;
            // Check if rendering is enabled
            if (!GameComponentProgressManager.Enabled)
            {
                return;
            }

            // Start rendering
            RequestDoRendering();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastRenderedHour, "lastRenderedHour", -999);
            Scribe_Values.Look(ref lastRenderedCounter, "lastRenderedCounter");
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
            if (PrModSettings.RenderFeedback == RenderFeedback.Window)
            {
                messageBox = new SmallMessageBox("LPR_Rendering".Translate());
                Find.WindowStack.Add(messageBox);
            }
            else if (PrModSettings.RenderFeedback == RenderFeedback.Message)
            {
                Messages.Message("LPR_Rendering".Translate(), MessageTypeDefOf.CautionInput, false);
            }
        }

        private void ShowRenderFailureMessage()
        {
            if (PrModSettings.RenderFeedback == RenderFeedback.Window)
            {
                messageBox = new SmallMessageBox("LPR_Rendering_Failure".Translate());
                Find.WindowStack.Add(messageBox);
            }
            else if (PrModSettings.RenderFeedback == RenderFeedback.Message)
            {
                Messages.Message("LPR_Rendering_Failure".Translate(), MessageTypeDefOf.CautionInput, false);
            }
        }

        private void RequestDoRendering(bool forceRenderFullMap = false)
        {
            lock (Utils.Lock)
            {
                Utils.Renderings.Enqueue(DoRendering(forceRenderFullMap));
            }

            Find.CameraDriver.StartCoroutine(Utils.ProcessRenderings());
        }

        private IEnumerator DoRendering(bool forceRenderFullMap)
        {
#if DEBUG
            Log.Message($"Map {map}: " + $"Local rendering lock is {Rendering}");
#endif
            yield return new WaitUntil(() => !Rendering);
            Rendering = true;
#if DEBUG
            Log.Message($"Map {map}: " + "Acquired local lock");
#endif

            ShowCurrentRenderMessage();
#if DEBUG
            Log.Message($"Map {map}: " + "Showed message box");
#endif

            yield return new WaitForFixedUpdate();
#if DEBUG
            Log.Message($"Map {map}: " + "WaitForFixedUpdate");
#endif

            #region Switch map

            // Temporary switch to this map for rendering
            var switchedMap = false;
            var rememberedMap = Find.CurrentMap;
            if (map != rememberedMap)
            {
                switchedMap = true;
                Current.Game.CurrentMap = map;
            }

            // Close world view if needed
            // TODO: Odyseey, don't jump to planet colony if in ship? How do we render ship vs colony?
            var rememberedWorldRendered = WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.Planet;
            if (rememberedWorldRendered)
            {
                CameraJumper.TryHideWorld();
            }

            #endregion

#if DEBUG
            Log.Message($"Map {map}: " + $"Switched map? {switchedMap}");
#endif

            #region Hide overlays

            var settings = Find.PlaySettings;
            var oldVisibilities = new VisibilitySettings
            {
                ShowZones = settings.showZones,
                ShowRoofOverlay = settings.showRoofOverlay,
                ShowFertilityOverlay = settings.showFertilityOverlay,
                ShowTerrainAffordanceOverlay = settings.showTerrainAffordanceOverlay,
                ShowPollutionOverlay = settings.showPollutionOverlay,
                ShowTemperatureOverlay = settings.showTemperatureOverlay
            };
            var oldHighlight = Prefs.DotHighlightDisplayMode;
            
            if (!PrModSettings.RenderZones)
                Find.PlaySettings.showZones = false;
            if (!PrModSettings.RenderOverlays)
            {
                Find.PlaySettings.showRoofOverlay = false;
                Find.PlaySettings.showFertilityOverlay = false;
                Find.PlaySettings.showTerrainAffordanceOverlay = false;
                Find.PlaySettings.showPollutionOverlay = false;
                Find.PlaySettings.showTemperatureOverlay = false;
            }


            Prefs.DotHighlightDisplayMode = DotHighlightDisplayMode.None;

            //Turn off Camera+ stuff
            if (PrMod.SkipCustomRenderingRef != null)
                PrMod.SkipCustomRenderingRef() = true;

            //TODO: Hide fog of war (stretch) 

            #endregion

#if DEBUG
            Log.Message($"Map {map}: " + "Hid things");
#endif

            #region Calculate rendered area

            float startX = 0;
            float startZ = 0;
            float endX = map.Size.x;
            float endZ = map.Size.z;
            if (!forceRenderFullMap)
            {
                var cornerMarkers =
                    map.designationManager.AllDesignations.FindAll(des => des.def == DesignationDefOf.CornerMarker);
                if (cornerMarkers.Count > 1)
                {
                    startX = endX;
                    startZ = endZ;
                    endX = 0;
                    endZ = 0;
                    foreach (var des in cornerMarkers)
                    {
                        var cell = des.target.Cell;
                        if (cell.x < startX)
                        {
                            startX = cell.x;
                        }

                        if (cell.z < startZ)
                        {
                            startZ = cell.z;
                        }

                        if (cell.x > endX)
                        {
                            endX = cell.x;
                        }

                        if (cell.z > endZ)
                        {
                            endZ = cell.z;
                        }
                    }

                    endX += 1;
                    endZ += 1;
                }
            }

            // Only use smoothing when rendering was not triggered manually
            if (!manuallyTriggered)
            {
                // Test if target render area changed to reset smoothing
                if (!(
                        rsTargetStartX.CloseEquals(startX) &&
                        rsTargetStartZ.CloseEquals(startZ) &&
                        rsTargetEndX.CloseEquals(endX) &&
                        rsTargetEndZ.CloseEquals(endZ)
                    ))
                {
                    // Check if area was manually reset or uninitialized (-1) to not smooth
                    if (
                        rsTargetStartX.CloseEquals(-1f) &&
                        rsTargetStartZ.CloseEquals(-1f) &&
                        rsTargetEndX.CloseEquals(-1f) &&
                        rsTargetEndZ.CloseEquals(-1f)
                    )
                    {
                        rsCurrentPosition = 1f;
                    }
                    else
                    {
                        rsCurrentPosition = 1f / (PrModSettings.SmoothRenderAreaSteps + 1);
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
                    rsCurrentPosition += 1f / (PrModSettings.SmoothRenderAreaSteps + 1);
                }
            }

            var distX = endX - startX;
            var distZ = endZ - startZ;

            #endregion

#if DEBUG
            Log.Message($"Map {map}: " + "Calculated the rendered area");
#endif

            #region Calculate texture size

            // Calculate basic values that are used for rendering
            int newImageWidth;
            int newImageHeight;
            int ppcValue;

            if (GameComponentProgressManager.QualityAdjustment ==
                JPGQualityAdjustmentSetting
                    .Automatic)
            {
                ppcValue = GameComponentProgressManager.PixelsPerCellWorld;
            }
            else
            {
                ppcValue = PrModSettings.PixelsPerCell;
            }

            if (PrModSettings.ScaleOutputImage)
            {
                newImageWidth = (int)(PrModSettings.OutputImageFixedHeight / distZ * distX);
                newImageHeight = PrModSettings.OutputImageFixedHeight;
            }
            else
            {
                // set limits for ppc depending on map size to prevent render failure. Only guaranteed to work with vanilla sizes                
                if (ppcValue * distX > 16250 | ppcValue * distZ > 16250) // 16250 is the max width / height, due to Unity hard texture size limits
                {
                    var lowerPpcValue = Math.Min((int)(16250 / distX), (int)(16250 / distZ));
                    Messages.Message("LPR_SettingspixelsPerCellTooHighWarning".Translate(lowerPpcValue), MessageTypeDefOf.CautionInput, false);
                }

                newImageWidth = (int)(distX * ppcValue);
                newImageHeight = (int)(distZ * ppcValue);
            }

            var mustUpdateTexture = false;
            if (newImageWidth != imageWidth || newImageHeight != imageHeight)
            {
                mustUpdateTexture = true;
                imageWidth = newImageWidth;
                imageHeight = newImageHeight;
            }

            #endregion

#if DEBUG
            Log.Message($"Map {map}: " + $"Calculated the texture size {ppcValue}ppc {newImageWidth} x {newImageHeight}");
#endif

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
            var camRectMinX = Math.Min((int)startX, camViewRect.minX);
            var camRectMinZ = Math.Min((int)startZ, camViewRect.minZ);
            var camRectMaxX = Math.Max((int)Math.Ceiling(endX), camViewRect.maxX);
            var camRectMaxZ = Math.Max((int)Math.Ceiling(endZ), camViewRect.maxZ);
            var camDriverTraverse = Traverse.Create(camDriver);
            camDriverTraverse.Field("lastViewRect")
                .SetValue(CellRect.FromLimits(camRectMinX, camRectMinZ, camRectMaxX, camRectMaxZ));
            camDriverTraverse.Field("lastViewRectGetFrame").SetValue(Time.frameCount);

            #endregion

#if DEBUG
            Log.Message($"Map {map}: " + "Initialized camera");
#endif

            yield return new WaitForEndOfFrame();
#if DEBUG
            Log.Message($"Map {map}: " + "WaitForEndOfFrame");
#endif

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
                if (PrModSettings.RenderWeather)
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

#if DEBUG
            Log.Message($"Map {map}: " + "Did render");
#endif

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
            Find.PlaySettings.showZones = oldVisibilities.ShowZones;
            Find.PlaySettings.showRoofOverlay = oldVisibilities.ShowRoofOverlay;
            Find.PlaySettings.showFertilityOverlay = oldVisibilities.ShowFertilityOverlay;
            Find.PlaySettings.showTerrainAffordanceOverlay = oldVisibilities.ShowTerrainAffordanceOverlay;
            Find.PlaySettings.showPollutionOverlay = oldVisibilities.ShowPollutionOverlay;
            Find.PlaySettings.showTemperatureOverlay = oldVisibilities.ShowTemperatureOverlay;
            Prefs.DotHighlightDisplayMode = oldHighlight;
            //Enable Camera+
            if (PrMod.SkipCustomRenderingRef != null)
                PrMod.SkipCustomRenderingRef() = false;

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

#if DEBUG
            Log.Message($"Map {map}: " + "Restored pre-rendered state");
#endif

            // Hide message box
            if (messageBox != null)
            {
                messageBox.Close();
                messageBox = null;
            }
#if DEBUG
            Log.Message($"Map {map}: " + "Hid message box");
#endif

            yield return null;
#if DEBUG
            Log.Message($"Map {map}: " + "Waited a frame");
#endif
            
            // Start encoding
            TryCompleteEncoding();
            encodingTask = Task.Run(DoEncoding);
#if DEBUG
            Log.Message($"Map {map}: " + "Started encoding task");
#endif

            // Signal finished rendering
            Rendering = false;
#if DEBUG
            Log.Message($"Map {map}: " + "Released local lock");
#endif
        }

        private void TryCompleteEncoding()
        {
            if (encodingTask == null || encodingTask.IsCompleted) return;
            if (encodingTask.Wait(10000)) return;
            Log.Warning("Progress Renderer is taking too long to write the last render, aborting");
            ShowRenderFailureMessage();
        }

        private void DoEncoding()
        {
            if (encoding)
            {
                Log.Error("Progress Renderer is still encoding an image while the encoder was called again. This can lead to missing or wrong data.");
            }
            
            if (imageTexture.IsAllBlack())
            {
                Log.Warning("Attempted to encode blank image, skipping");
                ShowRenderFailureMessage();
                return;
            }

            switch (PrModSettings.Encoding)
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
            int encodeQuality = 0;
            switch (GameComponentProgressManager.QualityAdjustment)
            {
                case JPGQualityAdjustmentSetting.Manual:
                    encodeQuality = PrModSettings.JPGQuality;
                    break;
                case JPGQualityAdjustmentSetting.Automatic:
                    encodeQuality = GameComponentProgressManager.JPGQualityWorld;
                    break;
            }

            var encodedImage = imageTexture.EncodeToJPG(encodeQuality);
            SaveUnityEncoding(encodedImage);
            while (PrModSettings.JPGQualityInitialize)
            {
                Thread.Sleep(500);
                encodeQuality = GameComponentProgressManager.JPGQualityWorld;
                encodedImage = imageTexture.EncodeToJPG(encodeQuality);
                SaveUnityEncoding(encodedImage);
            }
        }

        private void SaveUnityEncoding(byte[] encodedImage)
        {
            // Create file and save encoded image
            var filePath = CreateCurrentFilePath();

            File.WriteAllBytes(filePath, encodedImage);

            // Create tmp copy to file if needed
            if (!manuallyTriggered && PrModSettings.FileNamePattern == FileNamePattern.BothTmpCopy)
            {
                File.Copy(filePath, CreateFilePath(FileNamePattern.Numbered, true));
            }

            if (File.Exists(filePath))
            {
                if (PrModSettings.Encoding == EncodingType.UnityJPG & GameComponentProgressManager.QualityAdjustment ==
                    JPGQualityAdjustmentSetting.Automatic)
                {
                    AdjustJPGQuality(filePath);
                }

                DoEncodingPost();
            }
            else
            {
                Log.Warning("Progress renderer could not write render to " + filePath);
            }
        }

        private void AdjustJPGQuality(string filePath)
        {
            // Adjust JPG quality to reach target filesize. Prefer quality going up over down.
            var renderMessage = "";
            if (PrModSettings.JPGQualityInitialize)
            {
                renderMessage += "Initializing (please wait), ";
            }

            if (!File.Exists(filePath))
            {
                renderMessage += "file was not written, aborting initialization";
                Messages.Message(renderMessage, MessageTypeDefOf.CautionInput, false);
                PrModSettings.JPGQualityInitialize = false;
                Log.Message($"During progress renderer initialization, an image was not written: {filePath}");
                return;

            }

            //Get size in mb
            var renderInfo = new FileInfo(filePath);
            var renderSize = renderInfo.Length / 1048576f;


            // quality has been adjusted in settings
            if (GameComponentProgressManager.RenderSize != GameComponentProgressManager.JPGQualityLastTarget)
            {
                GameComponentProgressManager.JPGQualityLastTarget = GameComponentProgressManager.RenderSize;
                GameComponentProgressManager.JPGQualityGoingUp = false;
                GameComponentProgressManager.JPGQualitySteady = false;
                renderMessage += "Target size adjusted, quality adjustment started, ";
            }
            // map conditions have changed enough that we're outside of our target
            else if (GameComponentProgressManager.JPGQualitySteady &
                     ((renderSize > GameComponentProgressManager.JPGQualityTopMargin) |
                      (renderSize <
                       GameComponentProgressManager.JPGQualityBottomMargin))) // margin after size target reached
            {
                renderMessage += "JPG quality adjustment resumed, ";
                GameComponentProgressManager.JPGQualitySteady = false;
            }

            if (GameComponentProgressManager.JPGQualitySteady)
            {
                if (PrModSettings.JPGQualityInitialize)
                {
                    renderMessage += "Target size reached, initialization ended, ";
                    PrModSettings.JPGQualityInitialize = false;
                }
                Messages.Message(renderMessage, MessageTypeDefOf.CautionInput, false);
                return;
            }

            renderMessage = CalculateQuality(renderSize, renderMessage);

            // while initializing, delete the files after adjusting quality
            if (PrModSettings.JPGQualityInitialize)
            {
                File.Delete(filePath);
            }

            Messages.Message(renderMessage, MessageTypeDefOf.CautionInput, false);
        }

        private string CalculateQuality(float renderSize, string renderMessage)
        {
            // if render is too large, let's take a closer look
            if (renderSize > GameComponentProgressManager.RenderSize)
            {
                if (GameComponentProgressManager.JPGQualityWorld > 0)
                {
                    // just decrease the quality
                    if (!GameComponentProgressManager.JPGQualityGoingUp)
                    {
                        GameComponentProgressManager.JPGQualityWorld -= 1;
                        renderMessage += "JPG quality decreased to " +
                                         GameComponentProgressManager.JPGQualityWorld +
                                         "% · render size: " + renderSize.ToString("0.00") + " Target: " +
                                         GameComponentProgressManager.RenderSize;
                    }
                    // if quality was going up and then down again, we have found the target quality
                    else if (!GameComponentProgressManager.JPGQualitySteady)
                    {
                        GameComponentProgressManager.JPGQualitySteady = true;
                        GameComponentProgressManager.JPGQualityTopMargin = Convert.ToInt32(Math.Ceiling(renderSize));
                        PrModSettings.JPGQualityInitialize = false; // if initializing, end it now
                        renderMessage += "JPG quality target reached (" +
                                         GameComponentProgressManager.JPGQualityWorld +
                                         "%) · render size: " + renderSize.ToString("0.00") + " Target: " +
                                         GameComponentProgressManager.RenderSize;
                    }
                    GameComponentProgressManager.JPGQualityGoingUp = false;
                }
                else if (PrModSettings.JPGQualityInitialize) // we've reached 0 going down, end initialization now
                {
                    renderMessage += "done";
                    PrModSettings.JPGQualityInitialize = false;
                }
            }
            else if (renderSize <= GameComponentProgressManager.RenderSize) // render is too small, increase quality
            {
                if (GameComponentProgressManager.JPGQualityWorld < 100)
                {
                    GameComponentProgressManager.JPGQualityWorld += 1;
                    GameComponentProgressManager.JPGQualityBottomMargin = Convert.ToInt32(Math.Floor(renderSize));
                    renderMessage += "JPG quality increased to " +
                                     GameComponentProgressManager.JPGQualityWorld +
                                     "% · render size: " + renderSize + " Target: " +
                                     GameComponentProgressManager.RenderSize;
                    GameComponentProgressManager.JPGQualityGoingUp = true;
                }
                else if (PrModSettings.JPGQualityInitialize) //we've reached 100 going up, end initialization now
                {
                    renderMessage += "done";
                    PrModSettings.JPGQualityInitialize = false;
                }
            }

            return renderMessage;
        }

        private string CreateCurrentFilePath()
        {
            return CreateFilePath(manuallyTriggered ? FileNamePattern.DateTime : PrModSettings.FileNamePattern);
        }

        private string CreateFilePath(FileNamePattern fileNamePattern, bool addTmpSubdir = false)
        {
            // Build image name
            var imageName = fileNamePattern == FileNamePattern.Numbered
                ? CreateImageNameNumbered()
                : CreateImageNameDateTime();
            imageName = Escape(imageName, Path.GetInvalidFileNameChars());

            // Create path and subdirectory
            var path = PrModSettings.ExportPath;
            if (PrModSettings.CreateSubdirs)
            {
                var subDir = Escape(Find.World.info.seedString, Path.GetInvalidPathChars());
                path = Path.Combine(path, subDir);
                if (!manuallyTriggered & GameComponentProgressManager.TileFoldersEnabled) // start using tile folders when a new game is created to avoid confusion in existing games
                {
                    path = Path.Combine(path, "tile-" + map.Tile);
                }
            }
            Directory.CreateDirectory(path);
            if (!Directory.Exists(path))
            {
                Log.Error($"Progress renderer could not create directory for {path} please check settings");
                PrModSettings.JPGQualityInitialize = false;
            }

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
            var fileExt = EnumUtils.GetFileExtension(PrModSettings.Encoding);
            var fileName = $"{imageName}.{fileExt}";
            var filePath = Path.Combine(path, fileName);
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

        private string Escape(string str, char[] invalidChars)
        {
            foreach (var c in invalidChars)
            {
                str = str.Replace(c.ToString(), "");
            }

            return str;
        }

        private string CreateImageNameDateTime()
        {
            var tick = Find.TickManager.TicksAbs;
            var longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
            var year = GenDate.Year(tick, longitude);
            var quadrum = MoreGenDate.QuadrumInteger(tick, longitude);
            var day = GenDate.DayOfQuadrum(tick, longitude) + 1;
            var hour = GenDate.HourInteger(tick, longitude);
            string mapName = PrModSettings.UseMapNameInstead ? map.ToString() : map.Tile.ToString();
            return "rimworld-" + Find.World.info.seedString + "-" + year + "-" + quadrum + "-" +
                   ((day < 10) ? "0" : "") + day + "-" + ((hour < 10) ? "0" : "") + hour + "-" + mapName;
        }

        private string CreateImageNameNumbered()
        {
            string mapName = PrModSettings.UseMapNameInstead ? map.ToString() : map.Tile.ToString();
            return "rimworld-" + Find.World.info.seedString + "-" +
                   lastRenderedCounter.ToString("000000") + "-" + mapName;
        }
    }
}
