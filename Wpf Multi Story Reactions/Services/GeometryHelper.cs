using StructuralPlanner.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace StructuralPlanner.Services
{
    public static class GeometryHelper
    {
        public static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Project point p onto segment ab, return closest point on segment
        public static Point ProjectPointOntoSegment(Point p, Point a, Point b)
        {
            Vector ap = p - a;
            Vector ab = b - a;

            double ab2 = ab.X * ab.X + ab.Y * ab.Y;
            if (ab2 == 0) return a;

            double t = Vector.Multiply(ap, ab) / ab2;
            t = Math.Max(0, Math.Min(1, t));

            return a + ab * t;
        }

        public static Point MidPoint(Point a, Point b) => new Point((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);

        public static Point ProjectPerpendicular(Point origin, Point mousePos, StructuralPlanner.Models.StructuralMember edge)
        {
            if (edge == null) return mousePos;
            Vector edgeVec = edge.EndNode.Location - edge.StartNode.Location;
            if (edgeVec.Length == 0) return mousePos;
            edgeVec.Normalize();
            Vector mouseVec = mousePos - origin;
            Vector perp = new Vector(-edgeVec.Y, edgeVec.X); // perpendicular
            double length = Vector.Multiply(mouseVec, perp);
            return origin + perp * length;
        }

        public static List<(Point Start, Point End)> GetPolygonEdges(Polygon polygon)
        {
            var edges = new List<(Point Start, Point End)>();
            var pts = polygon.Points;
            if (pts.Count < 2) return edges;
            for (int i = 0; i < pts.Count; i++)
            {
                Point start = pts[i];
                Point end = (i == pts.Count - 1) ? pts[0] : pts[i + 1];
                edges.Add((start, end));
            }
            return edges;
        }

        public static Region GetPolygonContainingPoint(Point click, List<Region> regions)
        {
            foreach (var region in regions)
            {
                // Convert the polygon points to a StreamGeometry
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(region.Poly.Points[0], true, true); // is filled, is closed
                    ctx.PolyLineTo(region.Poly.Points.Skip(1).ToList(), true, true);
                }
                geometry.Freeze(); // optional for performance

                if (geometry.FillContains(click))
                    return region;
            }

            return null; // No polygon contains the point
        }

        public static  List<Node> OrderNodesClockwise(List<Node> nodes)
        {
            var center = new Point(nodes.Average(n => n.Location.X), nodes.Average(n => n.Location.Y));
            return nodes.OrderBy(n => Math.Atan2(n.Location.Y - center.Y, n.Location.X - center.X)).ToList();
        }

        public static List<(Point3D Start, Point3D End)> TrimLinesToPolygon(List<(Point3D Start, Point3D End)> lines, Polygon polygon)
        {
            List<(Point3D Start, Point3D End)> trimmed = new List<(Point3D Start, Point3D End)>();

            // Iterate each line
            foreach (var line in lines)
            {
                List<Point3D> intersections = new List<Point3D>();
                Point3D start = line.Start;
                Point3D end = line.End;

                // Iterate each polygon edge
                for (int i = 0; i < polygon.Points.Count; i++)
                {
                    Point p1 = polygon.Points[i];
                    Point p2 = polygon.Points[(i + 1) % polygon.Points.Count];

                    if (LineSegmentIntersection(start.ToPoint(), end.ToPoint(), p1, p2, out Point inter, true))
                    {
                        // Add intersection
                        if (inter != null)
                        {
                            intersections.Add(inter.ToPoint3D());
                        }
                    }
                }

                if(intersections.First() != intersections.Last())
                {
                    trimmed.Add((intersections.First(), intersections.Last()));
                } else
                {
                    trimmed.Add(line);
                }
            }
            return trimmed;
        }
        // Compute distance along line from start to projection of point
        private static double DistanceAlongLine(Point lineStart, Point lineEnd, Point p)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double len2 = dx * dx + dy * dy;
            if (len2 < 1e-10) return 0;
            double t = ((p.X - lineStart.X) * dx + (p.Y - lineStart.Y) * dy) / len2;
            return t;
        }

        /// <summary>
        /// Finds the intersection of two 2D line segments.
        /// </summary>
        /// <param name="p1">Start of first segment</param>
        /// <param name="p2">End of first segment</param>
        /// <param name="q1">Start of second segment</param>
        /// <param name="q2">End of second segment</param>
        /// <param name="intersection">Output intersection point</param>
        /// <param name="requireOnSegments">If true, intersection must lie on both segments. If false, intersection can be anywhere along the infinite lines.</param>
        /// <returns>True if intersection exists (per the mode), false otherwise</returns>
        public static bool LineSegmentIntersection(Point p1, Point p2, Point q1, Point q2, out Point intersection, bool requireOnSegments = true)
        {
            intersection = new Point();

            double dx1 = p2.X - p1.X;
            double dy1 = p2.Y - p1.Y;
            double dx2 = q2.X - q1.X;
            double dy2 = q2.Y - q1.Y;

            double denom = dx1 * dy2 - dy1 * dx2;

            const double EPS = 1e-8; // small tolerance

            if (Math.Abs(denom) < EPS)
            {
                // Lines are parallel or coincident
                return false;
            }

            double dx = q1.X - p1.X;
            double dy = q1.Y - p1.Y;

            double t = (dx * dy2 - dy * dx2) / denom;
            double u = (dx * dy1 - dy * dx1) / denom;

            intersection.X = p1.X + t * dx1;
            intersection.Y = p1.Y + t * dy1;

            if (requireOnSegments)
            {
                if (t < -EPS || t > 1 + EPS || u < -EPS || u > 1 + EPS)
                    return false; // Intersection is not on both segments
            }

            return true;
        }

    }
}