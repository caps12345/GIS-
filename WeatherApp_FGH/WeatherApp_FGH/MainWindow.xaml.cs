using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
// 移除 Esri.ArcGISRuntime.Geometry 和 Mapping 的全局引用，改用全名，避免冲突
// using Esri.ArcGISRuntime.Geometry; 
// using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // 需要这个来设置光标 (Cursors)
using System.Windows.Media; // 需要这个来设置按钮颜色 (Brushes)
using WeatherApp_FGH.Models;

namespace WeatherApp_FGH
{
    public partial class MainWindow : Window
    {
        // 【修复1】使用全名指定 Geometry，避免与 System.Windows.Media.Geometry 冲突
        private Esri.ArcGISRuntime.Geometry.Geometry _userGeometry;

        private GraphicsOverlay _drawingOverlay;
        private GraphicsOverlay _analysisOverlay;

        // UI 相关的颜色画笔
        private Brush _activeBrush = Brushes.LightGreen;
        private Brush _defaultBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221));

        public MainWindow()
        {
            InitializeComponent();

            // 初始化 SketchEditor
            MyMapView.SketchEditor = new SketchEditor();

            _drawingOverlay = new GraphicsOverlay();
            _analysisOverlay = new GraphicsOverlay();
            MyMapView.GraphicsOverlays.Add(_drawingOverlay);
            MyMapView.GraphicsOverlays.Add(_analysisOverlay);
        }

        // === H1: 地图点击 ===
        private async void MyMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (MyMapView.SketchEditor.Geometry != null) return;
            MyMapView.DismissCallout();
            try
            {
                var result = await MyMapView.IdentifyLayersAsync(e.Position, 10, false);
                if (result.Count > 0)
                {
                    var element = result.FirstOrDefault()?.GeoElements.FirstOrDefault();
                    if (element != null)
                    {
                        var attrs = element.Attributes;
                        string name = attrs.ContainsKey("Name") ? attrs["Name"]?.ToString() : "";
                        if (this.DataContext is MainViewModel vm && !string.IsNullOrEmpty(name)) vm.SelectedCity = name;

                        double lon = Convert.ToDouble(attrs["Longitude"] ?? 0);
                        double lat = Convert.ToDouble(attrs["Latitude"] ?? 0);

                        // Callout 定义
                        var def = new CalloutDefinition(name, $"城市: {name}\n经度: {lon:F4}\n纬度: {lat:F4}");
                        MyMapView.ShowCalloutAt(e.Location, def);
                    }
                }
            }
            catch { }
        }

        private void CityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CityListBox.SelectedItem is string selectedCity && !string.IsNullOrEmpty(selectedCity))
            {
                using (var context = new JiangsuWeatherContext())
                {
                    var station = context.WeatherStations.FirstOrDefault(s => s.City == selectedCity);
                    if (station?.Longitude != null && station?.Latitude != null)
                    {
                        // 【修复1】全名指定 MapPoint
                        var pt = new Esri.ArcGISRuntime.Geometry.MapPoint((double)station.Longitude, (double)station.Latitude, Esri.ArcGISRuntime.Geometry.SpatialReferences.Wgs84);
                        MyMapView.SetViewpointCenterAsync(pt, 500000);
                    }
                }
            }
        }

        // === H2: 属性查询 ===
        private async void AttributeQuery_Click(object sender, RoutedEventArgs e)
        {
            // 清除选中
            var featureLayer = MyMapView.Map.OperationalLayers.FirstOrDefault() as FeatureLayer;
            featureLayer?.ClearSelection();

            if (double.TryParse(TempInput.Text, out double tempThreshold))
            {
                var vm = this.DataContext as MainViewModel;
                if (vm == null) return;
                List<string> validCities = vm.QueryCitiesByTemp(tempThreshold);
                if (validCities.Count == 0) { MessageBox.Show("未找到符合条件的城市"); return; }

                if (featureLayer != null)
                {
                    var queryParams = new QueryParameters();
                    queryParams.WhereClause = $"Name IN ('{string.Join("','", validCities)}')";

                    // 【修复2】使用全名指定 SelectionMode
                    await featureLayer.SelectFeaturesAsync(queryParams, Esri.ArcGISRuntime.Mapping.SelectionMode.New);

                    MessageBox.Show($"找到 {validCities.Count} 个城市：\n{string.Join(", ", validCities)}");
                }
            }
        }

        // === UI 辅助方法 ===
        private void SetButtonActive(Button activeBtn)
        {
            BtnDrawPoint.Background = _defaultBrush;
            BtnDrawLine.Background = _defaultBrush;
            BtnDrawPolygon.Background = _defaultBrush;

            if (activeBtn != null) activeBtn.Background = _activeBrush;
            MyMapView.Cursor = Cursors.Cross;
        }

        private void ResetDrawingUI()
        {
            BtnDrawPoint.Background = _defaultBrush;
            BtnDrawLine.Background = _defaultBrush;
            BtnDrawPolygon.Background = _defaultBrush;
            MyMapView.Cursor = Cursors.Arrow;
        }

        private void PrepareDrawing()
        {
            _drawingOverlay.Graphics.Clear();
            _analysisOverlay.Graphics.Clear();
            var featureLayer = MyMapView.Map.OperationalLayers.FirstOrDefault() as FeatureLayer;
            featureLayer?.ClearSelection();
        }

        // === 绘图逻辑 ===
        private async void DrawPoint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetButtonActive(BtnDrawPoint);
                PrepareDrawing();
                // 【修复1】Geometry 类型明确
                _userGeometry = await MyMapView.SketchEditor.StartAsync(SketchCreationMode.Point, true);

                var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 10);
                _drawingOverlay.Graphics.Add(new Graphic(_userGeometry, symbol)); // 这里不会再报错了

                BtnBuffer.IsEnabled = true;
            }
            catch (TaskCanceledException) { }
            finally { ResetDrawingUI(); }
        }

        private async void DrawLine_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetButtonActive(BtnDrawLine);
                PrepareDrawing();
                _userGeometry = await MyMapView.SketchEditor.StartAsync(SketchCreationMode.Polyline, true);

                var symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 3);
                _drawingOverlay.Graphics.Add(new Graphic(_userGeometry, symbol));

                BtnBuffer.IsEnabled = true;
            }
            catch (TaskCanceledException) { }
            finally { ResetDrawingUI(); }
        }

        private async void DrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetButtonActive(BtnDrawPolygon);
                PrepareDrawing();
                _userGeometry = await MyMapView.SketchEditor.StartAsync(SketchCreationMode.Polygon, true);

                var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Red, 2);
                var fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(50, 255, 0, 0), lineSymbol);
                _drawingOverlay.Graphics.Add(new Graphic(_userGeometry, fillSymbol));

                BtnBuffer.IsEnabled = true;
            }
            catch (TaskCanceledException) { }
            finally { ResetDrawingUI(); }
        }

        private void CompleteDraw_Click(object sender, RoutedEventArgs e)
        {
            if (MyMapView.SketchEditor.IsEnabled && MyMapView.SketchEditor.CompleteCommand.CanExecute(null))
            {
                MyMapView.SketchEditor.CompleteCommand.Execute(null);
            }
        }

        // === 分析逻辑 ===
        private async void BufferAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (_userGeometry == null) return;

            var featureLayer = MyMapView.Map.OperationalLayers.FirstOrDefault() as FeatureLayer;
            featureLayer?.ClearSelection();
            _analysisOverlay.Graphics.Clear();

            if (double.TryParse(BufferInput.Text, out double distKm))
            {
                // 【修复1】全名指定 Geometry
                Esri.ArcGISRuntime.Geometry.Geometry searchGeometry = _userGeometry;
                if (distKm > 0)
                {
                    // 【修复1】全名调用 Buffer
                    searchGeometry = Esri.ArcGISRuntime.Geometry.GeometryEngine.Buffer(_userGeometry, distKm * 1000);
                }

                var bufferSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.FromArgb(100, 255, 255, 0),
                                   new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Orange, 2));
                _analysisOverlay.Graphics.Add(new Graphic(searchGeometry, bufferSymbol));

                if (featureLayer != null)
                {
                    var layerSR = featureLayer.FeatureTable.SpatialReference;
                    // 【修复1】全名调用 Project
                    var projectedGeometry = Esri.ArcGISRuntime.Geometry.GeometryEngine.Project(searchGeometry, layerSR);
                    var queryParams = new QueryParameters();
                    queryParams.Geometry = projectedGeometry;
                    queryParams.SpatialRelationship = SpatialRelationship.Intersects;

                    try
                    {
                        // 【修复2】全名指定 SelectionMode
                        var result = await featureLayer.SelectFeaturesAsync(queryParams, Esri.ArcGISRuntime.Mapping.SelectionMode.New);
                        int count = result.Count();
                        var names = result.Select(f => f.Attributes["Name"]?.ToString() ?? "未知");

                        if (count > 0) MessageBox.Show($"范围内共有 {count} 个站点：\n{string.Join(", ", names)}");
                        else MessageBox.Show("所选范围内没有气象站点。");
                    }
                    catch (Exception ex) { MessageBox.Show("查询出错: " + ex.Message); }
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var featureLayer = MyMapView.Map.OperationalLayers.FirstOrDefault() as FeatureLayer;
            featureLayer?.ClearSelection();

            _drawingOverlay.Graphics.Clear();
            _analysisOverlay.Graphics.Clear();
            _userGeometry = null;

            if (MyMapView.SketchEditor.IsEnabled) MyMapView.SketchEditor.Stop();
            BtnBuffer.IsEnabled = false;

            ResetDrawingUI();
        }
    }
}