using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using IntraLattice.CORE.Data;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Collections;
using System.Threading.Tasks;

namespace IntraLattice.CORE.Helpers
{
    class ImplementationTools
    {
        public static void normalizeSurfaces(Surface s1, Surface s2)
        {
            Interval unitDomain = new Interval(0, 1);
            s1.SetDomain(0, unitDomain); // s1 u-direction
            s1.SetDomain(1, unitDomain); // s1 v-direction
            s2.SetDomain(0, unitDomain); // s2 u-direction
            s2.SetDomain(1, unitDomain); // s2 v-direction
        }

        //MAKE THIS A STATIC FUNCTION FOR ALL COMPONENTS TO USE
        public static List<NurbsSurface> generateSurfaces()
        {
            NurbsSurface s1;
            NurbsSurface s2;
            var cellCorners = new List<Point3d>();
            ImplementationTools.MakeCornerNodes(ref cellCorners, 1);//get corner nodes

            List<NurbsSurface> surfaces = new List<NurbsSurface>();

            for (int i = 0; i < cellCorners.Count; i = i + 8)
            {
                s1 = NurbsSurface.CreateFromCorners(cellCorners[i], cellCorners[i + 1], cellCorners[i + 2], cellCorners[i + 3]);
                s2 = NurbsSurface.CreateFromCorners(cellCorners[i + 4], cellCorners[i + 5], cellCorners[i + 6], cellCorners[i + 7]);

                normalizeSurfaces(s1, s2);

                surfaces.Add(s1);
                surfaces.Add(s2);
            }

            return surfaces;
        }

        public static GH_Structure<GH_Point> performGeneration(float[] N, Surface s1, Surface s2)
        {
            List<Point3d> cellCorners = new List<Point3d>();
            ImplementationTools.MakeCornerNodes(ref cellCorners, 1);

            GH_Structure<GH_Point> cellTree = new GH_Structure<GH_Point>();

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        //make a path in the tree that corresponds to this UVW value
                        GH_Path path = new GH_Path(u, v, w);

                        //get the list of points at that path. this will be where we make the 8 points that will define our unit cell box
                        var singleCellPoints = cellTree.EnsurePath(path);

                        for (int i = 0; i < cellCorners.Count; i++)
                        {
                            double[] uvw = { u + cellCorners[i].X, v + cellCorners[i].Y, w + cellCorners[i].Z }; // uvw-position (global)

                            bool isOutsideSpace = (uvw[0] > N[0] || uvw[1] > N[1] || uvw[2] > N[2]);

                            //if the corners are within the surface space, then we create them
                            if (!isOutsideSpace)
                            {
                                // Initialize for surface 1
                                Point3d pt1; Vector3d[] derivatives1;

                                // Initialize for surface 2
                                Point3d pt2; Vector3d[] derivatives2;

                                s1.Evaluate(uvw[0] / N[0], uvw[1] / N[1], 2, out pt1, out derivatives1);
                                s2.Evaluate(uvw[0] / N[0], uvw[1] / N[1], 2, out pt2, out derivatives2);

                                // Create vector joining the two points (this is our w-range)
                                Vector3d wVect = pt2 - pt1;

                                var pt = new GH_Point(pt1 + wVect * uvw[2] / N[2]);
                                singleCellPoints.Add(pt);
                            }
                            else//otherwise we remove this path, so that we have no box at that location
                            {
                                cellTree.RemovePath(path);
                                break;
                            }
                        }
                    }
                }
            }
            return cellTree;
        }


        /// <summary>
        /// Explodes lines at intersections. (because all nodes must be defined)
        /// </summary>
        public static void FixIntersections(ref List<Line> lines)
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Check 2 - Fix any intersections, all nodes must be defined
            List<int> linesToRemove = new List<int>();
            List<Line> splitLines = new List<Line>();
            // Loop through all combinations of lines
            for (int a = 0; a < lines.Count; a++)
            {
                for (int b = a + 1; b < lines.Count; b++)
                {
                    // Line parameter at intersection, for line A and line B
                    double paramA, paramB;
                    bool intersectionFound = Intersection.LineLine(lines[a], lines[b], out paramA, out paramB, tol, true);

                    // If intersection was found
                    if (intersectionFound)
                    {
                        // If intersection isn't start/end point of line A, we split the line
                        if ((paramA > tol) && (1 - paramA > tol) && !linesToRemove.Contains(a))
                        {
                            // Store new split lines, and store the index of the line to remove
                            splitLines.Add(new Line(lines[a].From, lines[a].PointAt(paramA)));
                            splitLines.Add(new Line(lines[a].PointAt(paramA), lines[a].To));
                            linesToRemove.Add(a);
                        }
                        // Same for line B
                        if ((paramB > tol) && (1 - paramB > tol) && !linesToRemove.Contains(b))
                        {
                            splitLines.Add(new Line(lines[b].From, lines[b].PointAt(paramB)));
                            splitLines.Add(new Line(lines[b].PointAt(paramB), lines[b].To));
                            linesToRemove.Add(b);
                        }

                    }
                }
            }
            // Sort and reverse indices because we need to delete list items in decreasing index order
            linesToRemove.Sort();
            linesToRemove.Reverse();
            // Remove lines that were split, and add the new lines
            foreach (int index in linesToRemove) lines.RemoveAt(index);
            lines.AddRange(splitLines);
        }

        /// <summary>
        /// This creates struts from unit cells
        /// </summary>
        public static void createStrutsFromNodes(UnitCell cell, List<Point3d> p, List<Line> struts)
        {
            foreach (IndexPair nodePair in cell.NodePairs)
            {
                int node1 = nodePair.I;
                int node2 = nodePair.J;
                for (int j = 0; j < p.Count; j++)
                {
                    Line l = new Line(p[node1], p[node2]);
                    if (!struts.Contains(l))
                    {
                        struts.Add(l);
                    }
                }
            }
        }

        /// <summary>
        /// this returns a set of lines from the unit cells
        /// </summary>
        public static void createStrutsFromNodes(UnitCell cell, Point3dList p, List<Line> struts)
        {
            foreach (IndexPair nodePair in cell.NodePairs)
            {
                int node1 = nodePair.I;
                int node2 = nodePair.J;
                for (int j = 0; j < p.Count; j++)
                {
                    Line l = new Line(p[node1], p[node2]);
                    if (!struts.Contains(l))
                    {
                        struts.Add(l);
                    }
                }
            }
        }

        /// <summary>
        /// Quick method for generating the corner nodes of a cell.
        /// </summary>
        public static void MakeCornerNodes(ref List<Point3d> nodes, double d)
        {
            nodes.Add(new Point3d(0, 0, 0));
            nodes.Add(new Point3d(d, 0, 0));
            nodes.Add(new Point3d(d, d, 0));
            nodes.Add(new Point3d(0, d, 0));
            nodes.Add(new Point3d(0, 0, d));
            nodes.Add(new Point3d(d, 0, d));
            nodes.Add(new Point3d(d, d, d));
            nodes.Add(new Point3d(0, d, d));
        }


        /// <summary>
        //here is a method for searching for the closest point using an rtree
        /// </summary>

        //theis is used by the FindNeighboursRTree
        static void SearchCallback(object sender, RTreeEventArgs e)
        {
            SearchData data = e.Tag as SearchData;
            if (data == null)
                return;
            data.HitCount = data.HitCount + 1;
            Point3d vertex = data.Point[e.Id];
            double distance = data.SearchPoint.DistanceTo(vertex);
            if (data.Index == -1 || data.Distance > distance)
            {
                // shrink the sphere to help improve the test
                e.SearchSphere = new Sphere(data.SearchPoint, distance);
                data.Index = e.Id;
                data.Distance = distance;
            }
        }
        /// a class used by the FindNeighboursRTree
        class SearchData
        {
            public SearchData(List<Point3d> point, Point3d searchPoint)
            {
                Point = point;
                SearchPoint = searchPoint;
                HitCount = 0;
                Index = -1;
                Distance = 0;
            }
            public int HitCount { get; set; }
            public List<Point3d> Point { get; private set; }
            public Point3d SearchPoint { get; private set; }
            public int Index { get; set; }
            public double Distance { get; set; }
        }
        //points are the r tree //search points are the point we want to perform the closest point on
        public static List<int> FindNeighboursRTree(List<Point3d> points, List<Point3d> searchPoints)
        {
            List<int> ptID = new List<int>();
            /* Then, we use the R-Tree to find the neighbours */
            int count = 0;

            using (RTree tree = new RTree())
            {
                for (int i = 0; i < searchPoints.Count; i++)
                {
                    tree.Insert(searchPoints[i], i);
                }

                foreach (var pt in points)
                {
                    SearchData data = new SearchData(searchPoints, pt);

                    // Use the first vertex in the cloud to define a start sphere
                    double distance = pt.DistanceTo(searchPoints[0]);
                    Sphere sphere = new Sphere(pt, distance * 1.1);
                    if (tree.Search(sphere, SearchCallback, data))
                    {
                        ptID.Add(data.Index);
                    }
                    count++;
                }
            }
            return ptID;
        }


        //here is a method for valence searching for points using an r tree
        public static List<int> FindNeighboursRTree(List<Point3d> points)
        {
            /* First, build the R-Tree */
            RTree rTree = new RTree();
            List<int> iValence = new List<int>();
            //List<int> iValence = new List<int>();

            for (int i = 0; i < points.Count; i++)
                rTree.Insert(points[i], i);

            /* Then, we use the R-Tree to find the neighbours */
            foreach (Point3d pt in points)
            {
                List<Point3d> neighbours = new List<Point3d>();//our stash list

                EventHandler<RTreeEventArgs> rTreeCallback =
                    (object sender, RTreeEventArgs args) =>
                    {
                        neighbours.Add(points[args.Id]);
                    };
                rTree.Search(new Sphere(pt, .01), rTreeCallback);
                iValence.Add(neighbours.Count);

            }
            return iValence;
        }


    }
}

