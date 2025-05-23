﻿#define VERSION_1_5

using System;
using System.Collections;
using System.IO;
using System.Reflection;
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
            if (!map.IsPlayerHome && !PRModSettings.renderNonPlayerHomes)
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
            if (!GameComponentProgressManager.enabled)
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
            var rememberedWorldRendered = WorldRendererUtility.WorldRenderedNow;
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
                showZones = settings.showZones,
                showRoofOverlay = settings.showRoofOverlay,
                showFertilityOverlay = settings.showFertilityOverlay,
                showTerrainAffordanceOverlay = settings.showTerrainAffordanceOverlay,
                showPollutionOverlay = settings.showPollutionOverlay,
                showTemperatureOverlay = settings.showTemperatureOverlay
            };
#if VERSION_1_5
            var oldHighlight = Prefs.DotHighlightDisplayMode;
#endif

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
            
#if VERSION_1_5
            Prefs.DotHighlightDisplayMode = DotHighlightDisplayMode.None;
#endif
            //Turn off Camera+ stuff
            if(PRMod.SkipCustomRenderingRef != null)
                PRMod.SkipCustomRenderingRef() = true;
            
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

#if DEBUG
            Log.Message($"Map {map}: " + "Calculated the rendered area");
#endif

            #region Calculate texture size

            // Calculate basic values that are used for rendering
            int newImageWidth;
            int newImageHeight;
            int ppcValue = 1;
            
            if (GameComponentProgressManager.qualityAdjustment ==
                    JPGQualityAdjustmentSetting
                        .Automatic)
            {
                ppcValue = GameComponentProgressManager.pixelsPerCell_WORLD;
            }
            else
            {
                ppcValue = (int)PRModSettings.pixelsPerCell;
            }
            
            if (PRModSettings.scaleOutputImage)
            {
                newImageWidth = (int)(PRModSettings.outputImageFixedHeight / distZ * distX);
                newImageHeight = PRModSettings.outputImageFixedHeight;
            }
            else
            {
                // set limits for ppc depending on map size to prevent render failure. Only guaranteed to work with vanilla sizes                
                if ((ppcValue * distX > 16250) | (ppcValue * distZ > 16250)) // 16250 is the max width / height, due to Unity hard texture size limits
                {
                    ppcValue = Math.Min((int)(16250 / distX), (int)(16250 / distZ));
                    if (ppcValue > 0)
                        { Messages.Message($"Your progress renderer ppc setting is too high for this map size. Please reduce it to: {ppcValue}", MessageTypeDefOf.CautionInput, false); }
                    else // having a ppc of 0 is a really bad idea
                        { 
                            Messages.Message($"Unity can't handle your map size with progress renderer and it will likely fail", MessageTypeDefOf.CautionInput, false);
                            ppcValue = 1;
                        }
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
            Find.PlaySettings.showZones = oldVisibilities.showZones;
            Find.PlaySettings.showRoofOverlay = oldVisibilities.showRoofOverlay;
            Find.PlaySettings.showFertilityOverlay = oldVisibilities.showFertilityOverlay;
            Find.PlaySettings.showTerrainAffordanceOverlay = oldVisibilities.showTerrainAffordanceOverlay;
            Find.PlaySettings.showPollutionOverlay = oldVisibilities.showPollutionOverlay;
            Find.PlaySettings.showTemperatureOverlay = oldVisibilities.showTemperatureOverlay;
#if VERSION_1_5
            Prefs.DotHighlightDisplayMode = oldHighlight;
#endif
            //Enable Camera+
            if(PRMod.SkipCustomRenderingRef != null)
                PRMod.SkipCustomRenderingRef() = false;
            
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
            if (EncodingTask != null && !EncodingTask.IsCompleted)
                EncodingTask.Wait();
            EncodingTask = Task.Run(DoEncoding);
#if DEBUG
            Log.Message($"Map {map}: " + "Started encoding task");
#endif

            // Signal finished rendering
            Rendering = false;
#if DEBUG
            Log.Message($"Map {map}: " + "Released local lock");
#endif
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
            int encodeQuality = 0;
            switch (GameComponentProgressManager.qualityAdjustment)
            {
                case JPGQualityAdjustmentSetting.Manual:
                    encodeQuality = PRModSettings.JPGQuality;
                    break;
                case JPGQualityAdjustmentSetting.Automatic:
                    encodeQuality = GameComponentProgressManager.JPGQuality_WORLD;
                    break;
            }

            var encodedImage = imageTexture.EncodeToJPG(encodeQuality);
            SaveUnityEncoding(encodedImage);
            while (PRModSettings.JPGQualityInitialize)
            {
                System.Threading.Thread.Sleep(500);
                encodeQuality = GameComponentProgressManager.JPGQuality_WORLD;
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
            if (!manuallyTriggered && PRModSettings.fileNamePattern == FileNamePattern.BothTmpCopy)
            {
                File.Copy(filePath, CreateFilePath(FileNamePattern.Numbered, true));
            }

            if (File.Exists(filePath))
            {
                if (PRModSettings.encoding == EncodingType.UnityJPG & GameComponentProgressManager.qualityAdjustment ==
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
            if (PRModSettings.JPGQualityInitialize)
            {
                renderMessage += "Initializing (please wait), ";
            }
            
            if (!File.Exists(filePath))
            {
                renderMessage += "file was not written, aborting initialization";
                Messages.Message(renderMessage, MessageTypeDefOf.CautionInput, false);
                PRModSettings.JPGQualityInitialize = false;
                Log.Message($"During progress renderer initialization, an image was not written: {filePath}");
                return;

            }

            //Get size in mb
            var renderInfo = new FileInfo(filePath);
            var renderSize = renderInfo.Length / 1048576f;


            // quality has been adjusted in settings
            if (GameComponentProgressManager.renderSize != GameComponentProgressManager.JPGQualityLastTarget)
            {
                GameComponentProgressManager.JPGQualityLastTarget = GameComponentProgressManager.renderSize;
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
                if (PRModSettings.JPGQualityInitialize)
                {
                    renderMessage += "Target size reached, initialization ended, ";
                    PRModSettings.JPGQualityInitialize = false;
                }
                Messages.Message(renderMessage, MessageTypeDefOf.CautionInput, false);
                return;
            }

            renderMessage = CalculateQuality(renderSize, renderMessage);

            // while initializing, delete the files after adjusting quality
            if (PRModSettings.JPGQualityInitialize)
            {
                File.Delete(filePath);
            }

            Messages.Message(renderMessage, MessageTypeDefOf.CautionInput, false);
        }

        private string CalculateQuality(float renderSize, string renderMessage)
        {
            // if render is too large, let's take a closer look
            if (renderSize > GameComponentProgressManager.renderSize)
            {
                if (GameComponentProgressManager.JPGQuality_WORLD > 0)
                {
                    // just decrease the quality
                    if (!GameComponentProgressManager.JPGQualityGoingUp)
                    {
                        GameComponentProgressManager.JPGQuality_WORLD -= 1;
                        renderMessage += "JPG quality decreased to " +
                                         GameComponentProgressManager.JPGQuality_WORLD +
                                         "% · render size: " + renderSize.ToString("0.00") + " Target: " +
                                         GameComponentProgressManager.renderSize;
                    }
                    // if quality was going up and then down again, we have found the target quality
                    else if (!GameComponentProgressManager.JPGQualitySteady)
                    {
                        GameComponentProgressManager.JPGQualitySteady = true;
                        GameComponentProgressManager.JPGQualityTopMargin = Convert.ToInt32(Math.Ceiling(renderSize));
                        PRModSettings.JPGQualityInitialize = false; // if initializing, end it now
                        renderMessage += "JPG quality target reached (" +
                                         GameComponentProgressManager.JPGQuality_WORLD +
                                         "%) · render size: " + renderSize.ToString("0.00") + " Target: " +
                                         GameComponentProgressManager.renderSize;
                    }
                    GameComponentProgressManager.JPGQualityGoingUp = false;
                }
                else if (PRModSettings.JPGQualityInitialize) // we've reached 0 going down, end initialization now
                {
                    renderMessage += "done";
                    PRModSettings.JPGQualityInitialize = false;
                }
            }
            else if (renderSize <= GameComponentProgressManager.renderSize) // render is too small, increase quality
            {
                if (GameComponentProgressManager.JPGQuality_WORLD < 100)
                {
                    GameComponentProgressManager.JPGQuality_WORLD += 1;
                    GameComponentProgressManager.JPGQualityBottomMargin = Convert.ToInt32(Math.Floor(renderSize));
                    renderMessage += "JPG quality increased to " +
                                     GameComponentProgressManager.JPGQuality_WORLD +
                                     "% · render size: " + renderSize + " Target: " +
                                     GameComponentProgressManager.renderSize;
                    GameComponentProgressManager.JPGQualityGoingUp = true;
                }
                else if (PRModSettings.JPGQualityInitialize) //we've reached 100 going up, end initialization now
                {
                    renderMessage += "done";
                    PRModSettings.JPGQualityInitialize = false;
                }
            }

            return renderMessage;
        }

        private string CreateCurrentFilePath()
        {
            return CreateFilePath(manuallyTriggered ? FileNamePattern.DateTime : PRModSettings.fileNamePattern);
        }

        private string CreateFilePath(FileNamePattern fileNamePattern, bool addTmpSubdir = false)
        {
            // Build image name
            var imageName = fileNamePattern == FileNamePattern.Numbered
                ? CreateImageNameNumbered()
                : CreateImageNameDateTime();
            imageName = Escape(imageName, Path.GetInvalidFileNameChars());

            // Create path and subdirectory
            var path = PRModSettings.exportPath;
            if (PRModSettings.createSubdirs)
            {
                var subDir = Escape(Find.World.info.seedString, Path.GetInvalidPathChars());
                path = Path.Combine(path, subDir);
                if (!manuallyTriggered & GameComponentProgressManager.tileFoldersEnabled) // start using tile folders when a new game is created to avoid confusion in existing games
                {
                    path = Path.Combine(path, "tile-" + map.Tile.ToString());
                }
            }
            if (!Directory.Exists(path))
            {
                Log.Error($"Progress renderer could not create directory for {path} please check settings");
                PRModSettings.JPGQualityInitialize = false;

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
            string mapName = PRModSettings.useMapNameInstead ? map.ToString() : map.Tile.ToString();
            return "rimworld-" + Find.World.info.seedString + "-" + year + "-" + quadrum + "-" +
                   ((day < 10) ? "0" : "") + day + "-" + ((hour < 10) ? "0" : "") + hour + "-" + mapName;
        }

        private string CreateImageNameNumbered()
        {
            string mapName = PRModSettings.useMapNameInstead ? map.ToString() : map.Tile.ToString();
            return "rimworld-" + Find.World.info.seedString + "-" +
                   lastRenderedCounter.ToString("000000") + "-" + mapName;
        }
    }
}
