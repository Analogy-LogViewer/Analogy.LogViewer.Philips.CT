using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Analogy.LogViewer.Philips.CT
{
    #region Delegates

    /// <summary>
    /// Represents delegate for new batch of log messages arrieved from repository
    /// </summary>
    /// <param name="logTable">DataTable</param>
    public delegate void LogsArrivedDelegate(DataTable logTable);

    /// <summary>
    /// Delegate for fetching of logs from repository complete
    /// </summary>
    public delegate void FetchCompletedDelegate(int noOfLogs);

    /// <summary>
    /// Delegate for fetching of updated logs from repository complete
    /// </summary>
    /// <param name="logTable"></param>
    /// <param name="noOfLogs"></param>
    public delegate void UpdateCompletedDelegate(DataTable logTable, int noOfLogs);

    /// <summary>
    /// Delegate for showing status message in UI
    /// </summary>
    /// <param name="msg">string</param>
    /// <param name="visibility">bool</param>
    public delegate void StatusUpdatedDelegate(string msg, bool visibility);


    #endregion
    /// <summary>
    /// Responsible for fetching existing log data from the repository
    /// </summary>
    public class DatabaseLogFetcher : LogFetcher
    {
        #region Private Member Variables

        private string sqlFilter;
        private string sqlConString = LogViewerLogicConstants.SqlCon;
        private string viewName;
        private long firstRowMsgIDPage;
        private long lastRowMsgID;
        private long latestReadMessages;
        private long latestLogMsgID;
        private long msgToRead = 0;
        private long maxMsgId;
        #endregion Private Member Variables
        #region Public Methods

        /// <summary>
        /// Constructor to initialize the Database Log Fetcher
        /// </summary>
        /// <param name="filterObj"></param>
        public DatabaseLogFetcher(FilterObject filterObj)
            : base(filterObj)
        {
            sqlFilter = filterObject.GetSQLFilterString(null);

            //// Set up the Connection String
            //if (databaseFilePath != null)
            //{
            //    this.databaseFilePath = databaseFilePath;
            //    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            //    builder.DataSource = @"localhost\SQLEXPRESS";
            //    builder.AttachDBFilename = this.databaseFilePath;
            //    builder.UserInstance = true;
            //    builder.IntegratedSecurity = true;
            //    sqlConString = builder.ConnectionString;
            //}
            //else
            //{
            canDynamicUpdate = true;
            //}

            viewName = string.Empty;
            //todo

        }

        /// <summary>
        /// Fetches existing log data from log database server
        /// </summary>
        public override void FetchLogData()
        {
            if (isDisabled)
            {
                return;
            }

            /* Database operations */
            SqlConnection sqlCon = new SqlConnection(sqlConString);
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.Connection = sqlCon;
            sqlCmd.CommandTimeout = 180;

            if (!string.IsNullOrEmpty(viewName))
            {
                ExecuteSql("DROP VIEW " + viewName);
            }
            viewName = LogViewerLogicConstants.ViewName + DateTime.Now.Ticks.ToString();

            try
            {
                sqlCon.Open();

                if (isDisabled)
                {
                    return;
                }
                sqlCmd.CommandText = GetCreateViewSQL(sqlFilter, viewName);
                sqlCmd.ExecuteNonQuery();

                if (isDisabled)
                {
                    return;
                }
            }
            catch (Exception)
            {
                // InternalLog.Write(" ***** Database Fetch FAILED ***** \n DatabaseLogFetcher.StartPaging() : " + ex.Message);
                canDynamicUpdate = false;
                OnFetchComplete(0);
                MessageBox.Show("Unable to Connect to the Database", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            finally
            {
                sqlCon.Close();
            }

            if (isDisabled)
            {
                return;
            }

            //DataTable logTable = GetLastPage();
            //OnLogsArrived(logTable);
            //OnFetchComplete(logTable.Rows.Count);

        }

        /// <summary>
        /// Stop Fetching log data from log repository
        /// </summary>
        public override void Stop()
        {
            isDisabled = true;
            if (!string.IsNullOrEmpty(viewName))
            {
                ExecuteSql("DROP VIEW " + viewName);
            }

            viewName = null;
        }

        /// <summary>
        /// Update the values of the FirstRow and Last Row
        /// </summary>
        public override void AdvancePage(int numExistingRows, int numRowsAdded)
        {
            firstRow = firstRow + PageSize * (numRowsAdded / PageSize);
            lastRow = numExistingRows + firstRow + (numRowsAdded % PageSize) - 1;
        }
        /// <summary>
        ///  Returns the table schema for displaying Log data
        /// </summary>
        /// <returns>Data Table</returns>
        public static DataTable GetDisplayableDataTable()
        {
            DataTable dataTable = new DataTable();

            // Adding columns
            //LogTime	
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridDateColumn, Type.GetType("System.DateTime"));
            //LogLevel
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridLevelColumn, Type.GetType("System.String"));
            //ModuleID
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridModuleNameColumn, Type.GetType("System.String"));
            //Text Message
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridTextMessageColumn, Type.GetType("System.String"));
            //Additonal Info
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridAdditionalInfoColumn, Type.GetType("System.String"));
            //BlobInfo
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridBlobInfoColumn, Type.GetType("System.String"));
            //MessageId	
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridMessageIdColumn, Type.GetType("System.Int64"));
            //LogClass
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridClassColumn, Type.GetType("System.String"));
            //BinaryData
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridBinaryDataColumn, Type.GetType("System.String"));
            //TargetID
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridTargetNameColumn, Type.GetType("System.String"));

            //------------------------- Debug Informatiom --------------------------

            //Process Name
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridProcessNameColumn, Type.GetType("System.String"));

            //Process ID
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridProcessIDColumn, Type.GetType("System.Int32"));

            //Thread ID
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridThreadIDColumn, Type.GetType("System.Int32"));

            //File Name
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridFileNameColumn, Type.GetType("System.String"));

            //Method Name
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridMethodNameColumn, Type.GetType("System.String"));

            //Line Number
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridLineNumberColumn, Type.GetType("System.Int32"));

            //Stack Trace
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridStackTraceColumn, Type.GetType("System.String"));

            //Dict Format Text
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridDictFormatTextColumn, Type.GetType("System.String"));

            //Dictionary Id Text
            dataTable.Columns.Add(LogViewerLogicConstants.DataGridDictionaryIdTextColumn, Type.GetType("System.String"));
            // Setting Ordinal 

            dataTable.Columns[LogViewerLogicConstants.DataGridDateColumn].SetOrdinal(0);
            dataTable.Columns[LogViewerLogicConstants.DataGridLevelColumn].SetOrdinal(1);
            dataTable.Columns[LogViewerLogicConstants.DataGridModuleNameColumn].SetOrdinal(2);
            dataTable.Columns[LogViewerLogicConstants.DataGridTextMessageColumn].SetOrdinal(3);
            dataTable.Columns[LogViewerLogicConstants.DataGridAdditionalInfoColumn].SetOrdinal(4);
            dataTable.Columns[LogViewerLogicConstants.DataGridBlobInfoColumn].SetOrdinal(5);
            dataTable.DefaultView.Sort = "MessageId ASC";

            return dataTable;
        }

        /// <summary>
        /// Returns data table with converted data
        /// </summary>
        /// <param name="logDataTable"></param>
        /// <returns>Data Table</returns>
        public static DataTable ConvertLogData(DataTable logDataTable)
        {
            DataTable convertedTable = GetDisplayableDataTable();

            if (logDataTable.Rows.Count == 0)
            {
                return convertedTable;
            }
            Dictionary<string, DisplayConversion.DictInfo> dictLookUp = DisplayConversion.GetDictInfo();

            int noOfRows = logDataTable.Rows.Count;

            for (int index = noOfRows - 1; index >= 0; index--)
            {
                DataRow row = logDataTable.Rows[index];
                DataRow newRow = convertedTable.NewRow();

                try
                {
                    // -------------- General Log Details ------------------ 

                    // Date Time
                    DateTime dateTime = new DateTime((long)(row[LogViewerLogicConstants.DBLogTimeColumn]), DateTimeKind.Local);
                    newRow[LogViewerLogicConstants.DataGridDateColumn] = dateTime;

                    // MessageId
                    newRow[LogViewerLogicConstants.DataGridMessageIdColumn] = row[LogViewerLogicConstants.DBMessageIDColumn];

                    // Log Class
                    newRow[LogViewerLogicConstants.DataGridClassColumn] = LogProperties.LogClassLookUp.GetValue(int.Parse(row[LogViewerLogicConstants.DBLogClassColumn].ToString()));

                    // Log Level 
                    newRow[LogViewerLogicConstants.DataGridLevelColumn] = LogProperties.LogLevelLookUp.GetValue(int.Parse(row[LogViewerLogicConstants.DBLogLevelColumn].ToString()));

                    // Module Name 
                    try
                    {
                        newRow[LogViewerLogicConstants.DataGridModuleNameColumn] = LogProperties.LogModuleLookUp.GetValue(int.Parse(row[LogViewerLogicConstants.DBModuleIDColumn].ToString()));
                    }
                    catch
                    {
                        if (LogProperties.LogModuleLookUp.Contains(0))
                        {
                            newRow[LogViewerLogicConstants.DataGridModuleNameColumn] = LogProperties.LogModuleLookUp.GetValue(0);
                        }
                        else
                        {
                            newRow[LogViewerLogicConstants.DataGridModuleNameColumn] = "Generic";
                        }
                    }

                    // Target Name
                    newRow[LogViewerLogicConstants.DataGridTargetNameColumn] = LogProperties.LogTargetLookUp.GetValue(int.Parse(row[LogViewerLogicConstants.DBTargetIDColumn].ToString()));

                    // -------------- Binary Log Data ------------------
                    if (row[LogViewerLogicConstants.DBBinaryDataColumn].GetType().Name.Equals("String"))
                    {
                        newRow[LogViewerLogicConstants.DataGridBinaryDataColumn] = row[LogViewerLogicConstants.DBBinaryDataColumn].ToString();
                    }
                    else
                    {
                        if (row[LogViewerLogicConstants.DBBinaryDataColumn].Equals(DBNull.Value))
                        {
                            newRow[LogViewerLogicConstants.DataGridBinaryDataColumn] = string.Empty;
                        }
                        else
                        {
                            byte[] b;
                            b = (byte[])row[LogViewerLogicConstants.DBBinaryDataColumn];
                            newRow[LogViewerLogicConstants.DataGridBinaryDataColumn] = Encoding.ASCII.GetString(b);
                        }
                    }

                    // -------------- AdditionalInfo ------------------
                    newRow[LogViewerLogicConstants.DataGridAdditionalInfoColumn] = row[LogViewerLogicConstants.DBAdditionalInfoColumn];

                    // -------------- BlobInfo ------------------
                    if (!row[LogViewerLogicConstants.DBBlobInfoColumn].Equals(DBNull.Value) && (!string.IsNullOrEmpty(row[LogViewerLogicConstants.DBBlobInfoColumn].ToString())))
                    {
                        newRow[LogViewerLogicConstants.DataGridBlobInfoColumn] = "YES";
                    }
                    else
                    {
                        newRow[LogViewerLogicConstants.DataGridBlobInfoColumn] = "NO";
                    }

                    //--------------  Text Message ------------------
                    newRow[LogViewerLogicConstants.DataGridTextMessageColumn] = row[LogViewerLogicConstants.DBTextMesageColumn];

                    //--------------  Dictionary Messages -------------
                    if (row[LogViewerLogicConstants.DBDictionaryIdColumn].Equals(DBNull.Value))
                    {
                        // Do nothing
                    }
                    else
                    {
                        if (!row[LogViewerLogicConstants.DBTextMesageColumn].Equals(string.Empty))
                        {
                            try
                            {
                                row[LogViewerLogicConstants.DBDictFormatTextColumn] = dictLookUp[row[LogViewerLogicConstants.DBDictionaryIdColumn].ToString()].DictFormatText;
                                row[LogViewerLogicConstants.DBDictionaryIdTextColumn] = dictLookUp[row[LogViewerLogicConstants.DBDictionaryIdColumn].ToString()].DictIdText;
                            }
                            catch { }

                            string[] parameters = row[LogViewerLogicConstants.DBTextMesageColumn].ToString().Split("?".ToCharArray());
                            string str = string.Format(row[LogViewerLogicConstants.DBDictFormatTextColumn].ToString(), parameters);
                            if (str != null)
                            {
                                newRow[LogViewerLogicConstants.DataGridTextMessageColumn] = str;
                            }
                            else
                            {
                                newRow[LogViewerLogicConstants.DataGridTextMessageColumn] = row[LogViewerLogicConstants.DBDictFormatTextColumn].ToString();
                            }
                        }
                        else
                        {
                            newRow[LogViewerLogicConstants.DataGridTextMessageColumn] = row[LogViewerLogicConstants.DBDictFormatTextColumn].ToString();
                        }
                    }

                    // --------------  Debug Information ------------------

                    if (row[LogViewerLogicConstants.DBDebugInfoColumn].Equals(DBNull.Value))
                    {
                        newRow[LogViewerLogicConstants.DataGridThreadIDColumn] = DBNull.Value;
                        newRow[LogViewerLogicConstants.DataGridProcessIDColumn] = DBNull.Value;
                        newRow[LogViewerLogicConstants.DataGridProcessNameColumn] = string.Empty;
                        newRow[LogViewerLogicConstants.DataGridMethodNameColumn] = string.Empty;
                        newRow[LogViewerLogicConstants.DataGridFileNameColumn] = string.Empty;
                        newRow[LogViewerLogicConstants.DataGridLineNumberColumn] = DBNull.Value;
                        newRow[LogViewerLogicConstants.DataGridStackTraceColumn] = string.Empty;
                    }
                    else
                    {
                        // Separating the different information conatined Debug Info column obatined from database to the 
                        // corresponding  elements conatined by dataTable.
                        StringReader strOtherInfo = new StringReader(row[LogViewerLogicConstants.DBDebugInfoColumn].ToString());
                        XmlTextReader xr = new XmlTextReader(strOtherInfo);
                        if (xr.IsStartElement("OtherInfo"))
                        {
                            while (xr.Read())
                            {
                                if (xr.IsStartElement(LogViewerLogicConstants.DBProcessIDColumn))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        newRow[LogViewerLogicConstants.DataGridProcessIDColumn] = int.Parse(val);
                                    }
                                }
                                if (xr.IsStartElement(LogViewerLogicConstants.DBProcessNameColumn))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        newRow[LogViewerLogicConstants.DataGridProcessNameColumn] = val;
                                    }
                                }
                                if (xr.IsStartElement(LogViewerLogicConstants.DBThreadIDColumn))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        newRow[LogViewerLogicConstants.DataGridThreadIDColumn] = int.Parse(val);
                                    }
                                }
                                if (xr.IsStartElement(LogViewerLogicConstants.DBFileNameColumn))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        newRow[LogViewerLogicConstants.DataGridFileNameColumn] = val;
                                    }
                                }
                                if (xr.IsStartElement("Method"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        newRow[LogViewerLogicConstants.DataGridMethodNameColumn] = val;
                                    }
                                }
                                if (xr.IsStartElement("Linenumber"))
                                {
                                    string val = xr.ReadElementString();
                                    if (val.Length > 0)
                                    {
                                        newRow[LogViewerLogicConstants.DataGridLineNumberColumn] = int.Parse(val);
                                    }
                                }
                                if (xr.IsStartElement(LogViewerLogicConstants.DBStackTraceColumn))
                                {
                                    try
                                    {
                                        string val = xr.ReadElementString();
                                        if (val.Length > 0)
                                        {
                                            newRow[LogViewerLogicConstants.DataGridStackTraceColumn] = val;
                                        }
                                    }
                                    catch
                                    {
                                        string str = row[LogViewerLogicConstants.DBDebugInfoColumn].ToString();
                                        string[] splitterStrings = { "<StackTrace>", "</StackTrace>" };
                                        string[] rezStr = str.Split(splitterStrings, StringSplitOptions.None);
                                        if (rezStr[1] != "</OtherInfo>")
                                        {
                                            newRow[LogViewerLogicConstants.DataGridStackTraceColumn] = rezStr[1];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    convertedTable.Rows.Add(newRow);
                }

                catch (Exception)
                {
                }
            }

            //convertedTable.DefaultView.Sort = LogViewerLogicConstants.DataGridDateColumn;

            return convertedTable.DefaultView.Table;

        }

        /// <summary>
        /// Update the values of the FirstRow and Last Row
        /// </summary>
        public override void RemainOnPage(int numRowsAdded)
        {
            lastRow = lastRow + numRowsAdded;
        }

        /// <summary>
        /// Update the total number of rows
        /// </summary>
        public override void UpdateTotalNumOfRows()
        {
            totalNoOfRows = GetTotalNoOfRows();
        }
        /// <summary>
        /// Returns a data table as per schema in the Log Database
        /// </summary>
        /// <returns></returns>
        public static DataTable GetLogTable()
        {
            DataTable dataTable = new DataTable();

            // Adding columns
            //MessageID	
            dataTable.Columns.Add(LogViewerLogicConstants.DBMessageIDColumn, Type.GetType("System.Int64"));
            //TextMessage
            dataTable.Columns.Add(LogViewerLogicConstants.DBTextMesageColumn, Type.GetType("System.String"));
            //LogLevel
            dataTable.Columns.Add(LogViewerLogicConstants.DBLogLevelColumn, Type.GetType("System.Int16"));
            //LogClass
            dataTable.Columns.Add(LogViewerLogicConstants.DBLogClassColumn, Type.GetType("System.Int16"));
            //BinaryData
            dataTable.Columns.Add(LogViewerLogicConstants.DBBinaryDataColumn, Type.GetType("System.Byte[]"));
            //LogTime	
            dataTable.Columns.Add(LogViewerLogicConstants.DBLogTimeColumn, Type.GetType("System.Int64"));
            //ModuleID
            dataTable.Columns.Add(LogViewerLogicConstants.DBModuleIDColumn, Type.GetType("System.Int32"));
            //TargetID
            dataTable.Columns.Add(LogViewerLogicConstants.DBTargetIDColumn, Type.GetType("System.Int16"));
            //Row Num	
            dataTable.Columns.Add(LogViewerLogicConstants.DBRowNumColumn, Type.GetType("System.Int64"));

            //Dictionary ID
            dataTable.Columns.Add(LogViewerLogicConstants.DBDictionaryIdColumn, Type.GetType("System.String"));
            //DictFormatText
            dataTable.Columns.Add(LogViewerLogicConstants.DBDictFormatTextColumn, Type.GetType("System.String"));
            //DictionaryIdText
            dataTable.Columns.Add(LogViewerLogicConstants.DBDictionaryIdTextColumn, Type.GetType("System.String"));
            //Debug Info	
            dataTable.Columns.Add(LogViewerLogicConstants.DBDebugInfoColumn, Type.GetType("System.String"));
            //Additonal Info
            dataTable.Columns.Add(LogViewerLogicConstants.DBAdditionalInfoColumn, Type.GetType("System.String"));
            //BlobInfo
            dataTable.Columns.Add(LogViewerLogicConstants.DBBlobInfoColumn, Type.GetType("System.String"));



            return dataTable;
        }
        /// <summary>
        /// Returns updated logs in the datatable
        /// </summary>
        /// <returns></returns>
        public override DataTable GetUpdatedLogs()
        {
            DataTable logTable = GetLogTable();
            if (isDisabled)
            {
                return logTable;
            }

            string sqlString = string.Format(Queries.GetUpdatedLogs, totalNoOfRows - latestReadMessages, viewName);

            try
            {
                logTable = FetchRecords(sqlString);
                if (logTable.Rows.Count > 0)
                {
                    latestReadMessages += logTable.Rows.Count;
                }
            }
            catch (Exception)
            {
                //InternalLog.Write("DatabaseLogFetcher.GetAllLogs() : " + ex.Message);
            }
            UpdatePageStatus();

            OnFetchComplete(logTable.Rows.Count);
            DataTable convertedLogTable = ConvertLogData(logTable);
            return convertedLogTable;
        }

        /// <summary>
        /// Returns all logs in the datatable with filter till the specified Message ID
        /// </summary>
        /// <returns></returns>
        public override DataTable GetAllLogs()
        {
            DataTable logTable = GetLogTable();

            if (isDisabled)
            {
                return logTable;
            }

            string sqlString = string.Format(Queries.GetAllLogsByMsgId,
                viewName, latestLogMsgID);

            try
            {
                logTable = FetchRecords(sqlString);
            }
            catch (Exception)
            {
                //InternalLog.Write("DatabaseLogFetcher.GetAllLogs() : " + ex.Message);
            }
            DataTable convertedLogTable = ConvertLogData(logTable);

            return convertedLogTable;
        }

        /// <summary>
        /// Check for new logs in the view
        /// </summary>
        /// <returns>true if new logs present else false</returns>
        public override bool CheckForNewLogs()
        {

            try
            {
                var tempTotal = GetTotalNoOfRows();
                if (tempTotal > totalNoOfRows)
                {
                    totalNoOfRows = tempTotal;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the next analogyPage of data.
        /// </summary>
        /// <returns>log table</returns>
        /// <exception cref="SqlException">
        /// Throws SQLException and others
        /// </exception>
        public override DataTable GetNextPage()
        {
            DataTable logTable;
            string sqlString;
            bool isLastPage = false;

            totalNoOfRows = GetTotalNoOfRows();

            if ((lastRow + PageSize) > totalNoOfRows)
            {
                sqlString = string.Format(Queries.GetNextLastPage,
                    viewName, lastRowMsgID);
                isLastPage = true;
            }
            else
            {
                sqlString = string.Format(Queries.GetNextPage,
                    PageSize, viewName, lastRowMsgID);
            }

            logTable = FetchRecords(sqlString);
            if (logTable.Rows.Count > 0)
            {
                firstRowMsgIDPage = (long)logTable.Rows[0][LogViewerLogicConstants.DBMessageIDColumn];
                lastRowMsgID = (long)logTable.Rows[logTable.Rows.Count - 1][LogViewerLogicConstants.DBMessageIDColumn];
            }

            if (isLastPage)
            {
                latestLogMsgID = lastRowMsgID;
            }

            firstRow = firstRow + PageSize;
            lastRow = lastRow + logTable.Rows.Count;
            if (totalNoOfRows < lastRow)
            {
                lastRow = totalNoOfRows;
            }

            // Checking if Next and prev analogyPage exist
            UpdatePageStatus();
            OnFetchComplete(logTable.Rows.Count);
            DataTable convertedLogTable = ConvertLogData(logTable);
            return convertedLogTable;
        }

        /// <summary>
        /// Retrieves the previous analogyPage of data
        /// </summary>
        /// <returns>log table</returns>
        /// <exception cref="SqlException">
        /// Throws SQLException and others
        /// </exception>
        public override DataTable GetPrevPage()
        {
            DataTable logTable;
            string sqlString;

            totalNoOfRows = GetTotalNoOfRows();

            sqlString = string.Format(Queries.GetPrevPage,
                PageSize, viewName, firstRowMsgIDPage);

            logTable = FetchRecords(sqlString);

            if ((logTable == null) || (logTable.Rows.Count == 0))
            {
                return null;
            }

            firstRow = firstRow - PageSize;
            lastRow = firstRow + PageSize - 1;

            if (logTable.Rows.Count > 0)
            {
                firstRowMsgIDPage = (long)logTable.Rows[logTable.Rows.Count - 1][LogViewerLogicConstants.DBMessageIDColumn];
                lastRowMsgID = (long)logTable.Rows[0][LogViewerLogicConstants.DBMessageIDColumn];
            }

            DataTable convertedLogTable = ConvertLogData(logTable);
            // Checking if Next and prev analogyPage exist
            UpdatePageStatus();

            OnFetchComplete(logTable.Rows.Count);

            return convertedLogTable;
        }

        /// <summary>
        /// It applies filter and start fetching
        /// </summary>
        /// <param name="filterObj"></param>
        public override void ApplyFilter(FilterObject filterObj)
        {
            filterObject = filterObj;
            sqlFilter = filterObject.GetSQLFilterString(null);
            FetchLogData();
        }

        /// <summary>
        /// Sets the values of the First and Last row Message IDs
        /// </summary>
        /// <param name="logViewTable"></param>
        public void SetFirstLastRowMsgIDs(DataTable logViewTable)
        {
            firstRowMsgIDPage = (long)logViewTable
                .Rows[logViewTable.Rows.Count - 1][LogViewerLogicConstants.DBMessageIDColumn];
            lastRowMsgID = (long)logViewTable.Rows[0][LogViewerLogicConstants.DBMessageIDColumn];
        }

        /// <summary>
        /// Retrieves the first analogyPage of data.
        /// </summary>
        /// <returns>log table</returns>
        public override DataTable GetLastPage()
        {
            DataTable logTable = GetLogTable();

            if (isDisabled)
            {
                return logTable;
            }

            totalNoOfRows = GetTotalNoOfRows();
            msgToRead = totalNoOfRows;
            int lastPageSize = totalNoOfRows % PageSize;

            string sqlString = string.Format(Queries.GetLastPage, lastPageSize, viewName);
            try
            {
                OnFetchStart(totalNoOfRows);
                logTable = FetchRecords(sqlString);

                firstRow = totalNoOfRows - lastPageSize + 1;
                lastRow = totalNoOfRows;

                if (logTable.Rows.Count > 0)
                {
                    firstRowMsgIDPage = (long)logTable
                        .Rows[logTable.Rows.Count - 1][LogViewerLogicConstants.DBMessageIDColumn];
                    lastRowMsgID = (long)logTable.Rows[0][LogViewerLogicConstants.DBMessageIDColumn];
                }


            }
            catch (Exception)
            {
                //InternalLog.Write("DatabaseLogFetcher.GetFirstPage() : " + ex.Message);
            }

            // Checking if Next and prev analogyPage exist
            UpdatePageStatus();
            OnFetchComplete(logTable.Rows.Count);
            DataTable convertedLogTable = ConvertLogData(logTable);
            return convertedLogTable;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Update if previous analogyPage or next analogyPage exist
        /// </summary>
        private void UpdatePageStatus()
        {
            // Checking for Previous analogyPage
            if (firstRow > 1)
            {
                isPrevPageExist = true;
            }
            else
            {
                isPrevPageExist = false;
            }

            // Checking for Next analogyPage
            if (lastRow < totalNoOfRows)
            {
                isNextPageExist = true;
            }
            else
            {
                isNextPageExist = false;
            }
        }

        /// <summary>
        /// Retieves total no of rows the view
        /// </summary>
        /// <returns></returns>
        private int GetTotalNoOfRows()
        {
            if (viewName == null)
            {
                return 0;
            }
            object objMaxRow;
            try
            {
                objMaxRow = FetchScalar("SELECT COUNT(*) FROM " + viewName);
            }
            catch (Exception)
            {
                //InternalLog.Write("DatabaseLogFetcher.GetTotalNoOfRows() : " + ex.Message);
                return 0;
            }
            int maxRow = 0;

            if (objMaxRow != null && objMaxRow != DBNull.Value)
            {
                maxRow = (int)objMaxRow;
            }

            return maxRow;
        }

        /// <summary>
        /// Returns SQL string to create view
        /// </summary>
        /// <param name="sqlfilter"></param>
        /// <param name="viewName"></param>
        /// <returns></returns>
        private string GetCreateViewSQL(string sqlFilter, string viewName)
        {
            string sqlString;

            if (sqlFilter.Contains("TextMessage LIKE"))
            {
                string[] separator = { "TextMessage LIKE '" };
                string[] splitedStr = sqlFilter.Split(separator, StringSplitOptions.None);

                sqlString = "EXEC sp_createview '" + splitedStr[0] + " TextMessage LIKE " + "' , '" + viewName + "', " + " \"'" + splitedStr[1] + "\"";
            }
            else
            {
                sqlString = "EXEC sp_createview '" + sqlFilter + "' , '" + viewName + "'";
            }

            return sqlString;
        }

        /// <summary>
        /// Returns datatable from the database
        /// Throws SqlException and others
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        /// <exception cref="SqlException">
        /// Throws SQLException and others
        /// </exception>
        private DataTable FetchRecords(string sqlString)
        {
            string cmdString = sqlString;

            DataTable logTable = GetLogTable();

            /* Database operations */
            SqlConnection sqlCon = new SqlConnection(sqlConString);
            SqlCommand sqlCmd = new SqlCommand(cmdString, sqlCon);
            sqlCmd.CommandTimeout = 120;
            SqlDataAdapter sqlDataAdptr = new SqlDataAdapter(sqlCmd);

            sqlDataAdptr.Fill(logTable);

            return logTable;
        }

        /// <summary>
        /// Return a scalar value from DB as per the command string
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        private object FetchScalar(string sqlString)
        {
            object obj = null;
            string cmdString = sqlString;

            /* Database operations */
            SqlConnection sqlCon = new SqlConnection(sqlConString);
            SqlCommand sqlCmd = new SqlCommand(cmdString, sqlCon);
            sqlCmd.CommandTimeout = 120;

            try
            {
                sqlCon.Open();
                obj = sqlCmd.ExecuteScalar();
            }
            catch (Exception)
            {
                // InternalLog.Write("DatabaseLogFetcher.FetchScalar() : " + ex.Message);
                throw;
            }
            finally
            {
                sqlCon.Close();
            }

            return obj;
        }

        /// <summary>
        /// Execute a SQL command
        /// </summary>
        /// <param name="sqlString"></param>
        private void ExecuteSql(string sqlString)
        {
            /* Database operations */
            SqlConnection sqlCon = new SqlConnection(sqlConString);
            SqlCommand sqlCmd = new SqlCommand(sqlString, sqlCon);

            try
            {
                sqlCon.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                //InternalLog.Write("DatabaseLogFetcher.ExecuteSql() : " + ex.Message);
            }
            finally
            {
                sqlCon.Close();
            }
        }

        #endregion Private Methods


    }

    /// <summary>
    /// Responsible for fetching log data from the repository in batches 
    /// and expose the data through events 
    /// </summary>
    public abstract class LogFetcher
    {
        #region Constructors

        /// <summary>
        /// Initializes variables
        /// </summary>
        /// <param name="filterObj"></param>
        public LogFetcher(FilterObject filterObj)
        {
            filterObject = filterObj;
            isAudioMarker = false;
            canDynamicUpdate = false;
            totalNoOfRows = 0;
            isDisabled = false;
            isPrevPageExist = false;
            isNextPageExist = false;

        }

        #endregion

        #region Events

        /// <summary>
        /// Gets raised when new batch of Log messages arrieved
        /// </summary>
        public event LogsArrivedDelegate LogsArrived;
        /// <summary>
        /// Gets raised when new batch of Log messages to be merged
        /// </summary>
        public event LogsArrivedDelegate LogsUpdated;
        /// <summary>
        /// Gets raised when fetch operation has been complete
        /// </summary>
        public event FetchCompletedDelegate FetchCompleted;
        /// <summary>
        /// Gets raised at the start fetch operation 
        /// </summary>
        public event FetchCompletedDelegate FetchStarted;

        #endregion

        #region Public Methods

        /// <summary>
        /// Start Fetching log data from log repository 
        /// </summary>
        public abstract void FetchLogData();

        /// <summary>
        /// Retrieves the next analogyPage of data 
        /// </summary>
        /// <returns>log table</returns>
        public abstract DataTable GetNextPage();

        /// <summary>
        /// Retrieves the previous analogyPage of data
        /// </summary>
        /// <returns>log table</returns>
        public abstract DataTable GetPrevPage();

        /// <summary>
        /// Retrieves the Last analogyPage of data
        /// </summary>
        /// <returns>log table</returns>
        public abstract DataTable GetLastPage();

        /// <summary>
        /// Retrieves the total number of rows
        /// </summary>
        public abstract void UpdateTotalNumOfRows();

        /// <summary>
        /// Updates the first and last rows for the message label
        /// </summary>
        public abstract void AdvancePage(int numExistingRows, int numRowsAdded);

        /// <summary>
        /// Updates the first and last rows for the message label
        /// </summary>
        public abstract void RemainOnPage(int numRowsAdded);

        /// <summary>
        /// It applies filter and start fetching
        /// </summary>
        /// <param name="filterObj"></param>
        public virtual void ApplyFilter(FilterObject filterObj)
        {
            filterObject = filterObj;
            FetchLogData();
        }

        /// <summary>
        /// Stop Fetching log data from log repository if stil doing
        /// </summary>
        public virtual void Stop()
        {
            isDisabled = true;
        }

        /// <summary>
        /// Check for new logs in the view
        /// </summary>
        /// <returns>true if new logs present else false</returns>
        public abstract bool CheckForNewLogs();

        /// <summary>
        /// Returns updated logs in the datatable
        /// </summary>
        /// <returns></returns>
        public virtual DataTable GetUpdatedLogs()
        {
            return null;
        }

        /// <summary>
        /// Returns all logs in the datatable with filter till the last Message ID
        /// </summary>
        /// <returns></returns>
        public virtual DataTable GetAllLogs()
        {
            return null;
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the LogsArrived event
        /// </summary>
        /// <param name="logTable">DataTable</param>
        public void OnLogsArrived(DataTable logTable)
        {
            LogsArrived?.Invoke(logTable);
        }

        /// <summary>
        /// Raises the LogsUpdated event
        /// </summary>
        /// <param name="logTable">DataTable</param>
        protected void OnLogsUpdated(DataTable logTable)
        {
            LogsUpdated?.Invoke(logTable);
        }


        /// <summary>
        /// Raises the FetchCompleteDelegate event
        /// </summary>
        /// <param name="noOfLogs"></param>
        protected void OnFetchComplete(int noOfLogs)
        {
            if (isDisabled)
            {
                return;
            }

            FetchCompleted?.Invoke(noOfLogs);
        }
        /// <summary>
        /// Raises the FetchStartDelegate event
        /// </summary>
        /// <param name="noOfLogs"></param>
        protected void OnFetchStart(int noOfLogs)
        {
            if (isDisabled)
            {
                return;
            }

            FetchStarted?.Invoke(noOfLogs);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get or set if audio marker is unable or disable
        /// </summary>
        public bool IsAudioMarker
        {
            get => isAudioMarker;
            set => isAudioMarker = value;
        }

        /// <summary>
        /// [Read Only] Checks if it can dynamicaly update 
        /// </summary>
        public bool CanDynamicUpdate => canDynamicUpdate;

        /// <summary>
        /// [Read Only]Get total no of rows present 
        /// </summary>
        public int TotalNoOfRows => totalNoOfRows;


        /// <summary>
        /// [Read Only]Gets the first record number in the current analogyPage 
        /// </summary>
        public int FirstRow => firstRow;

        /// <summary>
		/// [Read Only] Gets the last record number in the current analogyPage 
        /// </summary>
		public int LastRow => lastRow;

        /// <summary>
        /// [Read Only] true if there exists a next analogyPage to the current 
        /// </summary>
        public bool IsNextPageExist => isNextPageExist;

        /// <summary>
        /// [Read Only] true if there exists a previous analogyPage to the current 
        /// </summary>
        public bool IsPrevPageExist => isPrevPageExist;

        /// <summary>
        /// [Read Only] Get the number of pages 
        /// </summary>
        public virtual int PageNumber => 1;

        /// <summary>
        /// [Read Only] Get the number of pages 
        /// </summary>
        public int NumExistingLogs
        {
            set => numExistingLogs = value;
            get => numExistingLogs;
        }


        /// <summary>
        /// [Read Only] Get the number of pages 
        /// </summary>
        public int PageSize { get; } = int.MaxValue;

        #endregion

        #region Member Variables

        protected FilterObject filterObject;
        protected bool canDynamicUpdate;
        protected bool isAudioMarker;
        protected int totalNoOfRows;
        protected bool isDisabled;
        protected bool isNextPageExist;
        protected bool isPrevPageExist;
        protected int numExistingLogs;

        /// <summary>
        /// Indicates the first row as displayed in
        /// the user message label
        /// </summary>
		protected int firstRow;

        /// <summary>
        /// Indicates the last row as displayed in
        /// the user message label
        /// </summary>
		protected int lastRow;

        #endregion
    }

    /// <summary>
    /// Represents all the filter criteria for log messages 
    /// </summary>
    public class FilterObject
    {
        #region Constructors

        /// <summary>
        /// Initializes the object with default filter criteria
        /// </summary>
        public FilterObject(bool isDefault)
        {
            this.isDefault = isDefault;
            dateTimeFrom = DateTime.Now.AddDays(-LogViewerLogicConstants.DayDifference);
            dateTimeTo = DateTime.Now;
            isDateTimeFrom = true;
            searchString = LogViewerLogicConstants.EmptyString;
            isDateTimeTo = false;

            try
            {
                InitializeFilter();
            }
            catch (Exception)
            {
                //InternalLog.Write("FilterObject.FilterObject() Error in Filter object " + ex.Message);
                //InternalLog.Write("FilterObject.FilterObject() Stack Trace " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Initializes the object with default filter criteria
        /// </summary>
        public FilterObject() : this(false)
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load the items from the default filter
        /// </summary>
        private void InitializeFilter()
        {
            // Log Details
            logModules = LogProperties.LogModuleLookUp.GetStringValues();
            logClasses = LogProperties.LogClassLookUp.GetStringValues();
            logLevels = LogProperties.LogLevelLookUp.GetStringValues();
            logTargets = LogProperties.LogTargetLookUp.GetStringValues();

        }
        /// <summary>
        /// Returns a concanated string
        /// </summary>
        /// <param name="str1">string</param>
        /// <param name="str2">string</param>
        /// <param name="itemList"></param>
        /// <returns>returns a string like ( str1 str2 (item1,item2) )</returns>
        private string GetSQLLine(string str1, string str2, string[] itemList, LookUpTable lookUpTable)
        {
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.Append(str1);
            strBuilder.Append(" ");
            strBuilder.Append(str2);
            strBuilder.Append(" ");

            strBuilder.Append("(");
            try
            {
                for (int cnt = 0; cnt < itemList.Length; cnt++)
                {
                    if (cnt != 0)
                    {
                        strBuilder.Append(",");
                    }
                    strBuilder.Append((lookUpTable.GetValue(itemList[cnt]).ToString()));

                }

                strBuilder.Append(") ");
            }
            catch (Exception)
            {
                //InternalLog.Write("FilterObject.GetSQLLine(...) : " + ex.Message);
                return LogViewerLogicConstants.EmptyString;
            }

            return strBuilder.ToString();
        }

        /// <summary>
        /// Returns a concanated string
        /// </summary>
        /// <param name="str1">string</param>
        /// <param name="str2">string</param>
        /// <param name="itemList"></param>
        /// <returns>returns a string like ( str1 str2 (item1,item2) )</returns>
        private string GetFilterLine(string str1, string str2, string[] itemList)
        {
            StringBuilder strBuilder = new StringBuilder();

            strBuilder.Append(str1);
            strBuilder.Append(" ");
            strBuilder.Append(str2);
            strBuilder.Append(" ");

            strBuilder.Append("(");

            for (int cnt = 0; cnt < itemList.Length; cnt++)
            {
                if (cnt != 0)
                {
                    strBuilder.Append(",");
                }
                strBuilder.Append("'");
                strBuilder.Append(itemList[cnt].ToString());
                strBuilder.Append("'");
            }

            strBuilder.Append(") ");

            return strBuilder.ToString();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the SQL Filter string
        /// </summary>
        /// <returns></returns>
        public string GetSQLFilterString(string qucikFilterString)
        {
            StringBuilder strBuilder = new StringBuilder();
            bool isFirstClause = true;

            //string gridFilterString = GetGridFilterString(qucikFilterString);

            try
            {
                strBuilder.Append(" ");

                // Filter based on Log Time
                if (isDateTimeFrom)
                {
                    strBuilder.Append("LogTime > ");
                    strBuilder.Append(dateTimeFrom.Ticks.ToString());
                    strBuilder.Append(" ");
                    isFirstClause = false;
                }
                if (isDateTimeTo)
                {
                    if (!isFirstClause)
                    {
                        strBuilder.Append(LogViewerLogicConstants.ANDString);
                        isFirstClause = false;
                    }
                    strBuilder.Append("LogTime < ");
                    strBuilder.Append(dateTimeTo.Ticks.ToString());
                    strBuilder.Append(" ");
                }

                // Filter based on Log Modules
                if (logModules.Length == 0)
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Module ..."));
                }
                else
                {
                    int count = 0;
                    bool showAllModules = true;
                    string[] selectedModules = new string[logModules.Length];

                    for (int i = 0; i < logModules.Length; i++)
                    {
                        selectedModules[count++] = logModules[i];
                    }
                    count = 0;
                    showAllModules = ((logModules.Length == 1) && (string.CompareOrdinal(logModules[0], "All") == 0));
                    if (!showAllModules)
                    {
                        strBuilder.Append(LogViewerLogicConstants.ANDString);
                        strBuilder.Append(GetSQLLine("ModuleId", "IN", selectedModules, LogProperties.LogModuleLookUp));
                    }
                }

                // Filter based on Log Level
                if (logLevels.Length != 0)
                {
                    if (LogLevels.Length < LogProperties.LogLevelLookUp.Length)
                    {
                        if (!isFirstClause)
                        {
                            strBuilder.Append(LogViewerLogicConstants.ANDString);
                            isFirstClause = false;
                        }
                        strBuilder.Append(GetSQLLine(LogViewerLogicConstants.DBLogLevelColumn, "IN", logLevels, LogProperties.LogLevelLookUp));
                    }
                }
                else
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Level ..."));
                }

                // Filter based on Log Class 
                if (logClasses.Length != 0)
                {
                    if (logClasses.Length < LogProperties.LogClassLookUp.Length)
                    {
                        if (!isFirstClause)
                        {
                            strBuilder.Append(LogViewerLogicConstants.ANDString);
                            isFirstClause = false;
                        }
                        strBuilder.Append(GetSQLLine(LogViewerLogicConstants.DBLogClassColumn, "IN", logClasses, LogProperties.LogClassLookUp));
                    }
                }
                else
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Class ... "));
                }

                // Filter based on Log Targets 
                if (logTargets.Length != 0)
                {
                    if (logTargets.Length < LogProperties.LogTargetLookUp.Length)
                    {
                        if (!isFirstClause)
                        {
                            strBuilder.Append(LogViewerLogicConstants.ANDString);
                            isFirstClause = false;
                        }
                        strBuilder.Append(GetSQLLine("TargetId", "IN", logTargets, LogProperties.LogTargetLookUp));
                    }
                }
                else
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Target ... "));
                }

                // Filter based on string pattern in message text
                if (searchString != LogViewerLogicConstants.EmptyString)
                {
                    if (!isFirstClause)
                    {
                        strBuilder.Append(LogViewerLogicConstants.ANDString);
                        isFirstClause = false;
                    }
                    strBuilder.Append("TextMessage LIKE '%");
                    strBuilder.Append(searchString);
                    strBuilder.Append("%' ");
                }
            }
            catch (Exception)
            {
                //InternalLog.Write("FilterObject.GetSQLFilterString() : Error in Getting SQL String " + ex.Message);
                //InternalLog.Write("FilterObject.GetSQLFilterString()  Stack Trace: " + ex.StackTrace);
                throw;
            }

            #region Adding grid filter

            //if (gridFilterString != LogViewerLogicConstants.EmptyString)
            //{
            //    if (!isFirstClause)
            //    {
            //        strBuilder.Append(LogViewerLogicConstants.ANDString);
            //        isFirstClause = false;
            //    }

            //    strBuilder.Append("(" + gridFilterString + ")");
            //}

            #endregion

            return strBuilder.ToString();
        }
        /// <summary>
        /// Returns the filter string for the data table
        /// </summary>
        /// <returns></returns>
        public string GetDataTableFilterString()
        {
            StringBuilder strBuilder = new StringBuilder();
            try
            {
                strBuilder.Append(" ");

                // Filter based on Log Class 
                if (logClasses.Length != 0)
                {
                    strBuilder.Append(GetFilterLine(LogViewerLogicConstants.DataGridClassColumn, "IN", logClasses));
                }
                else
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Class ..."));
                }

                // Filter based on Log Level
                if (logLevels.Length != 0)
                {
                    strBuilder.Append(LogViewerLogicConstants.ANDString);
                    strBuilder.Append(GetFilterLine(LogViewerLogicConstants.DataGridLevelColumn, "IN", logLevels));
                }
                else
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Level ... "));
                }

                // Filter based on Log Targets 
                if (logTargets.Length != 0)
                {
                    strBuilder.Append(LogViewerLogicConstants.ANDString);
                    strBuilder.Append(GetFilterLine(LogViewerLogicConstants.DataGridTargetNameColumn, "IN", logTargets));
                }
                else
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Target ... "));
                }


                // Filter based on Log Modules
                if (logModules.Length == 0)
                {
                    throw (new Exception("Invalid Filter : No Logs without having Log Module ... "));
                }
                else
                {
                    int count = 0;
                    bool showAllModules = true;
                    string[] selectedModules = new string[logModules.Length];

                    for (int i = 0; i < logModules.Length; i++)
                    {
                        selectedModules[count++] = logModules[i];
                    }
                    count = 0;
                    showAllModules = ((logModules.Length == 1) && (string.Compare(logModules[0], "All") == 0)) ? true : false;
                    if (!showAllModules)
                    {
                        strBuilder.Append(LogViewerLogicConstants.ANDString);
                        strBuilder.Append(GetFilterLine(LogViewerLogicConstants.DataGridModuleNameColumn, "IN", selectedModules));
                    }
                }

                // Filter based on Log Time
                if (isDateTimeFrom)
                {
                    strBuilder.Append(LogViewerLogicConstants.ANDString);
                    strBuilder.Append(LogViewerLogicConstants.DataGridDateColumn);
                    strBuilder.Append(" > ");
                    strBuilder.Append("#");
                    strBuilder.Append(dateTimeFrom.ToString());
                    strBuilder.Append("#");
                    strBuilder.Append(" ");

                }
                if (isDateTimeTo)
                {
                    strBuilder.Append(LogViewerLogicConstants.ANDString);
                    strBuilder.Append(LogViewerLogicConstants.DataGridDateColumn);
                    strBuilder.Append(" < ");
                    strBuilder.Append("#");
                    strBuilder.Append(dateTimeTo.ToString());
                    strBuilder.Append("#");
                    strBuilder.Append(" ");
                }

                // Filter based on string pattern in message text
                if (searchString != LogViewerLogicConstants.EmptyString)
                {
                    strBuilder.Append(LogViewerLogicConstants.ANDString);
                    strBuilder.Append("TextMessage LIKE '%");
                    strBuilder.Append(searchString);
                    strBuilder.Append("%'");
                }
            }
            catch (Exception)
            {
                //InternalLog.Write("FilterObject.GetDataTableFilterString() : " + ex.Message);
                return LogViewerLogicConstants.EmptyString;
            }

            return strBuilder.ToString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get or set True if Default filter and False if not
        /// </summary>
        public bool IsDefault
        {
            get => isDefault;
            set => isDefault = value;
        }

        /// <summary>
        /// Get or set the log modules of the filter criteria
        /// </summary>
        public string[] LogModules
        {
            get => logModules;
            set => logModules = value;
        }

        /// <summary>
        /// Get or set the log levels of the filter criteria
        /// </summary>
        public string[] LogLevels
        {
            get => logLevels;
            set => logLevels = value;
        }

        /// <summary>
        /// Get or set the log Classes of the filter criteria
        /// </summary>
        public string[] LogClasses
        {
            get => logClasses;
            set => logClasses = value;
        }

        /// <summary>
        /// Get or set the log Targets of the filter criteria
        /// </summary>
        public string[] LogTargets
        {
            get => logTargets;
            set => logTargets = value;
        }

        /// <summary>
        /// Get or set the From date of the filter criteria
        /// </summary>
        public DateTime DateTimeFrom
        {
            get => dateTimeFrom;
            set => dateTimeFrom = value;
        }

        /// <summary>
        /// Get or set the To date of the filter criteria
        /// </summary>
        public DateTime DateTimeTo
        {
            get => dateTimeTo;
            set => dateTimeTo = value;
        }

        /// <summary>
        /// Get or set binary value which indicate if DateTimeFrom is enabled for filter
        /// </summary>
        public bool IsDateTimeFrom
        {
            get => isDateTimeFrom;
            set => isDateTimeFrom = value;
        }

        /// <summary>
        /// Get or set binary value which indicate if DateTimeTo is enabled for filter
        /// </summary>
        public bool IsDateTimeTo
        {
            get => isDateTimeTo;
            set => isDateTimeTo = value;
        }

        /// <summary>
        /// Get or set the search string of the filter criteria
        /// </summary>
        public string SearchString
        {
            get => searchString;
            set => searchString = value;
        }


        #endregion

        #region Member Variables

        // Log Details 
        private string[] logModules;
        private string[] logClasses;
        private string[] logLevels;
        private string[] logTargets;

        // Date Time
        private DateTime dateTimeFrom;
        private DateTime dateTimeTo;

        // Seatch String
        private string searchString;

        // Specifying default filtering or not
        bool isDefault;

        //DateTime filtering specifer 
        bool isDateTimeTo;
        bool isDateTimeFrom;

        #endregion
    }

    /// <summary>
    /// Class representing serializable filter object
    /// </summary>
    [Serializable]
    internal class Filter
    {
        #region Constructor

        public Filter()
        {
            LogClasses = null;
            LogLevels = null;
            LogModules = null;
            LogTargets = null;
        }
        #endregion

        #region Member Variables

        public string[] LogModules;
        public string[] LogClasses;
        public string[] LogLevels;
        public string[] LogTargets;

        // Date Time
        public TimeSpan DateTimeFrom;
        public TimeSpan DateTimeTo;
        public bool IsDateTimeFrom;
        public bool IsDateTimeTo;

        // Seatch String
        public string SearchString;

        #endregion
    }
    /// <summary>
    /// Represents LogViewer Logic project Strings
    /// </summary>
    public class LogViewerLogicConstants
    {
        #region Constants
        /// <summary>
        /// Sql Connection String
        /// </summary>
		public const string SqlCon = @"Data Source = ilqhfaatc1msctv\SQLEXPRESS;Initial Catalog =LogDBDefault;Integrated Security=True;pooling='true';context connection=false";
        //public const string SqlCon = @"Data Source = localhost\SQLEXPRESS;Initial Catalog =LogDBDefault;Integrated Security=True;pooling='true';context connection=false";
        /// <summary>
        /// Runtime Log view String 
        /// </summary>
        public const string RuntimeLogView = "LogViewRunTime";

        /// <summary>
        /// Empty  view String 
        /// </summary>
        public const string EmptyString = "";

        /// <summary>
        ///Create Runtime View Command
        /// </summary>
        public const string CreateRuntimeViewComand = " EXEC sp_createRunTimeView  ";

        /// <summary>
        ///Create View Command
        /// </summary>
        public const string CreateViewComand = " EXEC sp_createView  ";

        /// <summary>
        /// Truncate Table Command
        /// </summary>
        public const string TruncateTableCommand = " TRUNCATE TABLE LogMessage_Temp ";

        /// <summary>
        /// AND String 
        /// </summary>
        public const string ANDString = " AND ";

        /// <summary>
        /// DataGrid Table Name String 
        /// </summary>
        public const string TableName = "MessageTable";


        /// <summary>
        /// DataGrid Date Column
        /// </summary>
        public const string DataGridDateColumn = "Date";

        /// <summary>
        /// DataGrid MessageId Column
        /// </summary>
        public const string DataGridMessageIdColumn = "MessageId";

        /// <summary>
        /// DataGrid TextMessage Column
        /// </summary>
        public const string DataGridTextMessageColumn = "TextMessage";

        /// <summary>
        /// DataGrid DictionaryId Column
        /// </summary>
        public const string DataGridDictionaryIdColumn = "DictionaryId";

        /// <summary>
        /// DataGrid DebugInfo Column
        /// </summary>
        public const string DataGridDebugInfoColumn = "DebugInfo";

        /// <summary>
        /// DataGrid AdditionalInfo Column
        /// </summary>
        public const string DataGridAdditionalInfoColumn = "AdditionalInfo";

        /// <summary>
        /// DataGrid DictionaryIdText Column
        /// </summary>
        public const string DataGridDictionaryIdTextColumn = "DictionaryIdText";

        /// <summary>
        /// DataGrid DictFormatText Column
        /// </summary>
        public const string DataGridDictFormatTextColumn = "DictFormatText";

        /// <summary>
        /// DataGrid BlobInfo Column
        /// </summary>
        public const string DataGridBlobInfoColumn = "BlobInfo";

        /// <summary>
        /// DataGrid Level Column
        /// </summary>
        public const string DataGridLevelColumn = "Level";

        /// <summary>
        /// DataGrid Class Column
        /// </summary>
        public const string DataGridClassColumn = "Class";

        /// <summary>
        /// DataGrid Binary Data Column
        /// </summary>
        public const string DataGridBinaryDataColumn = "Binary Data";

        /// <summary>
        /// DataGrid ModuleName Column
        /// </summary>
        public const string DataGridModuleNameColumn = "ModuleName";

        /// <summary>
        /// DataGrid TargetName Column
        /// </summary>
        public const string DataGridTargetNameColumn = "TargetName";

        /// <summary>
        /// DataGrid Thread ID Column
        /// </summary>
        public const string DataGridThreadIDColumn = "Thread ID";

        /// <summary>
        /// DataGrid Process ID Column
        /// </summary>
        public const string DataGridProcessIDColumn = "Process ID";

        /// <summary>
        /// DataGrid Process Name Column
        /// </summary>
        public const string DataGridProcessNameColumn = "Process Name";

        /// <summary>
        /// DataGrid Method Name Column
        /// </summary>
        public const string DataGridMethodNameColumn = "Method Name";

        /// <summary>
        /// DataGrid File Name Column
        /// </summary>
        public const string DataGridFileNameColumn = "File Name";

        /// <summary>
        /// DataGrid Line Number Column
        /// </summary>
        public const string DataGridLineNumberColumn = "Line Number";

        /// <summary>
        /// DataGrid Stack Trace Column
        /// </summary>
        public const string DataGridStackTraceColumn = "Stack Trace";

        /// <summary>
        /// DataGrid LogTime Column
        /// </summary>
        public const string DataGridLogTimeColumn = "LogTime";

        /// <summary>
        /// DataGrid LogLevel Column
        /// </summary>
        public const string DataGridLogLevelColumn = "LogLevel";

        /// <summary>
        /// DataGrid LogClass Column
        /// </summary>
        public const string DataGridLogClassColumn = "LogClass";

        /// <summary>
        /// DataGrid ModuleId Column
        /// </summary>
        public const string DataGridModuleIdColumn = "ModuleId";

        /// <summary>
        /// DataGrid TargetId Column
        /// </summary>
        public const string DataGridTargetIdColumn = "TargetId";

        /// <summary>
        /// Message ID Column
        /// </summary>
        public const string DBMessageIDColumn = "MessageID";

        /// <summary>
        /// Row Num Column
        /// </summary>
        public const string DBRowNumColumn = "RowNum";

        /// <summary>
        /// Text Message Column
        /// </summary>
        public const string DBTextMesageColumn = "TextMessage";

        /// <summary>
        /// LogLevel Column
        /// </summary>
        public const string DBLogLevelColumn = "LogLevel";

        /// <summary>
        /// LogClass Column
        /// </summary>
        public const string DBLogClassColumn = "LogClass";

        /// <summary>
        /// Binary Data Column
        /// </summary>
        public const string DBBinaryDataColumn = "BinaryData";

        /// <summary>
        /// Date Time Column
        /// </summary>
        public const string DBDateTimeColumn = "DateTime";

        /// <summary>
        /// ModuleName Column
        /// </summary>
        public const string DBModuleNameColumn = "ModuleName";

        /// <summary>
        /// TargetName Column
        /// </summary>
        public const string DBTargetNameColumn = "TargetName";

        /// <summary>
        /// AdditionalInfo Column
        /// </summary>
        public const string DBAdditionalInfoColumn = "AdditionalInfo";

        /// <summary>
        /// ProcessName Column
        /// </summary>
        public const string DBProcessNameColumn = "ProcessName";

        /// <summary>
        /// ProcessID Column
        /// </summary>
        public const string DBProcessIDColumn = "ProcessID";

        /// <summary>
        /// ThreadID Column
        /// </summary>
        public const string DBThreadIDColumn = "ThreadID";

        /// <summary>
        /// FileName Column
        /// </summary>
        public const string DBFileNameColumn = "FileName";

        /// <summary>
        /// MethodName Column
        /// </summary>
		public const string DBMethodNameColumn = "MethodName";

        /// <summary>
        /// LineNumber Column
        /// </summary>
        public const string DBLineNumberColumn = "LineNumber";

        /// <summary>
        /// StackTrace Column
        /// </summary>
		public const string DBStackTraceColumn = "StackTrace";

        /// <summary>
        /// LogTime Column
        /// </summary>
		public const string DBLogTimeColumn = "LogTime";

        /// <summary>
        /// DebugInfo Column
        /// </summary>
		public const string DBDebugInfoColumn = "DebugInfo";

        /// <summary>
        /// ModuleID Column
        /// </summary>
        public const string DBModuleIDColumn = "ModuleID";

        /// <summary>
        /// DictFormatText Column
        /// </summary>
		public const string DBDictFormatTextColumn = "DictFormatText";

        /// <summary>
        /// DictionaryId Column
        /// </summary>
		public const string DBDictionaryIdColumn = "DictionaryId";

        /// <summary>
        /// DictionaryIdText Column
        /// </summary>
		public const string DBDictionaryIdTextColumn = "DictionaryIdText";

        /// <summary>
        /// TargetID Column
        /// </summary>
		public const string DBTargetIDColumn = "TargetID";

        /// <summary>
        /// RowNum Column
        /// </summary>
        public const string RowNum = "RowNum";

        /// <summary>
        /// BlobInfo column
        /// </summary>
        public const string DBBlobInfoColumn = "BlobInfo";

        /// <summary>
        /// TargetID Column
        /// </summary>
        public const int DayDifference = 1;

        /// <summary>
        /// Warning message for filter results in more than 50000 rows
        /// </summary>
        public const string MessageWarning = "Results 1 - 50000 of about ";

        /// <summary>
        /// Default filter file name
        /// </summary>
        public const string DefaultFilterFileName = "DefaultFilter.cfg";

        /// <summary>
        /// Maximum number of rows to be displayed in the first analogyPage
        /// </summary>
        public const int DefaultPageSize = 20000;

        /// <summary>
        /// starting string of the Log View in the DB
        /// </summary>
        public const string ViewName = "LogView";

        /// <summary>
        /// User message format 1
        /// </summary>
        public const string UserMsgFormat1 = "Showing {0} to {1} of {2}";

        /// <summary>
        /// User Messages No Logs
        /// </summary>
		public const string UserMsgNoMessages = "Showing 0 to 0 of 0 ";

        /// <summary>
        /// User Messages Loading
        /// </summary>
        public const string UserMsgLoading = "Loading...";

        /// <summary>
        /// File Path for filter
        /// </summary>
        public const string FilterFilePath = @"C:\Pms\Config\System\Log\";

        /// <summary>
        /// Default Service Directory
        /// </summary>
        public const string DefaultServiceDirectory = @"C:\Pms\System";

        /// <summary>
        /// Default group in Filter
        /// </summary>
        public const string DefaultGroup = "Default";

        /// <summary>
        /// LogClasses group in Filter
        /// </summary>
        public const string LogClassesGroup = "LogClasses";

        /// <summary>
        /// LogLevels group in Filter
        /// </summary>
        public const string LogLevelsGroup = "LogLevels";

        /// <summary>
        /// LogTargets group in Filter
        /// </summary>
        public const string LogTargetsGroup = "LogTargets";

        /// <summary>
        /// LogModules group in Filter
        /// </summary>
        public const string LogModulesGroup = "LogModules";

        /// <summary>
        /// Regular expression to check for invalid characters
        /// </summary>
        public const string InvalidCharRegex = "[^a-zA-Z0-9_\\*;\\. ]";

        #endregion
    }

    class Queries
    {
        /// <summary>
        /// SQL String to fetch the updated Logs
        /// {0} -> View Name in the DB
        /// {1} -> Latest Message ID
        /// </summary>
        public const string GetUpdatedLogs = "SELECT TOP {0} * FROM {1} ORDER BY MessageId ASC ";

        /// <summary>
        /// SQL String to fetch first analogyPage of Log View
        /// {0} -> View Name in the DB
        /// {1} -> Latest Log Time
        /// </summary>
        public const string GetAllLogsByLogTime = "SELECT * FROM {0} WHERE LogTime <= {1} ORDER BY MessageId DESC ";

        /// <summary>
        /// SQL String to fetch first analogyPage of Log View
        /// {0} -> View Name in the DB
        /// {1} -> Latest Message ID
        /// </summary>
        public const string GetAllLogsByMsgId = "SELECT * FROM {0} WHERE MessageId <= {1} ORDER BY MessageId DESC ";

        /// <summary>
        /// SQL String to fetch the Max Message ID
        /// </summary>
        public const string GetMaxMsgId = "SELECT MAX(MessageId) FROM ";

        /// <summary>
        /// SQL String to fetch Next analogyPage of Log View
        /// {0} -> Number of messages
        /// {1} -> View Name in the DB
        /// {2} -> MessageID
        /// </summary>
        public const string GetNextPage = "SELECT TOP {0} * FROM {1} WHERE MessageId > {2} ORDER BY MessageId ASC ";

        /// <summary>
        /// SQL String to fetch Previous analogyPage of Log View
        /// {0} -> Number of messages
        /// {1} -> View Name in the DB
        /// {2} -> MessageID
        /// </summary>
        public const string GetPrevPage = "SELECT TOP {0} * FROM {1} WHERE MessageId < {2} ORDER BY MessageId DESC ";

        /// <summary>
        /// SQL String to fetch first analogyPage of Log View
        /// {0} -> Message Id
        /// {1} -> View Name in the DB
        /// </summary>
        public const string GetLastPage = "SELECT TOP {0} * FROM {1} ORDER BY MessageId DESC ";

        /// <summary>
        /// SQL String to fetch last analogyPage of Log View
        /// {0} -> Message Id
        /// {1} -> View Name in the DB
        /// </summary>
        public const string GetNextLastPage = "SELECT * FROM {0} WHERE MessageId > {1} ORDER BY MessageId ASC ";

    }

    /// <summary>
    /// It holds log details like Log Class, Log Level, Log Modules and Targets
    /// </summary>
    public static class LogProperties
    {
        #region Static Constructor

        static LogProperties()
        {
            LogLevelLookUp = new LookUpTable();
            LogClassLookUp = new LookUpTable();
            LogModuleLookUp = new LookUpTable();
            LogTargetLookUp = new LookUpTable();

            IntializeLogDetails();

        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Intialize the look up tables 
        /// </summary>
        private static void IntializeLogDetails()
        {
            // Filling Log Class look up table 
            string[] classes = Enum.GetNames(typeof(LogClass));
            for (int cnt = 0; cnt < classes.Length; cnt++)
            {
                LogClassLookUp.Add(cnt, classes[cnt]);
            }


            // Filling Log Levels look up table 
            string[] levels =
            {
                "Critical", "Error ", "Warning", "Event", "Event",
                "Verbose", "Debug", "Disabled", "DictionaryDefault"
            };

            LogLevelLookUp.Add(0, levels[0]);
            LogLevelLookUp.Add(1, levels[1]);
            LogLevelLookUp.Add(2, levels[2]);
            LogLevelLookUp.Add(3, levels[3]);
            LogLevelLookUp.Add(4, levels[4]);
            LogLevelLookUp.Add(5, levels[5]);
            LogLevelLookUp.Add(6, levels[6]);
            LogLevelLookUp.Add(7, levels[7]);
            CTModuleLoader moduleLoader = new CTModuleLoader();
            Dictionary<int, string> lookUpTable = moduleLoader.GetModuleLookUpTable();

            foreach (KeyValuePair<int, string> keyValuePair in lookUpTable)
            {
                LogModuleLookUp.Add(keyValuePair.Key, keyValuePair.Value);
            }



            // Filling Log Target look up table 
            string[] targets = Enum.GetNames(typeof(Target));
            for (int cnt = 0; cnt < targets.Length; cnt++)
            {
                LogTargetLookUp.Add(cnt, targets[cnt]);
            }

        }

        #endregion

        #region Public Static Variables

        /// <summary>
        /// Look up table for Log Levels
        /// </summary>
        public static LookUpTable LogLevelLookUp;
        /// <summary>
        /// Look up table for Log Class
        /// </summary>
        public static LookUpTable LogClassLookUp;
        /// <summary>
        /// Look up table for Log Modules
        /// </summary>
        public static LookUpTable LogModuleLookUp;
        /// <summary>
        /// Look up table for Log Targets
        /// </summary>
        public static LookUpTable LogTargetLookUp;

        #endregion
    }
    public class LookUpTable
    {
        #region Constructors

        /// <summary>
        /// Initializes class variables
        /// </summary>
        public LookUpTable()
        {
            intLookUp = new Dictionary<int, string>();
            stringLookUp = new Dictionary<string, int>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Get the number of items in the look up table
        /// </summary>
        public int Length => intLookUp.Count;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the key value pair to the look up table
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(int key, string value)
        {
            AddPair(key, value);
        }

        /// <summary>
        /// Adds the key value pair to the look up table
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        public void Add(string key, int value)
        {
            AddPair(value, key);
        }

        /// <summary>
        /// Returns the corresponding int value
        /// </summary>
        /// <param name="key"></param>
        /// <returns>int</returns>
        public int GetValue(string key)
        {
            if (stringLookUp.ContainsKey(key))
            {
                return stringLookUp[key];
            }
            else
            {
                throw (new Exception("Key not present"));
            }
        }

        /// <summary>
        /// Returns the corresponding string value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValue(int key)
        {
            if (key == 4)
            {
                key = 3;
            }

            if (intLookUp.ContainsKey(key))
            {
                return intLookUp[key];
            }
            else
            {
                throw (new Exception("Key not present"));
            }
        }

        /// <summary>
        /// Returns array of integer values
        /// </summary>
        /// <returns>int array</returns>
        public int[] GetIntValues()
        {
            int[] intValues = new int[intLookUp.Count];

            intLookUp.Keys.CopyTo(intValues, 0);

            return intValues;
        }

        /// <summary>
        /// Returns array of string values
        /// </summary>
        /// <returns>string array</returns>
        public string[] GetStringValues()
        {
            string[] stringValues = new string[stringLookUp.Count];

            stringLookUp.Keys.CopyTo(stringValues, 0);

            return stringValues;
        }

        /// <summary>
        /// Determine whether it contains the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>bool</returns>
        public bool Contains(int key)
        {
            return intLookUp.ContainsKey(key);
        }

        /// <summary>
        /// Determine whether it contains the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>bool</returns>
        public bool Contains(string key)
        {
            return stringLookUp.ContainsKey(key);
        }

        /// <summary>
        /// Clears the look up table
        /// </summary>
        public void Clear()
        {
            intLookUp.Clear();
            stringLookUp.Clear();
        }

        #endregion

        #region Private Methods

        private void AddPair(int key, string value)
        {
            try
            {
                while (stringLookUp.ContainsKey(value))
                {
                    value = value + "  ";
                }
                stringLookUp.Add(value, key);
                intLookUp.Add(key, value);
            }
            catch (Exception ex)
            {
                if (intLookUp.ContainsKey(key))
                {
                    intLookUp.Remove(key);
                }
                if (stringLookUp.ContainsKey(value))
                {
                    stringLookUp.Remove(value);
                }

                throw ex;
            }

        }

        #endregion

        #region Member Variables

        private Dictionary<int, string> intLookUp = new Dictionary<int, string>();
        private Dictionary<string, int> stringLookUp = new Dictionary<string, int>();

        #endregion

    }

    /// <summary>
    /// Responsible for fetching runtime log data from the default Repository
    /// </summary>
    public class DynamicLogFetcher : IDisposable
    {
        #region Public Methods

        /// <summary>
        /// Initializes the DynamicLogFetcher
        /// </summary>
        public DynamicLogFetcher(LogFetcher logFetcher)
        {
            this.logFetcher = logFetcher;

            updateInterval = 2000; // Make update in every one minute (Hard Coded)

            currentState = DynamicUpdateState.NotStarted;

            if ((this.logFetcher == null) || (!this.logFetcher.CanDynamicUpdate))
            {
                currentState = DynamicUpdateState.NotStarted;
            }
            else
            {
                dynamicUpdateThread = new Thread(Update);
                dynamicUpdateThread.IsBackground = true;
            }
        }

        /// <summary>
        /// Start dynamic update
        /// </summary>
        /// <returns>current state</returns>
        public DynamicUpdateState Start(int interval)
        {
            updateInterval = interval;

            switch (currentState)
            {
                case DynamicUpdateState.NotStarted:
                    Initialize();
                    break;

                case DynamicUpdateState.Started:
                    // Do nothing
                    break;

                case DynamicUpdateState.Paused:
                    currentState = DynamicUpdateState.Started;
                    break;
            }

            return currentState;
        }

        /// <summary>
        /// Pause dynamic update
        /// </summary>
        /// <returns></returns>
        public DynamicUpdateState Pause()
        {
            switch (currentState)
            {
                case DynamicUpdateState.NotStarted:
                    // Do nothing
                    break;

                case DynamicUpdateState.Started:
                    currentState = DynamicUpdateState.Paused;
                    break;

                case DynamicUpdateState.Paused:
                    // Do nothing
                    break;
            }

            return currentState;
        }

        /// <summary>
        /// Resume dynamic update from paused state
        /// </summary>
        /// <param name="interval">update interval in milli sec</param>
        /// <returns></returns>
        public DynamicUpdateState Resume(int interval)
        {
            updateInterval = interval;

            switch (currentState)
            {
                case DynamicUpdateState.NotStarted:
                    // Do nothing
                    break;

                case DynamicUpdateState.Started:
                    // Do nothing
                    break;

                case DynamicUpdateState.Paused:
                    currentState = DynamicUpdateState.Started;
                    break;
            }

            return currentState;
        }

        /// <summary>
        /// Resume dynamic update from paused state
        /// </summary>
        /// <returns></returns>
        public DynamicUpdateState Resume()
        {
            return Resume(updateInterval);
        }

        /// <summary>
        /// Implemention of IDisposable.
        /// </summary>
        public void Dispose()
        {
            currentState = DynamicUpdateState.Paused;
            // Clean up operation
            if (dynamicUpdateThread.IsAlive)
            {
                dynamicUpdateThread.Abort();
            }

            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // If disposing equals true, dispose all managed resources.
                if (disposing)
                {

                }
                // Release unmanaged resources
            }
            disposed = true;
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Make update on regular basis
        /// </summary>
        private void Update()
        {
            while (true)
            {
                if (currentState == DynamicUpdateState.Started)
                {
                    DataTable logTable = null;
                    try
                    {
                        if (!logFetcher.CheckForNewLogs())
                        {
                            continue;
                        }
                        logTable = logFetcher.GetUpdatedLogs();
                    }
                    catch (Exception)
                    {
                        //InternalLog.Write("DynamicLogFetcher.Update() : " + ex.Message);
                        currentState = DynamicUpdateState.NotStarted;

                    }

                    int noOfRow = logFetcher.TotalNoOfRows;

                    OnUpdateComplete(logTable, noOfRow);
                }
                else
                {
                    return;
                }
                Thread.Sleep(updateInterval);

            }
        }


        /// <summary>
        /// Initializes the dynamic update
        /// </summary>
        private void Initialize()
        {
            if (currentState != DynamicUpdateState.NotStarted)
            {
                return;
            }

            if (!dynamicUpdateThread.IsAlive)
            {
                dynamicUpdateThread.Start();
            }
            currentState = DynamicUpdateState.Started;
        }

        /// <summary>
        /// Raises UpdateComplete event
        /// </summary>
        /// <param name="logTable"></param>
        /// <param name="totalNoOfRows"></param>
        private void OnUpdateComplete(DataTable logTable, int totalNoOfRows)
        {
            if ((logTable == null) || logTable.Rows.Count == 0)
            {
                return;
            }

            // Raising the event
            UpdateCompleted?.Invoke(logTable, totalNoOfRows);
        }

        #endregion Private Methods

        #region Properties

        /// <summary>
        /// Get the current state
        /// </summary>
        public DynamicUpdateState CurrentState => currentState;

        /// <summary>
        /// Gets the refresh Interval
        /// </summary>
        public int UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = value;
        }

        #endregion Properties

        #region Events

        /// <summary>
        /// Gets raised when logs are updated from the repository
        /// </summary>
        public event UpdateCompletedDelegate UpdateCompleted;

        #endregion Events

        #region Member Variables

        private bool disposed;
        private Thread dynamicUpdateThread;
        private int updateInterval;
        private DynamicUpdateState currentState;
        private LogFetcher logFetcher;

        #endregion Member Variables
    }
    public enum DynamicUpdateState
    {
        /// <summary>
        /// The dynamic update is yet to be started 
        /// </summary>
        NotStarted,

        /// <summary>
        /// The dynamic update is started
        /// </summary>
        Started,

        /// <summary>
        /// The dynamic update is paused
        /// </summary>
        Paused
    }
}

