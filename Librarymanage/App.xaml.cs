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

            // Create updater
            _sparkle = new SparkleUpdater(
                "https://harolddjeumen.github.io/Library_Management/Librarymanage/appcast.xml",   // your appcast URL
                new Ed25519Checker(SecurityMode.Unsafe, null) // skip signing for now
            );

            // Set WPF UI factory
            _sparkle.UIFactory = new UIFactory();


            // Start checking for updates
            Task.Run(async () => await _sparkle.StartLoop(true, true));
        }
    }
}


