using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using Analogy.Interfaces;
using Analogy.LogViewer.Philips.CT.Properties;
using Analogy.LogViewer.Template;

namespace Analogy.LogViewer.Philips.CT.DataSources
{
    public class OnlineCTLogLogReader : OnlineDataProvider, IDisposable
    {
        public override Guid Id { get; set; }= new Guid("457ECE83-F59E-4679-9E70-95BFCDBCDA62");
        public override Image? ConnectedLargeImage { get; set; } = Resources.Database_32x32;
        public override Image? ConnectedSmallImage { get; set; } = Resources.Database_16x16;
        public override Image? DisconnectedLargeImage { get; set; } = Resources.DisconnectedDeleteDataSource_32x32;
        public override Image? DisconnectedSmallImage { get; set; } = Resources.DisconnectedDataSource_16x16;
        public override string? OptionalTitle { get; set; } = "Online Data Receiver";

        private bool disposed;

        private LogFetcher logFetcher;
        private DynamicLogFetcher dynamicLogFetcher;

        public OnlineCTLogLogReader()
        {
        }

        public override  Task<bool> CanStartReceiving() => Task.FromResult(true);
        
        public override Task StartReceiving()
        {
            FilterObject filterObject = new FilterObject(true);
            logFetcher = new DatabaseLogFetcher(filterObject);
            // Starting fetching log data
            logFetcher.LogsArrived += logTable =>
            {
                //LogsArrived?.Invoke(logTable);
            };
            logFetcher.LogsUpdated += logTable =>
            {
                //LogsUpdated?.Invoke(logTable);
            };
            logFetcher.FetchCompleted += noOfRecords =>
            {
                //FetchCompleted?.Invoke(noOfRecords);
            };
            logFetcher.FetchStarted += (noOfLogs) => { };
            logFetcher.FetchLogData();

            // Initializing Dynamic update
            dynamicLogFetcher = new DynamicLogFetcher(logFetcher);
            dynamicLogFetcher.UpdateCompleted += UpdateCompletedEventHandler;
            dynamicLogFetcher.Start(2000);
            return Task.CompletedTask;
        }

        public override Task StopReceiving()
        {
            Dispose();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Implements the event handler for the UpdateCompleted event of the DynamicLogFetcher
        /// </summary>
        /// <param name="logTable"></param>
        /// <param name="noOfRowsInView"></param>
        private void UpdateCompletedEventHandler(DataTable logTable, int noOfRowsInView)
        {
            foreach (DataRow dataRow in logTable.Rows)
            {
                AnalogyLogMessage m = new AnalogyLogMessage();
                m.Date = (DateTime)dataRow["Date"];
                m.Text = dataRow["TextMessage"].ToString();
                m.FileName = dataRow["File Name"].ToString();
                m.Category = "";
                m.Class = (AnalogyLogClass)Enum.Parse(typeof(AnalogyLogClass), dataRow["Class"].ToString());
                m.Level = (AnalogyLogLevel)Enum.Parse(typeof(AnalogyLogLevel), dataRow["Level"].ToString());
                int.TryParse(dataRow["Line Number"].ToString(), out int line);
                m.LineNumber = line;
                m.MethodName = dataRow["Method Name"].ToString();
                m.Module = dataRow["Process Name"].ToString();
                int.TryParse(dataRow["Process ID"].ToString(), out int process);
                m.ProcessId = process;
                m.Source = dataRow["ModuleName"].ToString();
                m.User = "";

                MessageReady(this, new AnalogyLogMessageArgs(m, Environment.MachineName, "", Id));
            }
        }

        public void Dispose()
        {
            try
            {
                logFetcher?.Stop();
                dynamicLogFetcher?.Dispose();

                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // If disposing equals true, dispose all managed resources
                if (disposing)
                {
                }
            }
            disposed = true;

        }
    }
}
