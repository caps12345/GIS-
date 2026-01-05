using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using WeatherApp_FGH.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Symbology;

namespace WeatherApp_FGH
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // === F1: 城市列表 ===
        public ObservableCollection<string> CityNames { get; set; }

        // === G2: 图表数据 ===
        public SeriesCollection TemperatureSeries { get; set; } // 折线图 (高/低)
        public SeriesCollection AvgTempSeries { get; set; }     // 柱状图 (平均)

        private List<string> _dateLabels;
        public List<string> DateLabels
        {
            get => _dateLabels;
            set { _dateLabels = value; OnPropertyChanged(); }
        }

        public Func<double, string> YFormatter { get; set; }

        // === H1: 地图对象 ===
        private Map _map;
        public Map MyMap
        {
            get => _map;
            set { _map = value; OnPropertyChanged(); }
        }

        private string _selectedCity;
        public string SelectedCity
        {
            get => _selectedCity;
            set
            {
                _selectedCity = value;
                OnPropertyChanged();
                if (!string.IsNullOrEmpty(_selectedCity)) UpdateChartData(_selectedCity);
            }
        }

        public MainViewModel()
        {
            CityNames = new ObservableCollection<string>();
            TemperatureSeries = new SeriesCollection();
            AvgTempSeries = new SeriesCollection();
            DateLabels = new List<string>();
            YFormatter = val => val + "℃";

            MyMap = new Map(SpatialReferences.WebMercator);
            LoadTiandituBasemap();
            MyMap.InitialViewpoint = new Viewpoint(32.5, 120.0, 3000000);

            LoadF1Data();
            _ = InitializeH1DataAsync();
        }

        private void LoadTiandituBasemap()
        {
            string token = "96cd361c8473c7c2d2c96bd05c598a2c";
            var subDomains = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7" };
            string vecUrl = @"http://t{subDomain}.tianditu.gov.cn/vec_w/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=vec&STYLE=default&TILEMATRIXSET=w&FORMAT=tiles&TILEMATRIX={level}&TILEROW={row}&TILECOL={col}&tk=" + token;
            WebTiledLayer baseLayer = new WebTiledLayer(vecUrl, subDomains) { Name = "天地图底图" };
            string cvaUrl = @"http://t{subDomain}.tianditu.gov.cn/cva_w/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=cva&STYLE=default&TILEMATRIXSET=w&FORMAT=tiles&TILEMATRIX={level}&TILEROW={row}&TILECOL={col}&tk=" + token;
            WebTiledLayer labelLayer = new WebTiledLayer(cvaUrl, subDomains) { Name = "天地图注记" };
            Basemap tdtBasemap = new Basemap();
            tdtBasemap.BaseLayers.Add(baseLayer);
            tdtBasemap.BaseLayers.Add(labelLayer);
            MyMap.Basemap = tdtBasemap;
        }

        private void LoadF1Data()
        {
            using (var context = new JiangsuWeatherContext())
            {
                context.Database.EnsureCreated();
                var cities = context.WeatherStations.Select(s => s.City).Distinct().ToList();
                foreach (var city in cities) if (!string.IsNullOrEmpty(city)) CityNames.Add(city);
            }
        }

        // === G2: 更新图表 (修正：直接从数据库读取 AvgTemperature) ===
        private void UpdateChartData(string cityName)
        {
            using (var context = new JiangsuWeatherContext())
            {
                var station = context.WeatherStations.FirstOrDefault(s => s.City == cityName);
                if (station == null) return;

                var dataList = context.WeatherData
                                      .Where(d => d.StationCode == station.StationCode)
                                      .OrderByDescending(d => d.RecordDate)
                                      .Take(30).ToList();
                dataList.Reverse();

                var maxValues = new ChartValues<double>();
                var minValues = new ChartValues<double>();
                var avgValues = new ChartValues<double>();
                var dates = new List<string>();

                foreach (var item in dataList)
                {
                    // 1. 读取最高/最低温
                    maxValues.Add((double)(item.MaxTemperature ?? 0));
                    minValues.Add((double)(item.MinTemperature ?? 0));

                    // 2. 【核心修改】直接读取数据库中的 AvgTemperature 字段
                    // 数据库字段通常映射为 decimal? 类型，需要强转 double
                    avgValues.Add((double)(item.AvgTemperature ?? 0));

                    // 3. 真实日期
                    dates.Add(item.RecordDate.ToString("yyyy-MM-dd"));
                }

                // 更新折线图
                TemperatureSeries.Clear();
                TemperatureSeries.Add(new LineSeries
                {
                    Title = "最高气温",
                    Values = maxValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    LineSmoothness = 0,
                    Stroke = Brushes.Red,
                    Fill = Brushes.Transparent,
                    DataLabels = false
                });
                TemperatureSeries.Add(new LineSeries
                {
                    Title = "最低气温",
                    Values = minValues,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    LineSmoothness = 0,
                    Stroke = Brushes.DodgerBlue,
                    Fill = Brushes.Transparent,
                    DataLabels = false
                });

                // 更新柱状图
                AvgTempSeries.Clear();
                AvgTempSeries.Add(new ColumnSeries
                {
                    Title = "平均气温",
                    Values = avgValues,
                    DataLabels = true,         // 静态显示数值
                    FontSize = 10,
                    Fill = Brushes.Orange,
                    MaxColumnWidth = 15
                });

                DateLabels = dates;
            }
        }

        // ... (H1/H2 初始化与查询逻辑保持不变) ...
        private async Task InitializeH1DataAsync()
        {
            try
            {
                string gdbPath = Path.Combine(Environment.CurrentDirectory, "weather_stations.geodatabase");
                if (File.Exists(gdbPath)) File.Delete(gdbPath);
                Geodatabase gdb = await Geodatabase.CreateAsync(gdbPath);
                var tableDesc = new TableDescription("weatherStationPoints", SpatialReferences.Wgs84, GeometryType.Point);
                tableDesc.FieldDescriptions.Add(new FieldDescription("Name", FieldType.Text));
                tableDesc.FieldDescriptions.Add(new FieldDescription("Longitude", FieldType.Float64));
                tableDesc.FieldDescriptions.Add(new FieldDescription("Latitude", FieldType.Float64));
                var table = await gdb.CreateTableAsync(tableDesc);
                using (var context = new JiangsuWeatherContext())
                {
                    var distinctStations = context.WeatherStations.ToList().GroupBy(s => s.City).Select(g => g.First()).ToList();
                    var list = new List<Feature>();
                    foreach (var s in distinctStations)
                    {
                        if (s.Longitude != null && s.Latitude != null)
                        {
                            double lon = (double)s.Longitude;
                            double lat = (double)s.Latitude;
                            var pt = new MapPoint(lon, lat, SpatialReferences.Wgs84);
                            var attr = new Dictionary<string, object> { { "Name", s.City }, { "Longitude", lon }, { "Latitude", lat } };
                            list.Add(table.CreateFeature(attr, pt));
                        }
                    }
                    await table.AddFeaturesAsync(list);
                }
                var featureTable = gdb.GeodatabaseFeatureTables.FirstOrDefault(t => t.TableName == "weatherStationPoints");
                if (featureTable != null)
                {
                    var layer = new FeatureLayer(featureTable);
                    ApplyCityRenderer(layer);
                    MyMap.OperationalLayers.Add(layer);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void ApplyCityRenderer(FeatureLayer layer)
        {
            var renderer = new UniqueValueRenderer(); renderer.FieldNames.Add("Name");
            var colors = new List<System.Drawing.Color> { System.Drawing.Color.Red, System.Drawing.Color.Blue, System.Drawing.Color.Green, System.Drawing.Color.Orange, System.Drawing.Color.Purple, System.Drawing.Color.Brown, System.Drawing.Color.Magenta, System.Drawing.Color.Teal, System.Drawing.Color.Crimson, System.Drawing.Color.DarkBlue, System.Drawing.Color.Olive, System.Drawing.Color.DeepPink, System.Drawing.Color.Indigo };
            int index = 0;
            foreach (var city in CityNames)
            {
                var color = colors[index % colors.Count];
                var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, color, 14);
                renderer.UniqueValues.Add(new UniqueValue(city, city, symbol, new List<object> { city }));
                index++;
            }
            layer.Renderer = renderer;
        }

        public List<string> QueryCitiesByTemp(double threshold)
        {
            var resultCities = new List<string>();
            using (var context = new JiangsuWeatherContext())
            {
                var stations = context.WeatherStations.ToList();
                foreach (var s in stations)
                {
                    // 这里查询逻辑也可以改用 AvgTemperature 字段，看你需求，目前保持原逻辑(算一次平均)也没问题
                    // 或者改为: if (weather != null && weather.AvgTemperature > threshold)
                    var weather = context.WeatherData.Where(w => w.StationCode == s.StationCode).OrderByDescending(w => w.RecordDate).FirstOrDefault();
                    if (weather != null)
                    {
                        double avgTemp = (double)(weather.AvgTemperature ?? 0); // 使用数据库字段
                        if (avgTemp > threshold) resultCities.Add(s.City);
                    }
                }
            }
            return resultCities.Distinct().ToList();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}