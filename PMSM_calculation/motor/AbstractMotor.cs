/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using fastJSON;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace calc_from_geometryOfMotor.motor
{
    public abstract class AbstractMotor
    {
        public AbstractMotor()
        {
            isPointsCoordCalculated = false;
        }

        // validation info
        [JsonIgnore]
        public List<ParamValidationInfo> ListParamsValidation { get; protected set; }
        public bool IsParamsValid()
        {
            // if calculation hasn't been done
            if (ListParamsValidation == null)
                return false;

            foreach (ParamValidationInfo pvi in ListParamsValidation)
                if (pvi.msgType == ParamValidationInfo.MessageType.Error)
                    return false;

            return true;
        }

        #region set parts (Rotor,Stator,Airgap) to motor

        private GeneralParameters generalparams;
        public GeneralParameters GeneralParams
        {
            get
            {
                return generalparams;
            }
            set
            {
                generalparams = value;
                generalparams.Motor = this;
            }
        }

        private AbstractRotor rotor;
        [JsonIgnore]
        internal AbstractRotor Rotor
        {
            get
            {
                return rotor;
            }
            set
            {
                rotor = value;
                rotor.Motor = this;
            }
        }

        private AbstractStator stator;
        [JsonIgnore]
        internal AbstractStator Stator
        {
            get
            {
                return stator;
            }
            set
            {
                stator = value;
                stator.Motor = this;
            }
        }

        private AbstractAirgap airgap;
        [JsonIgnore]
        internal AbstractAirgap Airgap
        {
            get
            {
                return airgap;
            }
            set
            {
                airgap = value;
                airgap.Motor = this;
            }
        }

        #endregion

        /// <summary>
        /// Align rotor angle to stator. Because if Q/(2p) odd, partial model is cut right at slot, need to rotate a little
        /// to prevent this.
        /// It is mechanical angle in degree, counter-clockwise.        
        /// </summary>
        //public virtual double OrgininalRotorAngle
        //{
        //    get
        //    {
        //        // the number of slots in one pole
        //        int nn = (Stator.Q / (2 * Rotor.p));
        //        // degrees need to rotate to get all slot in 1 pole (no slot be cut at boundary)
        //        return (nn % 2 == 0 ? 360.0 / Stator.Q : 0);
        //    }
        //}

        /// <summary>
        /// Normalize rotor angle to inside (-2alpha,2alpha)
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public double GetNormalizedRotorAngle(double angle)
        {
            var xRotorAngle = angle;
            if (!GeneralParams.FullBuildFEMModel)
            {
                double alphax2 = 2 * Rotor.alphaDegree;
                if (angle > alphax2 || angle < -alphax2)
                    xRotorAngle = angle - Math.Round(angle / (2 * alphax2)) * 2 * alphax2;
            }

            return xRotorAngle;
        }

        #region CalcPointsCoordinates

        protected bool isPointsCoordCalculated = false;
        public virtual void CalcPointsCoordinates()
        {
            if (Stator == null)
                throw new ArgumentNullException("StatorParams null");
            if (Rotor == null)
                throw new ArgumentNullException("RotorParams null");
            if (Airgap == null)
                throw new ArgumentNullException("AirgapParams null");

            Rotor.CalculatePoints();

            Stator.CalculatePoints();

            Airgap.CalculatePoints();

            ListParamsValidation = new List<ParamValidationInfo>();
            if (Rotor.ListParamsValidation != null)
                ListParamsValidation.AddRange(Rotor.ListParamsValidation);
            if (Stator.ListParamsValidation != null)
                ListParamsValidation.AddRange(Stator.ListParamsValidation);

            isPointsCoordCalculated = true;
        }

        #endregion

        #region Make femm model file

        /// <summary>
        /// Make a femm model using parameters
        /// This will check if file existed and use parameters the same to this one or not.
        /// If not, build new one
        /// </summary>
        /// <param name="outfile">Output femm model</param>
        /// <param name="original">The original femm model to insert into</param>
        /// <param name="forcebuild">True if build even file existed</param>
        /// <returns>0-OK,1-File existed, analyzed,-1-ERROR</returns>
        public virtual void BuildFEMModel(String outfile, FEMM femm = null)
        {
            // make sure coordinates were calculated
            if (!isPointsCoordCalculated)
                throw new InvalidOperationException("Points must be calculated before make FEM model.");

            if (femm == null)
                femm = FEMM.DefaultFEMM;

            femm.newdocument(FEMM.DocumentType.Magnetic);

            // setup problems params
            femm.mi_probdef(0, FEMM.UnitsType.millimeters, FEMM.ProblemType.planar, 1e-8, GeneralParams.MotorLength, 7, FEMM.ACSolverType.Succ_Approx);

            // build a rotor in femm
            Rotor.BuildInFEMM(femm);

            // build a stator in femm
            Stator.BuildInFEMM(femm);

            // build airgap (put label)
            Airgap.BuildInFEMM(femm);

            // clear selected, refresh, and go to natural zoom
            femm.mi_clearselected();
            femm.mi_zoomnatural();

            femm.mi_saveas(outfile);

            femm.mi_close();

            // write md5 to it
            FEMM.mi_modifyFEMMComment(outfile, GetMD5String());
        }

        public bool isFEMModelReady(String fn)
        {
            // check if FEMM and ANS file existed or not
            if (File.Exists(fn))
            {
                // check if FEMM was built using which parameters (use md5 to check)
                String md5 = FEMM.mi_getFEMMComment(fn);
                if (md5 == GetMD5String())
                {
                    //log.Info("No Action needed. File existed and analyzed: " + Path_FEMMFile);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Draw preview

        public virtual void DrawPreview(Graphics graphics, float Ox, float Oy, float scale)
        {
            Rotor.DrawPreview(graphics, Ox, Oy, scale);
            Stator.DrawPreview(graphics, Ox, Oy, scale);
        }

        #endregion

        /// <summary>
        /// MD5 of motor
        /// </summary>
        /// <returns></returns>
        public virtual string GetMD5String()
        {
            return Utils.CalculateObjectMD5Hash(this);
        }

        #region Abstract Analyser for motor

        public virtual AbstractAnalyticalAnalyser GetAnalyticalAnalyser()
        {
            return null;
        }

        public virtual AbstractStaticAnalyser GetStaticAnalyser()
        {
            return null;
        }

        public virtual AbstractMMAnalyser GetMMAnalyser()
        {
            return null;
        }

        #endregion

        #region Build Results for show

        /// <summary>
        /// Other parameters derived from input like: mass, volume, ...
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, object> GetDerivedParameters()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            return dict;
        }

        #endregion

        #region Properties

        public virtual double Volume { get { return 0; } }
        public virtual double Mass { get { return 0; } }

        #endregion
    }

    public abstract class AbstractMotorPart
    {
        public AbstractMotorPart()
        {
            isPointsCoordCalculated = false;
        }

        /// <summary>
        /// The motor that contains this part
        /// </summary>        
        [JsonIgnore]
        public AbstractMotor Motor { get; internal set; }

        /// <summary>
        /// Get list of validation information 
        /// </summary>
        [JsonIgnore]
        public List<ParamValidationInfo> ListParamsValidation { get; protected set; }
        protected void addValidationInfo(String p, String errormsg, ParamValidationInfo.MessageType t)
        {
            if (ListParamsValidation == null)
                ListParamsValidation = new List<ParamValidationInfo>();

            ListParamsValidation.Add(new ParamValidationInfo(p, errormsg, t));
        }

        protected bool isPointsCoordCalculated { get; set; }

        public virtual void CalculatePoints()
        {
            isPointsCoordCalculated = true;
        }

        public virtual void BuildInFEMM(FEMM femm = null)
        {
        }

        public virtual void DrawPreview(Graphics graphics, float Ox, float Oy, float scale)
        {
        }

        #region Properties

        public virtual double Volume { get { return 0; } }
        public virtual double Mass { get { return 0; } }

        #endregion
    }

    public abstract class AbstractRotor : AbstractMotorPart
    {
        [ParamInfo(HelpText = "Number of pair of poles")]
        public int p { get; set; }

        [ParamInfo(HelpText = "Diameter of Rotor (at airgap)")]
        public double DiaGap { get; set; }//2*Rrotor

        [ParamInfo(HelpText = "Diameter of Shaft (at yoke)")]
        public double DiaYoke { get; set; }//shaft diameter

        // 1/2 angle of pole, expecting that the other 1/2 is symmetrical
        public double alpha { get { return 2 * Math.PI / (4 * p); } }
        public double alphaDegree { get { return 180.0 / (2 * p); } }
        public double RGap { get { return DiaGap / 2; } }
        public double RYoke { get { return DiaYoke / 2; } }

        [ParamInfo(HelpText = "Angle to preview rotor rotation")]
        [ExcludeFromMD5Calculation]
        public double PreviewRotateAngle { get; set; }

        public virtual void RotateRotorInFEMM(double rotorAngle, FEMM femm = null)
        {
        }

        public virtual double getTorqueInAns(FEMM femm)
        {
            return 0;
        }

        #region Properties

        public virtual double Inertia { get { return 0; } }

        #endregion
    }

    public abstract class AbstractStator : AbstractMotorPart
    {
        [ParamInfo(HelpText = "Diameter of Stator (at airgap)")]
        public double DiaGap { get; set; }

        [ParamInfo(HelpText = "Diameter of Shaft (at yoke)")]
        public double DiaYoke { get; set; }

        [ParamInfo(HelpText = "Slot number")]
        public int Q { get; set; }//slot count

        public double RGap { get { return DiaGap / 2; } }
        public double RYoke { get { return DiaYoke / 2; } }

        /// <summary>
        /// Position of stator for the teeth to fit best in side one pole of rotor.
        /// This means outer lines of rotor pole don't cut the slot, only cut the teeth
        /// Only partial model need to care about this
        /// </summary>
        public virtual double RotatedForFEMMAngle
        {
            get
            {
                return ((Q / (2 * Motor.Rotor.p)) % 2 == 0) ? (180.0 / Q) : 0;
            }
        }

        /// <summary>
        /// Set current amps
        /// </summary>
        /// <param name="currents"></param>
        /// <param name="femm"></param>
        public virtual void SetStatorCurrentsInFEMM(IDictionary<String, double> currents, FEMM femm)
        {
            if (currents == null)
                return;
            // edit some transient params (usually stator currents) ?
            var tkeys = currents.Keys;
            foreach (String key in tkeys)
                femm.mi_modifycircuitCurrent(key, currents[key]);
        }

        /// <summary>
        /// Get all circuits properties in an opened window femm (with opened ans file)
        /// </summary>
        /// <param name="femm"></param>
        /// <returns></returns>
        public virtual Dictionary<String, FEMM.CircuitProperties> getCircuitsPropertiesInAns(FEMM femm)
        {
            return null;
        }
    }

    public abstract class AbstractAirgap : AbstractMotorPart
    {
        /// <summary>
        /// When partial model is analyzed, rotor rotate and need to rebuild boundary in airgap part.
        /// This method is called right before rotor rotate to have this clear the old boundary
        /// </summary>
        public virtual void RemoveBoundary(FEMM femm = null)
        {
        }

        /// <summary>
        /// When partial model is analyzed, rotor rotate and need to rebuild boundary in airgap part
        /// This method is called after rotor rotate to have this build new boundary
        /// </summary>
        /// <param name="rotorAngle"></param>
        public virtual void AddBoundaryAtAngle(double rotorAngle, FEMM femm = null)
        {
        }
    }

    public class GeneralParameters : AbstractMotorPart
    {
        // general params
        [ParamInfo(HelpText = "Length of motor")]
        public double MotorLength { get; set; }//motor length            

        // general params
        [ParamInfo(HelpText = "Build fullmodel in femm?"), ExcludeFromMD5Calculation]
        public bool FullBuildFEMModel { get; set; }
    }
}
