﻿using IntraLattice.CORE.Helpers;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Summary:     This class is used to validate and format a unit cell.
//              Refer to the developer documentation for more information.
// =====================================================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Data
{
    /// <summary>
    /// Represents the unit cell, in a manner which can be used to map topology to a UVWi node tree, without creating duplicate nodes/struts.
    /// </summary>
    public class UnitCell
    {
        #region Fields
        private Point3dList m_nodes;
        private List<IndexPair> m_nodePairs;
        private List<int[]> m_nodePaths;
        private Point3dList m_middleNodes;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public UnitCell()
        {
            m_nodes = new Point3dList();
            m_nodePairs = new List<IndexPair>();
            m_nodePaths = new List<int[]>();
            m_middleNodes = new Point3dList();


        }
        /// <summary>
        /// Instance constructor based on a list of lines.
        /// </summary>
        public UnitCell(List<Line> rawCell)
        {
            m_nodes = new Point3dList();
            m_nodePairs = new List<IndexPair>();
            m_nodePaths = new List<int[]>();

            ExtractTopology(rawCell);
            NormaliseTopology();
        }
        public UnitCell(List<Line> rawCell, bool normalise)
        {

            m_nodes = new Point3dList();
            m_nodePairs = new List<IndexPair>();
            m_nodePaths = new List<int[]>();

            ExtractTopology(rawCell);
            if (normalise)
            {
                NormaliseTopology();
            }
        }

        public UnitCell(List<Line> rawCell, bool normalise, List<Point3d> bbx)
        {

            m_nodes = new Point3dList();
            m_nodePairs = new List<IndexPair>();
            m_nodePaths = new List<int[]>();

            ExtractTopology(rawCell);
            NormaliseTopology(bbx);

        }
        /// <summary>
        /// Copy constructor.
        /// </summary>
        public UnitCell Duplicate()
        {
            var dup = new UnitCell();
            foreach (Point3d node in Nodes)
            {
                dup.Nodes.Add(node);
            }
            foreach (IndexPair nodePair in NodePairs)
            {
                dup.NodePairs.Add(nodePair);
            }
            foreach (int[] nodePath in NodePaths)
            {
                dup.NodePaths.Add(new int[4] { nodePath[0], nodePath[1], nodePath[2], nodePath[3] });
            }
            return dup;
        }
        #endregion

        #region Properties
        /// <summary>
        /// List of unique nodes
        /// </summary>
        public Point3dList Nodes
        {
            get { return m_nodes; }
            set { m_nodes = value; }
        }
        /// <summary>
        /// List of struts as node index pairs.
        /// </summary>
        public List<IndexPair> NodePairs
        {
            get { return m_nodePairs; }
            set { m_nodePairs = value; }
        }
        /// <summary>
        /// List of relative paths in tree. (parallel to Nodes list)
        /// </summary>
        public List<int[]> NodePaths
        {
            get { return m_nodePaths; }
            set { m_nodePaths = value; }
        }
        /// <summary>
        /// Verifies validity of unit cell.
        /// </summary>
        public bool isValid
        {
            get
            {
                int flag = this.CheckValidity();
                if (flag == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Formats the line input into the UnitCell object. It converts the list of lines into
        /// a list of unique nodes and unique node pairs, ignoring duplicates.
        /// </summary>
        private void ExtractTopology(List<Line> lines)
        {
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            ImplementationTools.FixIntersections(ref lines);

            // Iterate through list of lines
            foreach (Line line in lines)
            {
                // Get line, and it's endpoints
                Point3d[] pts = new Point3d[] { line.From, line.To };
                List<int> nodeIndices = new List<int>();

                // Loop over end points, being sure to not create the same node twice
                foreach (Point3d pt in pts)
                {
                    int closestIndex = this.Nodes.ClosestIndex(pt);  // find closest node to current pt
                    // If node already exists
                    if (this.Nodes.Count != 0 && this.Nodes[closestIndex].EpsilonEquals(pt, tol))
                    {
                        nodeIndices.Add(closestIndex);
                    }
                    // If it doesn't exist, add it
                    else
                    {
                        this.Nodes.Add(pt);
                        nodeIndices.Add(this.Nodes.Count - 1);
                    }
                }

                IndexPair nodePair = new IndexPair(nodeIndices[0], nodeIndices[1]);
                // If not duplicate strut, save it
                if (this.NodePairs.Count == 0 || !NodePairs.Contains(nodePair))
                {
                    this.NodePairs.Add(nodePair);
                }

            }
        }
        /// <summary>
        /// Scales the unit cell down to unit size (1x1x1) and moves it to the origin.
        /// </summary>
        private void NormaliseTopology()
        {
            // Get the bounding box size (check for extreme values)
            var xRange = new Interval();
            var yRange = new Interval();
            var zRange = new Interval();
            foreach (Point3d node in this.Nodes)
            {
                if (node.X < xRange.T0) xRange.T0 = node.X;
                if (node.X > xRange.T1) xRange.T1 = node.X;
                if (node.Y < yRange.T0) yRange.T0 = node.Y;
                if (node.Y > yRange.T1) yRange.T1 = node.Y;
                if (node.Z < zRange.T0) zRange.T0 = node.Z;
                if (node.Z > zRange.T1) zRange.T1 = node.Z;
            }

            // Move cell to origin (i.e. move all nodes)
            Vector3d toOrigin = new Vector3d(-xRange.T0, -yRange.T0, -zRange.T0);
            this.Nodes.Transform(Transform.Translation(toOrigin));
            // Normalise to 1x1x1 bounding box size
            this.Nodes.Transform(Transform.Scale(Plane.WorldXY, 1 / xRange.Length, 1 / yRange.Length, 1 / zRange.Length));
        }

        /// <summary>
        /// Scales the unit cell down to unit size (1x1x1) and moves it to the origin.
        /// </summary>
        private void NormaliseTopology(List<Point3d> bbx)
        {
            //List<Point3d> boxset = SimpleConverter.convertGHPoints(bbx);

            // Get the bounding box size (check for extreme values)
            var xRange = new Interval(double.MaxValue, double.MinValue);
            var yRange = new Interval(double.MaxValue, double.MinValue);
            var zRange = new Interval(double.MaxValue, double.MinValue);
            foreach (Point3d node in bbx)
            {
                if (node.X < xRange.T0)
                    xRange.T0 = node.X;
                if (node.X > xRange.T1)
                    xRange.T1 = node.X;

                if (node.Y < yRange.T0)
                    yRange.T0 = node.Y;
                if (node.Y > yRange.T1)
                    yRange.T1 = node.Y;

                if (node.Z < zRange.T0)
                    zRange.T0 = node.Z;
                if (node.Z > zRange.T1)
                    zRange.T1 = node.Z;
            }
            // Move cell to origin (i.e. move all nodes)
            Vector3d toOrigin = new Vector3d(-xRange.T0, -yRange.T0, -zRange.T0);
            this.Nodes.Transform(Transform.Translation(toOrigin));
            // Normalise to 1x1x1 bounding box size
            this.Nodes.Transform(Transform.Scale(Plane.WorldXY, 1 / xRange.Length, 1 / yRange.Length, 1 / zRange.Length));
        }

        /// <summary>
        /// Checks validity of the unit cell. Note that the cell should be extracted and normalised before running this method.
        /// </summary>
        /// <returns>
        /// -1 : Invalid - opposing faces must have mirror nodes (continuity)
        ///  0 : Invalid - all faces must have at least 1 node (continuity)
        ///  1 : Valid
        /// </returns>
        public int CheckValidity()
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // The check - Opposing faces must be identical
            // Set up the face planes
            Plane[] xy = new Plane[2];
            xy[0] = new Plane(new Point3d(0, 0, 0), Plane.WorldXY.ZAxis);
            xy[1] = new Plane(new Point3d(0, 0, 1), Plane.WorldXY.ZAxis);
            Plane[] yz = new Plane[2];
            yz[0] = new Plane(new Point3d(0, 0, 0), Plane.WorldXY.XAxis);
            yz[1] = new Plane(new Point3d(1, 0, 0), Plane.WorldXY.XAxis);
            Plane[] zx = new Plane[2];
            zx[0] = new Plane(new Point3d(0, 0, 0), Plane.WorldXY.YAxis);
            zx[1] = new Plane(new Point3d(0, 1, 0), Plane.WorldXY.YAxis);

            // To make sure each pair of faces has a node lying on it
            bool[] minCheck = new bool[3] { false, false, false };

            // Loop through nodes
            foreach (Point3d node in this.Nodes)
            {
                // Essentially, for every node, we must find it's mirror node on the opposite face
                // First, check if node requires a mirror node, and where that mirror node should be (testPoint)
                Point3d testPoint = Point3d.Unset;

                // XY faces
                if (Math.Abs(xy[0].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(node.X, node.Y, xy[1].OriginZ);
                    minCheck[0] = true;
                }
                if (Math.Abs(xy[1].DistanceTo(node)) < tol)
                    testPoint = new Point3d(node.X, node.Y, xy[0].OriginZ);
                // YZ faces
                if (Math.Abs(yz[0].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(yz[1].OriginX, node.Y, node.Z);
                    minCheck[1] = true;
                }
                if (Math.Abs(yz[1].DistanceTo(node)) < tol)
                    testPoint = new Point3d(yz[0].OriginX, node.Y, node.Z);
                // ZX faces
                if (Math.Abs(zx[0].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(node.X, zx[1].OriginY, node.Z);
                    minCheck[2] = true;
                }
                if (Math.Abs(zx[1].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(node.X, zx[0].OriginY, node.Z);
                }

                // Now, check if the mirror node exists
                //if (testPoint != Point3d.Unset)
                //{
                //    if (testPoint.X != 1 && testPoint.Y !=1 && testPoint.)
                //    if (testPoint.DistanceTo(this.Nodes[this.Nodes.ClosestIndex(testPoint)]) > tol)
                //    {
                //        return -1;
                //    }

                //}
            }

            // Finally, ensure that all faces have a node on it (only need to check 3 faces, since mirror condition ensures the others)
            if (minCheck[0] == false || minCheck[1] == false || minCheck[2] == false)
            {
                return 0;
            }

            return 1;
        }
        /// <summary>
        /// Defines relative paths of nodes for node pairing.
        /// ASSUMPTION: valid, normalized unit cell.
        /// </summary>
        public void FormatTopology()
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Set up boundary planes (struts and nodes on these planes belong to other cells)
            Plane xy = Plane.WorldXY; xy.Translate(new Vector3d(0, 0, 1));
            Plane yz = Plane.WorldYZ; yz.Translate(new Vector3d(1, 0, 0));
            Plane zx = Plane.WorldZX; zx.Translate(new Vector3d(0, 1, 0));

            // Create the relative uvw tree paths, refer to dev docs for better understanding
            foreach (Point3d node in Nodes)
            {
                // Check top plane first
                if (Math.Abs(xy.DistanceTo(node)) < tol)
                {
                    // Node belongs to 1,1,1 neighbour
                    if (node.DistanceTo(new Point3d(1, 1, 1)) < tol)
                    {
                        NodePaths.Add(new int[] { 1, 1, 1, Nodes.ClosestIndex(new Point3d(0, 0, 0)) });
                    }
                    // Node belongs to 1,0,1 neighbour
                    else if (Math.Abs(node.X - 1) < tol && Math.Abs(node.Z - 1) < tol)
                    {
                        NodePaths.Add(new int[] { 1, 0, 1, Nodes.ClosestIndex(new Point3d(0, node.Y, 0)) });
                    }
                    // Node belongs to 0,1,1 neighbour
                    else if (Math.Abs(node.Y - 1) < tol && Math.Abs(node.Z - 1) < tol)
                    {
                        NodePaths.Add(new int[] { 0, 1, 1, Nodes.ClosestIndex(new Point3d(node.X, 0, 0)) });
                    }
                    // Node belongs to 0,0,1 neighbour
                    else
                    {
                        NodePaths.Add(new int[] { 0, 0, 1, Nodes.ClosestIndex(new Point3d(node.X, node.Y, 0)) });
                    }
                }
                // Check yz boundary plane
                else if (Math.Abs(yz.DistanceTo(node)) < tol)
                {
                    // Node belongs to 1,1,0 neighbour
                    if (Math.Abs(node.X - 1) < tol && Math.Abs(node.Y - 1) < tol)
                    {
                        NodePaths.Add(new int[] { 1, 1, 0, Nodes.ClosestIndex(new Point3d(0, 0, node.Z)) });
                    }
                    // Node belongs to 1,0,0 neighbour      
                    else
                    {
                        NodePaths.Add(new int[] { 1, 0, 0, Nodes.ClosestIndex(new Point3d(0, node.Y, node.Z)) });
                    }
                }
                // Check last boundary plane
                // Node belongs to 0,1,0 neighbour
                else if (Math.Abs(zx.DistanceTo(node)) < tol)
                {
                    NodePaths.Add(new int[] { 0, 1, 0, Nodes.ClosestIndex(new Point3d(node.X, 0, node.Z)) });
                }
                // If not on those planes, the node belongs to the current cell
                else
                {
                    NodePaths.Add(new int[] { 0, 0, 0, Nodes.IndexOf(node) });
                }
            }

            // Now locate any struts that lie on the boundary planes
            List<int> strutsToRemove = new List<int>();
            for (int i = 0; i < this.NodePairs.Count; i++)
            {
                Point3d node1 = this.Nodes[this.NodePairs[i].I];
                Point3d node2 = this.Nodes[this.NodePairs[i].J];

                bool toRemove = false;

                if (Math.Abs(xy.DistanceTo(node1)) < tol && Math.Abs(xy.DistanceTo(node2)) < tol) toRemove = true;
                if (Math.Abs(yz.DistanceTo(node1)) < tol && Math.Abs(yz.DistanceTo(node2)) < tol) toRemove = true;
                if (Math.Abs(zx.DistanceTo(node1)) < tol && Math.Abs(zx.DistanceTo(node2)) < tol) toRemove = true;

                if (toRemove)
                {
                    strutsToRemove.Add(i);
                }
            }
            strutsToRemove.Reverse();
            foreach (int strutToRemove in strutsToRemove) this.NodePairs.RemoveAt(strutToRemove);

        }
        #endregion

    }
}