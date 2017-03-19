/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using log4net;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using calc_from_geometryOfMotor.motor;
using calc_from_geometryOfMotor.motor.PMMotor;
using System.Collections;
using System.Reflection;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;
using fastJSON;
using NLua;

namespace calc_from_geometryOfMotor
{
    class ProjectManager
    {
        private static readonly ILog log = LogManager.GetLogger("PM");

        #region Instance - default

        private static ProjectManager instance;
        private ProjectManager() { }
        public static ProjectManager GetInstance()
        {
            if (instance == null)
            {
                instance = new ProjectManager();                
            }
            return instance;
        }

        #endregion

        #region Project Components

        // objects
        public AbstractMotor Motor { get; private set; }
        public IDictionary<String, AbstractTransientAnalyser> TransAnalysisGroup { get; private set; }
        public AbstractMMAnalyser mmAnalyser { get; private set; }
        public PM_DQCurrentAnalyser dqcurrentAnalyser { get; private set; }
        public AbstractStaticAnalyser staticAnalyser { get; private set; }
        public AbstractAnalyticalAnalyser analyticalAnalyser { get; private set; }
        public ParamSweeper pSweeper { get; private set; }

        // parameters collections
        public ParametersCollection MotorParams { get; private set; }
        // All of components of project: motor, analysis config,...        
        public IDictionary<String, object> Components;

        /// <summary>
        /// Refresh the list component
        /// </summary>
        /// <returns></returns>
        private void RefreshComponents()
        {
            Components = new Dictionary<String, object>();

            Components.Add("Motor\\General(" + Motor.GeneralParams.GetType().Name + ")", Motor.GeneralParams);
            Components.Add("Motor\\Rotor(" + Motor.Rotor.GetType().Name + ")", Motor.Rotor);
            Components.Add("Motor\\Stator(" + Motor.Stator.GetType().Name + ")", Motor.Stator);
            //TODO: material here maybe
            Components.Add("Motor\\Airgap(" + Motor.Airgap.GetType().Name + ")", Motor.Airgap);

            foreach (String ta_name in TransAnalysisGroup.Keys)
                Components.Add(ta_name, TransAnalysisGroup[ta_name]);

            Components.Add("MMAnalysis", mmAnalyser);

            Components.Add("DQCurrentAnalysis", dqcurrentAnalyser);

            // static analyser
            staticAnalyser = Motor.GetStaticAnalyser();
            staticAnalyser.OutputPath = CurrentOutputFolder;
            staticAnalyser.Path_OriginalFEMFile = CurrentFEMMFile;

            // make sure transient analysis has name, and associated with motor
            foreach (String ta_name in TransAnalysisGroup.Keys)
            {
                TransAnalysisGroup[ta_name].Motor = Motor;//also folder results
                TransAnalysisGroup[ta_name].AnalysisName = ta_name;//sub folder results
                TransAnalysisGroup[ta_name].OutputPath = CurrentOutputFolder;
                TransAnalysisGroup[ta_name].Path_OriginalFEMFile = CurrentFEMMFile;
            }

            // make sure has name and associated with motor
            mmAnalyser.OutputPath = CurrentOutputFolder;
            mmAnalyser.Path_OriginalFEMFile = CurrentFEMMFile;
            mmAnalyser.Motor = Motor;

            dqcurrentAnalyser.OutputPath = CurrentOutputFolder;
            dqcurrentAnalyser.Path_OriginalFEMFile = CurrentFEMMFile;
            dqcurrentAnalyser.Motor = Motor;
        }

        public object GetComponent(String name)
        {
            if (Components.ContainsKey(name))
                return Components[name];
            else return null;
        }

        #endregion

        #region Lua-Script-Wrapper

        /*
        Lua script wrapper:
        - Like enviroment for calculation, storing variable, constants, ...
        */

        private Lua lua;

        private string luascript_main_script;

        public Lua GetLuaInstance()
        {
            return lua;
        }

        private void initLua()
        {
            if (lua == null)
            {
                lua = new Lua();
            }
        }

        private void closeLua()
        {
            if (lua != null)
            {
                if (lua.IsExecuting)
                    throw new InvalidOperationException("Lua is executing");

                lua.Close();
                lua = null;
            }
        }

        public Dictionary<string, object> GetLuaVars()
        {
            if (lua != null)
            {
                var dict = new Dictionary<string, object>();
                foreach (string var in lua.Globals)
                {
                    dict.Add(var, lua[var]);
                }

                return dict;
            }

            return null;
        }

        #endregion

        #region Analysis results

        public IDictionary<String, object> AnalysisResults { get; private set; }

        public String[] GetAnalysisResultsNames()
        {
            return AnalysisResults.Keys.ToArray();
        }

        public object GetAnalysisResults(String name)
        {
            if (AnalysisResults.ContainsKey(name))
                return AnalysisResults[name];
            else return null;
        }

        private void refreshAnalysisResults()
        {
            AnalysisResults = new Dictionary<String, object>();

            foreach (String name in TransAnalysisGroup.Keys)
                AnalysisResults.Add(name, TransAnalysisGroup[name].Results);

            AnalysisResults.Add("MMAnalysis", mmAnalyser.Results);
            AnalysisResults.Add("DQCAnalysis", dqcurrentAnalyser.Results);
        }

        private void loadResultsFromDisk()
        {
            staticAnalyser.LoadResultsFromDisk();
            foreach (var ta in TransAnalysisGroup.Values)
            {
                ta.LoadResultsFromDisk();
            }
            mmAnalyser.LoadResultsFromDisk();
            dqcurrentAnalyser.LoadResultsFromDisk();
        }

        #endregion

        #region Objects(motor/trans-analysis)-Params-Conversion

        /*
        MotorParams - all params: motor (rotor,stator,...), transient, mm analysis. Storing like Dictionary<string, object>
        Component - real object
        */

        // inform that params changed
        public void InvalidateParams()
        {
            WriteParamsToComponents();

            validateMotor();

            // re-read results from disk
            loadResultsFromDisk();

            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        private void ComponentsToNewParams()
        {
            // create params collection from this sample motor/trans-analysis
            MotorParams = new ParametersCollection();
            foreach (String name in Components.Keys)
            {
                MotorParams.AddRange(ParametersCollection.FromObject(name, Components[name]));
            }
        }

        /// <summary>
        /// Convert params collection to motor object, transient analysis
        /// </summary>
        private void WriteParamsToComponents()
        {
            foreach (String name in Components.Keys)
            {
                MotorParams.putValuesToObject(name, Components[name]);
            }
        }

        /// <summary>
        /// Calculate points, magnetic circuit, and error show
        /// </summary>
        private void validateMotor()
        {
            // calculate points ( and validate params also)
            Motor.CalcPointsCoordinates();

            // check validation and put error/warning back to status field in each param in ListParamInput            
            foreach (Parameter pi in MotorParams)
            {
                foreach (ParamValidationInfo pvi in Motor.ListParamsValidation)
                {
                    if (pvi.ParamName == pi.name)
                    {
                        pi.status = pvi.msgType + ": " + pvi.message;
                        break;
                    }
                }
            }

            // build analytical analyser and calculate
            analyticalAnalyser = Motor.GetAnalyticalAnalyser();
            analyticalAnalyser.RunAnalysis();
        }

        public ParametersCollection MotorToParams(AbstractMotor motor)
        {
            // Parameters from motor            
            Dictionary<String, object> objs = new Dictionary<string, object>();
            objs.Add("Motor\\General(" + motor.GeneralParams.GetType().Name + ")", motor.GeneralParams);
            objs.Add("Motor\\Rotor(" + motor.Rotor.GetType().Name + ")", motor.Rotor);
            objs.Add("Motor\\Stator(" + motor.Stator.GetType().Name + ")", motor.Stator);
            //objs.Add("Motor\\Materials", motor.MaterialParams);
            objs.Add("Motor\\Airgap(" + motor.Airgap.GetType().Name + ")", motor.Airgap);

            ParametersCollection motorParams = new ParametersCollection();
            foreach (String objname in objs.Keys)
            {
                motorParams.AddRange(ParametersCollection.FromObject(objname, objs[objname]));
            }

            return motorParams;
        }

        public AbstractMotor ParamsToMotor(ParametersCollection pc)
        {
            AbstractMotor motor = GetSampleMotor();//new default
            Dictionary<String, object> objs = new Dictionary<string, object>();
            objs.Add("Motor\\General(" + motor.GeneralParams.GetType().Name + ")", motor.GeneralParams);
            objs.Add("Motor\\Rotor(" + motor.Rotor.GetType().Name + ")", motor.Rotor);
            objs.Add("Motor\\Stator(" + motor.Stator.GetType().Name + ")", motor.Stator);
            //objs.Add("Motor\\Materials", motor.MaterialParams);
            objs.Add("Motor\\Airgap(" + motor.Airgap.GetType().Name + ")", motor.Airgap);
            foreach (String objname in objs.Keys)
            {
                pc.putValuesToObject(objname, objs[objname]);
            }

            return motor;
        }

        #endregion

        #region Analysis

        public event EventHandler OnMotorAnalysisResultsUpdate = new EventHandler(delegate (object o, EventArgs e) { });

        #region Static analysis

        // forcefully build fem file
        public void buildFEMM()
        {
            FEMM.CloseFemm();
            Motor.BuildFEMModel(CurrentFEMMFile);
            FEMM.CloseFemm();
        }

        public void runStaticAnalysis()
        {
            // load from disk first
            staticAnalyser.LoadResultsFromDisk();
            if (staticAnalyser.Results == null)
            {
                FEMM.CloseFemm();

                // make sure ans file is created
                if (!Motor.isFEMModelReady(CurrentFEMMFile))
                    Motor.BuildFEMModel(CurrentFEMMFile);

                staticAnalyser.OnFinishedAnalysis -= staticAnalyser_OnFinishedAnalysis;
                staticAnalyser.OnFinishedAnalysis += staticAnalyser_OnFinishedAnalysis;
                // do measure
                staticAnalyser.RunAnalysis();

                FEMM.CloseFemm();
            }
            else staticAnalyser_OnFinishedAnalysis(this, staticAnalyser.Results);
        }

        void staticAnalyser_OnFinishedAnalysis(object sender, AbstractResults e)
        {
            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        #endregion

        #region Transient analysis

        private Stopwatch sw;

        public void runTransientAnalysis(String name = "Transient\\sample")
        {
            // make sure femm file is created
            if (!Motor.isFEMModelReady(CurrentFEMMFile))
                Motor.BuildFEMModel(CurrentFEMMFile);

            // run static analysis
            runStaticAnalysis();

            sw = new Stopwatch();
            sw.Start();

            TransAnalysisGroup[name].OnFinishedAnalysis -= OnFinishAnalysis_internal;
            TransAnalysisGroup[name].OnFinishedAnalysis += OnFinishAnalysis_internal;

            TransAnalysisGroup[name].StartAnalysis();//start async
        }

        private void OnFinishAnalysis_internal(object sender, AbstractResults results)
        {
            sw.Stop();
            log.Info("Analysis time :" + sw.Elapsed.TotalSeconds + "s");
            FEMM.CloseFemm();

            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        public void runAllTransientAnalysis()
        {
            Thread thread = new Thread(new ThreadStart(runAllTransientAnalysis_Sync));

            thread.Start();
        }

        private void runAllTransientAnalysis_Sync()
        {
            // make sure femm file is created
            if (!Motor.isFEMModelReady(CurrentFEMMFile))
                Motor.BuildFEMModel(CurrentFEMMFile);

            // run static first
            runStaticAnalysis();

            sw = new Stopwatch();
            sw.Start();

            // now run the transient analysis
            foreach (String name in TransAnalysisGroup.Keys)
            {
                // un-subcribe event
                TransAnalysisGroup[name].OnFinishedAnalysis -= OnFinishAnalysis_internal;

                // run in sync
                TransAnalysisGroup[name].RunAnalysis();

                // close all opened
                FEMM.CloseFemm();
            }

            sw.Stop();
            log.Info("Analysis time :" + sw.Elapsed.TotalSeconds + "s");

            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        //public event EventHandler<TransientResults> OnFinishTransientAnalysis = new EventHandler<TransientResults>(delegate(object sender, TransientResults r) { });

        public bool addTransientAnalysis(String name = "Transient\\sample")
        {
            //rename until no more collision
            String origin = name;
            int k = 1;
            while (TransAnalysisGroup.ContainsKey(name))
            {
                name = String.Format("{0}({1})", origin, k);
                k++;
            }

            // add new one
            AbstractTransientAnalyser ta = GetDefaultTransientAnalyser();
            ta.Motor = Motor;
            ta.AnalysisName = name;
            TransAnalysisGroup.Add(name, ta);

            // refresh components
            RefreshComponents();
            ComponentsToNewParams();
            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);//changed results (transient list)
            return true;
        }

        public bool removeTransientAnalysis(String name)
        {
            if (TransAnalysisGroup.ContainsKey(name))
            {
                AbstractTransientAnalyser ta = TransAnalysisGroup[name];
                ta.removeMe();
                TransAnalysisGroup.Remove(name);

                // refresh components
                RefreshComponents();
                ComponentsToNewParams();
                refreshAnalysisResults();
                OnMotorAnalysisResultsUpdate(this, null);//changed results (transient list)
                return true;
            }

            return false;
        }

        public bool renameTransientAnalysis(String name, String newname)
        {
            if (TransAnalysisGroup.ContainsKey(newname))
            {
                log.Error(newname + " already exists");
                return false;
            }

            if (TransAnalysisGroup.ContainsKey(name))
            {
                AbstractTransientAnalyser ta = TransAnalysisGroup[name];
                TransAnalysisGroup.Remove(name);
                ta.renameMe(newname);
                TransAnalysisGroup.Add(newname, ta);

                // refresh components
                RefreshComponents();
                ComponentsToNewParams();
                refreshAnalysisResults();
                OnMotorAnalysisResultsUpdate(this, null);//changed results (transient list)
                return true;
            }

            return false;
        }

        #endregion

        #region MMAnalysis

        public void runMMAnalysis()
        {
            // make sure femm file is created
            if (!Motor.isFEMModelReady(CurrentFEMMFile))
                Motor.BuildFEMModel(CurrentFEMMFile);

            // static analysis first
            runStaticAnalysis();

            sw = new Stopwatch();
            sw.Start();

            mmAnalyser.OnFinishedAnalysis -= mmAnalysis_OnFinishedAnalysis;
            mmAnalyser.OnFinishedAnalysis += mmAnalysis_OnFinishedAnalysis;

            mmAnalyser.StartAnalysis();//start async
        }

        private void mmAnalysis_OnFinishedAnalysis(object sender, AbstractResults e)
        {
            sw.Stop();
            FEMM.CloseFemm();

            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        public void makeSimulinkModel(String outputfile)
        {
            mmAnalyser.MakeMatlabSimulinkModelFile(outputfile);
        }

        #endregion

        #region DQCurrentAnalysis

        public void runDQCurrentAnalysis()
        {
            // make sure femm file is created
            if (!Motor.isFEMModelReady(CurrentFEMMFile))
                Motor.BuildFEMModel(CurrentFEMMFile);

            // static analysis first
            runStaticAnalysis();

            sw = new Stopwatch();
            sw.Start();

            dqcurrentAnalyser.OnFinishedAnalysis -= dqcurrentAnalyser_OnFinishedAnalysis;
            dqcurrentAnalyser.OnFinishedAnalysis += dqcurrentAnalyser_OnFinishedAnalysis;

            dqcurrentAnalyser.StartAnalysis();//start async
        }

        private void dqcurrentAnalyser_OnFinishedAnalysis(object sender, AbstractResults e)
        {
            sw.Stop();
            log.Info("Analysis time :" + sw.Elapsed.TotalSeconds + "s");
            FEMM.CloseFemm();

            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        #endregion

        #endregion

        #region Project actions

        private readonly string JSONNAME_DATA = "Data";
        private readonly string JSONNAME_DATATYPE = "DataType";
        private readonly string JSONNAME_MOTOR = "Motor";
        private readonly string JSONNAME_TRANSIENTANALYSERS = "TransientAnalysers";
        private readonly string JSONNAME_MMANALYSERS = "MMAnalysers";
        private readonly string JSONNAME_DQCURRENTANALYSER = "DQCAnalyser";        

        public readonly string ProjectFileExtension = ".prm";

        private String __currentProjectFile;
        /// <summary>
        /// Project destination file
        /// </summary>
        public String CurrentProjectFile
        {
            get
            {
                return __currentProjectFile;
            }

            set
            {
                __currentProjectFile = value;
                Properties.Settings.Default.LastProjectFile = __currentProjectFile;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Default FEM file (original FEM of project)
        /// </summary>       
        public string CurrentFEMMFile
        {
            get
            {
                return Path.GetDirectoryName(CurrentProjectFile) + "\\" + Path.GetFileNameWithoutExtension(CurrentProjectFile) + ".FEM";
            }
        }

        /// <summary>
        /// Dictionary where output data will be stored
        /// </summary>
        public String CurrentOutputFolder
        {
            get
            {
                return Path.GetDirectoryName(CurrentProjectFile) + "\\" + Path.GetFileNameWithoutExtension(CurrentProjectFile);
            }
        }

        /// <summary>
        /// Type of current motor
        /// </summary>
        public Type CurrentMotorType { get { return Motor.GetType(); } }

        private Type DefaultMotorType = typeof(SPMMotor);

        /// <summary>
        /// List of motor type you can choose from
        /// </summary>
        public readonly Type[] ListMotorType = {
                                          typeof(VPMMotor),
                                          typeof(SPMMotor)
                                      };

        /// <summary>
        /// Type of transient analyser according with type of motor
        /// </summary>
        private Dictionary<Type, Type> TypeForTransientAnalyser = new Dictionary<Type, Type>()
        {
            {typeof(VPMMotor),typeof(PMTransientAnalyser)},
            {typeof(SPMMotor),typeof(PMTransientAnalyser)}
        };

        /// <summary>
        /// Type of mm analyser according with type of motor
        /// </summary>
        private Dictionary<Type, Type> TypeForMMAnalyser = new Dictionary<Type, Type>()
        {
            {typeof(VPMMotor),typeof(PM_MMAnalyser)},
            {typeof(SPMMotor),typeof(PM_MMAnalyser)}
        };

        /// <summary>
        /// Type of dq current analyser according with type of motor
        /// </summary>
        private Dictionary<Type, Type> TypeForDQCurrentAnalyser = new Dictionary<Type, Type>()
        {
            {typeof(VPMMotor),typeof(PM_DQCurrentAnalyser)},
            {typeof(SPMMotor),typeof(PM_DQCurrentAnalyser)}
        };


        /// <summary>
        /// Get the default motor of current researching type
        /// </summary>
        /// <returns></returns>
        public AbstractMotor GetSampleMotor(Type motorType = null)
        {
            if (motorType == null)
                motorType = CurrentMotorType;

            MethodInfo mi = motorType.GetMethod("GetSampleMotor");

            if (mi == null)
                throw new NotImplementedException("public static AbstractMotor GetSampleMotor() methods is not implemented in " + CurrentMotorType.Name);

            AbstractMotor motor = mi.Invoke(null, null) as AbstractMotor;

            if (motor == null)
                throw new NotImplementedException("public static AbstractMotor GetSampleMotor() methods return not the AbstractMotor (" + CurrentMotorType.Name + ")");

            return motor;
        }

        public AbstractTransientAnalyser GetDefaultTransientAnalyser()
        {
            // let it throw exception if not existed key
            Type transType = TypeForTransientAnalyser[CurrentMotorType];

            MethodInfo mi = transType.GetMethod("GetSampleMe");

            if (mi == null)
                throw new NotImplementedException("public static AbstractTransientAnalyser GetSampleMe() method is not implemented in " + transType.Name);

            AbstractTransientAnalyser ta = mi.Invoke(null, new object[] { Motor }) as AbstractTransientAnalyser;

            if (ta == null)
                throw new NotImplementedException("public static AbstractTransientAnalyser GetSampleMe() method returns wrong type (" + transType.Name + ")");

            return ta;
        }

        public AbstractMMAnalyser GetDefaultMMAnalyser()
        {
            // let it throw exception if not existed key
            Type mmaType = TypeForMMAnalyser[CurrentMotorType];

            MethodInfo mi = mmaType.GetMethod("GetSampleMe");

            if (mi == null)
                throw new NotImplementedException("public static AbstractMMAnalyser GetSampleMe() method is not implemented in " + mmaType.Name);

            AbstractMMAnalyser mma = mi.Invoke(null, new object[] { Motor }) as AbstractMMAnalyser;

            if (mma == null)
                throw new NotImplementedException("public static AbstractMMAnalyser GetSampleMe() method returns wrong type (" + mmaType.Name + ")");

            return mma;
        }

        public PM_DQCurrentAnalyser GetDefaultDQCurrentAnalyser()
        {
            // let it throw exception if not existed key
            Type dqaType = TypeForDQCurrentAnalyser[CurrentMotorType];

            MethodInfo mi = dqaType.GetMethod("GetSampleMe");

            if (mi == null)
                throw new NotImplementedException("public static PM_DQCurrentAnalyser GetSampleMe() method is not implemented in " + dqaType.Name);

            PM_DQCurrentAnalyser dqa = mi.Invoke(null, new object[] { Motor }) as PM_DQCurrentAnalyser;

            if (dqa == null)
                throw new NotImplementedException("public static PM_DQCurrentAnalyser GetSampleMe() method returns wrong type (" + dqaType.Name + ")");

            return dqa;
        }

        /// <summary>
        /// create a sample motor
        /// </summary>
        private void createNewSampleProject(Type motorType)
        {
            // sample motor
            Motor = GetSampleMotor(motorType);
            // get analyser attached to motor
            staticAnalyser = Motor.GetStaticAnalyser();
            analyticalAnalyser = Motor.GetAnalyticalAnalyser();
            // sample analysis config
            TransAnalysisGroup = new Dictionary<String, AbstractTransientAnalyser>();
            AbstractTransientAnalyser ta_sample = GetDefaultTransientAnalyser();
            TransAnalysisGroup.Add(ta_sample.AnalysisName, ta_sample);

            mmAnalyser = GetDefaultMMAnalyser();
            dqcurrentAnalyser = GetDefaultDQCurrentAnalyser();

            pSweeper = new ParamSweeper();
        }

        /// <summary>
        /// Create new motor with type base on what currently have
        /// </summary>
        private void createNewProjectBaseOnCurrent(Type newMotorType)
        {
            // check if already this type?
            if (CurrentMotorType.Equals(newMotorType))
                return;

            // check if type in the list, if not throw exception since this maybe programming mistake
            if (!ListMotorType.Contains(newMotorType))
                throw new ArgumentException("Type " + newMotorType.Name + " is not in the list.");

            BindingFlags flag = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
            PropertyInfo pnewStator = newMotorType.GetProperty("Stator", flag);
            PropertyInfo pnewRotor = newMotorType.GetProperty("Rotor", flag);
            PropertyInfo pnewAirgap = newMotorType.GetProperty("Airgap", flag);

            AbstractMotor oldMotor = Motor;
            var oldTrans = TransAnalysisGroup;
            var oldmma = mmAnalyser;
            var olddqa = dqcurrentAnalyser;
            Type oldTransType = TypeForTransientAnalyser[oldMotor.GetType()];
            Type oldmmaType = TypeForMMAnalyser[oldMotor.GetType()];
            Type olddqaType = TypeForDQCurrentAnalyser[oldMotor.GetType()];

            // create sample project with motortype, Motor is new
            createNewSampleProject(newMotorType);

            Type newTransType = TypeForTransientAnalyser[Motor.GetType()];
            Type newmmaType = TypeForMMAnalyser[Motor.GetType()];
            Type newdqaType = TypeForDQCurrentAnalyser[Motor.GetType()];

            // copy from old one if type is match
            if (oldMotor.Stator.GetType().Equals(pnewStator.PropertyType))
                Motor.Stator = oldMotor.Stator;
            if (oldMotor.Rotor.GetType().Equals(pnewRotor.PropertyType))
                Motor.Rotor = oldMotor.Rotor;
            if (oldMotor.Airgap.GetType().Equals(pnewAirgap.PropertyType))
                Motor.Airgap = oldMotor.Airgap;
            if (oldTransType.Equals(newTransType))
                TransAnalysisGroup = oldTrans;
            if (oldmmaType.Equals(newmmaType))
                mmAnalyser = oldmma;
            if (olddqaType.Equals(newdqaType))
                dqcurrentAnalyser = olddqa;
        }

        #region Public methods

        /// <summary>
        /// New blank project with motor type to choose
        /// </summary>
        public void createNewProject(Type motorType = null)
        {
            if (motorType == null)
                motorType = DefaultMotorType;

            CurrentProjectFile = Application.StartupPath + "\\temp" + ProjectFileExtension;
            createNewSampleProject(motorType);

            // refresh components
            RefreshComponents();

            // make params collection
            ComponentsToNewParams();

            // validate motor, show error in params collection
            validateMotor();

            // refresh results
            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        /// <summary>
        /// Should be call on startup. It try to open last opened project. 
        /// If fail, new project will be created.
        /// </summary>
        public void loadStartupProject()
        {
            try
            {
                openProject(Properties.Settings.Default.LastProjectFile);
                return;
            }
            catch (Exception ex)
            {
                log.Error("Error when try load startup project. Use default one now.");
                log.Error("     Error details: " + ex.Message);
                createNewProject();
            }
        }

        /// <summary>
        /// Modify project motor to other type
        /// </summary>
        public void changeMotorType(Type newMotorType)
        {
            // check if already this type?
            if (CurrentMotorType.Equals(newMotorType))
                return;

            // check if type in the list, if not throw exception since this maybe programming mistake
            if (!ListMotorType.Contains(newMotorType))
                throw new ArgumentException("Type " + newMotorType.Name + " is not in the list.");

            // save backup to output folder
            String thisTypeToBackup = CurrentOutputFolder + "\\" + CurrentMotorType.Name + ProjectFileExtension;
            saveProjectAsFile(thisTypeToBackup);

            // try open a backup
            String newTypeInBackup = CurrentOutputFolder + "\\" + newMotorType.Name + ProjectFileExtension;
            if (File.Exists(newTypeInBackup))
            {
                // copy backup to current
                File.Copy(newTypeInBackup, CurrentProjectFile, true);

                // try load 
                try
                {
                    //reload project
                    openProject(CurrentProjectFile);
                    return;
                }
                catch (Exception ex)
                {
                    log.Error("Error when try load backup project. Create fresh one now.");
                    log.Error("     Error details: " + ex.Message);
                    createNewProjectBaseOnCurrent(newMotorType);
                }
            }
            else // no backup, then create new one
            {
                createNewProjectBaseOnCurrent(newMotorType);
            }

            // refresh components
            RefreshComponents();

            // make params collection
            ComponentsToNewParams();

            // validate motor, show error in params collection
            validateMotor();

            // refresh results
            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);
        }

        /// <summary>
        /// Save/Save as project        
        /// </summary>
        /// <param name="fn">File to saveas or not specify if save with current name</param>
        public void saveProject(String fn = "")
        {
            if (fn == "")
                fn = CurrentProjectFile;

            bool success = saveProjectAsFile(fn);
            if (!success)
                return;

            // update new file name
            CurrentProjectFile = fn;

            // refresh list of components
            RefreshComponents();
        }

        private bool saveProjectAsFile(String fn)
        {
            try
            {
                //create output for project
                Directory.CreateDirectory(CurrentOutputFolder);

                using (StreamWriter sw = new StreamWriter(fn))
                {
                    JsonObject jsonAll = new JsonObject();
                    // data type here
                    JsonObject joDatatype = new JsonObject();
                    joDatatype.Add(JSONNAME_MOTOR, Motor.GetType().FullName);
                    Type transtype = null;
                    foreach (AbstractTransientAnalyser ta in TransAnalysisGroup.Values)
                    {
                        transtype = ta.GetType();
                        break;
                    }
                    joDatatype.Add(JSONNAME_TRANSIENTANALYSERS, transtype.FullName);
                    joDatatype.Add(JSONNAME_MMANALYSERS, mmAnalyser.GetType().FullName);
                    joDatatype.Add(JSONNAME_DQCURRENTANALYSER, dqcurrentAnalyser.GetType().FullName);
                    jsonAll.Add(JSONNAME_DATATYPE, joDatatype);

                    // real data here
                    JsonObject joData = new JsonObject();
                    joData.Add(JSONNAME_MOTOR, Motor);
                    joData.Add(JSONNAME_TRANSIENTANALYSERS, TransAnalysisGroup);
                    joData.Add(JSONNAME_MMANALYSERS, mmAnalyser);
                    joData.Add(JSONNAME_DQCURRENTANALYSER, dqcurrentAnalyser);
                    jsonAll.Add("Data", joData);

                    // write to file
                    sw.Write(JSON.Beautify(JSON.ToJSON(jsonAll)));
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Error("Error writing file " + fn + ": " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Open existed project
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public bool openProject(String fn)
        {
            // read params from file
            using (StreamReader sr = new StreamReader(fn))
            {
                //String str = sr.ReadToEnd();        
                var dict = JSON.Parse(sr.ReadToEnd()) as Dictionary<string, object>;
                JsonObject jsonAll = dict;

                JsonObject joDatatype = (JsonObject)jsonAll[JSONNAME_DATATYPE];
                JsonObject joData = (JsonObject)jsonAll[JSONNAME_DATA];

                // motor
                Type motortype = Type.GetType(joDatatype[JSONNAME_MOTOR].ToString());
                if (motortype != null)
                {
                    string str = JSON.ToJSON(joData[JSONNAME_MOTOR]);
                    Motor = JSON.ToObject(str, motortype) as AbstractMotor;
                }
                else
                {
                    log.Error("Cannot load motor as " + joDatatype[JSONNAME_MOTOR].ToString());
                    Motor = GetSampleMotor(DefaultMotorType);
                }

                // transient
                TransAnalysisGroup = new Dictionary<String, AbstractTransientAnalyser>();
                Type taType = Type.GetType(joDatatype[JSONNAME_TRANSIENTANALYSERS].ToString());
                if (taType != null)
                {
                    var TransAnalysers = JSON.Parse(JSON.ToJSON(joData[JSONNAME_TRANSIENTANALYSERS])) as IDictionary<string, object>;
                    foreach (string ta_name in TransAnalysers.Keys)
                    {
                        var ta_value = TransAnalysers[ta_name];
                        AbstractTransientAnalyser ta = (AbstractTransientAnalyser)JSON.ToObject(JSON.ToJSON(ta_value), taType);
                        TransAnalysisGroup.Add(ta_name.ToString(), ta);
                    }
                }
                else //type not match, just get a default one
                {
                    log.Error("Cannot load transient analyser as " + joDatatype[JSONNAME_TRANSIENTANALYSERS].ToString());
                    AbstractTransientAnalyser ta_sample = GetDefaultTransientAnalyser();
                    TransAnalysisGroup.Add(ta_sample.AnalysisName, ta_sample);
                }

                // mm
                Type mmatype = Type.GetType(joDatatype[JSONNAME_MMANALYSERS].ToString());
                if (mmatype != null)
                    mmAnalyser = (AbstractMMAnalyser)JSON.ToObject(JSON.ToJSON(joData[JSONNAME_MMANALYSERS]), mmatype);
                else
                {
                    log.Error("Cannot load mm analyser as " + joDatatype[JSONNAME_MMANALYSERS].ToString());
                    mmAnalyser = GetDefaultMMAnalyser();
                }

                // dqa
                Type dqatype = joDatatype.ContainsKey(JSONNAME_DQCURRENTANALYSER) ? Type.GetType(joDatatype[JSONNAME_DQCURRENTANALYSER].ToString()) : null;
                if (dqatype != null)
                    dqcurrentAnalyser = (PM_DQCurrentAnalyser)JSON.ToObject(JSON.ToJSON(joData[JSONNAME_DQCURRENTANALYSER]), dqatype);
                else
                {
                    log.Error("Cannot load dq current analyser");
                    dqcurrentAnalyser = GetDefaultDQCurrentAnalyser();
                }

                // finished load params to a parameterscollection
                log.Info("Loaded params from " + fn + ".");

                // assign new name                 
                CurrentProjectFile = fn;
            }

            // refresh components since references to those before open new file are expired
            RefreshComponents();

            // from components make params (save/load with components)
            ComponentsToNewParams();

            // calculate gmc, show error 
            validateMotor();

            // new sweeper
            pSweeper = new ParamSweeper();

            // try load analysis results from disk
            loadResultsFromDisk();

            // update UI
            refreshAnalysisResults();
            OnMotorAnalysisResultsUpdate(this, null);

            return true;
        }

        #endregion

        #endregion
    }
}
