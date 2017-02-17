using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Femm;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using log4net;

namespace calc_from_geometryOfMotor
{
    public class FEMM
    {
        private static readonly ILog log = LogManager.GetLogger("FEMM");

        // Get the active X call
        private static FEMM defaultFemm = null;
        public static FEMM DefaultFEMM
        {
            get
            {
                if (defaultFemm == null)
                {
                    defaultFemm = new FEMM();
                }

                return defaultFemm;
            }

            private set
            {
                defaultFemm = value;
            }
        }

        private IActiveFEMM femm;
        public FEMM()
        {
            //femm = new ActiveFEMMClass(); //before NET4.0
            femm = new ActiveFEMM();//NET4.0+
        }       

        /// <summary>
        /// Kill all femm
        /// </summary>
        public static void CloseFemm()
        {
            quit();
            DefaultFEMM = null;
        }

        public String callFemm(String cmd, params object[] args)
        {
            return femm.call2femm(String.Format(cmd, args));
        }

        #region common commands

        public void clearconsole()
        {
            callFemm("clearconsole()");
        }

        public void hideconsole()
        {
            callFemm("hideconsole()");
        }

        public void showconsole()
        {
            callFemm("showconsole()");
        }

        /// <summary>
        /// Open new document
        /// </summary>
        /// <param name="doctype">Type of document to open</param>
        public void newdocument(DocumentType doctype)
        {
            callFemm("newdocument({0})", doctype.GetHashCode());
        }
        public enum DocumentType
        {
            Magnetic = 0,
            Electrostatic = 1,
            Heatflow = 2,
            Currentflow = 3
        }

        /// <summary>
        /// Quit because the command quit seems useless
        /// </summary>
        public static void quit()
        {
            Process[] p = Process.GetProcessesByName("femm");
            foreach (Process pc in p)
            {
                try
                {
                    pc.Kill();
                }
                catch (Exception ex)
                {
                    log.Error("Error: " + ex.Message);
                }
            }
        }

        public void showpointprops()
        {
            callFemm("showpointprops()");
        }

        public void open(String filename)
        {
            callFemm("open(\"{0}\")", filename.Replace('\\', '/'));
        }

        #endregion

        #region FEMM magnetic editing

        #region Object add/remove commands
        //add a new node at x,y
        public void mi_addnode(double x, double y)
        {
            callFemm("mi_addnode({0},{1})", x, y);
        }

        // add a new line segment from node cloest to x1,y1 to x2,y2
        public void mi_addsegment(double x1, double y1, double x2, double y2)
        {
            callFemm("mi_addsegment({0},{1},{2},{3})", x1, y1, x2, y2);
        }

        //add a new block label at x,y
        public void mi_addblocklabel(double x, double y)
        {
            callFemm("mi_addblocklabel({0},{1})", x, y);
        }

        //add a new arc segment from the nearest node to x1,y1 to x2,y2 with angle divided into maxseg 
        public void mi_addarc(double x1, double y1, double x2, double y2, double angle, double maxseg)
        {
            callFemm("mi_addarc({0},{1},{2},{3},{4},{5})", x1, y1, x2, y2, angle, maxseg);
        }

        // delete all selected objects
        public void mi_deleteselected()
        {
            callFemm("mi_deleteselected()");
        }

        // delete all selected objects
        public void mi_deleteselectednodes()
        {
            callFemm("mi_deleteselectednodes()");
        }

        // delete all selected objects
        public void mi_deleteselectedlabels()
        {
            callFemm("mi_deleteselectedlabels()");
        }

        // delete all selected objects
        public void mi_deleteselectedsegments()
        {
            callFemm("mi_deleteselectedsegments()");
        }

        // delete all selected objects
        public void mi_deleteselectedarcsegments()
        {
            callFemm("mi_deleteselectedarcsegments()");
        }

        #endregion

        #region Geometry selection commands

        //de-select objects
        public void mi_clearselected()
        {
            callFemm("mi_clearselected()");
        }

        //select line segment near x,y
        public void mi_selectsegment(double x, double y)
        {
            callFemm("mi_selectsegment({0},{1})", x, y);
        }

        //select node near x,y
        public void mi_selectnode(double x, double y)
        {
            callFemm("mi_selectnode({0},{1})", x, y);
        }

        //select label near x,y
        public void mi_selectlabel(double x, double y)
        {
            callFemm("mi_selectlabel({0},{1})", x, y);
        }

        //select arc segment near x,y
        public void mi_selectarcsegment(double x, double y)
        {
            callFemm("mi_selectarcsegment({0},{1})", x, y);
        }

        //select group n-th
        public void mi_selectgroup(int n)
        {
            callFemm("mi_selectgroup({0})", n);
        }

        #endregion

        #region Object labeling commands

        //set the selected nodes to have the nodal property mi_"propname" and group number n
        public void mi_setnodeprop(String propname, int n)
        {
            callFemm("mi_setnodeprop(\"{0}\",{1})", propname, n);
        }

        /** Set selected block to have the following properties:
         *  - Block: "blockname". (block type)
            – automesh: 0 = mesher defers to mesh size constraint defined in meshsize, 1 = mesher
            automatically chooses the mesh density.            
            – meshsize: size constraint on the mesh in the block marked by this label.
            – Block is a member of the circuit named "incircuit"
            – The magnetization is directed along an angle in measured in degrees denoted by the
            parameter magdirection. Alternatively, magdirection can be a string containing a
            formula that prescribes the magnetization direction as a function of element position.
            In this formula theta and R denotes the angle in degrees of a line connecting the center
            each element with the origin and the length of this line, respectively; x and y denote
            the x- and y-position of the center of the each element. For axisymmetric problems, r
            and z should be used in place of x and y.
            – A member of group number group
            – The number of turns associated with this label is denoted by turns
         * */
        public void mi_setblockprop(String blockname, bool automesh, double meshsize, String incircuit, double magnetdirection, int group, int turns)
        {
            callFemm("mi_setblockprop(\"{0}\",{1},{2},\"{3}\",{4},{5},{6})",
                blockname, automesh ? 1 : 0, meshsize, incircuit, magnetdirection, group, turns);
        }

        /// <summary>
        /// Set the select segments the properties
        /// </summary>
        /// <param name="propname">Boundary property "propname"</param>
        /// <param name="elementsize">Local element size along segment no greater than elementsize</param>
        /// <param name="automesh">automesh: 0 = mesher defers to the element constraint defined by elementsize, 1 =
        ///  mesher automatically chooses mesh size along the selected segments</param>
        /// <param name="hide">hide: 0 = not hidden in post-processor, 1 == hidden in post processor</param>
        /// <param name="group">A member of group number group</param>
        public void mi_setsegmentprop(String propname, double elementsize, bool automesh, bool hide, int group)
        {
            callFemm("mi_setsegmentprop(\"{0}\",{1},{2},{3},{4})",
                propname, elementsize, automesh ? 1 : 0, hide ? 1 : 0, group);
        }

        /**
         * mi setarcsegmentprop(maxsegdeg, "propname", hide, group) Set the selected arc
           segments to:
            – Meshed with elements that span at most maxsegdeg degrees per element
            – Boundary property "propname"
            – hide: 0 = not hidden in post-processor, 1 == hidden in post processor
            – A member of group number group
         * */
        public void mi_setarcsegmentprop(double maxsegdeg, String propname, bool hide, int group)
        {
            callFemm("mi_setarcsegmentprop({0},\"{1}\",{2},{3})",
                maxsegdeg, propname, hide ? 1 : 0, group);
        }

        #endregion

        #region Problem commands

        /// <summary>
        /// Changes the problem definition.          
        /// </summary>
        /// <param name="frequency">Set frequency to the desired frequency in Hertz. </param>
        /// <param name="units">Valid "units" entries are "inches", "millimeters", "centimeters", "mils", "meters, and "micrometers".</param>
        /// <param name="problemtype">Set the parameter problemtype to "planar" for a 2-D planar problem, or to "axi" for an
        ///    axisymmetric problem.</param>
        /// <param name="precision">The precision parameter dictates the precision required by the
        ///    solver. For example, entering 1E-8 requires the RMS of the residual to be less than 1e−8.</param>
        /// <param name="depth">representing the depth of the problem in the into-the-page direction for
        ///  2-D planar problems, can also also be specified</param>
        /// <param name="minangle">minimum angle constraint sent to the mesh generator</param>
        /// <param name="acsolver">"Succ. Approx" or "Newton"</param>
        public void mi_probdef(double frequency, UnitsType units, ProblemType problemtype, double precision, double depth, double minangle, ACSolverType acsolver = ACSolverType.Succ_Approx)
        {
            callFemm("mi_probdef({0},\"{1}\",\"{2}\",{3},{4},{5},\"{6}\")",
                frequency, units, problemtype, precision, depth, minangle, acsolver.GetHashCode());
        }

        public enum UnitsType
        {
            inches, millimeters, centimeters, mils, meters, micrometers
        }

        public enum ProblemType
        {
            planar, axi
        }

        public enum ACSolverType
        {
            Succ_Approx = 0,
            Newton = 1
        }

        /**
         * mi analyze(flag) runs fkern to solve the problem. The flag parameter controls whether
            the fkern window is visible or minimized. For a visible window, either specify no value for
            flag or specify 0. For a minimized window, flag should be set to 1.
         * */
        public void mi_analyze(bool minimizeWindow = false)
        {
            callFemm("mi_analyze({0})", minimizeWindow ? 1 : 0);
        }

        /**
         * mi loadsolution() loads and displays the solution corresponding to the current geometry.
         */
        public void mi_loadsolution()
        {
            callFemm("mi_loadsolution()");
        }

        /**
         * mi saveas("filename") saves the file with name "filename". Note if you use a path you
            must use two backslashes e.g. "c:\\temp\\myfemmfile.fem"
         * */
        public void mi_saveas(String filename)
        {
            callFemm("mi_saveas(\"{0}\")", filename.Replace('\\', '/'));
        }

        #endregion

        #region Mesh command
        //comming soon
        #endregion

        #region editing command

        /**
         * mi copyrotate(bx, by, angle, copies, (editaction) )
        – bx, by – base point for rotation
        – angle – angle by which the selected objects are incrementally shifted to make each
        copy. angle is measured in degrees.
        – copies – number of copies to be produced from the selected objects.
         * */
        public void mi_copyrotate(double bx, double by, double angle, int copies, EditMode editaction)
        {
            callFemm("mi_copyrotate({0},{1},{2},{3},{4})", bx, by, angle, copies, editaction.GetHashCode());
        }

        /**
         * mi copytranslate(dx, dy, copies, (editaction))
            – dx,dy – distance by which the selected objects are incrementally shifted.
            – copies – number of copies to be produced from the selected objects.
            – editaction 0 –nodes, 1 – lines (segments), 2 –block labels, 3 – arc segments, 4- group
         * */
        public void mi_copytranslate(double dx, double dy, int copies, EditMode editaction)
        {
            callFemm("mi_copytranslate({0},{1},{2},{3})", dx, dy, copies, editaction.GetHashCode());
        }

        /**
         * mi createradius(x,y,r)turnsacornerlocatedat(x,y)intoacurveofradiusr
         * */
        public void mi_createradius(double x, double y, double r)
        {
            callFemm("mi_createradius({0},{1},{2})", x, y, r);
        }

        /**
         * mi moverotate(bx,by,shiftangle (editaction))
            – bx, by – base point for rotation
            – shiftangle – angle in degrees by which the selected objects are rotated.
            – editaction 0 –nodes, 1 – lines (segments), 2 –block labels, 3 – arc segments, 4- group
         * */
        public void mi_moverotate(double bx, double by, double shiftangle, EditMode editaction)
        {
            callFemm("mi_moverotate({0},{1},{2},{3})", bx, by, shiftangle, editaction.GetHashCode());
        }

        /**
         * mi movetranslate(dx,dy,(editaction))
        – dx,dy – distance by which the selected objects are shifted.
        – editaction 0 –nodes, 1 – lines (segments), 2 –block labels, 3 – arc segments, 4- group
         * */
        public void mi_movetranslate(double dx, double dy, EditMode editaction)
        {
            callFemm("mi_movetranslate({0},{1},{2})", dx, dy, editaction.GetHashCode());
        }

        /**
         * mi scale(bx,by,scalefactor,(editaction))
        – bx, by – base point for scaling
        – scalefactor – a multiplier that determines how much the selected objects are scaled
        – editaction 0 –nodes, 1 – lines (segments), 2 –block labels, 3 – arc segments, 4- group
         * */
        public void mi_scale(double bx, double by, double scalefactor, EditMode editaction)
        {
            callFemm("mi_scale({0},{1},{2},{3})", bx, by, scalefactor, editaction.GetHashCode());
        }

        /**
         * mi mirror(x1,y1,x2,y2,(editaction)) mirror the selected objects about a line passing
        through the points (x1,y1) and (x2,y2). Valid editaction entries are 0 for nodes, 1 for
        lines (segments), 2 for block labels, 3 for arc segments, and 4 for groups
         * */
        public void mi_mirror(double x1, double y1, double x2, double y2, EditMode editaction)
        {
            callFemm("mi_mirror({0},{1},{2},{3},{4})", x1, y1, x2, y2, editaction.GetHashCode());
        }

        /**
         * mi seteditmode(editmode) Sets the current editmode to:
            – "nodes" - nodes
            – "segments" - line segments
            – "arcsegments" - arc segments
            – "blocks" - block labels
            – "group" - selected group
            This command will affect all subsequent uses of the other editing commands, if they are used
            WITHOUT the editaction parameter.
         * */
        public void mi_seteditmode(EditMode ed)
        {
            callFemm("mi_seteditmode(\"{0}\")", ed);
        }

        public enum EditMode
        {
            nodes = 0,
            segments = 1,
            blocks = 2,
            arsegments = 3,
            group = 4
        }

        #endregion

        #region Zoom commands

        /**
         * mi zoomnatural() zooms to a “natural” view with sensible extents.
            • mi zoomout() zooms out by a factor of 50%.
            • mi zoomin() zoom in by a factor of 200%.
            • mi zoom(x1,y1,x2,y2) Set the display area to be from the bottom left corner specified by
            (x1,y1) to the top right corner specified by (x2,y2).
         * */


        public void mi_zoomnatural()
        {
            callFemm("mi_zoomnatural()");
        }

        public void mi_zoomout()
        {
            callFemm("mi_zoomout()");
        }

        public void mi_zoomin()
        {
            callFemm("mi_zoomin()");
        }

        public void mi_zoom(double x1, double y1, double x2, double y2)
        {
            callFemm("mi_zoom({0},{1},{2},{3})", x1, y1, x2, y2);
        }
        #endregion

        #region View commands

        /**
         * mi_showgrid() Show the grid points.
            • mi_hidegrid() Hide the grid points points.
            • mi_grid_snap("flag") Setting flag to ”on” turns on snap to grid, setting flag to "off"
            turns off snap to grid.
            • mi_setgrid(density,"type") Change the grid spacing. The density parameter specifies the space between grid points, and the type parameter is set to "cart" for cartesian
            coordinates or "polar" for polar coordinates.
            • mi refreshview() Redraws the current view.
            • mi minimize() minimizes the active magnetics input view.
            • mi maximize() maximizes the active magnetics input view.
            • mi restore() restores the active magnetics input view from a minimized or maximized
            state.
            • mi resize(width,height) resizes the active magnetics input window client area to width
            × height.
         */

        public void mi_showgrid()
        {
            callFemm("mi_showgrid()");
        }

        public void mi_hidegrid()
        {
            callFemm("mi_hidegrid()");
        }

        public void mi_grid_snap(bool snaptogrid)
        {
            callFemm("mi_grid_snap(\"{0}\")", snaptogrid ? "on" : "off");
        }

        public void mi_setgrid(double density, String type)
        {
            callFemm("mi_setgrid({0},\"{1}\")", density, type);
        }

        public void mi_refreshview()
        {
            callFemm("mi_refreshview()");
        }

        public void mi_minimize()
        {
            callFemm("mi_minimize()");
        }

        public void mi_maximize()
        {
            callFemm("mi_maximize()");
        }

        public void mi_restore()
        {
            callFemm("mi_restore()");
        }

        public void mi_resize(int w, int h)
        {
            callFemm("mi_resize({0},{1})", w, h);
        }

        #endregion

        #region Object properties - material properties

        /// <summary>
        /// fetches the material specified by materialname from the materials library.
        /// </summary>
        /// <param name="materialname"></param>
        /// <returns></returns>
        public String mi_getmaterial(String materialname)
        {
            return callFemm("mi_getmaterialname({0})", materialname);
        }

        /// <summary>
        /// Full params method for add material
        /// • mi addmaterial("materialname", mu x, mu y, H c, J, Cduct, Lam d, Phi hmax,
        ///lam fill, LamType, Phi hx, Phi hy),NStrands,WireD adds a new material with called
        ///"materialname" with the material properties:
        ///– mu x Relative permeability in the x- or r-direction.
        ///– mu y Relative permeability in the y- or z-direction.
        ///– H c Permanent magnet coercivity in Amps/Meter.
        ///– J Real Applied source current density in Amps/mm2.
        ///– Cduct Electrical conductivity of the material in MS/m.
        ///– Lam d Lamination thickness in millimeters.
        ///– Phi hmax Hysteresis lag angle in degrees, used for nonlinear BH curves.
        ///– Lam fill Fraction of the volume occupied per lamination that is actually filled with
        ///iron (Note that this parameter defaults to 1 the femme preprocessor dialog box because,
        ///by default, iron completely fills the volume)
        ///– Lamtype Set to
        ///
        ///∗ 0 – Not laminated or laminated in plane
        ///∗  – laminated x or r
        ///∗ 2 – laminated y or z
        ///∗ 3 – Magnet wire
        ///∗ 4 – Plain stranded wire
        ///∗ 5 – Litz wire
        ///∗ 6 – Square wire
        ///– Phi hx Hysteresis lag in degrees in the x-direction for linear problems.
        ///– Phi hy Hysteresis lag in degrees in the y-direction for linear problems.
        ///– NStrands Number of strands in the wire build. Should be 1 for Magnet or Square wire.
        ///– WireD Diameter of each wire constituent strand in millimeters.
        ///Note that not all properties need be defined–properties that aren’t defined are assigned default
        ///values.
        /// </summary>
        /// <param name="materialname"></param>
        /// <param name="mu_x"></param>
        /// <param name="mu_y"></param>
        /// <param name="H_c"></param>
        /// <param name="J"></param>
        /// <param name="Cduct"></param>
        /// <param name="Lam_d"></param>
        /// <param name="Phi_hmax"></param>
        /// <param name="Lam_fill"></param>
        /// <param name="Lamtype"></param>
        /// <param name="Phi_hx"></param>
        /// <param name="Phi_hy"></param>
        /// <param name="NStrands"></param>
        /// <param name="WireD"></param>
        public void mi_addmaterial(String materialname, double mu_x, double mu_y, double H_c, double J, double Cduct,
            double Lam_d, double Phi_hmax, double Lam_fill, LaminationType Lamtype, double Phi_hx, double Phi_hy, int NStrands, double WireD)
        {
            callFemm("mi_addmaterial(\"{0}\",{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13})",
                materialname,
                MyDoubleToString(mu_x),
                MyDoubleToString(mu_y),
                MyDoubleToString(H_c),
                MyDoubleToString(J),
                MyDoubleToString(Cduct),
                MyDoubleToString(Lam_d),
                MyDoubleToString(Phi_hmax),
                MyDoubleToString(Lam_fill),
                Lamtype.GetHashCode(),
                MyDoubleToString(Phi_hx),
                MyDoubleToString(Phi_hy),
                MyDoubleToString(NStrands),
                MyDoubleToString(WireD));
        }

        private static String MyDoubleToString(double d)
        {
            return (d >= 0 ? d.ToString() : "");
        }

        public enum LaminationType
        {
            NotLaminated = 0,
            LaminatedXR = 1,
            LaminatedYZ = 2,
            MagnetWire = 3,
            PlainStrandedWire = 4,
            LitzWire = 5,
            SquareWire = 6
        }

        /// <summary>
        /// Default material air
        /// </summary>
        /// <param name="materialname"></param>
        public void mi_addmaterialAir(String materialname)
        {
            mi_addmaterial(materialname, 1, 1, 0, 0, 0, 0, 0, 1, LaminationType.NotLaminated, 0, 0, 0, 0);
        }

        /// <summary>
        /// Add magnet
        /// </summary>
        /// <param name="materialname"></param>
        /// <param name="muR"></param>
        /// <param name="Hc"></param>
        /// <param name="Cduct"></param>
        public void mi_addmaterialMagnet(String materialname, double muR, double Hc, double Cduct)
        {
            mi_addmaterial(materialname, muR, muR, Hc, 0, Cduct, 0, 0, 1, LaminationType.NotLaminated, 0, 0, 0, 0);
        }

        public void mi_addmaterialSteel(String materialname, double muR, double Cduct, double Lam_d, double Lam_fill, LaminationType Lamtype, double[] b, double[] h)
        {
            mi_addmaterial(materialname, muR, muR, 0, 0, Cduct, Lam_d, 0, Lam_fill, Lamtype, 0, 0, 0, 0);
            if (b != null && h != null & b.Length == h.Length)
            {
                for (int i = 0; i < b.Length; i++)
                {
                    mi_addbhpoint(materialname, b[i], h[i]);
                }
            }
        }

        public enum WireType
        {
            MagnetWire = 3,
            PlainStrandedWire = 4,
            LitzWire = 5,
            SquareWire = 6
        }

        public void mi_addmaterialCopper(String materialname, double Cduct, WireType wiretype, int NStrands, double WireD)
        {
            mi_addmaterial(materialname, 1, 1, 0, 0, Cduct, 0, 0, 1, (LaminationType)wiretype.GetHashCode(), 0, 0, NStrands, WireD);
        }

        /// <summary>
        /// Adds a B-H data point the the material specified by
        /// the string "blockname". The point to be added has a flux density of b in units of Teslas and
        /// a field intensity of h in units of Amps/Meter.
        /// </summary>
        /// <param name="materialname"></param>
        /// <param name="b"></param>
        /// <param name="h"></param>
        public void mi_addbhpoint(String materialname, double b, double h)
        {
            if (b >= 0 && h >= 0)
                callFemm("mi_addbhpoint(\"{0}\",{1},{2})", materialname, b, h);
        }

        /// <summary>
        /// Clears all B-H data points associated with the material
        /// </summary>
        /// <param name="materialname"></param>
        public void mi_clearbhpoints(String materialname)
        {
            callFemm("mi_clearbhpoints(\"{0}\")", materialname);
        }


        /// <summary>
        /// Adds a new point property of name "pointpropname"
        /// with either a specified potential a in units Webers/Meter or a point current j in units of Amps.
        /// Set the unused parameter pairs to 0.
        /// </summary>
        /// <param name="pointpropname"></param>
        /// <param name="a"></param>
        /// <param name="j"></param>
        public void mi_addpointprop(String pointpropname, double a, double j)
        {
            callFemm("mi_addpointprop(\"{0}\",{1},{2})", pointpropname, a, j);
        }

        /// <summary>
        ///         mi addboundprop("propname", A0, A1, A2, Phi, Mu, Sig, c0, c1, BdryFormat)
        ///adds a new boundary property with name "propname"
        ///– For a “Prescribed A” type boundary condition, set the A0, A1, A2 and Phi parameters
        ///as required. Set all other parameters to zero.
        ///– For a “Small Skin Depth” type boundary condtion, set the Mu to the desired relative
        ///permeability and Sig to the desired conductivity in MS/m. Set BdryFormat to 1 and
        ///all other parameters to zero.
        ///– To obtain a “Mixed” type boundary condition, set C1 and C0 as required and BdryFormat
        ///to 2. Set all other parameters to zero.
        ///– For a “Strategic dual image” boundary, set BdryFormat to 3 and set all other parameters
        ///to zero.
        ///– For a “Periodic” boundary condition, set BdryFormat to 4 and set all other parameters
        ///to zero.
        ///– For an “Anti-Perodic” boundary condition, set BdryFormat to 5 set all other parameters
        ///to zero.
        /// </summary>
        /// <param name="propname"></param>
        /// <param name="A0"></param>
        /// <param name="A1"></param>
        /// <param name="A2"></param>
        /// <param name="Phi"></param>
        /// <param name="Mu"></param>
        /// <param name="Sig"></param>
        /// <param name="c0"></param>
        /// <param name="c1"></param>
        /// <param name="BdryFormat"></param>
        public void mi_addboundprop(String propname, double A0, double A1, double A2, double Phi, double Mu, double Sig, double c0, double c1, BoundaryFormat BdryFormat)
        {
            callFemm("mi_addboundprop(\"{0}\",{1},{2},{3},{4},{5},{6},{7},{8},{9})",
                propname, A0, A1, A2, Phi, Mu, Sig, c0, c1, BdryFormat.GetHashCode());
        }

        public enum BoundaryFormat
        {
            Prescribed_A = 0,
            SmallSkinDepth = 1,
            Mixed = 2,
            StrategicDualImage = 3,
            Periodic = 4,
            AntiPeriodic = 5
        }

        /// <summary>
        /// Set boundary properties for Prescribed A type
        /// </summary>
        /// <param name="propname"></param>
        /// <param name="A0"></param>
        /// <param name="A1"></param>
        /// <param name="A2"></param>
        /// <param name="Phi"></param>
        public void mi_addboundprop_Prescribed_A(String propname, double A0, double A1, double A2, double Phi)
        {
            mi_addboundprop(propname, A0, A1, A2, Phi, 0, 0, 0, 0, BoundaryFormat.Prescribed_A);
        }

        public void mi_addboundprop_AntiPeriodic(String propname)
        {
            mi_addboundprop(propname, 0, 0, 0, 0, 0, 0, 0, 0, BoundaryFormat.AntiPeriodic);
        }

        public void mi_addboundprop_Periodic(String propname)
        {
            mi_addboundprop(propname, 0, 0, 0, 0, 0, 0, 0, 0, BoundaryFormat.Periodic);
        }

        /// <summary>
        /// adds a new circuit property with name "circuitname" with a prescribed current, i. The
        /// circuittype parameter is 0 for a parallel-connected circuit and 1 for a series-connected
        /// circuit.
        /// </summary>
        /// <param name="circuitname"></param>
        /// <param name="i"></param>
        /// <param name="circuittype"></param>
        public void mi_addcircprop(String circuitname, double i, CircuitType circuittype)
        {
            callFemm("mi_addcircprop(\"{0}\",{1},{2})", circuitname, i, circuittype.GetHashCode());
        }

        public enum CircuitType
        {
            parallel = 0,
            series = 1
        }

        /// <summary>
        /// This function allows for modification of a circuit property. The circuit property to be modified is specified by "CircName".
        /// The next parameter is the number of the property to be set. The last number is the value to
        /// be applied to the specified property. The various properties that can be modified are listed
        /// below:
        /// * propnum Symbol Description
        ///    0 CircName Name of the circuit property
        ///    1 i Total current
        ///    2 CircType 0 = Parallel, 1 = Series 
        /// </summary>
        /// <param name="circuitname"></param>
        /// <param name="propnum"></param>
        /// <param name="value"></param>
        public void mi_modifycircprop(String circuitname, int propnum, double value)
        {
            callFemm("mi_modifycircprop(\"{0}\",{1},{2})", circuitname, propnum, value);
        }

        /// <summary>
        /// Modify circuit current only
        /// </summary>
        /// <param name="circuitname"></param>
        /// <param name="value"></param>
        public void mi_modifycircuitCurrent(String circuitname, double value)
        {
            mi_modifycircprop(circuitname, 1, value);
        }

        /// <summary>
        /// Modify circuit current only
        /// </summary>
        /// <param name="circuitname"></param>
        /// <param name="value">real or complex-value as string</param>
        public void mi_modifycircuitCurrent(string circuitname, string value)
        {
            callFemm("mi_modifycircprop(\"{0}\",{1},{2})", circuitname, 1, value);
        }

        /// <summary>
        /// deletes the material named materialname.
        /// </summary>
        /// <param name="materialname"></param>
        public void mi_deletematerial(String materialname)
        {
            callFemm("mi_deletematerial(\"{0}\")", materialname);
        }

        /// <summary>
        /// deletes the boundary property named "propname".
        /// </summary>
        /// <param name="propname"></param>
        public void mi_deleteboundprop(String propname)
        {
            callFemm("mi_deleteboundprop(\"{0}\")", propname);
        }

        /// <summary>
        /// deletes the circuit named circuitname.
        /// </summary>
        /// <param name="circuitname"></param>
        public void mi_deletecircuit(String circuitname)
        {
            callFemm("mi_deletecircuit(\"{0}\")", circuitname);
        }

        // Some modify material and boundary, comming soon

        #endregion

        #region Misc

        /**
         * mi savebitmap("filename") saves a bitmapped screenshot of the current view to the file
        specified by "filename", subject to the printf-type formatting explained previously for
        the savefemmfile command.
         * */
        public void mi_savebitmap(String filename)
        {
            callFemm("mi_savebitmap(\"{0}\")", filename);
        }

        /**
         * mi savemetafile("filename") saves a metafile screenshot of the current view to the file
        specified by "filename", subject to the printf-type formatting explained previously for
        the savefemmfile command.
         * */

        public void mi_savemetafile(String filename)
        {
            callFemm("mi_savemetafile(\"{0}\")", filename);
        }

        /**
         * mi close() Closes current magnetics preprocessor document and destroys magnetics preprocessor window.
         * */
        public void mi_close()
        {
            callFemm("mi_close()");
        }

        /**
         * mi shownames(flag) This function allow the user to display or hide the block label names
        on screen. To hide the block label names, flag should be 0. To display the names, the
        parameter should be set to 1.
         * */
        public void mi_shownames(bool show)
        {
            callFemm("mi_shownames({0})", show ? 1 : 0);
        }

        /**
         * mi readdxf("filename") This function imports a dxf file specified by "filename".
        • mi savedxf("filename") This function saves geometry informationin a dxf file specified
        by "filename".
         * */

        public void mi_readdxf(String filename)
        {
            callFemm("mi_readdxf(\"{0}\")", filename);
        }


        public void mi_savedxf(String filename)
        {
            callFemm("mi_savedxf(\"{0}\")", filename);
        }

        #endregion

        #endregion

        #region FEMM magnetic post processor commands

        #region Data extraction commands

        #region Point values

        public class PointValues
        {
            public double A;//A vector potential A or flux φ
            public double B1;//B1 flux density Bx if planar, Br if axisymmetric
            public double B2;//B2 flux density By if planar, Bz if axisymmetric
            public double Sig;//Sig electrical conductivity σ
            public double E;//E stored energy density
            public double H1;//H1 field intensity Hx if planar, Hr if axisymmetric
            public double H2;//H2 field intensity Hy if planar, Hz if axisymmetric
            public double Je;//Je eddy current density
            public double Js;//Js source current density
            public double Mu1;//Mu1 relative permeability µx if planar, µr if axisymmetric
            public double Mu2;//Mu2 relative permeability µy if planar, µz if axisymmetric
            public double Pe;//Pe Power density dissipated through ohmic losses
            public double Ph;//Ph Power density dissipated by hysteresis
        }

        /// <summary>
        /// Measure values at point
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public PointValues mo_getpointvalues(double x, double y)
        {
            PointValues pv = new PointValues();

            String str = callFemm("mo_getpointvalues({0},{1})", x, y);
            String[] ss = str.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            double[] values = new double[13];
            for (int i = 0; i < 13; i++)
            {
                if (i < ss.Length)
                {
                    bool b = double.TryParse(ss[i], out values[i]);
                    if (!b)
                        values[i] = double.NaN;
                }
                else values[i] = double.NaN;
            }

            pv.A = values[0];
            pv.B1 = values[1];
            pv.B2 = values[2];
            pv.Sig = values[3];
            pv.E = values[4];
            pv.H1 = values[5];
            pv.H2 = values[6];
            pv.Je = values[7];
            pv.Js = values[8];
            pv.Mu1 = values[9];
            pv.Mu2 = values[10];
            pv.Pe = values[11];
            pv.Ph = values[12];

            return pv;
        }

        #endregion

        #region Line integral
        /**
         * mo lineintegral(type) Calculate the line integral for the defined contour
            type    | name              || values 1       | values 2      | values 3      | values 4
            0       | B.n               || total B.n      | avg B.n - 
            1       | H.t               || total H.t      | avg H.t - 
            2       | len+area          || length         | surface area - 
            3       | StressTensorForce || DC r/x force   |DC y/z force   |2× r/x force   |2× y/z force
            4       |StressTensorTorque || DC torque      |2× torque - 
            5       |(B.n)ˆ2            || total (B.n)ˆ2  | avg (B.n)ˆ2 -
            Returns typically two (possibly complex) values as results. For force and torque results, the
            2× results are only relevant for problems where ω != 0.
         * */
        public class LineIntegralResult
        {
            public double totalBn;//flux
            public double avgBn;//average Bn
            public double totalHt;//also: drop mmf follow the contour
            public double avgHt;
            public double length;
            public double surface_area;
            public double DCxForce;//for planar, but DC r force if axisymmetric
            public double DCyForce;//for planar, but DC z force if axisymmetric
            public double ACxForce;// if w!=0 (frequency of system !=0)
            public double ACyForce;// if w!=0
            public double DCtorque;
            public double ACtorque;
            public double totalBn2;
            public double avgBn2;
        }

        public enum LineIntegralType
        {
            Bn = 0,
            Ht = 1,
            Length = 2,
            Force = 3,
            Torque = 4,
            Bn2 = 5,
            all = 10
        }

        // accumulate into one lir
        public LineIntegralResult mo_lineintegral(LineIntegralType type, LineIntegralResult lir = null)
        {
            if (lir == null)
                lir = new LineIntegralResult();

            try
            {
                String str = callFemm("mo_lineintegral({0})", type.GetHashCode());
                String[] ss = str.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                switch (type)
                {
                    case LineIntegralType.Bn:
                        lir.totalBn = double.Parse(ss[0]);
                        lir.avgBn = double.Parse(ss[1]);
                        break;
                    case LineIntegralType.Ht:
                        lir.totalHt = double.Parse(ss[0]);
                        lir.avgHt = double.Parse(ss[1]);
                        break;
                    case LineIntegralType.Length:
                        lir.length = double.Parse(ss[0]);
                        lir.surface_area = double.Parse(ss[1]);
                        break;
                    case LineIntegralType.Force:
                        lir.DCxForce = double.Parse(ss[0]);
                        lir.DCyForce = double.Parse(ss[1]);
                        //may be 3,4th value
                        break;
                    case LineIntegralType.Torque:
                        lir.DCtorque = double.Parse(ss[0]);
                        //may be ac 
                        break;
                    case LineIntegralType.Bn2:
                        lir.totalBn2 = double.Parse(ss[0]);
                        lir.avgBn2 = double.Parse(ss[1]);
                        break;
                }

                return lir;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return lir;
            }
        }

        // get all result
        public LineIntegralResult mo_lineintegral_full()
        {
            LineIntegralResult lir = new LineIntegralResult();
            LineIntegralType type = 0;
            for (int i = 0; i <= 5; i++)
            {
                type = (LineIntegralType)Enum.ToObject(typeof(LineIntegralType), i);
                mo_lineintegral(type, lir);
            }

            return lir;
        }

        #endregion

        #region Block integral

        public enum BlockIntegralType
        {
            AJ = 0,
            A = 1,
            Magnetic_field_energy = 2,
            Losses_Hysteresis = 3,
            Losses_Resistive = 4,
            Area_Block_cross_section = 5,
            Losses_Total = 6,
            Current_Total = 7,
            Volume = 10,
            Steady_state_Lorentz_torque = 15,
            Magnetic_field_coenergy = 17,
            Steady_state_weighted_stress_tensor_torque = 22
        }

        public double mo_blockintegral(BlockIntegralType type)
        {
            String s = callFemm("mo_blockintegral({0})", type.GetHashCode());
            String[] ss = s.Split('\n');
            Complex c = Complex.Parse(ss[0]);
            return c.a;//get real part for now
        }

        #endregion

        #region Circuit properties

        public class CircuitProperties
        {
            public String name;
            public double current;
            public double volts;
            public double fluxlinkage;
        }

        /**
         * mo_getcircuitproperties("circuit") Used primarily to obtain impedance information
        associated with circuit properties. Properties are returned for the circuit property named
        "circuit". Three values are returned by the function. In order, these results are:
        – current Current carried by the circuit
        – volts Voltage drop across the circuit
        – flux_re Circuit’s flux linkage
         * */
        public CircuitProperties mo_getcircuitproperties(String circuit)
        {
            String str = callFemm("mo_getcircuitproperties(\"{0}\")", circuit);
            String[] ss = str.Split('\n');
            CircuitProperties cp = new CircuitProperties();
            cp.name = circuit;
            cp.current = double.Parse(ss[0]);
            cp.volts = double.Parse(ss[1]);
            cp.fluxlinkage = double.Parse(ss[2]);
            return cp;
        }

        #endregion

        #endregion

        #region Select commands

        public enum SelectMode
        {
            point = 0,
            contour = 1,
            area = 2
        }

        /**
         * mo seteditmode(mode) Sets the mode of the postprocessor to point, contour, or area mode.
            Valid entries for mode are "point", "contour", and "area".*/
        public void mo_seteditmode(SelectMode mode)
        {
            callFemm("mo_seteditmode(\"{0}\")", mode);
        }

        /**
         * • mo selectblock(x,y) Select the block that contains point (x,y).
         * */
        public void mo_selectblock(double x, double y)
        {
            callFemm("mo_selectblock({0},{1})", x, y);
        }

        /**
         * mo groupselectblock(n) Selects all of the blocks that are labeled by block labels that are
        members of group n. If no number is specified (i.e. mo groupselectblock() ), all blocks
        are selected.
         * */
        public void mo_groupselectblock(int n)
        {
            callFemm("mo_groupselectblock({0})", n);
        }

        /** • mo addcontour(x,y) Adds a contour point at (x,y). If this is the first point then it starts a
        contour, if there are existing points the contour runs from the previous point to this point.
        The mo addcontour command has the same functionality as a right-button-click contour
        point addition when the program is running in interactive mode.
         */
        public void mo_addcontour(double x, double y)
        {
            callFemm("mo_addcontour({0},{1})", x, y);
        }

        /** • mo bendcontour(angle,anglestep) Replaces the straight line formed by the last two
        points in the contour by an arc that spans angle degrees. The arc is actually composed
        of many straight lines, each of which is constrained to span no more than anglestep degrees. The angle parameter can take on values from -180 to 180 degrees. The anglestep
        parameter must be greater than zero. If there are less than two points defined in the contour,
        this command is ignored.
         */
        public void mo_bendcontour(double angle, double anglestep)
        {
            callFemm("mo_bendcontour({0},{1})", angle, anglestep);
        }


        /// <summary>
        /// Adds a contour point at the closest input point to (x,y). 
        /// If the selected point and a previous selected points lie at the ends of an arcsegment, a contour is added
        /// that traces along the arcsegment. The mo selectpoint command has the same functionality as the left-button-click 
        /// contour point selection when the program is running in interactive mode.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void mo_selectpoint(double x, double y)
        {
            callFemm("mo_selectpoint({0},{1})", x, y);
        }

        /**
        • mo clearcontour() Clear a prevously defined contour
        • mo clearblock() Clear block selection
         * */
        public void mo_clearcontour()
        {
            callFemm("mo_clearcontour()");
        }
        public void mo_clearblock()
        {
            callFemm("mo_clearblock()");
        }
        #endregion

        #region Zoom commands
        /** mo_zoomnatural() Zoom to the natural boundaries of the geometry.
            • mo_zoomin() Zoom in one level.
            • mo_zoomout() Zoom out one level.
            • mo zoom(x1,y1,x2,y2) Zoom to the window defined by lower left corner (x1,y1) and upper
            right corner (x2,y2).*/
        public void mo_zoomnatural()
        {
            callFemm("mo_zoomnatural()");
        }
        public void mo_zoomin()
        {
            callFemm("mo_zoomin()");
        }
        public void mo_zoomout()
        {
            callFemm("mo_zoomout()");
        }
        public void mo_zoom(double x1, double y1, double x2, double y2)
        {
            callFemm("mo_zoom({0},{1},{2},{3})", x1, y1, x2, y2);
        }
        #endregion

        #region View commands

        /** 
         * mo_hidedensityplot() hides the flux density plot.        
         */
        public void mo_hidedensityplot()
        {
            callFemm("mo_hidedensityplot()");
        }

        /**
         * • mo_showdensityplot(legend,gscale,upper_B,lower_B,type) Shows the flux density
        plot with options:
        96
        – legend Set to 0 to hide the plot legend or 1 to show the plot legend.
        – gscale Set to 0 for a colour density plot or 1 for a grey scale density plot.
        – upper_B Sets the upper display limit for the density plot.
        – lower_B Sets the lower display limit for the density plot.
        – type Type of density plot to display. Valid entries are "bmag", "breal", and "bimag"
        for magnitude, real component, and imaginary component of flux density (B), respectively; "hmag", "hreal", and "himag" for magnitude, real component, and imaginary
        component of field intensity (H ); and "jmag", "jreal", and "jimag" for magnitude,
        real component, and imaginary component of current density (J ).
        if legend is set to -1 all parameters are ignored and default values are used e.g.:
        mo_showdensityplot(-1)
         * */
        public void mo_showdensityplot(bool showlegend, bool grayscale, double lowerlimit, double upperlimit, DensityPlotType type)
        {
            callFemm("mo_showdensityplot({0},{1},{2},\"{3}\")", showlegend ? 1 : 0, grayscale ? 1 : 0, upperlimit, lowerlimit, type);
        }

        public enum DensityPlotType
        {
            bmag = 0,
            breal = 1,
            bimag = 2,
            hmag = 3,
            hreal = 4,
            himag = 5,
            jmag = 6,
            jreal = 7,
            jimag = 8
        }

        /**
         * mo_hidecontourplot() Hides the contour plot.
         * */
        public void mo_hidecontourplot()
        {
            callFemm("mo_hidecontourplot()");
        }

        /**
        • mo_showcontourplot(numcontours,lower_A,upper_A,type) shows the A contour plot
        with options:
        – numcontours Number of A equipotential lines to be plotted.
        – upper_A Upper limit for A contours.
        – lower_A Lower limit for A contours.
        – type Choice of "real", "imag", or "both" to show either the real, imaginary of both
        real and imaginary components of A.
        If numcontours is -1 all parameters are ignored and default values are used, e.g.:
        mo_showcontourplot(-1)
         * */
        public void mo_showcontourplot(int numcontours, double lowerlimit, double upperlimit, ContourPlotType type)
        {
            callFemm("mo_showcontourplot({0},{1},{2},{3})", numcontours, lowerlimit, upperlimit, type);
        }

        public enum ContourPlotType
        {
            real = 0,
            imag = 1,
            both = 2
        }

        // vector here but later

        /**
         * mo minimize minimizes the active magnetics output view.
        • mo maximize maximizes the active magnetics output view.
        • mo restore restores the active magnetics output view from a minimized or maximized state.
        • mo resize(width,height) resizes the active magnetics output window client area to width
        × height.
         * */
        public void mo_minimize()
        {
            callFemm("mo_minimize()");
        }

        public void mo_maximize()
        {
            callFemm("mo_maximize()");
        }

        public void mo_restore()
        {
            callFemm("mo_restore()");
        }

        public void mo_resize(int w, int h)
        {
            callFemm("mo_resize({0},{1})", w, h);
        }

        #endregion

        #region Misc

        /**
         * mo close() Closes the current post-processor instance.*/
        public void mo_close()
        {
            callFemm("mo_close()");
        }

        /**
        • mo refreshview() Redraws the current view.*/
        public void mo_refreshview()
        {
            callFemm("mo_refreshview()");
        }

        /**
        • mo reload() Reloads the solution from disk.*/
        public void mo_reload()
        {
            callFemm("mo_reload()");
        }

        /**
        • mo savebitmap("filename") saves a bitmapped screen shot of the current view to the file
        specified by "filename". Note that if you use a path you must use two backslashes (e.g.
        "c:\\temp\\myfemmfile.fem"). If the file name contains a space (e.g. file names like
        c:\program files\stuff) you must enclose the file name in (extra) quotes by using a \"
        sequence. For example:
        mo_save_bitmap("\"c:\\temp\\screenshot.bmp\"") */
        public void mo_savebitmap(String filename)
        {
            callFemm("mo_savebitmap(\"{0}\")", filename.Replace('\\', '/'));
        }

        /**
        • mo savemetafile("filename") saves a metafile screenshot of the current view to the file
        specified by "filename", subject to the printf-type formatting explained previously for
        the savebitmap command.
         * */
        public void mo_savemetafile(String filename)
        {
            callFemm("mo_savemetafile(\"{0}\")", filename.Replace('\\', '/'));
        }

        #region Elements-methods

        //monumnodes()Returns the number of nodes in the in focus magnetics output mesh.
        public int mo_numnodes()
        {
            String str = callFemm("mo_numnodes()");
            String[] ss = str.Split('\n');
            if (ss.Length == 0)
                return 0;

            int n = 0;
            bool b = int.TryParse(ss[0], out n);
            return n;
        }

        //• mo_numelements()Returns the number of elements in the in focus magnets outputmesh.
        public int mo_numelements()
        {
            String str = callFemm("mo_numelements()");
            String[] ss = str.Split('\n');
            if (ss.Length == 0)
                return 0;

            int n = 0;
            bool b = int.TryParse(ss[0], out n);
            return n;
        }

        /// <summary>
        /// mo_getnode(n) Returns the(x, y) or(r, z) position of the nth mesh node.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public PointD mo_getnode(int n)
        {
            String str = callFemm("mo_getnode({0})", n);
            String[] ss = str.Split('\n');
            PointD p = new PointD();
            if (ss.Length < 2)
                return p;

            p.X = double.Parse(ss[0]);
            p.Y = double.Parse(ss[1]);

            return p;
        }

        [Serializable]
        public class Element
        {
            /// <summary>
            /// Indices of nodes of this element (3 point made up this triangle)
            /// </summary>
            public int[] nodes;

            /// <summary>
            /// Center point of triangle
            /// </summary>
            public PointD center;

            /// <summary>
            /// element area using the length unit defined for the problem
            /// </summary>
            public double area;

            /// <summary>
            /// group number associated with the element
            /// </summary>
            public int group;
        }

        //• mogetelement(n)MOGetElement[n] returns the following proprerties for thenth element:
        //1. Index of first element node
        //2. Index of second element node
        //3. Index of third element node
        //4. x(or r) coordinate of the element centroid
        //5. y(or z) coordinate of the element centroid
        //6. element area using the length unit defined for the problem
        //7. group number associated with the element
        public Element mo_getelement(int n)
        {
            String str = callFemm("mo_getelement({0})", n);
            String[] ss = str.Split('\n');

            Element e = new Element()
            {
                nodes = new int[3]
                {
                    int.Parse(ss[0]),
                    int.Parse(ss[1]),
                    int.Parse(ss[2]),
                },

                center = new PointD()
                {
                    X = double.Parse(ss[3]),
                    Y = double.Parse(ss[4]),
                },

                area = double.Parse(ss[5]),
                group = int.Parse(ss[6]),
            };

            return e;
        }

        #endregion

        #endregion

        #endregion

        #region Extended commands

        public double epsilon = 1e-8;

        /// <summary>
        /// add a segment and add it to a group
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="group"></param>
        public void mi_addSegmentEx(double x1, double y1, double x2, double y2, int group, bool addnode = true)
        {
            // if too small, just quit
            if (Math.Abs(x2 - x1) < epsilon && Math.Abs(y2 - y1) < epsilon)
                return;

            if (addnode)
            {
                mi_addnode(x1, y1);
                mi_addnode(x2, y2);
            }

            mi_addsegment(x1, y1, x2, y2);
            mi_clearselected();
            mi_selectsegment((x1 + x2) / 2, (y1 + y2) / 2);
            mi_setsegmentprop("", 0, false, false, group);
        }

        /// <summary>
        /// Add an arc segment and add it to a group. If addnode = true, nodes (x1,y1),(x2,y2) will be added as well
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="angle">0-180 degree</param>
        /// <param name="maxdegseg"></param>
        /// <param name="group"></param>
        /// <param name="addnode"></param>
        public void mi_addArcEx(double x1, double y1, double x2, double y2, double angle, double maxdegseg, int group, bool addnode = true)
        {
            // if too small, just quit
            if (Math.Abs(x2 - x1) < epsilon && Math.Abs(y2 - y1) < epsilon)
                return;

            if (addnode)
            {
                mi_addnode(x1, y1);
                mi_addnode(x2, y2);
            }

            // add an arc
            if (angle >= 0)
                mi_addarc(x1, y1, x2, y2, angle, maxdegseg);
            else mi_addarc(x2, y2, x1, y1, -angle, maxdegseg);

            //calculate the middle point of the arc
            double d = Math.Sqrt((y2 - y1) * (y2 - y1) + (x2 - x1) * (x2 - x1));
            double vx = (y2 - y1) / d;
            double vy = -(x2 - x1) / d;
            double mn = d / 2 * Math.Tan(angle / 4 * Math.PI / 180);
            double x = (x2 + x1) / 2 + mn * vx;
            double y = (y2 + y1) / 2 + mn * vy;

            // select the segment and set its group
            mi_clearselected();
            mi_selectarcsegment(x, y);
            mi_setarcsegmentprop(maxdegseg, "", false, group);
        }

        /// <summary>
        /// Add a block label
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockname">Name of this block (material name)</param>
        /// <param name="group">Which group this block belongs</param>
        /// <param name="magnetdirection">Magnet direction if this is magnet block</param>
        /// <param name="incircuit">Circuit name if this block is conductor</param>
        /// <param name="turns">How many conductors in this block</param>
        /// <param name="automesh">True if Mesh automatic</param>
        /// <param name="meshsize">Size of mesh if automesh = false</param>
        public void mi_addBlockLabelEx(double x, double y, String blockname, int group,
            double magnetdirection, String incircuit, int turns, bool automesh, double meshsize)
        {
            mi_addblocklabel(x, y);
            mi_clearselected();
            mi_selectlabel(x, y);
            mi_setblockprop(blockname, automesh, meshsize, incircuit, magnetdirection, group, turns);
        }

        /// <summary>
        /// Add magnet block
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockname"></param>
        /// <param name="group"></param>
        /// <param name="magdirection"></param>
        public void mi_addBlockLabelEx(double x, double y, String blockname, int group, double magdirection)
        {
            mi_addBlockLabelEx(x, y, blockname, group, magdirection, "", 0, true, 0);
        }

        /// <summary>
        /// Add a coil, conductor block
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockname"></param>
        /// <param name="group"></param>
        /// <param name="incircuit"></param>
        /// <param name="turns"></param>
        public void mi_addBlockLabelEx(double x, double y, String blockname, int group, String incircuit, int turns)
        {
            mi_addBlockLabelEx(x, y, blockname, group, 0, incircuit, turns, true, 0);
        }

        /// <summary>
        /// Add a normal block like air or steel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="blockname"></param>
        /// <param name="group"></param>
        public void mi_addBlockLabelEx(double x, double y, String blockname, int group)
        {
            mi_addBlockLabelEx(x, y, blockname, group, 0, "", 0, true, 0);
        }

        #endregion

        #region Extension function that modify directly on FEMM file (since no function supports)

        private static object lock_access_femm_file = new object();

        /// <summary>
        /// Modify comment, only work on non-opened file
        /// </summary>
        /// <param name="femmFile"></param>
        /// <param name="comment"></param>
        public static void mi_modifyFEMMComment(String femmFile, String comment)
        {
            lock (lock_access_femm_file)
            {
                String str;
                using (StreamReader sr = new StreamReader(femmFile))
                {
                    str = sr.ReadToEnd();
                    int index = str.IndexOf("[Comment]");
                    index = str.IndexOf('\"', index);
                    int index2 = str.IndexOf('\"', index + 1);
                    str = str.Remove(index, index2 - index + 1);
                    str = str.Insert(index, "\"" + comment + "\"");
                }

                using (StreamWriter sw = new StreamWriter(femmFile))
                {
                    sw.Write(str);
                }
            }
        }

        public static String mi_getFEMMComment(String femmFile)
        {
            String str = null;

            lock (lock_access_femm_file)
            {
                using (StreamReader sr = new StreamReader(femmFile))
                {
                    str = sr.ReadToEnd();
                    int index = str.IndexOf("[Comment]");
                    index = str.IndexOf('\"', index);
                    int index2 = str.IndexOf('\"', index + 1);
                    str = str.Substring(index + 1, index2 - index - 1);
                }
            }
            return str;
        }

        #endregion

        #region Utils

        public class Complex
        {
            public double a;
            public double b;

            public Complex(double a, double b)
            {
                this.a = a;
                this.b = b;
            }

            /// <summary>
            /// Convert string format a+I*b (FEMM format) to complex 
            /// </summary>
            /// <param name="complex_number"></param>
            /// <returns></returns>
            public static Complex Parse(string complex_number)
            {
                complex_number = complex_number.Replace(" ", "").ToUpper();//remove spaces, make sure i,I become I
                int i = complex_number.IndexOf("I*");
                if (i < 0)
                    return new Complex(double.Parse(complex_number), 0);

                string s_a = complex_number.Substring(0, i - 1);//before the +/- before I*
                string s_b = complex_number.Substring(i - 1).Replace("I*", "");

                return new Complex(double.Parse(s_a), double.Parse(s_b));
            }
        }

        #endregion
    }
}
