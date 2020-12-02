using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Analogy.Interfaces;

namespace Analogy.LogViewer.Philips.CT
{
    public class LogXmlLoader : LogLoader
    {
        protected string FileName;
        private ILogMessageCreatedHandler LogWindow;
        private char[] xmlInvalid = new[] { '&' };

        public override async Task<IEnumerable<AnalogyLogMessage>> ReadFromFile(string filename, CancellationToken token, ILogMessageCreatedHandler logWindow)
        {
            FileName = filename;
            LogWindow = logWindow;
            return await base.ReadFromFile(filename,token, logWindow);


        }
        public async Task<string> FixFile(string filename, CancellationToken token, IProgress<string> reportError)
        {
            var lines = await FileEx.ReadAllLinesAsync(filename,token);
            await Sanitized(lines, reportError);
            return string.Join(Environment.NewLine, lines);


        }
        private async Task Sanitized(string[] lines, IProgress<string> reportError)
        {
            await Task.Factory.StartNew(() =>
            {
                bool textSection = false;
                for (var i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    var start = line.TrimStart().StartsWith("<Text>");
                    if (start)
                    {
                        textSection = true;
                    }
                    var end = line.TrimEnd().EndsWith("</Text>");


                    if (textSection)
                    {
                        string rep = start ? line.Replace("<Text>", "") : line;
                        string temp = end ? (rep).Replace("</Text>", "") : rep;
                        if (xmlInvalid.Any(c => temp.Contains(c)))
                        {
                            reportError.Report($"Line {i + 1} has invalid character. removing it");
                            string valid = RemoveInvalidXmlChars(temp);
                            lines[i] = (start ? "<Text>" : "") + valid + (end ? "</Text>" : "");
                        }
                    }

                    if (end)
                    {
                        textSection = false;
                    }
                }
            });
            string RemoveInvalidXmlChars(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return text;
                }

                int length = text.Length;
                StringBuilder stringBuilder = new StringBuilder(length);

                for (int i = 0; i < length; ++i)
                {
                    var i1 = i;
                    if (xmlInvalid.All(c => c != text[i1]))
                    {
                        stringBuilder.Append(text[i]);
                    }
                }

                return stringBuilder.ToString();
            }


        }

        public override async Task<IEnumerable<AnalogyLogMessage>> ReadFromStream(Stream dataStream, CancellationToken token, ILogMessageCreatedHandler logWindow)
        {

            return await Task<IEnumerable<AnalogyLogMessage>>.Factory.StartNew(() =>
           {
               List<AnalogyLogMessage> messages = new List<AnalogyLogMessage>();
               try
               {
                   return TryProcessFile(dataStream,token);
               }
               catch (Exception e)
               {
                   if (MessageBox.Show(
                            $"the file {FileName} is corrupted. Do you want to attempt to fix it and read it again?{Environment.NewLine}{Environment.NewLine} Error:{e.Message}",
                            "Error Reading File", MessageBoxButtons.YesNo) == DialogResult.Yes)
                   {
                       string filename = Path.GetFileNameWithoutExtension(FileName);
                       string final = Path.Combine(Path.GetDirectoryName(FileName), filename + "_Fixed.Log");
                       var fixedData = FixFile(FileName,token, new Progress<string>(s => { })).Result;
                       File.WriteAllText(final, fixedData);
                       bool loaded = false;
                       using (FileStream fs = new FileStream(final, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                       {
                           try
                           {
                               messages = TryProcessFile(fs,token);
                               loaded = true;

                           }
                           catch (Exception e2)
                           {
                               loaded = false;
                               MessageBox.Show(
                                    $"Unable to fix file.{Environment.NewLine}{Environment.NewLine}Error: {e2.Message}",
                                    "Error Reading File", MessageBoxButtons.OK);
                           }
                       }

                       if (loaded)
                       {
                           MessageBox.Show($"File was fixed and saved to {final}", "File Fixed Successfully",
                               MessageBoxButtons.OK);
                       }
                   }
               }
               return messages;

           });
        }

        private List<AnalogyLogMessage> TryProcessFile(Stream dataStream, CancellationToken token)
        {
            if (dataStream == null || LogWindow == null)
            {
                return new List<AnalogyLogMessage>();
            }
            
            XmlNodeType ndType = XmlNodeType.Element;
            XmlParserContext xp = new XmlParserContext(null, null, null, XmlSpace.Default);
            XmlTextReader xr = new XmlTextReader(dataStream, ndType, xp);
            List<AnalogyLogMessage> messages = new List<AnalogyLogMessage>();
            while (xr.Read())
            {
                if (xr.IsStartElement("Message"))
                {
                    bool allOldFields = false;
                    AnalogyLogMessage logM = new AnalogyLogMessage();
                    while (xr.Read())
                    {
                        if (xr.IsStartElement("ID"))
                        {
                        }
                        else if (xr.IsStartElement("Date"))
                        {
                            logM.Date = DateTime.ParseExact(xr.ReadElementContentAsString(), "yyyy-MM-dd HH:mm:ss.ff", null);
                        }
                        else if (xr.IsStartElement("Text"))
                        {
                            logM.Text = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("Category"))
                        {
                            logM.Category = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("Source"))
                        {
                            logM.Source = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("Level"))
                        {
                            logM.Level = (AnalogyLogLevel)Enum.Parse(typeof(AnalogyLogLevel), xr.ReadElementContentAsString(), true);
                        }
                        else if (xr.IsStartElement("Class"))
                        {
                            logM.Class = (AnalogyLogClass)Enum.Parse(typeof(AnalogyLogClass), xr.ReadElementContentAsString(), true);
                        }
                        else if (xr.IsStartElement("Module"))
                        {
                            logM.Module = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("MethodName"))
                        {
                            logM.MethodName = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("AuditType"))
                        {
                            logM.Category = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("Method"))
                        {
                            logM.MethodName = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("FileName"))
                        {
                            logM.FileName = xr.ReadElementContentAsString();
                        }
                        else if (xr.IsStartElement("LineNumber"))
                        {
                            try
                            {
                                logM.LineNumber = int.Parse(xr.ReadElementContentAsString());
                            }
                            catch
                            {
                            }
                        }
                        else if (xr.IsStartElement("ProcessID"))
                        {
                            try
                            {
                                logM.ProcessId = int.Parse(xr.ReadElementContentAsString());
                            }
                            catch
                            {
                            }

                            //break;
                            allOldFields = true;
                        }
                        else if (xr.IsStartElement("User"))
                        {
                            logM.User = xr.ReadElementContentAsString();
                            break;
                        }
                        else if (allOldFields)
                        {
                            break;
                        }
                    } // while

                    messages.Add(logM);
                }
                if (token.IsCancellationRequested)
                {
                    string msg = "Processing cancelled by User.";
                    messages.Add(new AnalogyLogMessage(msg, AnalogyLogLevel.Information, AnalogyLogClass.General, "Analogy", "None"));
                    break;
                }
            }

            xr.Close();
            if (!messages.Any())
            {
                AnalogyLogMessage empty = new AnalogyLogMessage( $"File {FileName} is empty or corrupted",  AnalogyLogLevel.Error, AnalogyLogClass.General, "Analogy","None");
                empty.Source = "Analogy";
                empty.Module = Process.GetCurrentProcess().ProcessName;
                messages.Add(empty);
            }
            LogWindow.AppendMessages(messages, Utils.GetFileNameAsDataSource(FileName));
            return messages;
        }

        public async Task<List<AnalogyLogMessage>> ReadFileContent(string filename)
        {
            if (!File.Exists(filename))
            {
                return new List<AnalogyLogMessage>();
            }

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {

                XmlNodeType ndType = XmlNodeType.Element;
                XmlParserContext xp = new XmlParserContext(null, null, null, XmlSpace.Default);

                XmlTextReader xr = new XmlTextReader(fs, ndType, xp);
                return await Task.Factory.StartNew(() =>
                 {
                     List<AnalogyLogMessage> messages = new List<AnalogyLogMessage>();
                     while (xr.Read())
                     {
                         if (xr.IsStartElement("Message"))
                         {
                             bool allOldFields = false;
                             AnalogyLogMessage logM = new AnalogyLogMessage();
                             while (xr.Read())
                             {
                                 if (xr.IsStartElement("ID"))
                                 {
                                 }
                                 else if (xr.IsStartElement("Date"))
                                 {
                                     logM.Date = DateTime.ParseExact(xr.ReadElementContentAsString(), "yyyy-MM-dd HH:mm:ss.ff", null);
                                 }
                                 else if (xr.IsStartElement("Text"))
                                 {
                                     logM.Text = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("Category"))
                                 {
                                     logM.Category = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("Source"))
                                 {
                                     logM.Source = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("Level"))
                                 {
                                     logM.Level = (AnalogyLogLevel)Enum.Parse(typeof(AnalogyLogLevel), xr.ReadElementContentAsString(), true);
                                 }
                                 else if (xr.IsStartElement("Class"))
                                 {
                                     logM.Class = (AnalogyLogClass)Enum.Parse(typeof(AnalogyLogClass), xr.ReadElementContentAsString(), true);
                                 }
                                 else if (xr.IsStartElement("Module"))
                                 {
                                     logM.Module = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("AuditType"))
                                 {
                                     logM.Category = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("Method"))
                                 {
                                     logM.MethodName = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("FileName"))
                                 {
                                     logM.FileName = xr.ReadElementContentAsString();
                                 }
                                 else if (xr.IsStartElement("LineNumber"))
                                 {
                                     try
                                     {
                                         logM.LineNumber = int.Parse(xr.ReadElementContentAsString());
                                     }
                                     catch
                                     {
                                     }
                                 }
                                 else if (xr.IsStartElement("ProcessID"))
                                 {
                                     try
                                     {
                                         logM.ProcessId = int.Parse(xr.ReadElementContentAsString());
                                     }
                                     catch
                                     {
                                     }

                                     //break;
                                     allOldFields = true;
                                 }
                                 else if (xr.IsStartElement("User"))
                                 {
                                     logM.User = xr.ReadElementContentAsString();
                                     break;
                                 }
                                 else if (allOldFields)
                                 {
                                     break;
                                 }
                             } // while

                             messages.Add(logM);
                         }
                     }

                     xr.Close();
                     if (!messages.Any())
                     {
                         AnalogyLogMessage empty = new AnalogyLogMessage($"File {FileName} is empty or corrupted", AnalogyLogLevel.Error, AnalogyLogClass.General, "Analogy","None");
                         messages.Add(empty);
                     }
                     return messages;
                 });
            }
        }

    }
}
