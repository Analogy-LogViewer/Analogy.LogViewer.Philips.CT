using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Analogy.LogViewer.Philips.CT.Properties;

namespace Analogy.LogViewer.Philips.CT.CustomActions
{
    //public class LogConfiguratorAction: IAnalogCustomAction
    //{
    //    public Action Action => OpenLogConfigurator;
    //    public Guid ID { get; }= new Guid("6808072B-8186-4BFC-9061-4FEB8E9BE472");
    //    public Image Image { get; } = Resources.PageSetup_32x32;
    //    public string Title { get; } = "External Log Configurator";
    //    private string logConfiguratorEXE = "LogConfigurator.exe";

    //    private void OpenLogConfigurator()
    //    {
    //        if (File.Exists(logConfiguratorEXE))
    //        {
    //            try
    //            {
    //                Process.Start(logConfiguratorEXE);
    //            }
    //            catch (Exception exception)
    //            {
    //                MessageBox.Show(exception.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
    //            }
    //        }
    //        //LogConfiguratorForm log = new LogConfiguratorForm();
    //        //log.Show(this);
    //    }
    //}

    //public class LogConfiguratorInternalAction : IAnalogCustomAction
    //{
    //    public Action Action => OpenInternalLogConfigurator;
    //    public Guid ID { get; } = new Guid("40D381DB-407A-409B-920E-73C1A2E4A798");
    //    public Image Image { get; } = Resources.PageSetup_32x32;
    //    public string Title { get; } = "Internal Log Configurator";

    //    private void OpenInternalLogConfigurator()
    //    {
    //        LogConfiguratorForm log = new LogConfiguratorForm();
    //        log.Show();
    //    }
    //}

    //public class FixCorruptedFilelAction : IAnalogCustomAction
    //{
    //    public Action Action => InternalAction;
    //    public Guid ID { get; } = new Guid("874C40CC-5FFA-4C8F-B494-2EAFB8EDC112");
    //    public Image Image { get; } = Resources.PageSetup_32x32;
    //    public string Title { get; } = "Fix Corrupted XML File";

    //    private void InternalAction()
    //    {
    //        FixFileForm f = new FixFileForm();
    //        f.Show();

    //    }
    //}


}
