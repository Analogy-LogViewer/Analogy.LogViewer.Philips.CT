using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Analogy.Interfaces;
using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;

namespace Analogy.LogViewer.Philips.CT
{
    public enum LogClass
    {
        General,
        Security,
        Hazard,
        UserMessage,
        DictionaryDefault,
    }
    public enum Target
    {
        Generic,
        BIG,
        Service,
        Analyzer,
        CTRadar,
    }
    public class Utils
    {
        static string[] spliter = { Environment.NewLine };

        private static bool ContainsGuid(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            string[] spliter = { Environment.NewLine };
            string[] logData = message.Split(spliter, StringSplitOptions.None);
            if (Guid.TryParse(logData[0], out _))
            {
                return true;
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AnalogyLogLevel GetLogLevel(string severity)
        {
            switch (severity)
            {
                case "Critical":
                case "Fatal":
                    return AnalogyLogLevel.Critical;
                case "DebugInfo":
                    return AnalogyLogLevel.Verbose;
                case "None":
                    return AnalogyLogLevel.None;
                case "Error":
                    return AnalogyLogLevel.Error;
                case "Info":
                    return AnalogyLogLevel.Information;
                case "DebugVerbose":
                    return AnalogyLogLevel.Debug;
                case "Warning":
                    return AnalogyLogLevel.Warning;
                case "Event":
                    return AnalogyLogLevel.Information;
                case "Verbose":
                    return AnalogyLogLevel.Verbose;
                case "Debug":
                    return AnalogyLogLevel.Debug;
                case "Disabled":
                    return AnalogyLogLevel.None;
            }

            return AnalogyLogLevel.Information;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AnalogyLogLevel GetAnalogyLogLevel(string severity)
        {
            switch (severity)
            {
                case "Critical":
                case "Fatal":
                    return AnalogyLogLevel.Critical;
                case "DebugInfo":
                    return AnalogyLogLevel.Verbose;
                case "None":
                    return AnalogyLogLevel.None;
                case "Error":
                    return AnalogyLogLevel.Error;
                case "Info":
                    return AnalogyLogLevel.Information;
                case "DebugVerbose":
                    return AnalogyLogLevel.Debug;
                case "Warning":
                    return AnalogyLogLevel.Warning;
                case "Event":
                    return AnalogyLogLevel.Information;
                case "Verbose":
                    return AnalogyLogLevel.Verbose;
                case "Debug":
                    return AnalogyLogLevel.Debug;
                case "Disabled":
                    return AnalogyLogLevel.None;
            }

            return AnalogyLogLevel.Information;
        }

        public static List<FileInfo> GetSupportedFiles(DirectoryInfo dirInfo, bool recursive)
        {
            List<FileInfo> files = dirInfo.GetFiles("*.etl").Concat(dirInfo.GetFiles("*.log"))
                .Concat(dirInfo.GetFiles("*.nlog")).Concat(dirInfo.GetFiles("*.json"))
                .Concat(dirInfo.GetFiles("defaultFile_*.xml")).Concat(dirInfo.GetFiles("*.evtx")).ToList();
            if (!recursive)
            {
                return files;
            }

            try
            {
                foreach (DirectoryInfo dir in dirInfo.GetDirectories())
                {
                    files.AddRange(GetSupportedFiles(dir, true));
                }
            }
            catch (Exception)
            {
                return files;
            }

            return files;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AnalogyLogMessage CreateMessageFromEvent(EventLogEntry eEntry)
        {
            AnalogyLogMessage m = new AnalogyLogMessage();
            switch (eEntry.EntryType)
            {
                case EventLogEntryType.Error:
                    m.Level = AnalogyLogLevel.Error;
                    break;
                case EventLogEntryType.Warning:
                    m.Level = AnalogyLogLevel.Warning;
                    break;
                case EventLogEntryType.Information:
                    m.Level = AnalogyLogLevel.Information;
                    break;
                case EventLogEntryType.SuccessAudit:
                    m.Level = AnalogyLogLevel.Information;
                    break;
                case EventLogEntryType.FailureAudit:
                    m.Level = AnalogyLogLevel.Error;
                    break;
                default:
                    m.Level = AnalogyLogLevel.Information;
                    break;
            }

            m.Category = eEntry.Category;
            m.Date = eEntry.TimeGenerated;
            m.Id = Guid.NewGuid();
            m.Source = eEntry.Source;
            m.Text = eEntry.Message;
            m.User = eEntry.UserName;
            m.Module = eEntry.Source;
            return m;
        }

        public static string GetFileNameAsDataSource(string fileName)
        {
            string file = Path.GetFileName(fileName);
            return fileName.Equals(file) ? fileName : $"{file} ({fileName})";

        }

   
    }

    public abstract class Saver
    {
        public static void ExportToJson(DataTable data, string filename)
        {
            List<AnalogyLogMessage> messages = new List<AnalogyLogMessage>();
            foreach (DataRow dtr in data.Rows)
            {

                AnalogyLogMessage log = (AnalogyLogMessage)dtr["Object"];
                messages.Add(log);
            }

            string json = JsonConvert.SerializeObject(messages);
            File.WriteAllText(filename, json);
        }
        public static void ExportToJson(List<AnalogyLogMessage> messages, string filename)
        {
            string json = JsonConvert.SerializeObject(messages);
            File.WriteAllText(filename, json);
        }
        public static void ExportToCSV(List<AnalogyLogMessage> messages, string fileName)
        {
            string text = string.Join(Environment.NewLine, messages.Select(GetCSVFromMessage).ToArray());
            File.WriteAllText(fileName, text);
        }

        private static string GetCSVFromMessage(AnalogyLogMessage m) =>
        $"ID:{m.Id};Text:{m.Text};Category:{m.Category};Source:{m.Source};Level:{m.Level};Class:{m.Class};Module:{m.Module};Method:{m.MethodName};FileName:{m.FileName};LineNumber:{m.LineNumber};ProcessID:{m.ProcessId};User:{m.User};Parameters:{(m.AdditionalInformation == null ? string.Empty : string.Join(",", m.AdditionalInformation))}";
    }

    /// <summary>
    /// Represents custom filter item types.
    /// </summary>
    public enum DateRangeFilter
    {
        /// <summary>
        /// No filter
        /// </summary>
        None,
        /// <summary>
        /// Current date
        /// </summary>
        Today,
        /// <summary>
        /// Current date and yesterday
        /// </summary>
        Last2Days,
        /// <summary>
        /// Today, yesterday and the day before yesterday
        /// </summary>
        Last3Days,
        /// <summary>
        /// Last 7 days
        /// </summary>
        LastWeek,
        /// <summary>
        /// Last 2 weeks
        /// </summary>
        Last2Weeks,
        /// <summary>
        /// Last one month
        /// </summary>
        LastMonth
    }
}