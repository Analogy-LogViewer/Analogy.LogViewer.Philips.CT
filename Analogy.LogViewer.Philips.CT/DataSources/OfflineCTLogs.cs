using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analogy.Interfaces;
using Analogy.LogViewer.Template;

namespace Analogy.LogViewer.Philips.CT.DataSources
{
    public class OfflineCTLogs : OfflineDataProvider
    {
        public override Guid Id { get; set; } = new Guid("9824830F-459D-41DD-BFEA-0A4E7FB50EC3");

        public override string FileOpenDialogFilters { get; set; } = "CT Logs files (*.xml)|*.xml";
        public override IEnumerable<string> SupportFormats { get; set; } = new[] { "defaultFile_*.xml", };
        public override string? InitialFolderFullPath { get; set; }
        public override string? OptionalTitle { get; set; } = "CT logs";


        public override async Task<IEnumerable<AnalogyLogMessage>> Process(string fileName, CancellationToken token, ILogMessageCreatedHandler messagesHandler)
        {
            if (CanOpenFile(fileName))
            {
                CTXmlLoader logLoader = new CTXmlLoader();
                var messages = await logLoader.ReadFromFile(fileName,token, messagesHandler).ConfigureAwait(false);
                return messages;
            }
            else
            {
                AnalogyLogMessage m = new AnalogyLogMessage();
                m.Text = $"Unsupported file: {fileName}. Skipping file";
                m.Level = AnalogyLogLevel.Critical;
                m.Source = "Analogy";
                m.Module = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                m.ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
                m.Class = AnalogyLogClass.General;
                m.User = Environment.UserName;
                m.Date = DateTime.Now;
                messagesHandler.AppendMessage(m, Environment.MachineName);
                return new List<AnalogyLogMessage>() { m };
            }
        }

        public IEnumerable<FileInfo> GetSupportedFiles(DirectoryInfo dirInfo, bool recursiveLoad)
        {
            return GetSupportedFilesInternal(dirInfo, recursiveLoad);
        }
        
        public override bool CanOpenFile(string fileName) => fileName.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase);

        protected override List<FileInfo> GetSupportedFilesInternal(DirectoryInfo dirInfo, bool recursive)
        {
            List<FileInfo> files = dirInfo.GetFiles("defaultFile_*.xml").ToList();
            if (!recursive)
            {
                return files;
            }

            try
            {
                foreach (DirectoryInfo dir in dirInfo.GetDirectories())
                {
                    files.AddRange(GetSupportedFilesInternal(dir, true));
                }
            }
            catch (Exception)
            {
                return files;
            }

            return files;
        }
    }
}
