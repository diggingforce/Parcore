using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;

namespace Parcore
{
    public partial class MainWindow : Window
    {
        private readonly Computer _computer;
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            _computer = new Computer();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                UpdateStats();
            };

            System.Threading.Tasks.Task.Run(() =>
            {
                _computer.Open();

                void UpdateLoading(string text)
                {
                    Dispatcher.Invoke(() => LoadingText.Text = text);
                }

                UpdateLoading("Loading CPU...");
                _computer.IsCpuEnabled = true;

                UpdateLoading("Loading Memory...");
                _computer.IsMemoryEnabled = true;

                UpdateLoading("Loading Storage...");
                _computer.IsStorageEnabled = true;

                UpdateLoading("Loading Network...");
                _computer.IsNetworkEnabled = true;

                UpdateLoading("Loading GPU...");
                _computer.IsGpuEnabled = true;

                UpdateLoading("Loading Motherboard...");
                _computer.IsMotherboardEnabled = true;
                _computer.IsControllerEnabled = true;

                Dispatcher.Invoke(() =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MainContent.Visibility = Visibility.Visible;
                    _timer.Start();
                    UpdateStats();
                });
            });
        }

        private void UpdateStats()
        {
            float cpuLoad = 0;
            string cpuName = "--";
            var cpuThreadLoads = new List<(string Name, float Value)>();
            float? cpuTdie = null, cpuTctl = null, cpuTpackage = null;
            var cpuCoreTempList = new List<(string Name, float Value)>();

            float ramUsedGb = 0, ramAvailGb = 0, ramUsedPercent = 0, virtualUsedPercent = 0;

            float gpuLoad = 0;
            string gpuName = "--";
            float gpuCoreTemp = 0;
            var gpuEngines = new List<(string Name, float Value)>();
            var gpuTempList = new List<(string Name, float Value)>();

            var driveStats = new List<(string Name, float Read, float Write)>();
            var fanList = new List<(string Name, float Value)>();

            float netDownload = 0, netUpload = 0;
            string netName = "Network";

            foreach (var hw in _computer.Hardware)
            {
                hw.Update();

                switch (hw.HardwareType)
                {
                    case HardwareType.Cpu:
                        cpuName = hw.Name;
                        foreach (var s in hw.Sensors)
                        {
                            if (!s.Value.HasValue) continue;
                            float v = s.Value.Value;

                            if (s.SensorType == SensorType.Load)
                            {
                                if (s.Name == "CPU Total" || s.Name == "CPU Package")
                                    cpuLoad = v;
                                else if (s.Name.Contains("Core") && !s.Name.Contains("Max") && !s.Name.Contains("Average"))
                                    cpuThreadLoads.Add((CleanCoreName(s.Name), v));
                            }
                            else if (s.SensorType == SensorType.Temperature)
                            {
                                if (s.Name.Contains("Tdie") && !s.Name.Contains("CCD"))
                                    cpuTdie = v;
                                else if (s.Name.Contains("Tctl") && !s.Name.Contains("CCD"))
                                    cpuTctl = v;
                                else if (s.Name.Contains("Package") || s.Name == "CPU Package")
                                    cpuTpackage = v;
                                else if (s.Name.StartsWith("CCD"))
                                    cpuCoreTempList.Add((s.Name, v));
                                else if (s.Name.Contains("Core") && !s.Name.Contains("Max") && !s.Name.Contains("Average") && !s.Name.Contains("Distance"))
                                    cpuCoreTempList.Add((CleanCoreName(s.Name), v));
                            }
                        }
                        foreach (var sub in hw.SubHardware)
                        {
                            sub.Update();
                            foreach (var s in sub.Sensors)
                                if (s.SensorType == SensorType.Fan && s.Value.HasValue)
                                    fanList.Add((s.Name, s.Value.Value));
                        }
                        break;

                    case HardwareType.Memory:
                        foreach (var s in hw.Sensors)
                        {
                            if (!s.Value.HasValue) continue;
                            float v = s.Value.Value;

                            if (s.SensorType == SensorType.Load)
                            {
                                if (s.Name == "Memory") ramUsedPercent = v;
                                else if (s.Name.Contains("Virtual")) virtualUsedPercent = v;
                            }
                            else if (s.SensorType == SensorType.Data)
                            {
                                if (s.Name == "Memory Used") ramUsedGb = v;
                                else if (s.Name == "Memory Available") ramAvailGb = v;
                                else if (s.Name.Contains("Virtual") && s.Name.Contains("Used"))
                                    virtualUsedPercent = v;
                            }
                        }
                        break;

                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        gpuName = hw.Name;
                        foreach (var s in hw.Sensors)
                        {
                            if (!s.Value.HasValue) continue;
                            float v = s.Value.Value;

                            if (s.SensorType == SensorType.Load)
                            {
                                if (s.Name == "GPU Core" || s.Name == "3D")
                                    gpuLoad = v;
                                else
                                    gpuEngines.Add((s.Name.Replace("GPU ", ""), v));
                            }
                            else if (s.SensorType == SensorType.Temperature)
                            {
                                if (s.Name == "GPU Core" || s.Name == "GPU" || s.Name == "Core")
                                    gpuCoreTemp = v;
                                gpuTempList.Add((s.Name.Replace("GPU ", ""), v));
                            }
                        }
                        break;

                    case HardwareType.Storage:
                        float stRead = 0, stWrite = 0;
                        foreach (var s in hw.Sensors)
                        {
                            if (!s.Value.HasValue) continue;
                            if (s.SensorType == SensorType.Throughput)
                            {
                                if (s.Name.Contains("Read")) stRead = s.Value.Value / 1024f / 1024f;
                                else if (s.Name.Contains("Write")) stWrite = s.Value.Value / 1024f / 1024f;
                            }
                        }
                        driveStats.Add((hw.Name, stRead, stWrite));
                        break;

                    case HardwareType.Network:
                        if (IsVirtualNetAdapter(hw.Name)) break;

                        float hwDown = 0, hwUp = 0;
                        foreach (var s in hw.Sensors)
                        {
                            if (!s.Value.HasValue) continue;
                            if (s.SensorType == SensorType.Throughput)
                            {
                                if (s.Name == "Download Speed") hwDown = s.Value.Value / 1024f;
                                else if (s.Name == "Upload Speed") hwUp = s.Value.Value / 1024f;
                            }
                        }
                        if (hwDown + hwUp > netDownload + netUpload)
                        {
                            netDownload = hwDown;
                            netUpload = hwUp;
                            netName = hw.Name;
                        }
                        break;

                    case HardwareType.Motherboard:
                        foreach (var sub in hw.SubHardware)
                        {
                            sub.Update();
                            foreach (var s in sub.Sensors)
                                if (s.SensorType == SensorType.Fan && s.Value.HasValue)
                                    fanList.Add((s.Name, s.Value.Value));
                        }
                        break;
                }
            }

            float cpuTempBest = cpuTdie ?? cpuTpackage ?? cpuTctl ?? 0f;
            float ramTotalGb = ramUsedGb + ramAvailGb;

            RenderCpuCard(cpuName, cpuLoad, cpuThreadLoads);
            RenderRamCard(ramUsedPercent, virtualUsedPercent, ramUsedGb, ramTotalGb);
            RenderGpuCard(gpuName, gpuLoad, gpuEngines);
            RenderDrivesCard(driveStats);
            RenderFansCard(fanList);
            RenderNetworkCard(netName, netDownload, netUpload);
            RenderCpuTempCard(cpuCoreTempList, cpuTempBest);
            RenderRamUsageCard(ramUsedPercent, ramUsedGb, ramTotalGb);
            RenderGpuTempCard(gpuTempList, gpuCoreTemp);
        }

        private static string CleanCoreName(string name)
        {
            name = name.Replace("CPU Core ", "Core ");
            name = name.Replace("CPU Core#", "Core #");
            return name.Trim();
        }

        private static bool IsVirtualNetAdapter(string name)
        {
            return name.Contains("Filter")
                || name.Contains("Scheduler")
                || name.Contains("Driver")
                || name.Contains("Loopback")
                || name.Contains("Pseudo")
                || name.Contains("Miniport");
        }

        private void RenderCpuCard(string name, float load, List<(string Name, float Value)> threads)
        {
            CpuName.Text = name;
            CpuLoadText.Text = $"{load:F0}";
            DrawArc(CpuArcCanvas, load / 100f);

            CpuThreadList.Children.Clear();
            foreach (var (tname, val) in threads)
                CpuThreadList.Children.Add(MakeSensorRow(tname, $"{val:F0}%", val / 100f));
        }

        private void RenderRamCard(float usedPct, float virtualPct, float usedGb, float totalGb)
        {
            RamInfo.Text = $"{usedGb:F1} / {totalGb:F1} GB";
            RamLoadText.Text = $"{usedPct:F0}";
            DrawArc(RamArcCanvas, usedPct / 100f);

            RamList.Children.Clear();
            RamList.Children.Add(MakeSensorRow("Virtual", $"{virtualPct:F0}%", virtualPct / 100f));
        }

        private void RenderGpuCard(string name, float load, List<(string Name, float Value)> engines)
        {
            GpuName.Text = name;
            GpuLoadText.Text = $"{load:F0}";
            DrawArc(GpuArcCanvas, load / 100f);

            GpuEngineList.Children.Clear();
            var seen = new HashSet<string>();
            foreach (var (ename, val) in engines)
            {
                if (!seen.Add(ename)) continue;
                GpuEngineList.Children.Add(MakeSensorRow(ename, $"{val:F0}%", val / 100f));
            }
        }

        private void RenderDrivesCard(List<(string Name, float Read, float Write)> drives)
        {
            DriveList.Children.Clear();
            foreach (var (dname, r, w) in drives)
            {
                string n = Truncate(dname, 28);
                DriveList.Children.Add(MakeSensorRow($"{n}  ↑", $"{r:F1} MB/s", Math.Min(r / 500f, 1f)));
                DriveList.Children.Add(MakeSensorRow($"{n}  ↓", $"{w:F1} MB/s", Math.Min(w / 500f, 1f)));
            }
        }

        private void RenderFansCard(List<(string Name, float Value)> fans)
        {
            FanList.Children.Clear();
            var seen = new HashSet<string>();
            int idx = 1;
            foreach (var (fname, val) in fans)
            {
                if (!seen.Add(fname)) continue;
                FanList.Children.Add(MakeSensorRow($"Fan {idx}", $"{val:F0} RPM", Math.Min(val / 3000f, 1f)));
                idx++;
            }
        }

        private void RenderNetworkCard(string name, float down, float up)
        {
            NetList.Children.Clear();
            string n = Truncate(name, 22);
            NetList.Children.Add(MakeSensorRow($"{n}  ↓", FormatSpeed(down), Math.Min(down / 10240f, 1f)));
            NetList.Children.Add(MakeSensorRow($"{n}  ↑", FormatSpeed(up), Math.Min(up / 10240f, 1f)));
        }

        private void RenderCpuTempCard(List<(string Name, float Value)> cores, float avg)
        {
            CpuTempAvg.Text = $"{avg:F0} °C avg";
            CpuTempList.Children.Clear();
            foreach (var (cname, val) in cores)
                CpuTempList.Children.Add(MakeTempRow($"{cname}  {val:F0} °C", val, 100f));
        }

        private void RenderRamUsageCard(float usedPct, float usedGb, float totalGb)
        {
            RamUsageInfo.Text = $"{usedGb:F1} / {totalGb:F1} GB";
            RamUsageList.Children.Clear();
            RamUsageList.Children.Add(MakeTempRow($"RAM  {usedPct:F0}%", usedPct, 100f));
        }

        private void RenderGpuTempCard(List<(string Name, float Value)> temps, float coreTemp)
        {
            GpuTempAvg.Text = $"{coreTemp:F0} °C avg";
            GpuTempList.Children.Clear();
            var seen = new HashSet<string>();
            foreach (var (tname, val) in temps)
            {
                if (!seen.Add(tname)) continue;
                GpuTempList.Children.Add(MakeTempRow($"{tname}  {val:F0} °C", val, 100f));
            }
        }

        private static UIElement MakeSensorRow(string label, string valueText, float fraction)
        {
            var outer = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = label,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            lbl.SetResourceReference(TextBlock.ForegroundProperty, "SensorRowLabel");

            var val = new TextBlock
            {
                Text = valueText,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            val.SetResourceReference(TextBlock.ForegroundProperty, "SensorRowValue");

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            row.Children.Add(lbl);
            row.Children.Add(val);

            var bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = Math.Clamp(fraction * 100d, 0, 100),
                Height = 2,
                Margin = new Thickness(0, 4, 0, 0),
                BorderThickness = new Thickness(0)
            };
            bar.SetResourceReference(ProgressBar.ForegroundProperty, "SensorRowProgressBarForeground");

            outer.Children.Add(row);
            outer.Children.Add(bar);
            return outer;
        }

        private static UIElement MakeTempRow(string label, float value, float max)
        {
            double pct = Math.Clamp(value / max * 100d, 0, 100);

            double v1 = Math.Min(pct, 33.3);
            double v2 = Math.Max(Math.Min(pct, 66.6) - 33.3, 0);
            double v3 = Math.Max(pct - 66.6, 0);

            var outer = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };

            var txt = new TextBlock
            {
                Text = label,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 4)
            };
            txt.SetResourceReference(TextBlock.ForegroundProperty, "TempRowLabel");
            outer.Children.Add(txt);

            var segGrid = new Grid { Height = 4 };
            segGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(33.3, GridUnitType.Star) });
            segGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(33.3, GridUnitType.Star) });
            segGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(33.4, GridUnitType.Star) });
            segGrid.Children.Add(MakeSegBar(0, 33.3, v1, "TempRowSegBarV1", 0));
            segGrid.Children.Add(MakeSegBar(0, 33.3, v2, "TempRowSegBarV2", 1));
            segGrid.Children.Add(MakeSegBar(0, 33.4, v3, "TempRowSegBarV3", 2));

            outer.Children.Add(segGrid);
            return outer;
        }

        private static ProgressBar MakeSegBar(double min, double max, double val, string resourceKey, int col)
        {
            var pb = new ProgressBar
            {
                Minimum = min,
                Maximum = max,
                Value = val,
                Height = 4,
                BorderThickness = new Thickness(0)
            };
            pb.SetResourceReference(ProgressBar.ForegroundProperty, resourceKey);
            Grid.SetColumn(pb, col);
            return pb;
        }

        private static void DrawArc(Canvas canvas, float fraction)
        {
            canvas.Children.Clear();

            const double size = 110, stroke = 8;
            double r = (size - stroke) / 2;
            double cx = size / 2, cy = size / 2;

            canvas.Children.Add(ArcPath(cx, cy, r, -210, 240, stroke, "ArcBackground"));

            double sweep = 240 * Math.Clamp(fraction, 0f, 1f);
            if (sweep > 0.5)
                canvas.Children.Add(ArcPath(cx, cy, r, -210, sweep, stroke, "ArcValue"));
        }

        private static Path ArcPath(double cx, double cy, double r, double startDeg, double sweepDeg, double thickness, string resourceKey)
        {
            double s = startDeg * Math.PI / 180;
            double e = (startDeg + sweepDeg) * Math.PI / 180;

            var fig = new PathFigure
            {
                StartPoint = new Point(cx + r * Math.Cos(s), cy + r * Math.Sin(s))
            };

            fig.Segments.Add(new ArcSegment
            {
                Point = new Point(cx + r * Math.Cos(e), cy + r * Math.Sin(e)),
                Size = new Size(r, r),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = Math.Abs(sweepDeg) > 180
            });

            var geo = new PathGeometry();
            geo.Figures.Add(fig);

            var path = new Path
            {
                Data = geo,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            path.SetResourceReference(Shape.StrokeProperty, resourceKey);
            return path;
        }

        private static string Truncate(string s, int max) => s.Length > max ? s[..max] : s;

        private static string FormatSpeed(float kb) =>
            kb >= 1024f ? $"{kb / 1024f:F2} MB/s" : $"{kb:F1} KB/s";

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
            else DragMove();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var sw = new SettingsWindow { Owner = this };
            sw.ShowDialog();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void ToggleMaximize() =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
