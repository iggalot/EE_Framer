using StructuralPlanners.Utilities;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;


namespace StructuralPlanner.Utilities
{
    /// <summary>
    /// Class object for data relating to finding intersection points of lines with polylines.
    /// Mostly used for passing back full data.
    /// </summary>
    public class IntersectPointData
    {
        public Point3D Point;
        public bool isParallel;
        public bool isWithinSegment;
        public string logMessage = "";

        public Vector3D u1;  // unit vector for line segment 1
        public Vector3D u2;  // unit vector for line segment 2
    }

    public static class EE_Helpers
    {
        public static string DisplayPrint3DCollection(Point3DCollection coll)
        {
            string str = "";
            foreach (Point3D point in coll)
            {
                str += point.X + " , " + point.Y + "\n";
            }
            return str;
        }

        /// <summary>
        /// Bubble sort for x-direction of Point3DCollection
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static Point3D[] sortPoint3DPointCollectionByHorizontally(Point3DCollection coll)
        {
            Point3D[] sort_arr = new Point3D[coll.Count];
            coll.CopyTo(sort_arr, 0);
            Point3D temp;

            for (int j = 0; j < coll.Count - 1; j++)
            {
                for (int i = 0; i < coll.Count - 1; i++)
                {
                    if (sort_arr[i].X > sort_arr[i + 1].X)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                }
            }
            return sort_arr;
        }

        /// <summary>
        /// Sort an array of Point3D[] by X value from smallest X to largest X
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static Point3D[] sortPoint3DListByHorizontally(List<Point3D> lst)
        {
            Point3D[] sort_arr = lst.ToArray();
            Point3D temp;

            for (int j = 0; j < lst.Count - 1; j++)
            {
                for (int i = 0; i < lst.Count - 1; i++)
                {
                    if (sort_arr[i].X > sort_arr[i + 1].X)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                }
            }
            return sort_arr;
        }

        /// <summary>
        /// Sort an array of Point3D[] by X value from smallest Y to largest Y
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        public static Point3D[] sortPoint3DListByVertically(List<Point3D> lst)
        {
            Point3D[] sort_arr = lst.ToArray();
            Point3D temp;

            for (int j = 0; j < lst.Count - 1; j++)
            {
                for (int i = 0; i < lst.Count - 1; i++)
                {
                    if (sort_arr[i].Y > sort_arr[i + 1].Y)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                }
            }
            return sort_arr;
        }

        /// <summary>
        /// Bubble sort for Point3D of y-direction
        /// </summary>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static Point3D[] sortPoint3DCollectionByVertically(Point3DCollection coll)
        {
            Point3D[] sort_arr = new Point3D[coll.Count];
            coll.CopyTo(sort_arr, 0);
            Point3D temp;

            for (int j = 0; j < coll.Count - 1; j++)
            {
                for (int i = 0; i < coll.Count - 1; i++)
                {
                    if (sort_arr[i].Y > sort_arr[i + 1].Y)
                    {
                        temp = sort_arr[i + 1];
                        sort_arr[i + 1] = sort_arr[i];
                        sort_arr[i] = temp;
                    }
                }
            }
            return sort_arr;
        }

        /// <summary>
        /// Sorts a list points on a horizontal line by smallest x to largest x coordinate or
        /// on a vertical line by smallest y to largest y coordinate
        /// </summary>
        /// <param name="lst">a list points</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public static Point3D[] SortPointsHorizontallyOrVertically(List<Point3D> lst, double tolerance)
        {
            Point3D[] sorted_list;
            // If the point is horizontal
            if (Math.Abs(lst[1].Y - lst[0].Y) < tolerance)
            {
                sorted_list = sortPoint3DListByHorizontally(lst);
            }
            // Otherwise it is vertical
            else
            {
                sorted_list = sortPoint3DListByVertically(lst);
            }

            if (sorted_list is null)
            {
                throw new System.Exception("\nError sorting the intersection points in TrimLines method.");
            }

            return sorted_list;
        }

        public static bool IsClosed(Polyline polyline, double tolerance = 0.001)
        {
            if (polyline == null || polyline.Points.Count < 3)
                return false;

            Point first = polyline.Points[0];
            Point last = polyline.Points[polyline.Points.Count - 1];

            return (Math.Abs(first.X - last.X) < tolerance &&
                    Math.Abs(first.Y - last.Y) < tolerance);
        }


        public static List<IntersectPointData> FindPolylineIntersectionPoints(Line ln, Polyline poly, bool require_closed_polyline = true)
        {
            if (poly == null || (require_closed_polyline && IsClosed(poly) == false))
            {
                throw new InvalidOperationException("Invalid polyline object.  Make sure the polyline is closed and has at least 2 vertices.");
            }
            int numVerts = poly.Points.Count;

            if (ln == null)
            {
                throw new InvalidOperationException("Invalid line object.");
            }

            Point3D b1 = new Point3D(ln.X1, ln.Y1, 0);
            Point3D b2 = new Point3D(ln.X2, ln.Y2, 0);

            List<IntersectPointData> intPtDataList = new List<IntersectPointData>();

            int max = numVerts;

            // add one to the end if the polyline is closed.
            if (require_closed_polyline == true)
                max = numVerts + 1;

            for (int i = 0; i < max - 1; i++)
            {
                Point p1_p2D = poly.Points[i % numVerts];
                Point3D p1 = new Point3D(p1_p2D.X, p1_p2D.Y, 0);
                Point p2_p2D = poly.Points[(i + 1) % numVerts];
                Point3D p2 = new Point3D(p2_p2D.X, p2_p2D.Y, 0);


                //Determine if the intersection point is a valid point within the polyline segment.
                IntersectPointData intersectPointData = (EE_Helpers.FindPointOfIntersectLines_FromPoint3D(b1, b2, p1, p2));

                if (intersectPointData == null)
                    continue;

                Point3D intPt = intersectPointData.Point;

                if (intersectPointData.isParallel is true)
                {
                    continue;
                }
                else
                {
                    if (intersectPointData.isWithinSegment is true)
                    {
                        intPtDataList.Add(intersectPointData);
                    }
                }
            }

            return intPtDataList;
        }

        /// <summary>
        /// Find the location where two line segements intersect
        /// </summary>
        /// <param name="l1">autocad line object #1</param>
        /// <param name="l2">autocad line objtxt #2</param>
        /// <param name="withinSegment">The coordinate must be within the line segments</param>
        /// <param name="areParallel">returns if the lines are parallel. This needs to be checked everytime as the intersection point defaults to a really large value otherwise</param>
        /// <returns></returns>
        public static IntersectPointData FindPointOfIntersectLines_2D(Line l1, Line l2)
        {
            double tol = 0.001;  // a tolerance fudge factor since autocad is having issues with rounding at the 9th and 10th decimal place
            double A1 = l1.Y2 - l1.Y1;
            double A2 = l2.Y2 - l2.Y1;
            double B1 = l1.X1 - l1.X2;
            double B2 = l2.X1 - l2.X2;
            double C1 = A1 * l1.X1 + B1 * l1.Y1;
            double C2 = A2 * l2.X1 + B2 * l2.Y1;

            // compute the determinant
            double det = A1 * B2 - A2 * B1;

            double intX, intY;

            IntersectPointData intPtData = new IntersectPointData();
            intPtData.isParallel = LinesAreParallel(l1, l2);

            Vector3D v1 = new Vector3D(l1.X2 - l1.X1, l1.Y2 - l1.Y1, 0);
            Vector3D v2 = new Vector3D(l2.X2 - l2.X1, l2.Y2 - l2.Y1, 0);

            // Create the unit vectors
            intPtData.u1 = MathHelpers.Normalize(v1);
            intPtData.u2 = MathHelpers.Normalize(v2);

            if (intPtData.isParallel is true)
            {
                // Lines are parallel, but are they the same line?
                intX = double.MaxValue;
                intY = double.MaxValue;
                intPtData.isWithinSegment = false; // cant intersect if the lines are parallel
                //MessageBox.Show("segment is parallel");
                //MessageBox.Show("A1: " + A1 + "\n" + "  B1: " + B1 + "\n" + "  C1: " + C1 + "\n" +
                //    "A2: " + A2 + "\n" + "  B2: " + B2 + "\n" + "  C2: " + C2 + "\n" +
                //    "delta: " + delta);
            }
            else
            {
                intX = (B2 * C1 - B1 * C2) / det;
                intY = (A1 * C2 - A2 * C1) / det;

                intPtData.isWithinSegment = true;
                string msg = "";
                //// Check that the intersection point is between the endpoints of both lines assuming it isnt
                if (((Math.Min(l1.X1, l1.X2) - tol <= intX) && (Math.Max(l1.X1, l1.X2) + tol >= intX)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 1 X - failed";
                }
                else if (((Math.Min(l2.X1, l2.X2) - tol <= intX) && (Math.Max(l2.X1, l2.X2) + tol >= intX)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 2 X - failed";

                }
                else if (((Math.Min(l1.Y1, l1.Y2) - tol <= intY) && (Math.Max(l1.Y1, l1.Y2) + tol >= intY)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 3 X - failed";

                }
                else if (((Math.Min(l2.Y1, l2.Y2) - tol <= intY) && (Math.Max(l2.Y1, l2.Y2) + tol >= intY)) is false)
                {
                    intPtData.isWithinSegment = false;
                    msg += "line 4 X - failed";

                }
                else
                {
                    intPtData.isWithinSegment = true;
                    msg += "intersection point is within line segment limits";

                }
            }

            intPtData.Point = new Point3D(intX, intY, 0);

            return intPtData;
        }

        public static IntersectPointData FindPointOfIntersectLines_FromPoint3D(Point3D A1, Point3D A2, Point3D B1, Point3D B2)
        {
            // Check if the points are the same -- usually occurs when one line is comparing to itself
            if (A1 == B1 && A2 == B2)
            {
                return null;
            }


            Line l1, l2;
            if (A1.X < A2.X)
            {
                l1 = new Line();
                l1.X1 = A1.X;
                l1.X2 = A2.X;
                l1.Y1 = A1.Y;
                l1.Y2 = A2.Y;
            }
            else
            {
                l1 = new Line();
                l1.X1 = A1.X;
                l1.X2 = A2.X;
                l1.Y1 = A1.Y;
                l1.Y2 = A2.Y;
            }

            if (B1.X < B2.X)
            {
                l2 = new Line();
                l2.X1 = B1.X;
                l2.X2 = B2.X;
                l2.Y1 = B1.Y;
                l2.Y2 = B2.Y;
            }
            else
            {
                l2 = new Line();
                l2.X1 = B1.X;
                l2.X2 = B2.X;
                l2.Y1 = B1.Y;
                l2.Y2 = B2.Y;
            }

            return FindPointOfIntersectLines_2D(l1, l2);
        }

        /// <summary>
        /// Function to determine if three Point3D objects are colinear
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static bool PointtsAreColinear_2D(Point3D p1, Point3D p2, Point3D p3)
        {
            double x1 = p1.X;
            double y1 = p1.Y;
            double x2 = p2.X;
            double y2 = p2.Y;
            double x3 = p3.X;
            double y3 = p3.Y;

            /* Calculation the area of 
            triangle. We have skipped
            multiplication with 0.5 to
            avoid floating point computations */
            double a = x1 * (y2 - y3) + x2 * (y3 - y1) + x3 * (y1 - y2);

            if (a == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static double GetSlopeOfPts(Point3D p1, Point3D p2)
        {
            return (Math.Atan((p2.Y - p1.Y) / (p2.X - p1.X)));
        }

        public static bool LinesAreParallel(Line l1, Line l2)
        {
            double A1 = l1.Y2 - l1.Y1;
            double A2 = l2.Y2 - l2.Y1;
            double B1 = l1.X1 - l1.X2;
            double B2 = l2.X1 - l2.X2;
            double C1 = A1 * l1.X1 + B1 * l1.Y1;
            double C2 = A2 * l2.X1 + B2 * l2.Y1;

            double det = A1 * B2 - A2 * B1;
            return det == 0;
        }

        /// <summary>
        /// Finds all of the intersection points of a line segment crossing a closed polygon
        /// </summary>
        /// <param name="b1">first point on the line segment</param>
        /// <param name="b2">second point on the line segment</param>
        /// <param name="poly">the closed polyline to evaluate</param>
        /// <returns>A Point3D[] array of the points sorted from lowest X to highest X or from lowest Y to highest Y</returns>
        /// <exception cref="System.Exception"></exception>
        public static Point3D[] TrimAndSortIntersectionPoints(Point3D b1, Point3D b2, Polyline poly, string layer_name)
        {
            int numVerts = poly.Points.Count;
            double radius = 10;
            double tolerance = 0.001;

            List<Point3D> beam_points = new List<Point3D>();

            //MessageBox.Show("Polyline has " + numVerts.ToString() + " vertices");
            for (int i = 0; i < numVerts; i++)
            {
                try
                {
                    // Get the ends of the interior current polyline segment
                    Point p1_p2D = poly.Points[i % numVerts];
                    Point3D p1 = new Point3D(p1_p2D.X, p1_p2D.Y, 0);
                    Point p2_p2D = poly.Points[(i + 1) % numVerts];
                    Point3D p2 = new Point3D(p2_p2D.X, p2_p2D.Y, 0);
                    //DrawCircle(p1, EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_BEAMS_TRIMMED_LAYER);

                    //if (p1 == b1 || p1 == b2)
                    //{
                    //    beam_points.Add(p1);
                    //    continue;
                    //}
                    //if (p2 == b1 || p2 == b2)
                    //{
                    //    beam_points.Add(p2);
                    //    continue;
                    //}

                    double dist = MathHelpers.Distance3DBetween(p1, p2);

                    Point3D grade_beam_intPt;

                    IntersectPointData intersectPtData = FindPointOfIntersectLines_FromPoint3D(
                        b1,
                        b2,
                        p1,
                        p2
                        );

                    if (intersectPtData == null)
                        continue;

                    grade_beam_intPt = intersectPtData.Point;

                    if (grade_beam_intPt == null)
                    {
                        //MessageBox.Show("No intersection point found");
                        continue;
                    }
                    else
                    {
                        //DrawCircle(grade_beam_intPt, EE_Settings.DEFAULT_INTERSECTION_CIRCLE_RADIUS, EE_Settings.DEFAULT_FDN_ANNOTATION_LAYER);

                        //MessageBox.Show("-- intersection point found");

                        //                      beam_points.Add(grade_beam_intPt);

                        double slope1_line_segment = EE_Helpers.GetSlopeOfPts(b1, b2);
                        double slope2_line_segment = EE_Helpers.GetSlopeOfPts(b2, b1);
                        double slope_polyline_segment = EE_Helpers.GetSlopeOfPts(p1, p2);
                        // if the slope of the two line segments are parallel and the X or Y coordinates match, add the intersection as the average of the two polyline segment end points 
                        if ((slope1_line_segment == slope_polyline_segment) || (slope2_line_segment == slope_polyline_segment))
                        {
                            // if the vertices of the polyline are on the line segment
                            //     (vertical segment test)       ||      (horizontal segment test)
                            if ((b1.X == p1.X && b1.X == p2.X && b2.X == p1.X && b2.X == p2.X)
                                || (b1.Y == p1.Y && b1.Y == p2.Y && b2.Y == p1.Y && b2.Y == p2.Y))
                            {
                                // add both points to the list
                                beam_points.Add(p1);
                                beam_points.Add(p2);
                                //                                // assign the midpoint of the polyline segment as the intersection point
                                //                                beam_points.Add(new Point3D(0.5 * (p1.X + p2.X), 0.5 * (p1.Y + p2.Y), 0));
                                continue;
                            }
                        }

                        // If the first point is exactly a vertex point, add it to the list
                        // We wont do it for the second point as it should only be assigned to one segment
                        if (p1 == b1 || p1 == b2)
                        {
                            beam_points.Add(p1);
                            continue;
                        }
                        else
                        {
                            // If the distance from the intPt to both p1 and P2 is less than the distance between p1 and p2
                            // the intPT must be between P1 and P2 
                            if ((MathHelpers.Distance3DBetween(grade_beam_intPt, p1) <= dist) && (MathHelpers.Distance3DBetween(grade_beam_intPt, p2) <= dist))
                            {
                                beam_points.Add(grade_beam_intPt);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    MessageBox.Show("-------ERROR---------" + e.Message);
                    return null;
                }
            }

            //foreach (var p in beam_points)
            //{
            //    DrawCircle(p, radius, layer_name);
            //}

            try
            {
                if (beam_points is null)
                {
                    return null;
                }
                else if (beam_points.Count < 2)
                {
                    if (beam_points.Count == 0)
                    {
                        //MessageBox.Show("No intersection points found");
                        return null;
                    }
                    else
                    {
                        //MessageBox.Show(beam_points.Count.ToString() + " intersection point found at " + beam_points[0].X + " , " + beam_points[0].Y);
                        Point3D[] sorted_points = new Point3D[beam_points.Count];
                        beam_points.CopyTo(sorted_points, 0);
                        return sorted_points;
                    }
                }
                else
                {
                    try
                    {
                        Point3D[] sorted_points = new Point3D[beam_points.Count];

                        // If the point is horizontal
                        if (Math.Abs(beam_points[1].Y - beam_points[0].Y) < tolerance)
                        {
                            sorted_points = sortPoint3DListByHorizontally(beam_points);
                        }
                        // Otherwise it is vertical
                        else
                        {
                            sorted_points = sortPoint3DListByVertically(beam_points);
                        }

                        if (sorted_points is null)
                        {
                            throw new System.Exception("\nError sorting the intersection points in TrimLines method.");
                        }

                        return sorted_points;
                    }
                    catch (System.Exception e)
                    {
                        MessageBox.Show("\nError finding sorted intersection points: " + e.Message);
                        return null;
                    }

                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show("\nError in TrimAndSortIntersectionPoints function" + e.Message);
                return null;
            }
        }
    }
}
