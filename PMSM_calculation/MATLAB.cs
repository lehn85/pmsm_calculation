/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using MLApp;

namespace calc_from_geometryOfMotor
{
    public class MATLAB
    {
        private static readonly ILog log = LogManager.GetLogger("MatlabApp");

        // Default instance of MatlabHelper
        private static MATLAB defaultinstance = null;
        public static MATLAB DefaultInstance
        {
            get
            {
                if (defaultinstance == null)
                {
                    defaultinstance = new MATLAB();
                }

                return defaultinstance;
            }

            private set
            {
                defaultinstance = value;
            }
        }

        // MLApp object
        private MLApp.MLApp matlab;
        public MLApp.MLApp MatlabApp
        {
            get
            {
                return matlab;
            }
        }

        // constructor
        public MATLAB()
        {
            //matlab = new MLAppClass();
            matlab = new MLApp.MLApp();
        }

        #region Basic commands

        public String ExecuteCommand(String cmd, params object[] args)
        {
            String outstr = matlab.Execute(String.Format(cmd, args));
            log.Debug(outstr);
            return outstr;
        }

        public object GetVariable(String name, String workspace = "base")
        {
            try
            {
                object obj = matlab.GetVariable(name, workspace);
                return obj;
            }
            catch (Exception ex)
            {
                log.Error("Variable " + name + " may not exist");
                return null;
            }
        }

        public void SetVariable(String name, object data, String workspace = "base")
        {
            matlab.PutWorkspaceData(name, workspace, data);
        }

        public void Quit()
        {
            matlab.Quit();
        }

        public void MinimizeCommandWindow()
        {
            matlab.MinimizeCommandWindow();
        }

        public void MaximizeCommandWindow()
        {
            matlab.MaximizeCommandWindow();
        }

        public void ChangeWorkingFolder(String path)
        {
            ExecuteCommand("cd '{0}'", path);
        }        

        #endregion

        #region Model-system command

        /// <summary>
        /// Load system with name
        /// </summary>
        /// <param name="name"></param>
        public void load_system(String name)
        {
            ExecuteCommand("load_system('{0}')", name);
        }

        /// <summary>
        /// Close model
        /// </summary>
        /// <param name="name">Model name</param>
        /// <param name="exitflag">0 = don't save, 1 = save</param>
        public void close_system(String name, int exitflag = 0)
        {
            ExecuteCommand("close_system('{0}',{1})", name, exitflag);
        }

        /// <summary>
        /// Save or saveas if asname specified
        /// </summary>
        /// <param name="name"></param>
        /// <param name="asname"></param>
        public void save_system(String name, String asname = "")
        {
            if (asname == "")
                ExecuteCommand("save_system('{0}')", name);
            else ExecuteCommand("save_system('{0}','{1}')", name, asname);
        }

        public void set_param(String objname, String paramname, String value)
        {
            ExecuteCommand("set_param('{0}','{1}','{2}')", objname, paramname, value);
        }

        #endregion
    }
}
