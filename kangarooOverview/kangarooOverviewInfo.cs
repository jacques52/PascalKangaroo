using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using IntraLattice.CORE.Helpers;
using KangarooSolver;
using Rhino.Geometry;
using KangarooSolver.Goals;
namespace IntraLattice.CORE.Components.kangaroo
{
    public class kangarooTest : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the kangarooTest class.
        /// </summary>
        public kangarooTest()
            : base("kangarooTest", "Nickname",
                "Description",
                "Category", "Subcategory")
        {
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("point", "point", "", GH_ParamAccess.tree);
            pManager.AddMeshParameter("mesh", "mesh", "mesh", GH_ParamAccess.item);

            pManager.AddIntegerParameter("iters", "", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("anchor strength", "", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("spring stiffness", "", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("spring rest", "", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("colinear strength", "", "", GH_ParamAccess.item);
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("0", "", "", GH_ParamAccess.list);
            pManager.AddGenericParameter("1", "", "", GH_ParamAccess.list);
            pManager.AddGenericParameter("2", "", "", GH_ParamAccess.list);
            pManager.AddGenericParameter("3", "", "", GH_ParamAccess.list);
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> inputTree;
            Mesh snapMesh = new Mesh();
            int iters = 0;
            var anchorStrength = new GH_Number();
            var springStiff = new GH_Number();
            var springRest = new GH_Number();
            var colinearStrength = new GH_Number();

            if (!DA.GetDataTree(0, out inputTree)) { return; };
            if (!DA.GetData(1, ref snapMesh)) { return; };
            if (!DA.GetData(2, ref iters)) { return; };
            if (!DA.GetData(3, ref anchorStrength)) { return; };
            if (!DA.GetData(4, ref springStiff)) { return; };
            if (!DA.GetData(5, ref springRest)) { return; };
            if (!DA.GetData(6, ref colinearStrength)) { return; };

            var PS = new PhysicalSystem();
            List<IGoal> Goals = new List<IGoal>();
            List<Point3d> pointList = SimpleConverter.convertGHPoints(inputTree);//This is a simple conversion to a flattened pointset
            #region anchorPoints

            //#region snap to edge - here we are going to create our anchor points
            List<int> iValence = ImplementationTools.FindNeighboursRTree(pointList);//This refences our rtree setup in tools to find valence
            List<Point3d> anchorPoints = new List<Point3d>();
            for (int i = 0; i < pointList.Count(); i++)
            {
                var snapPoint = snapMesh.ClosestPoint(pointList[i]);
                var dist = pointList[i].DistanceTo(snapPoint);
                if (dist <= 4.3)
                {
                    anchorPoints.Add(pointList[i]);
                    var anchor = new Anchor(pointList[i], anchorStrength.Value);
                    Goals.Add(anchor);
                }
            }
            #endregion
            #region coLinear and springs
            //var msh = new OnMesh(anchorPoints, snapMesh, 100);
            //Goals.Add(msh);
            var crvs = new List<Curve>();
            for (int i = 0; i < pointList.Count; i += 8)
            {
                var curPts = new List<Point3d>();
                //var curInds = new List<int>();
                for (int j = 0; j < 8; j++)
                {
                    curPts.Add(pointList[i + j]);
                    //curInds.Add(i+j);
                }
                crvs.Add(new LineCurve(curPts[0], curPts[6]));
                crvs.Add(new LineCurve(curPts[1], curPts[7]));
                crvs.Add(new LineCurve(curPts[2], curPts[4]));
                crvs.Add(new LineCurve(curPts[3], curPts[5]));
                // var spring0 = new Spring(curPts[0], curPts[6], springRest.Value, springStiff.Value);
                //var spring1 = new Spring(curPts[1], curPts[7], springRest.Value, springStiff.Value);
                //var spring2 = new Spring(curPts[2], curPts[4], springRest.Value, springStiff.Value);
                //var spring3 = new Spring(curPts[3], curPts[5], springRest.Value, springStiff.Value);
                var clst0 = new List<Point3d> { curPts[0], curPts[4] };
                var colinear0 = new CoLinear(clst0, colinearStrength.Value);

                var clst1 = new List<Point3d> { curPts[1], curPts[5] };
                var colinear1 = new CoLinear(clst1, colinearStrength.Value);

                var clst2 = new List<Point3d> { curPts[2], curPts[6] };
                var colinear2 = new CoLinear(clst2, colinearStrength.Value);

                var clst3 = new List<Point3d> { curPts[3], curPts[7] };
                var colinear3 = new CoLinear(clst3, colinearStrength.Value);
                Goals.Add(colinear0);
                Goals.Add(colinear1);
                Goals.Add(colinear2);
                Goals.Add(colinear3);
                // Goals.Add(spring0);
                //Goals.Add(spring1);//
                //Goals.Add(spring2);
                //Goals.Add(spring3);
            }
            #endregion

            var length = new EqualLength(crvs, 1);
            Goals.Add(length);

            foreach (var goal in Goals)
            {
                PS.AssignPIndex(goal, .001, true);
            }

            //here we step the solver
            for (int i = 0; i < iters; i++)
            {
                 PS.Step(Goals, true, .0001);
             }

            var outs = PS.GetOutput(Goals);
            
                //the index is present in the goal afer one step. the goal class stores these. 

            // DA.SetDataList(0, PS.GetPositions());
            //DA.SetDataList(1, crvs);
            DA.SetDataList(2, anchorPoints);
            DA.SetDataList(3, Goals);
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{2044edc9-2e6a-4d32-943d-04a073cc2e0e}"); }
        }
    }
}