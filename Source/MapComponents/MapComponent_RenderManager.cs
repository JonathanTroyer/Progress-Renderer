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
    public class MapComponentRenderManager : MapComponent
    {
        private static int _nextGlobalRenderManagerTickOffset;
        private bool _ctrlEncodingPost;
        private bool _encoding;

        private Task _encodingTask;
        private Texture2D _imageTexture;

        private int _imageWidth, _imageHeight;
        private int _lastRenderedCounter;
        private int _lastRenderedHour = -999;

        private bool _manuallyTriggered;
        private SmallMessageBox _messageBox;
        private float _rsCurrentPosition = 1f;
        private float _rsOldEndX = -1f;
        private float _rsOldEndZ = -1f;
        private float _rsOldStartX = -1f;
        private float _rsOldStartZ = -1f;
        private float _rsTargetEndX = -1f;
        private float _rsTargetEndZ = -1f;
        private float _rsTargetStartX = -1f;
        private float _rsTargetStartZ = -1f;

        private int _tickOffset = -1;

        public MapComponentRenderManager(Map map) : base(map)
        {
        }
        public bool Rendering { get; private set; }

        public override void FinalizeInit() // New map and after loading
        {
            if (_tickOffset < 0)
            {
                _tickOffset = _nextGlobalRenderManagerTickOffset;
                _nextGlobalRenderManagerTickOffset = (_nextGlobalRenderManagerTickOffset + 5) % GenTicks.TickRareInterval;
            }
        }

        public override void MapComponentUpdate()
        {
            // Watch for finished encoding to clean up and free memory
            if (_ctrlEncodingPost)
            {
                DoEncodingPost();
                _ctrlEncodingPost = false;
            }
        }

        public override void MapComponentTick()
        {
            // TickRare
            if (Find.TickManager.TicksGame % 250 != _tickOffset)
            {
                return;
            }

            // Check for rendering
            // Only render player home maps
            if (!map.IsPlayerHome && !PrModSettings.RenderNonPlayerHomes)
            {
                return;
            }

            // Check for correct time to render
            var longLat = Find.WorldGrid.LongLatOf(map.Tile);
            var currHour = MoreGenDate.HoursPassedInteger(Find.TickManager.TicksAbs, longLat.x);
            if (currHour <= _lastRenderedHour)
            {
                return;
            }

            if (currHour % PrModSettings.Interval != PrModSettings.TimeOfDay % PrModSettings.Interval)
            {
                return;
            }

            // Update timing variables
            _lastRenderedHour = currHour;
            _lastRenderedCounter++;
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
            Scribe_Values.Look(ref _lastRenderedHour, "lastRenderedHour", -999);
            Scribe_Values.Look(ref _lastRenderedCounter, "lastRenderedCounter");
            Scribe_Values.Look(ref _rsOldStartX, "rsOldStartX", -1f);
            Scribe_Values.Look(ref _rsOldStartZ, "rsOldStartZ", -1f);
            Scribe_Values.Look(ref _rsOldEndX, "rsOldEndX", -1f);
            Scribe_Values.Look(ref _rsOldEndZ, "rsOldEndZ", -1f);
            Scribe_Values.Look(ref _rsTargetStartX, "rsTargetStartX", -1f);
            Scribe_Values.Look(ref _rsTargetStartZ, "rsTargetStartZ", -1f);
            Scribe_Values.Look(ref _rsTargetEndX, "rsTargetEndX", -1f);
            Scribe_Values.Look(ref _rsTargetEndZ, "rsTargetEndZ", -1f);
            Scribe_Values.Look(ref _rsCurrentPosition, "rsCurrentPosition", 1f);
        }

        public static void TriggerCurrentMapManualRendering(bool forceRenderFullMap = false)
        {
            Find.CurrentMap.GetComponent<MapComponentRenderManager>().DoManualRendering(forceRenderFullMap);
        }

        private void DoManualRendering(bool forceRenderFullMap = false)
        {
            _manuallyTriggered = true;
            RequestDoRendering(forceRenderFullMap);
        }

        private void ShowCurrentRenderMessage()
        {
            if (PrModSettings.RenderFeedback == RenderFeedback.Window)
            {
                _messageBox = new SmallMessageBox("LPR_Rendering".Translate());
                Find.WindowStack.Add(_messageBox);
            }
            else if (PrModSettings.RenderFeedback == RenderFeedback.Message)
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
            if (!_manuallyTriggered)
            {
                // Test if target render area changed to reset smoothing
                if (!(
                        _rsTargetStartX.CloseEquals(startX) &&
                        _rsTargetStartZ.CloseEquals(startZ) &&
                        _rsTargetEndX.CloseEquals(endX) &&
                        _rsTargetEndZ.CloseEquals(endZ)
                    ))
                {
                    // Check if area was manually reset or uninitialized (-1) to not smooth
                    if (
                        _rsTargetStartX.CloseEquals(-1f) &&
                        _rsTargetStartZ.CloseEquals(-1f) &&
                        _rsTargetEndX.CloseEquals(-1f) &&
                        _rsTargetEndZ.CloseEquals(-1f)
                    )
                    {
                        _rsCurrentPosition = 1f;
                    }
                    else
                    {
                        _rsCurrentPosition = 1f / (PrModSettings.SmoothRenderAreaSteps + 1);
                    }

                    _rsOldStartX = _rsTargetStartX;
                    _rsOldStartZ = _rsTargetStartZ;
                    _rsOldEndX = _rsTargetEndX;
                    _rsOldEndZ = _rsTargetEndZ;
                    _rsTargetStartX = startX;
                    _rsTargetStartZ = startZ;
                    _rsTargetEndX = endX;
                    _rsTargetEndZ = endZ;
                }

                // Apply smoothing to render area
                if (_rsCurrentPosition < 1f)
                {
                    startX = _rsOldStartX + (_rsTargetStartX - _rsOldStartX) * _rsCurrentPosition;
                    startZ = _rsOldStartZ + (_rsTargetStartZ - _rsOldStartZ) * _rsCurrentPosition;
                    endX = _rsOldEndX + (_rsTargetEndX - _rsOldEndX) * _rsCurrentPosition;
                    endZ = _rsOldEndZ + (_rsTargetEndZ - _rsOldEndZ) * _rsCurrentPosition;
                    _rsCurrentPosition += 1f / (PrModSettings.SmoothRenderAreaSteps + 1);
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
            if (PrModSettings.ScaleOutputImage)
            {
                newImageWidth = (int)(PrModSettings.OutputImageFixedHeight / distZ * distX);
                newImageHeight = PrModSettings.OutputImageFixedHeight;
            }
            else
            {
                newImageWidth = (int)(distX * PrModSettings.PixelsPerCell);
                newImageHeight = (int)(distZ * PrModSettings.PixelsPerCell);
                if (GameComponentProgressManager.QualityAdjustment ==
                    JPGQualityAdjustmentSetting
                        .Automatic) // if image quality is set to automatic, PPC is also stored per map
                {
                    newImageWidth = (int)(distX * GameComponentProgressManager.PixelsPerCellWorld);
                    newImageHeight = (int)(distZ * GameComponentProgressManager.PixelsPerCellWorld);
                }
            }

            var mustUpdateTexture = false;
            if (newImageWidth != _imageWidth || newImageHeight != _imageHeight)
            {
                mustUpdateTexture = true;
                _imageWidth = newImageWidth;
                _imageHeight = newImageHeight;
            }

            #endregion

#if DEBUG
            Log.Message($"Map {map}: " + "Calculated the texture size");
#endif

            #region Initialize camera and textures

            var cameraPosX = distX / 2;
            var cameraPosZ = distZ / 2;
            var orthographicSize = cameraPosZ;
            var cameraBasePos = new Vector3(cameraPosX, 15f + (orthographicSize - 11f) / 49f * 50f, cameraPosZ);

            // Initialize cameras and textures
            var renderTexture = RenderTexture.GetTemporary(_imageWidth, _imageHeight, 24);
            if (_imageTexture == null || mustUpdateTexture)
            {
                _imageTexture = new Texture2D(_imageWidth, _imageHeight, TextureFormat.RGB24, false);
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
                _imageTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
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
            if (_messageBox != null)
            {
                _messageBox.Close();
                _messageBox = null;
            }
#if DEBUG
            Log.Message($"Map {map}: " + "Hid message box");
#endif

            yield return null;
#if DEBUG
            Log.Message($"Map {map}: " + "Waited a frame");
#endif

            // Start encoding
            if (_encodingTask != null && !_encodingTask.IsCompleted)
                _encodingTask.Wait();
            _encodingTask = Task.Run(DoEncoding);
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
            if (_encoding)
            {
                Log.Error("Progress Renderer is still encoding an image while the encoder was called again. This can lead to missing or wrong data.");
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
            // Clean up unused objects
            //UnityEngine.Object.Destroy(imageTexture);

            // Signal finished encoding
            _manuallyTriggered = false;
            _encoding = false;
        }

        private void EncodeUnityPng()
        {
            var encodedImage = _imageTexture.EncodeToPNG();
            SaveUnityEncoding(encodedImage);
        }

        private void EncodeUnityJpg()
        {
            var encodeQuality = 0;
            switch (GameComponentProgressManager.QualityAdjustment)
            {
                case JPGQualityAdjustmentSetting.Manual:
                    encodeQuality = PrModSettings.JPGQuality;
                    break;
                case JPGQualityAdjustmentSetting.Automatic:
                    encodeQuality = GameComponentProgressManager.JPGQualityWorld;
                    break;
            }

            var encodedImage = _imageTexture.EncodeToJPG(encodeQuality);
            SaveUnityEncoding(encodedImage);
            while (PrModSettings.JPGQualityInitialize)
            {
                Thread.Sleep(500);
                encodeQuality = GameComponentProgressManager.JPGQualityWorld;
                encodedImage = _imageTexture.EncodeToJPG(encodeQuality);
                SaveUnityEncoding(encodedImage);
            }
        }

        private void SaveUnityEncoding(byte[] encodedImage)
        {
            // Create file and save encoded image
            var filePath = CreateCurrentFilePath();

            File.WriteAllBytes(filePath, encodedImage);

            // Create tmp copy to file if needed
            if (!_manuallyTriggered && PrModSettings.FileNamePattern == FileNamePattern.BothTmpCopy)
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

            if (!File.Exists(filePath)) return;

            //Get size in mb
            var renderInfo = new FileInfo(filePath);
            var renderSize = renderInfo.Length / 1048576f;

            var renderMessage = "";
            if (PrModSettings.JPGQualityInitialize)
            {
                renderMessage += "Initializing (please wait), ";
            }

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
                     (renderSize > GameComponentProgressManager.JPGQualityTopMargin |
                      renderSize <
                      GameComponentProgressManager.JPGQualityBottomMargin)) // margin after size target reached
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
            return CreateFilePath(_manuallyTriggered ? FileNamePattern.DateTime : PrModSettings.FileNamePattern);
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
            }

            Directory.CreateDirectory(path);
            // Add subdir for manually triggered renderings
            if (_manuallyTriggered)
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
            var mapName = PrModSettings.UseMapNameInstead ? map.ToString() : map.Tile.ToString();
            return "rimworld-" + Find.World.info.seedString + "-" + mapName + "-" + year + "-" + quadrum + "-" +
                   (day < 10 ? "0" : "") + day + "-" + (hour < 10 ? "0" : "") + hour;
        }

        private string CreateImageNameNumbered()
        {
            var mapName = PrModSettings.UseMapNameInstead ? map.ToString() : map.Tile.ToString();
            return "rimworld-" + Find.World.info.seedString + "-" + mapName + "-" +
                   _lastRenderedCounter.ToString("000000");
        }

        private struct VisibilitySettings
        {
            public bool ShowZones;
            public bool ShowRoofOverlay;
            public bool ShowFertilityOverlay;
            public bool ShowTerrainAffordanceOverlay;
            public bool ShowPollutionOverlay;
            public bool ShowTemperatureOverlay;
        }
    }
}
