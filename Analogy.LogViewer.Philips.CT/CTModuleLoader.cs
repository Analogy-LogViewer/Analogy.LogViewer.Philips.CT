using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Analogy.LogViewer.Philips.CT
{
    /// <summary>
    /// Loads the module id assemblies created by the users/developers.
    /// This class should be used only the Log viewer application and log configurator .
    /// </summary>

    class CTModuleLoader
    {
        /// <summary>
        /// Privates...
        /// </summary>
        private List<int> eModuleIds;
        private Dictionary<int, string> eModuleIDMap;
        private string systemFolder = @"c:\pms\system";
        /// <summary>
        /// Constructor. Loads module id assemblies.
        /// </summary>
        public CTModuleLoader()
        {
            eModuleIds = new List<int>();
            eModuleIDMap = new Dictionary<int, string>();
            eModuleIds.Add(0);
            eModuleIDMap.Add(0, "Not Found");
            try
            {
                string[] moduleIdFiles = Directory.GetFiles(systemFolder, @"*LogModule*");
                foreach (string aFile in moduleIdFiles)
                {
                    string extn = Path.GetExtension(aFile);
                    if (extn == ".dll")
                    {
                        try
                        {
                            Assembly anAssembly = Assembly.LoadFile(Path.GetFullPath(aFile));
                            Type[] types = anAssembly.GetTypes();
                            foreach (Type aType in types)
                            {
                                // The assumption was *LogModule* assembly contains only one class
                                // To allow adding another classes to assembly 
                                // we need to go over all types and find the rhight one
                                try
                                {
                                    if (aType.GetInterface("ILogModule") != null)
                                    {
                                        object anObject = Activator.CreateInstance(aType);
                                        var ids = anObject.GetType().GetMethod("GetModuleIDs");
                                        Array mArray = (Array)ids.Invoke(anObject, null);
                                        foreach (int var in mArray)
                                        {
                                            eModuleIds.Add(var);
                                            eModuleIDMap.Add(var, (string)anObject.GetType().GetMethod("GetModuleName").Invoke(anObject, new object[] { var }));
                                        }
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    //todo
                                    //Log.WriteInternalLog("Error in ModuleLoader() Constructor with Error Code:" + Marshal.GetLastWin32Error());
                                    //Log.WriteInternalLog("Log: " + ex.Message);
                                    //continue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //todo
                            //Log.WriteInternalLog("Error in ModuleLoader() Constructor with Error Code:" + Marshal.GetLastWin32Error());
                            //Log.WriteInternalLog("ModuleLoader.ModuleLoader(): " + ex.Message);
                            //continue;
                        }
                    }
                }

            }
            catch (Exception)
            {
                //todo
                //Log.WriteInternalLog("ModuleLoader.ModuleLoader(): " + ex.Message);
                //Log.WriteInternalLog(ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of ints representing the module ids
        /// </summary>
        /// <returns>List of integers</returns>
        public List<int> GetModuleIDs()
        {
            return eModuleIds;
        }

        /// <summary>
        /// Returns string module names corresponding to an id.
        /// </summary>
        /// <param name="moduleID"></param>
        /// <returns>string</returns>
        public string GetModuleName(int moduleID)
        {
            string aModuleName = "";
            eModuleIDMap.TryGetValue(moduleID, out aModuleName);
            return aModuleName;
        }

        /// <summary>
        /// Returns module Id corresponding to a module name.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public int GetModuleID(string moduleName)
        {
            if (eModuleIDMap.ContainsValue(moduleName))
            {
                foreach (KeyValuePair<int, string> aPair in eModuleIDMap)
                {
                    if (aPair.Value == moduleName)
                        return aPair.Key;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns module look up dictionary
        /// </summary>
        /// <returns>Dictionary[int-string]</returns>
        public Dictionary<int, string> GetModuleLookUpTable()
        {
            return eModuleIDMap;
        }
    }


}
