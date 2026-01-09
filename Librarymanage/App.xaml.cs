using System.Configuration;
using System.Data;
using System.Windows;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.WPF;

using System.Threading.Tasks;

namespace Librarymanage
{
    public partial class App : Application
    {
        private SparkleUpdater _sparkle;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

           //  Create updater
            _sparkle = new SparkleUpdater(
                  "https://github.com/harolddjeumen/Library_Management/releases/download/v1.0.0/appcast.xml",
                  new Ed25519Checker(SecurityMode.Unsafe, null)
            );


          //  Set WPF UI factory
            _sparkle.UIFactory = new UIFactory();


           //  Start checking for updates
           Task.Run(async () => await _sparkle.StartLoop(true, true));
        }
    }
}


