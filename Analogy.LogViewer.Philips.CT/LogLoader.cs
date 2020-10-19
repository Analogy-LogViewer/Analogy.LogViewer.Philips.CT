using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Analogy.Interfaces;

namespace Analogy.LogViewer.Philips.CT
{
    public abstract class LogLoader
    {
        public abstract Task<IEnumerable<AnalogyLogMessage>> ReadFromStream(Stream dataStream, CancellationToken token, ILogMessageCreatedHandler logWindow);

        public virtual async Task<IEnumerable<AnalogyLogMessage>> ReadFromFile(string filename, CancellationToken token, ILogMessageCreatedHandler logWindow)
        {

            if (!File.Exists(filename))
            {
                await Task.CompletedTask;
                return new List<AnalogyLogMessage>();
            }

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return await ReadFromStream(fs,token, logWindow);
            }
        }
    }

}

