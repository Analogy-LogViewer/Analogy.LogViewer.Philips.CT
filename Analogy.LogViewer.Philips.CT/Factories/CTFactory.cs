using Analogy.Interfaces;
using Analogy.LogViewer.Philips.CT.DataSources;
using Analogy.LogViewer.Philips.CT.Properties;
using Analogy.LogViewer.Template;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Analogy.LogViewer.Philips.CT.Factories
{
    public class CTFactory : PrimaryFactory
    {
        internal static Guid Id { get; set; } = new Guid("AC39F3B0-C9DB-4E6B-B1F6-87C1432DD372");
        public override Guid FactoryId { get; set; } = Id;
        public override string Title { get; set; } = "Philips CT BU Logs";
        public override IEnumerable<IAnalogyChangeLog> ChangeLog { get; set; }
        public override string About { get; set; } = "Philips CT BU Logs";
        public override Image? SmallImage { get; set; } = Resources.Database_16x16;
        public override Image? LargeImage { get; set; } = Resources.Database_32x32;

        public CTFactory()
        {
            ChangeLog = new List<IAnalogyChangeLog>();
        }


    }

    public sealed class CTDataSourcesFactory : DataProvidersFactory
    {
        public override Guid FactoryId { get; set; } = CTFactory.Id;
        public override string Title { get; set; } = "Philips CT BU Data Sources";
        public override IEnumerable<IAnalogyDataProvider> DataProviders { get; set; }
      
        public CTDataSourcesFactory()
        {
            var dataSources = new List<IAnalogyDataProvider>();
            dataSources.Add(new OnlineCTLogLogReader());
            dataSources.Add(new OfflineCTLogs());
            DataProviders = dataSources;
        }


    }
}
