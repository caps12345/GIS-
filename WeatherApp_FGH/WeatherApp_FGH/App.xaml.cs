using System.Windows;
using Esri.ArcGISRuntime; // 必须引用
using System.Windows;
using Esri.ArcGISRuntime;

namespace WeatherApp_FGH
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // 初始化 ArcGIS (即使没 Key，天地图也能用)
                ArcGISRuntimeEnvironment.Initialize();
            }
            catch
            {
                // 忽略错误
            }
        }
    }
}