using StructuralPlanner.Utilities;
using System.Windows.Media.Media3D;

namespace StructuralPlanners.Utilities
{
    public static class MathHelpers
    {
        /// <summary>
        /// Determine the magnitude (length) of a 3d vector
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double Magnitude(Vector3D v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        /// <summary>
        /// Create a unit vector from a Vector3D
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3D Normalize(Vector3D v)
        {
            var length = Magnitude(v);

            return (new Vector3D(v.X / length, v.Y / length, v.Z / length));
        }

        /// <summary>
        /// Returns a cross product C = A x B
        /// </summary>
        /// <param name="a">Vector A</param>
        /// <param name="b">Vector B</param>
        /// <returns></returns>
        public static Vector3D CrossProduct(Vector3D a, Vector3D b)
        {
            double i_coeff = a.Y * b.Z - a.Z * b.Y;
            double j_coeff = (-1.0) * (a.X * b.Z - a.Z * b.X);
            double k_coeff = a.X * b.Y - a.Y * b.X;

            return new Vector3D(i_coeff, j_coeff, k_coeff);
        }

        /// <summary>
        /// Computes a dot product or vector projection of A onto nonzero vector b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double DotProduct(Vector3D a, Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// Returns a Point3D coordinate from a vector offset from a point.
        /// </summary>
        /// <param name="p0">Base point</param>
        /// <param name="offset">Vector offset</param>
        /// <returns></returns>
        public static Point3D Point3DFromVectorOffset(Point3D p0, Vector3D offset)
        {
            return (p0 + offset);
        }

        /// <summary>
        /// Find the planar distance between two Point points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Distance2DBetween(Point p1, Point p2)
        {
            Vector3D v = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, 0.0);
            return Magnitude(v);
        }

        /// <summary>
        /// Find the planar distance between two Point points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Distance3DBetween(Point3D p1, Point3D p2)
        {
            Vector3D v = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Magnitude(v);
        }

        public static Point PointTransformByAngle(Point p1, double angle)
        {
            double vx = p1.X * Math.Cos(angle) + p1.Y * Math.Sin(angle);
            double vy = -p1.X * Math.Sin(angle) + p1.Y * Math.Cos(angle);

            return new Point(vx, vy);
        }

        public static Point3D GetMidpoint(Point3D p1, Point3D p2)
        {
            Point3D p = new Point3D(0.5 * (p1.X + p2.X), 0.5 * (p1.Y + p2.Y), 0.5 * (p1.Z + p2.Z));
            return p;
        }

        /// <summary>
        /// Find the nearest point on perpendicular line to line segment passing through the picked point
        /// </summary>
        /// <param name="pick_pt">point in space</param>
        /// <param name="start_of_line">first point on line segment</param>
        /// <param name="end_of_line">second point on line segment</param>
        /// <returns></returns>
        public static Point3D NearestPointOnLine(Point3D pick_pt, Point3D start_of_line, Point3D end_of_line)
        {
            Vector3D v_line = end_of_line - start_of_line;
            Vector3D uv_perpendicular = MathHelpers.CrossProduct(v_line, new Vector3D(0, 0, 1));

            Point3D perp_line_pt1 = MathHelpers.Point3DFromVectorOffset(pick_pt, 10 * uv_perpendicular);
            IntersectPointData int_point = EE_Helpers.FindPointOfIntersectLines_FromPoint3D(start_of_line, end_of_line, pick_pt, perp_line_pt1);

            return int_point.Point;
        }

        /// <summary>
        /// Helper function to determine the angle between two vectors A and B measured from vector B 
        /// </summary>
        /// <param name="a">vector A</param>
        /// <param name="b">vector B</param>
        /// <returns></returns>
        public static double GetAngleBetweenVectors(Vector3D a, Vector3D b)
        {
            double A = MathHelpers.Magnitude(a);
            double B = MathHelpers.Magnitude(b); ;
            double dotProd = MathHelpers.DotProduct(a, b);
            double angle = Math.Acos(dotProd / (A * B));  // measured in radians

            return angle;
        }


        /// <summary>
        /// Returns the cartesian quadrant I, II, III, or IV for the position of a vector
        /// </summary>
        /// <param name="a">the vector</param>
        /// <returns></returns>
        public static int GetCartesianQuadrantOfVector(Vector3D a)
        {
            int quadrant = 1;
            if (a.X < 0)
            {
                if (a.Y >= 0)
                {
                    // quadrant II
                    quadrant = 2;
                }
                else
                {
                    // quadrant III
                    quadrant = 3;
                }
            }
            else
            {
                if (a.Y >= 0)
                {
                    // quadrant I
                    quadrant = 1;
                }
                else
                {
                    // quadrant IV
                    quadrant = 4;
                }
            }

            return quadrant;
        }

        /// <summary>
        /// A quick Point definition that allows use of doubles
        /// </summary>
        public struct Point
        {
            public double X;
            public double Y;

            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }

            public static Point operator +(Point a, Vector2D v) => new Point(a.X + v.X, a.Y + v.Y);
        }

        public struct Vector2D
        {
            public double X;
            public double Y;

            public Vector2D(double x, double y)
            {
                X = x;
                Y = y;
            }

            public Vector2D GetNormal()
            {
                double len = Math.Sqrt(X * X + Y * Y);
                if (len < 1e-9) throw new InvalidOperationException("Zero-length vector cannot be normalized.");
                return new Vector2D(X / len, Y / len);
            }

            public static Vector2D operator *(Vector2D v, double scalar) => new Vector2D(v.X * scalar, v.Y * scalar);
        }

        public static class ParallelLineHelper
        {
            /// <summary>
            /// Finds a point p2 on a line parallel to the original line,
            /// at perpendicular distance d, with same X as p1 (vertically below).
            /// </summary>
            public static Point FindVerticallyBelowPoint(Point p1, Point lineStart, Point lineEnd, double d)
            {
                double dx = lineEnd.X - lineStart.X;
                double dy = lineEnd.Y - lineStart.Y;

                // Handle vertical line
                if (Math.Abs(dx) < 1e-9)
                {
                    double xOffset = d; // shift to the right by d; use -d for left
                    return new Point(p1.X + xOffset, p1.Y);
                }

                // Slope of original line
                double m = dy / dx;
                double b1 = lineStart.Y - m * lineStart.X;

                // Perpendicular distance offset
                double deltaB = d * Math.Sqrt(m * m + 1);
                double b2 = b1 - deltaB; // "below" line

                // Intersection with vertical line through p1
                double y2 = m * p1.X + b2;
                return new Point(p1.X, y2);
            }
        }
    }
}
