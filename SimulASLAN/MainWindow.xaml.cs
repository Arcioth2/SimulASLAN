using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfApp1
{
    [ComVisible(true)]
    public partial class MainWindow : Window
    {
        private readonly DronePhysics _physics = new DronePhysics();
        private DateTime _lastFrameTime;
        private bool _isSimulationRunning = false;
        private bool _bKeyPressed = false;

        private double _cameraAzimuth = 0;
        private double _cameraElevation = 25;
        private double _cameraDistance = 30;

        private double _centerLat;
        private double _centerLon;
        private int _mapScale;
        private string _language;
        private const double DefaultMapSideMeters = 833;
        private readonly double _mapSideMeters;
        private (double metersPerLat, double metersPerLon) _metersPerDegree;
        private (double swLat, double swLon, double neLat, double neLon) _mapBounds;

        private MissionPlan _activeMission = new MissionPlan { Name = "New Mission" };
        private bool _missionRunning = false;
        private int _currentWaypointIndex = 0;
        private bool _mapReady = false;
        private readonly string _missionsFolder;
        private readonly ModelVisual3D _waypointsGroup = new ModelVisual3D();

        public MainWindow(double lat, double lon, int quality, string language, double mapSideMeters)
        {
            InitializeComponent();

            _centerLat = lat;
            _centerLon = lon;
            _mapScale = quality;
            _language = language;
            _mapSideMeters = mapSideMeters;
            _missionsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "missions");
            Directory.CreateDirectory(_missionsFolder);

            ApplyLanguage();

            txtCoords.Text = (_language == "TR" ? "Konum: " : "Loc: ") + $"{_centerLat:F5}, {_centerLon:F5} | Q: {_mapScale} | {_mapSideMeters:F0}m";
            InitializeMissionPlannerFields();
            PopulateSavedMissions();
            InitializeSimulatorAsync();
        }

        public MainWindow() : this(41.145253, 29.3678, 4, "EN", DefaultMapSideMeters) { }

        private void ApplyLanguage()
        {
            if (_language == "TR")
            {
                Title = "İstanbul Drone Simülatörü";
                lblFlightData.Text = "UÇUŞ VERİLERİ";
                lblCtrlThrottle.Text = "BOŞLUK: Gaz | Z: Turbo";
                lblCtrlImpulse.Text = "B: SÜPER İTİŞ (10x Güç)";
                lblCtrlPitch.Text = "W/S: Yunuslama | A/D: Yuvarlanma";
                lblCtrlYaw.Text = "Q/E: Sapma";
                lblCtrlCam.Text = "OK TUŞLARI: Kamera";
                lblInitTitle.Text = "GÖREV BAŞLATILIYOR";
                lblInitWait.Text = "Arazi Verisi Bekleniyor...";
                txtTelemetry.Text = "Başlatılıyor...";
                btnStartMission.Content = "Görevi Başlat";
                btnStopMission.Content = "Görevi Durdur";
                btnFailsafe.Content = "Acil Stop";
            }
            else
            {
                Title = "Istanbul Drone Simulator";
                lblFlightData.Text = "FLIGHT DATA";
                lblCtrlThrottle.Text = "SPACE: Throttle | Z: Turbo Boost";
                lblCtrlImpulse.Text = "B: SUPER IMPULSE (10x Kick)";
                lblCtrlPitch.Text = "W/S: Pitch | A/D: Roll";
                lblCtrlYaw.Text = "Q/E: Yaw";
                lblCtrlCam.Text = "ARROWS: Move Camera";
                lblInitTitle.Text = "INITIALIZING MISSION";
                lblInitWait.Text = "Waiting for Terrain Data...";
                txtTelemetry.Text = "Initializing...";
                btnStartMission.Content = "Start Mission";
                btnStopMission.Content = "Stop Mission";
                btnFailsafe.Content = "Failsafe";
            }
        }

        private async void InitializeSimulatorAsync()
        {
            try
            {
                viewPort.Children.Clear();
                viewPort.Children.Add(new DefaultLights());

                LoadingOverlay.Visibility = Visibility.Visible;
                txtStatus.Text = _language == "TR" ? "Başlatılıyor..." : "Starting up...";

                await InitializeMapAsync();
                InitializeDroneVisuals();
                EnsureWaypointVisualLayer();

                LoadingOverlay.Visibility = Visibility.Collapsed;
                _isSimulationRunning = true;
                _lastFrameTime = DateTime.Now;

                CompositionTarget.Rendering -= GameLoop;
                CompositionTarget.Rendering += GameLoop;
            }
            catch (Exception ex)
            {
                txtStatus.Text = _language == "TR" ? "Kritik Hata!" : "Critical Error!";
                MessageBox.Show($"Setup Failed: {ex.Message}");
            }
        }

        private async Task InitializeMapAsync()
        {
            txtStatus.Text = _language == "TR" ? "Koordinatlar Hesaplanıyor..." : "Calculating Coordinates...";

            double metersPerLat = 111132;
            double metersPerLon = 111132 * Math.Cos(_centerLat * Math.PI / 180.0);
            _metersPerDegree = (metersPerLat, metersPerLon);

            double deltaLat = (_mapSideMeters / 2.0) / metersPerLat;
            double deltaLon = (_mapSideMeters / 2.0) / metersPerLon;

            double swLat = _centerLat - deltaLat;
            double swLon = _centerLon - deltaLon;
            double neLat = _centerLat + deltaLat;
            double neLon = _centerLon + deltaLon;

            _mapBounds = (swLat, swLon, neLat, neLon);

            string url = string.Format(CultureInfo.InvariantCulture,
                "https://www.google.com/maps/d/u/0/mapimage?mid=1p__1h3xMyAPFLMpgxqZStptq4kwdx_I&llsw={0},{1}&llne={2},{3}&w=1600&h=1600&scale={4}",
                swLat, swLon, neLat, neLon, _mapScale);

            string fileName = string.Format(CultureInfo.InvariantCulture, "map_{0}_{1}_{2}m.jpg", _centerLat, _centerLon, _mapSideMeters);
            string finalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string sourceFile = Path.Combine(downloadsFolder, "mapimage.jpg");
            string bundledFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mapimage.jpg");

            if (!File.Exists(finalPath))
            {
                bool copiedImmediately = TryCopyDownloadedMap(sourceFile, bundledFile, finalPath);

                if (!copiedImmediately)
                {
                    txtStatus.Text = _language == "TR" ? "Tarayıcı Açılıyor... Lütfen Resmi Kaydedin." : "Opening Browser... Please Save Image.";

                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch
                    {
                        Clipboard.SetText(url);
                        MessageBox.Show(_language == "TR" ? "Link kopyalandı. Lütfen tarayıcıya yapıştırın." : "Link copied to clipboard. Please paste into your browser.");
                    }

                    txtStatus.Text = _language == "TR" ? "'mapimage.jpg' İndirilenler Klasöründe Bekleniyor..." : "Waiting for 'mapimage.jpg' in Downloads...";

                    bool found = false;
                    for (int i = 0; i < 60; i++)
                    {
                        if (TryCopyDownloadedMap(sourceFile, bundledFile, finalPath))
                        {
                            found = true;
                            break;
                        }

                        await Task.Delay(1000);
                    }

                    if (!found)
                    {
                        string msg = _language == "TR"
                            ? $"Dosya bulunamadı.\nDosyayı buraya kaydettiniz mi:\n{downloadsFolder}?"
                            : $"Timed out waiting for 'mapimage.jpg'.\nDid you save it to:\n{downloadsFolder}?";

                        MessageBox.Show(msg);
                        Application.Current.Shutdown();
                        return;
                    }
                }
            }

            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(finalPath));
                MissionMapImage.Source = bitmap;
                _mapReady = true;
                txtMapStatus.Text = _language == "TR" ? "Harita hazır - yeni nokta için tıkla" : "Map ready - click to add";
                SyncMapWaypoints();
            }
            catch (Exception ex)
            {
                txtMapStatus.Text = _language == "TR" ? $"Harita yüklenemedi: {ex.Message}" : $"Map failed to load: {ex.Message}";
            }

            txtStatus.Text = _language == "TR" ? "Oluşturuluyor..." : "Rendering...";
            var material = MaterialHelper.CreateImageMaterial(finalPath, 1.0);

            var ground = new BoxVisual3D()
            {
                Center = new Point3D(0, 0, -2.0),
                Length = _mapSideMeters,
                Width = _mapSideMeters,
                Height = 0.1,
                Material = material
            };

            var transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 180)));
            ground.Transform = transformGroup;

            viewPort.Children.Add(ground);
        }

        private bool TryCopyDownloadedMap(string sourceFile, string bundledFile, string destination)
        {
            try
            {
                if (File.Exists(bundledFile))
                {
                    File.Copy(bundledFile, destination, true);
                    return true;
                }

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, destination, true);
                    return true;
                }
            }
            catch
            {
            }

            return File.Exists(destination);
        }

        private void InitializeDroneVisuals()
        {
            var importer = new ModelImporter();
            try
            {
                Model3DGroup droneModel = importer.Load("Models/drone.obj");
                var transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
                transformGroup.Children.Add(new ScaleTransform3D(0.1, 0.1, 0.1));
                droneModel.Transform = transformGroup;
                droneObject.Content = droneModel;
            }
            catch
            {
                var arm1 = new BoxVisual3D() { Center = new Point3D(0, 0, 0), Length = 1.0, Width = 0.1, Height = 0.1, Fill = Brushes.Red };
                var arm2 = new BoxVisual3D() { Center = new Point3D(0, 0, 0), Length = 0.1, Width = 1.0, Height = 0.1, Fill = Brushes.Red };
                var front = new BoxVisual3D() { Center = new Point3D(0, 0.5, 0), Length = 0.2, Width = 0.2, Height = 0.2, Fill = Brushes.LimeGreen };

                droneObject.Children.Add(arm1);
                droneObject.Children.Add(arm2);
                droneObject.Children.Add(front);
            }

            if (!viewPort.Children.Contains(droneObject))
            {
                viewPort.Children.Add(droneObject);
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!_isSimulationRunning) return;

            var now = DateTime.Now;
            double dt = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            HandleInput();
            _physics.Update(dt);
            UpdateMissionAutopilot(dt);
            UpdateGraphics();
        }

        private void HandleInput()
        {
            if (_missionRunning)
            {
                _physics.ThrottleInput = 0.0;
                _physics.IsBoosting = false;
                _physics.Pitch = 0;
                _physics.Roll = 0;
                return;
            }

            if (Keyboard.IsKeyDown(Key.Space)) _physics.ThrottleInput = 1.0;
            else _physics.ThrottleInput = 0.0;

            _physics.IsBoosting = Keyboard.IsKeyDown(Key.Z);

            if (Keyboard.IsKeyDown(Key.B))
            {
                if (!_bKeyPressed)
                {
                    double currentSpeed = new Vector3D(_physics.Velocity.X, _physics.Velocity.Y, 0).Length;
                    double kick = (currentSpeed > 5.0) ? currentSpeed * 10.0 : 50.0;
                    _physics.ApplyForwardImpulse(kick);
                    _bKeyPressed = true;
                }
            }
            else
            {
                _bKeyPressed = false;
            }

            if (Keyboard.IsKeyDown(Key.W)) _physics.Pitch = -20;
            else if (Keyboard.IsKeyDown(Key.S)) _physics.Pitch = 20;
            else _physics.Pitch = 0;

            if (Keyboard.IsKeyDown(Key.A)) _physics.Roll = -20;
            else if (Keyboard.IsKeyDown(Key.D)) _physics.Roll = 20;
            else _physics.Roll = 0;

            if (Keyboard.IsKeyDown(Key.Q)) _physics.Yaw += 1;
            if (Keyboard.IsKeyDown(Key.E)) _physics.Yaw -= 1;

            double camSpeed = 1.5;
            if (Keyboard.IsKeyDown(Key.Left)) _cameraAzimuth -= camSpeed;
            if (Keyboard.IsKeyDown(Key.Right)) _cameraAzimuth += camSpeed;
            if (Keyboard.IsKeyDown(Key.Up)) _cameraElevation += camSpeed;
            if (Keyboard.IsKeyDown(Key.Down)) _cameraElevation -= camSpeed;

            if (_cameraElevation < 5) _cameraElevation = 5;
            if (_cameraElevation > 85) _cameraElevation = 85;
        }

        private void UpdateMissionAutopilot(double deltaTime)
        {
            if (!_missionRunning || !_activeMission.Waypoints.Any())
            {
                UpdateMissionStatusText();
                return;
            }

            if (_currentWaypointIndex >= _activeMission.Waypoints.Count)
            {
                _missionRunning = false;
                txtNextAction.Text = _language == "TR" ? "Görev tamamlandı" : "Mission complete";
                return;
            }

            var targetWp = _activeMission.Waypoints[_currentWaypointIndex];
            var targetPoint = LatLonToLocal(targetWp.Latitude, targetWp.Longitude);
            Vector3D targetPos = new Vector3D(targetPoint.X, targetPoint.Y, targetWp.Altitude);
            Vector3D toTarget = targetPos - _physics.Position;
            double distance = toTarget.Length;

            if (distance < 1.0)
            {
                _currentWaypointIndex++;
                UpdateMissionStatusText();
                return;
            }

            toTarget.Normalize();
            double speed = 12.0;
            _physics.Position += toTarget * speed * deltaTime;
            _physics.OverrideVelocity(toTarget * speed);
            _physics.Yaw = targetWp.Heading;

            txtNextAction.Text = _language == "TR"
                ? $"WP {_currentWaypointIndex + 1}/{_activeMission.Waypoints.Count} noktasına ilerleniyor"
                : $"Heading to WP {_currentWaypointIndex + 1}/{_activeMission.Waypoints.Count}";
        }

        private void UpdateGraphics()
        {
            rotPitch.Angle = _physics.Pitch;
            rotRoll.Angle = _physics.Roll;
            rotYaw.Angle = _physics.Yaw;

            transPos.OffsetX = _physics.Position.X;
            transPos.OffsetY = _physics.Position.Y;
            transPos.OffsetZ = _physics.Position.Z;

            double radAzimuth = (_cameraAzimuth - 90) * Math.PI / 180.0;
            double radElevation = _cameraElevation * Math.PI / 180.0;

            double camOffsetX = _cameraDistance * Math.Cos(radElevation) * Math.Cos(radAzimuth);
            double camOffsetY = _cameraDistance * Math.Cos(radElevation) * Math.Sin(radAzimuth);
            double camOffsetZ = _cameraDistance * Math.Sin(radElevation);

            cam.Position = new Point3D(
                _physics.Position.X + camOffsetX,
                _physics.Position.Y + camOffsetY,
                _physics.Position.Z + camOffsetZ
            );

            cam.LookDirection = new Vector3D(-camOffsetX, -camOffsetY, -camOffsetZ);

            UpdateDroneShadow();

            if (txtTelemetry != null)
            {
                string boostText = _physics.IsBoosting ? " [TURBO]" : "";
                txtTelemetry.Text = $"Alt: {_physics.Position.Z:F1}m\nPos: {_physics.Position.X:F0}, {_physics.Position.Y:F0}{boostText}";
            }
        }

        private (double X, double Y) LatLonToLocal(double lat, double lon)
        {
            double x = (lon - _centerLon) * _metersPerDegree.metersPerLon;
            double y = (lat - _centerLat) * _metersPerDegree.metersPerLat;
            return (x, y);
        }

        private void UpdateDroneShadow()
        {
            if (ShadowYawTransform == null || ShadowHeadingRotate == null || ShadowSkewTransform == null || ShadowFootprint == null || ShadowBody == null)
            {
                return;
            }

            ShadowYawTransform.Angle = _physics.Yaw;
            ShadowHeadingRotate.Angle = _physics.Yaw;

            ShadowSkewTransform.AngleX = _physics.Roll / 3.0;
            ShadowSkewTransform.AngleY = -_physics.Pitch / 3.0;

            double altitudeOpacity = Math.Max(0.25, 1.0 - (_physics.Position.Z / 80.0));
            ShadowFootprint.Opacity = altitudeOpacity;
            ShadowBody.Opacity = Math.Max(0.4, altitudeOpacity);
        }

        private void BtnStartMission_Click(object sender, RoutedEventArgs e)
        {
            if (!_activeMission.Waypoints.Any())
            {
                txtNextAction.Text = _language == "TR" ? "Hiç görev noktası yok" : "No waypoints defined";
                return;
            }

            _missionRunning = true;
            _currentWaypointIndex = 0;
            UpdateMissionStatusText();
        }

        private void BtnStopMission_Click(object sender, RoutedEventArgs e)
        {
            _missionRunning = false;
            txtNextAction.Text = _language == "TR" ? "Görev durduruldu" : "Mission stopped";
        }

        private void BtnFailsafe_Click(object sender, RoutedEventArgs e)
        {
            _missionRunning = false;
            _physics.Position = new Vector3D(0, 0, 0.5);
            _physics.OverrideVelocity(new Vector3D(0, 0, 0));
            txtNextAction.Text = _language == "TR" ? "Acil durum devrede" : "Failsafe triggered";
        }

        private void InitializeMissionPlannerFields()
        {
            txtLat.Text = _centerLat.ToString(CultureInfo.InvariantCulture);
            txtLon.Text = _centerLon.ToString(CultureInfo.InvariantCulture);
            txtAlt.Text = "20";
            txtHeading.Text = "0";
            txtMissionName.Text = _activeMission.Name;
        }

        private void BtnAddWaypoint_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadWaypointInputs(out MissionWaypoint waypoint)) return;

            _activeMission.Waypoints.Add(waypoint);
            UpdateMissionListDisplay();
            SyncMapWaypoints();
        }

        private void BtnUpdateWaypoint_Click(object sender, RoutedEventArgs e)
        {
            if (lstWaypoints.SelectedIndex < 0) return;
            if (!TryReadWaypointInputs(out MissionWaypoint waypoint)) return;

            _activeMission.Waypoints[lstWaypoints.SelectedIndex] = waypoint;
            UpdateMissionListDisplay();
            SyncMapWaypoints();
        }

        private void BtnRemoveWaypoint_Click(object sender, RoutedEventArgs e)
        {
            if (lstWaypoints.SelectedIndex < 0) return;
            _activeMission.Waypoints.RemoveAt(lstWaypoints.SelectedIndex);
            UpdateMissionListDisplay();
            SyncMapWaypoints();
        }

        private void LstWaypoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstWaypoints.SelectedIndex < 0 || lstWaypoints.SelectedIndex >= _activeMission.Waypoints.Count) return;
            var wp = _activeMission.Waypoints[lstWaypoints.SelectedIndex];
            txtLat.Text = wp.Latitude.ToString(CultureInfo.InvariantCulture);
            txtLon.Text = wp.Longitude.ToString(CultureInfo.InvariantCulture);
            txtAlt.Text = wp.Altitude.ToString(CultureInfo.InvariantCulture);
            txtHeading.Text = wp.Heading.ToString(CultureInfo.InvariantCulture);
        }

        private bool TryReadWaypointInputs(out MissionWaypoint waypoint)
        {
            waypoint = null;
            if (!double.TryParse(txtLat.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat)) return false;
            if (!double.TryParse(txtLon.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon)) return false;
            if (!double.TryParse(txtAlt.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double alt)) return false;
            if (!double.TryParse(txtHeading.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double heading)) return false;

            waypoint = new MissionWaypoint
            {
                Latitude = lat,
                Longitude = lon,
                Altitude = alt,
                Heading = heading
            };
            return true;
        }

        private void UpdateMissionListDisplay()
        {
            lstWaypoints.ItemsSource = null;
            lstWaypoints.ItemsSource = _activeMission.Waypoints
                .Select((wp, i) => $"{i + 1}. {wp.Latitude:F5}, {wp.Longitude:F5} | Alt {wp.Altitude}m | Hdg {wp.Heading}°")
                .ToList();
            txtMissionName.Text = _activeMission.Name;
        }

        private void SyncMapWaypoints()
        {
            if (!_mapReady) return;

            try
            {
                RenderWaypointOverlays();
                RenderWaypointModels();
            }
            catch (Exception ex)
            {
                txtMapStatus.Text = ($"Map update failed: {ex.Message}");
            }
        }

        private void BtnSaveMission_Click(object sender, RoutedEventArgs e)
        {
            string name = string.IsNullOrWhiteSpace(txtMissionName.Text) ? "mission" : txtMissionName.Text.Trim();
            string safeName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(_missionsFolder, safeName + ".json");

            _activeMission.Name = name;
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(_activeMission, options));
            PopulateSavedMissions();
            txtMapStatus.Text = _language == "TR" ? "Görev kaydedildi" : "Mission saved";
        }

        private void BtnLoadMission_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSavedMissions.SelectedItem is not string fileName) return;
            string path = Path.Combine(_missionsFolder, fileName);
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                _activeMission = JsonSerializer.Deserialize<MissionPlan>(json) ?? new MissionPlan();
                UpdateMissionListDisplay();
                SyncMapWaypoints();
                txtMapStatus.Text = _language == "TR" ? "Görev yüklendi" : "Mission loaded";
            }
            catch (Exception ex)
            {
                txtMapStatus.Text = $"Load failed: {ex.Message}";
            }
        }

        private void PopulateSavedMissions()
        {
            if (!Directory.Exists(_missionsFolder)) return;
            var files = Directory.GetFiles(_missionsFolder, "*.json").Select(Path.GetFileName).OrderBy(f => f).ToList();
            cmbSavedMissions.ItemsSource = files;
            if (files.Any()) cmbSavedMissions.SelectedIndex = 0;
        }

        private void BtnClearMission_Click(object sender, RoutedEventArgs e)
        {
            _activeMission = new MissionPlan { Name = "New Mission" };
            UpdateMissionListDisplay();
            SyncMapWaypoints();
            InitializeMissionPlannerFields();
        }

        private void UpdateMissionStatusText()
        {
            if (!_activeMission.Waypoints.Any())
            {
                txtNextAction.Text = _language == "TR" ? "Görev bekliyor" : "Awaiting mission";
                return;
            }

            if (!_missionRunning)
            {
                txtNextAction.Text = _language == "TR" ? "Hazır" : "Ready";
                return;
            }

            int nextIndex = Math.Min(_currentWaypointIndex, _activeMission.Waypoints.Count - 1);
            txtNextAction.Text = _language == "TR"
                ? $"Sonraki: WP {nextIndex + 1}"
                : $"Next: WP {nextIndex + 1}";
        }

        private void BtnReloadApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty,
                    UseShellExecute = true
                });
            }
            catch
            {
            }

            Application.Current.Shutdown();
        }

        public void AddWaypointFromMap(double latitude, double longitude)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    txtLat.Text = latitude.ToString(CultureInfo.InvariantCulture);
                    txtLon.Text = longitude.ToString(CultureInfo.InvariantCulture);

                    _activeMission ??= new MissionPlan { Name = "New Mission" };

                    if (!double.TryParse(txtAlt.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double altitude))
                    {
                        altitude = 20;
                        txtAlt.Text = altitude.ToString(CultureInfo.InvariantCulture);
                    }

                    if (!double.TryParse(txtHeading.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double heading))
                    {
                        heading = 0;
                        txtHeading.Text = heading.ToString(CultureInfo.InvariantCulture);
                    }

                    var waypoint = new MissionWaypoint
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        Altitude = altitude,
                        Heading = heading
                    };

                    _activeMission.Waypoints.Add(waypoint);
                    UpdateMissionListDisplay();
                    SyncMapWaypoints();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txtMapStatus.Text = _language == "TR"
                        ? $"Harita tıklaması işlenemedi: {ex.Message}"
                        : $"Map click failed: {ex.Message}";
                });
            }
        }

        private void EnsureWaypointVisualLayer()
        {
            if (!viewPort.Children.Contains(_waypointsGroup))
            {
                viewPort.Children.Add(_waypointsGroup);
            }
        }

        private void RenderWaypointModels()
        {
            EnsureWaypointVisualLayer();
            _waypointsGroup.Children.Clear();

            foreach (var wp in _activeMission.Waypoints)
            {
                var local = LatLonToLocal(wp.Latitude, wp.Longitude);
                var sphere = new SphereVisual3D
                {
                    Center = new Point3D(local.X, local.Y, wp.Altitude),
                    Radius = 1.5,
                    Material = MaterialHelper.CreateMaterial(Colors.Red)
                };

                _waypointsGroup.Children.Add(sphere);
            }
        }

        private void RenderWaypointOverlays()
        {
            MissionMapOverlay.Children.Clear();

            if (MissionMapImage.Source == null || MissionMapImage.ActualWidth <= 0 || MissionMapImage.ActualHeight <= 0) return;

            var polyline = new System.Windows.Shapes.Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0, 188, 212)),
                StrokeThickness = 2
            };

            foreach (var wp in _activeMission.Waypoints)
            {
                var (x, y) = LatLonToPixel(wp.Latitude, wp.Longitude, MissionMapImage.ActualWidth, MissionMapImage.ActualHeight);

                var marker = new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(marker, x - 5);
                Canvas.SetTop(marker, y - 5);
                MissionMapOverlay.Children.Add(marker);

                polyline.Points.Add(new System.Windows.Point(x, y));
            }

            if (polyline.Points.Count > 1)
            {
                MissionMapOverlay.Children.Insert(0, polyline);
            }

            txtMapStatus.Text = _language == "TR" ? $"{_activeMission.Waypoints.Count} nokta" : $"{_activeMission.Waypoints.Count} waypoints";
        }

        private (double lat, double lon) PixelToLatLon(double x, double y, double width, double height)
        {
            double lon = _mapBounds.swLon + (_mapBounds.neLon - _mapBounds.swLon) * (x / width);
            double lat = _mapBounds.neLat - (_mapBounds.neLat - _mapBounds.swLat) * (y / height);
            return (lat, lon);
        }

        private (double x, double y) LatLonToPixel(double lat, double lon, double width, double height)
        {
            double x = (lon - _mapBounds.swLon) / (_mapBounds.neLon - _mapBounds.swLon) * width;
            double y = (_mapBounds.neLat - lat) / (_mapBounds.neLat - _mapBounds.swLat) * height;
            return (x, y);
        }

        private void MapHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_mapReady || MissionMapImage.ActualWidth <= 0 || MissionMapImage.ActualHeight <= 0) return;

            var pos = e.GetPosition(MissionMapImage);
            var (lat, lon) = PixelToLatLon(pos.X, pos.Y, MissionMapImage.ActualWidth, MissionMapImage.ActualHeight);
            AddWaypointFromMap(lat, lon);
        }
    }
}
