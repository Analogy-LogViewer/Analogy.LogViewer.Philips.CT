using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Analogy.Interfaces;

namespace Analogy.LogViewer.Philips.CT
{
    public class CTXmlLoader : LogLoader
    {
        protected string FileName;


        public override async Task<IEnumerable<AnalogyLogMessage>> ReadFromFile(string filename, CancellationToken token, ILogMessageCreatedHandler logWindow)
        {
            FileName = filename;
            return await base.ReadFromFile(filename, token, logWindow);

        }
        public override async Task<IEnumerable<AnalogyLogMessage>> ReadFromStream(Stream dataStream, CancellationToken token, ILogMessageCreatedHandler logWindow)
        {
            if (dataStream == null || logWindow == null)
            {
                return new List<AnalogyLogMessage>();
            }

            return await Task<IEnumerable<AnalogyLogMessage>>.Factory.StartNew(() =>
             {
                 List<AnalogyLogMessage> messages = new List<AnalogyLogMessage>();
                 DataSet dataSet = new DataSet();
                 dataSet.ReadXml(dataStream);
                 dataStream.Close();
                 dataSet.Tables[0].TableName = "MessageTable";

                 DisplayConversion.WriteToDataSet(ref dataSet);
                 DataTable logTable = dataSet.Tables["MessageTable"];
                 foreach (DataRow dataRow in logTable.Rows)
                 {
                     AnalogyLogMessage m = new AnalogyLogMessage();
                     m.Date = (DateTime)dataRow["Date"];
                     m.Text = dataRow["TextMessage"].ToString();
                     m.FileName = dataRow["File Name"].ToString();
                     m.Category = "";
                     m.Class = (AnalogyLogClass)Enum.Parse(typeof(AnalogyLogClass), dataRow["Class"].ToString());
                     m.Level = (AnalogyLogLevel)Enum.Parse(typeof(AnalogyLogLevel), dataRow["Level"].ToString());
                     if (!Enum.IsDefined(typeof(AnalogyLogClass), m.Class))
                     {
                         m.Class = AnalogyLogClass.General;
                     }
                     if (!Enum.IsDefined(typeof(AnalogyLogLevel), m.Level))
                     {
                         m.Level = AnalogyLogLevel.Information;
                     }
                     int.TryParse(dataRow["Line Number"].ToString(), out int line);
                     m.LineNumber = line;
                     m.MethodName = dataRow["Method Name"].ToString();
                     m.Module = dataRow["Process Name"].ToString();
                     int.TryParse(dataRow["Process ID"].ToString(), out int process);
                     m.ProcessId = process;
                     m.Source = dataRow["ModuleName"].ToString();
                     m.User = "";
                     messages.Add(m);
                     logWindow.AppendMessage(m, Utils.GetFileNameAsDataSource(FileName));
                     if (token.IsCancellationRequested)
                     {
                         string msg = "Processing cancelled by User.";
                         messages.Add(new AnalogyLogMessage(msg, AnalogyLogLevel.Information, AnalogyLogClass.General, "Analogy", "None"));
                         logWindow.AppendMessages(messages, Utils.GetFileNameAsDataSource(FileName));
                         return messages;
                     }
                 }

                 if (!messages.Any())
                 {
                     AnalogyLogMessage empty = new AnalogyLogMessage($"File {FileName} is empty or corrupted", AnalogyLogLevel.Error, AnalogyLogClass.General, "Analogy", "None");
                     empty.Source = "Analogy";
                     empty.Module = Process.GetCurrentProcess().ProcessName;
                     messages.Add(empty);
                     logWindow.AppendMessage(empty, Utils.GetFileNameAsDataSource(FileName));
                 }

                 return messages;
             });

        }

    }

    public class DisplayConversion
    {
        #region Constructor
        /// <summary>
        /// Static Constructor for initialising the variables used in static methods.
        /// </summary>
        static DisplayConversion()
        {
            moduleLoader = new CTModuleLoader();
            moduleList = moduleLoader.GetModuleIDs();
        }
        #endregion


        /// <summary>
        /// convert the database value into displaying format
        /// </summary>
        /// <param name="dataSet"></param>
        public static void WriteToDataSet(ref DataSet dataSet)
        {

            try
            {
                //Adding new columns
                dataSet.Tables["MessageTable"].Columns.Add("Date", Type.GetType("System.DateTime"));
                dataSet.Tables["MessageTable"].Columns["Date"].DateTimeMode = DataSetDateTime.UnspecifiedLocal;
                dataSet.Tables["MessageTable"].Columns.Add("Level", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("Class", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("BinData", Type.GetType("System.String"));
                if (!dataSet.Tables["MessageTable"].Columns.Contains("Binary Data"))
                {
                    dataSet.Tables["MessageTable"].Columns.Add("Binary Data", Type.GetType("System.String"));
                }
                if (!dataSet.Tables["MessageTable"].Columns.Contains("TextMessage"))
                {
                    dataSet.Tables["MessageTable"].Columns.Add("TextMessage", Type.GetType("System.String"));
                }
                if (!dataSet.Tables["MessageTable"].Columns.Contains("DictionaryId"))
                {
                    dataSet.Tables["MessageTable"].Columns.Add("DictionaryId", Type.GetType("System.String"));
                }
                if (!dataSet.Tables["MessageTable"].Columns.Contains("DebugInfo"))
                {
                    dataSet.Tables["MessageTable"].Columns.Add("DebugInfo", Type.GetType("System.String"));
                }
                dataSet.Tables["MessageTable"].Columns.Add("ModuleName", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("TargetName", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("Thread ID", Type.GetType("System.Int32"));
                dataSet.Tables["MessageTable"].Columns.Add("Process ID", Type.GetType("System.Int32"));
                dataSet.Tables["MessageTable"].Columns.Add("Process Name", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("Method Name", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("File Name", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("Line Number", Type.GetType("System.Int32"));
                dataSet.Tables["MessageTable"].Columns.Add("Stack Trace", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("DictionaryIdText", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns.Add("DictFormatText", Type.GetType("System.String"));
                dataSet.Tables["MessageTable"].Columns["MessageId"].ColumnName = "MsgID";
                dataSet.Tables["MessageTable"].Columns.Add("MessageId", Type.GetType("System.Int64"));


                ConvertValues(ref dataSet);


                //Removing the columns
                dataSet.Tables["MessageTable"].Columns.Remove("LogTime");
                dataSet.Tables["MessageTable"].Columns.Remove("LogLevel");
                dataSet.Tables["MessageTable"].Columns.Remove("LogClass");
                dataSet.Tables["MessageTable"].Columns.Remove("Binary Data");
                dataSet.Tables["MessageTable"].Columns.Remove("MsgID");
                dataSet.Tables["MessageTable"].Columns["BinData"].ColumnName = "Binary Data";
                dataSet.Tables["MessageTable"].Columns.Remove("ModuleId");
                dataSet.Tables["MessageTable"].Columns.Remove("TargetId");

                // Setting the display position
                dataSet.Tables["MessageTable"].Columns["Date"].SetOrdinal(0);
                dataSet.Tables["MessageTable"].Columns["TextMessage"].SetOrdinal(1);
            }
            catch (Exception e)
            {
                string str = "Message " + e.Message + " Source " + e.Source;
                //todo
            }

        }


        #region Private Methods
        /// <summary>
        /// Converting values from data base to the display table format.
        /// </summary>
        /// <param name="dataSet">Dataset conatining the details of the LogTable from the data base </param>
        private static void ConvertValues(ref DataSet dataSet)
        {
            AnalogyLogLevel level;
            AnalogyLogClass lgClass;
            string value = null;
            DataTable dataTable = null;
            XmlParserContext xp = new XmlParserContext(null, null, null, XmlSpace.Default);

            //Dictionary<string, DictInfo> dictLookUp = GetDictInfo();

            try
            {
                dataTable = dataSet.Tables["MessageTable"];
            }
            catch (Exception)
            {
                //todo
            }
            foreach (DataRow dataRow in dataTable.Rows)
            {
                try
                {
                    // Converting the Message ID 
                    dataRow["MessageId"] = long.Parse(dataRow["MsgID"].ToString());

                    // Changing ModuleId to string Module Name 
                    if (moduleList.Contains(Convert.ToInt32(dataRow["ModuleId"])))
                    {
                        dataRow["ModuleName"] = moduleLoader.GetModuleName(Convert.ToInt32(dataRow["ModuleId"]));
                    }
                    else
                    {
                        dataRow["ModuleName"] = moduleLoader.GetModuleName(0);
                    }
                    // Changing TargetId to Target Name
                    dataRow["TargetName"] = Enum.GetName(typeof(Target), Convert.ToInt32(dataRow["TargetId"].ToString())).ToString();

                    // Converting other values to correponding table format
                    DateTime dateTime = new DateTime((long.Parse(dataRow["LogTime"].ToString())));

                    dataRow["Date"] = dateTime;
                    value = dataRow["LogLevel"].ToString();
                    level = (AnalogyLogLevel)(Convert.ToInt16(value));
                    dataRow["Level"] = level.ToString();
                    value = dataRow["LogClass"].ToString();
                    lgClass = (AnalogyLogClass)(Convert.ToInt16(value));
                    dataRow["Class"] = lgClass.ToString();

                    if (dataRow["Binary Data"].GetType().Name.Equals("String"))
                    {
                        dataRow["BinData"] = dataRow["Binary Data"].ToString();
                    }
                    else
                    {
                        if (dataRow["Binary Data"].Equals(DBNull.Value))
                        {
                            dataRow["BinData"] = string.Empty;
                        }
                        else
                        {
                            byte[] b;
                            b = (byte[])dataRow["Binary Data"];
                            dataRow["BinData"] = Encoding.ASCII.GetString(b);
                        }
                    }



                    if (dataRow["TextMessage"].Equals(DBNull.Value))
                    {
                        dataRow["TextMessage"] = string.Empty;
                    }
                    if (dataRow["DictionaryId"].Equals(DBNull.Value))
                    {
                        dataRow["DictionaryId"] = string.Empty;
                    }
                    else
                    {
                        if (!dataRow["TextMessage"].Equals(string.Empty))
                        {
                            //    try
                            //    {
                            //        dataRow["DictFormatText"] = dictLookUp[dataRow["DictionaryId"].ToString()].DictFormatText;
                            //        dataRow["DictionaryIdText"] = dictLookUp[dataRow["DictionaryId"].ToString()].DictIdText;
                            //    }
                            //    catch { }

                            //    string[] parameters = dataRow["TextMessage"].ToString().Split("?".ToCharArray());
                            //    string str = string.Format(dataRow["DictFormatText"].ToString(), parameters);
                            //    if (str != null)
                            //    {
                            //        dataRow["TextMessage"] = str;
                            //    }
                            //    else
                            //    {
                            //        dataRow["TextMessage"] = dataRow["DictFormatText"].ToString();
                            //    }
                            //}
                            //else
                            //{
                            //    dataRow["TextMessage"] = dataRow["DictFormatText"].ToString();
                        }
                    }

                    if (dataRow["DebugInfo"].Equals(DBNull.Value))
                    {
                        dataRow["DebugInfo"] = string.Empty;
                        dataRow["Thread ID"] = DBNull.Value;
                        dataRow["Process ID"] = DBNull.Value;
                        dataRow["Process Name"] = string.Empty;
                        dataRow["Method Name"] = string.Empty;
                        dataRow["File Name"] = string.Empty;
                        dataRow["Line Number"] = DBNull.Value;
                        dataRow["Stack Trace"] = string.Empty;
                    }
                    else
                    {
                        // Separating the different information conatined otherinfo column obatined from database to the 
                        // corresponding  elements conatined by dataTable.
                        StringReader strOtherInfo = new StringReader(dataRow["DebugInfo"].ToString());
                        XmlTextReader xr = new XmlTextReader(strOtherInfo);
                        if (xr.IsStartElement("OtherInfo"))
                        {
                            while (xr.Read())
                            {
                                if (xr.IsStartElement("ProcessID"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        dataRow["Process ID"] = int.Parse(val);
                                    }
                                }
                                if (xr.IsStartElement("ProcessName"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        dataRow["Process Name"] = val;
                                    }
                                }
                                if (xr.IsStartElement("ThreadID"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        dataRow["Thread ID"] = int.Parse(val);
                                    }
                                }
                                if (xr.IsStartElement("FileName"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        dataRow["File Name"] = val;
                                    }
                                }
                                if (xr.IsStartElement("Method"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        dataRow["Method Name"] = val;
                                    }
                                }
                                if (xr.IsStartElement("Linenumber"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        dataRow["Line Number"] = int.Parse(val);
                                    }
                                }
                                if (xr.IsStartElement("StackTrace"))
                                {
                                    try
                                    {
                                        string val = xr.ReadElementString();
                                        if (val.Length > 0)
                                        {
                                            dataRow["Stack Trace"] = val;
                                        }
                                    }
                                    catch
                                    {
                                        string str = dataRow["DebugInfo"].ToString();
                                        string[] splitterStrings = { "<StackTrace>", "</StackTrace>" };
                                        string[] rezStr = str.Split(splitterStrings, StringSplitOptions.None);
                                        if (rezStr[1] != "</OtherInfo>")
                                        {
                                            dataRow["Stack Trace"] = rezStr[1];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    //todo
                }
            }
        }

        /// <summary>
        /// Convert the displaying format values into database values
        /// </summary>

        public struct DictInfo
        {
            public string DictIdText;
            public string DictFormatText;
        }

        public static Dictionary<string, DictInfo> GetDictInfo()
        {
            Dictionary<string, DictInfo> dictLookUp = new Dictionary<string, DictInfo>();

            DataTable dt = new DataTable();

            try
            {
                string conn = LogViewerLogicConstants.SqlCon;
                //SqlConnection sqlCon = new SqlConnection(@"Data Source = localhost\SQLEXPRESS;Initial Catalog =LogDBDefault;Integrated Security=True;pooling='true'");
                SqlConnection sqlCon = new SqlConnection(conn);
                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.Connection = sqlCon;
                sqlCmd.CommandText = "SELECT DictionaryId, DictionaryIdText, DictFormatText FROM Dictionary ";

                SqlDataAdapter adptr = new SqlDataAdapter(sqlCmd);

                adptr.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    DictInfo dictInfo = new DictInfo();

                    dictInfo.DictFormatText = row[2].ToString();
                    dictInfo.DictIdText = row[1].ToString();

                    try
                    {
                        dictLookUp.Add(row[0].ToString(), dictInfo);
                    }
                    catch { }

                }
            }
            catch { }


            return dictLookUp;


        }

        #endregion

        #region Member Variables

        private static CTModuleLoader moduleLoader;
        private static List<int> moduleList;

        #endregion
    }
}

