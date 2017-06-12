using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Helpers
{
    public class SimpleConverter
    {

        public static List<Point3d> convertGHPoints(List<GH_Point> list)
        {
            List<Point3d> pts3d = new List<Point3d>();
            foreach (GH_Point pt in list)
            {
                Point3d p;
                pt.CastTo(out p);
                pts3d.Add(p);
            }

            return pts3d;
        }

        public static List<Point3d> convertGHPoints(GH_Structure<GH_Point> inputTree)
        {

            List<Point3d> pts3d = new List<Point3d>();
            foreach (List<GH_Point> pts in inputTree.Branches)
            {
                List<Point3d> list = SimpleConverter.convertGHPoints(pts);
                foreach (Point3d pt in list)
                {
                    pts3d.Add(pt);
                }
            }


            return pts3d;
        }

        public static List<Point3d> convertGHPoints(IList<GH_Point> list)
        {
            List<Point3d> pts3d = new List<Point3d>();
            foreach (GH_Point pt in list)
            {
                Point3d p;
                pt.CastTo(out p);
                pts3d.Add(p);
            }

            return pts3d;
        }

        public static List<Line> convertGH_Line(List<GH_Line> list)
        {
            List<Line> ls = new List<Line>();

            foreach (GH_Line l in list)
            {
                Line r;
                l.CastTo(out r);
                ls.Add(r);
            }

            return ls;
        }
    }
}