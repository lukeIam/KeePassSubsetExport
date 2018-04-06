using System.Drawing;
using KeePass.Forms;
using KeePass.Plugins;

namespace KeePassSubsetExport
{
    public class KeePassSubsetExportExt : Plugin
    {
        private IPluginHost _host = null;

        public override Image SmallIcon
        {
            get { return Properties.Resources.Key; }
        }

        public override string UpdateUrl
        {
            get { return "https://github.com/lukeIam/KeePassSubsetExport/raw/master/keepass.version"; }
        }

        public override bool Initialize(IPluginHost host)
        {
            _host = host;

            _host.MainWindow.FileSaved += StartExport;

            return true;
        }

        private void StartExport(object sender, FileSavedEventArgs args)
        {
            Exporter.Export(args.Database);
        }

        public override void Terminate()
        {
            if (_host != null)
            {
                _host.MainWindow.FileSaved -= StartExport;
            }
        }
    }
}