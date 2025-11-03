using StructuralPlanners.Utilities;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace StructuralPlanner.Services
{
    public static class MemberLayoutService
    {

        /// <summary>
        /// Create horizontal rafters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static List<(Point3D Start, Point3D End)> CreateVerticalRafters(Polygon polygon, double spacing = 16)
        {
            if (polygon == null) return null;

            List<(Point3D Start, Point3D End)> rafters = new List<(Point3D Start, Point3D End)>();

            Vector3D dir_unit_vec = new Vector3D(0, 1, 0);
            // get unit vect perpendicular to selected edge
            Vector3D perp_unit_vec = MathHelpers.Normalize(MathHelpers.CrossProduct(dir_unit_vec, (-1.0) * new Vector3D(0, 0, 1)));

            // find furthest point on perpendicular line
            Point3D current_vertex = new Point3D(0, 0, 0);

            // Base point is at the mid-height of bounding box
            Point3D p0 = new Point3D(polygon.Points[0].X, polygon.Points[0].Y, 0);
            Point3D p1 = new Point3D(polygon.Points[1].X, polygon.Points[1].Y, 0);
            Point3D p2 = new Point3D(polygon.Points[2].X, polygon.Points[2].Y, 0);
            Point3D p3 = new Point3D(polygon.Points[3].X, polygon.Points[3].Y, 0);

            double minX = Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X));
            double minY = Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y));

            double maxX = Math.Max(Math.Max(p0.X, p1.X), Math.Max(p2.X, p3.X));
            double maxY = Math.Max(Math.Max(p0.Y, p1.Y), Math.Max(p2.Y, p3.Y));

            // Lower-left (minX, minY), upper-right (maxX, maxY)
            Point3D lowerLeft = new Point3D(minX, minY, 0);
            Point3D upperRight = new Point3D(maxX, maxY, 0);

            double bb_minX = lowerLeft.X;
            double bb_maxX = upperRight.X;
            double bb_minY = lowerLeft.Y;
            double bb_maxY = upperRight.Y;

            double boxWidth = maxX - minX;

            // Compute number of lines to fit in box
            int numLines = (int)Math.Floor(boxWidth / spacing) + 1; // include both edges

            // Compute actual start X so pattern is centered
            double totalWidthUsed = (numLines - 1) * spacing;
            double startX = bb_minX + (boxWidth - totalWidthUsed) / 2;

            for (int i = 0; i < numLines; i++)
            {
                double x = startX + i * spacing;
                Point3D start = new Point3D(x, bb_minY, 0);
                Point3D end = new Point3D(x, bb_maxY, 0);
                rafters.Add((start, end));
            }

            rafters = GeometryHelper.TrimLinesToPolygon(rafters, polygon);
            return rafters;
        }

        /// <summary>
        /// Create horizontal rafters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static List<(Point3D Start, Point3D End)> CreateHorizontalRafters(Polygon polygon, double spacing = 16)
        {
            if (polygon == null) return null;

            List<(Point3D Start, Point3D End)> rafters = new List<(Point3D Start, Point3D End)>();

            Vector3D dir_unit_vec = new Vector3D(0, 1, 0);
            // get unit vect perpendicular to selected edge
            Vector3D perp_unit_vec = MathHelpers.Normalize(MathHelpers.CrossProduct(dir_unit_vec, (-1.0) * new Vector3D(0, 0, 1)));

            // find furthest point on perpendicular line
            Point3D current_vertex = new Point3D(0, 0, 0);

            // Base point is at the mid-height of bounding box
            Point3D p0 = new Point3D(polygon.Points[0].X, polygon.Points[0].Y, 0);
            Point3D p1 = new Point3D(polygon.Points[1].X, polygon.Points[1].Y, 0);
            Point3D p2 = new Point3D(polygon.Points[2].X, polygon.Points[2].Y, 0);
            Point3D p3 = new Point3D(polygon.Points[3].X, polygon.Points[3].Y, 0);

            double minX = Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X));
            double minY = Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y));

            double maxX = Math.Max(Math.Max(p0.X, p1.X), Math.Max(p2.X, p3.X));
            double maxY = Math.Max(Math.Max(p0.Y, p1.Y), Math.Max(p2.Y, p3.Y));

            // Lower-left (minX, minY), upper-right (maxX, maxY)
            Point3D lowerLeft = new Point3D(minX, minY, 0);
            Point3D upperRight = new Point3D(maxX, maxY, 0);

            double bb_minX = lowerLeft.X;
            double bb_maxX = upperRight.X;
            double bb_minY = lowerLeft.Y;
            double bb_maxY = upperRight.Y;

            double boxHeight = maxY - minY;

            // Compute number of lines to fit in box
            int numLines = (int)Math.Floor(boxHeight / spacing) + 1; // include both edges

            // Compute actual start X so pattern is centered
            double totalWidthUsed = (numLines - 1) * spacing;
            double startY = bb_minY + (boxHeight - totalWidthUsed) / 2;

            for (int i = 0; i < numLines; i++)
            {
                double y = startY + i * spacing;
                Point3D start = new Point3D(bb_minX, y, 0);
                Point3D end = new Point3D(bb_maxX, y, 0);
                rafters.Add((start, end));
            }

            rafters = GeometryHelper.TrimLinesToPolygon(rafters, polygon);
            return rafters;
        }

        /// <summary>
        /// Creates rafters that are perpendicular to the edge between start and end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static List<(Point3D Start, Point3D End)> CreatePerpendicularRafters(
            Polygon polygon, Point3D edgeStart, Point3D edgeEnd, double spacing = 16)
        {
            if (polygon == null || polygon.Points.Count < 2) return null;

            List<(Point3D Start, Point3D End)> rafters = new List<(Point3D Start, Point3D End)>();

            // --- 1. Edge vector and perpendicular ---
            Vector3D edgeVec = new Vector3D(edgeEnd.X - edgeStart.X, edgeEnd.Y - edgeStart.Y, 0);
            Vector3D edgeUnit = MathHelpers.Normalize(edgeVec);
            Vector3D perpUnit = new Vector3D(-edgeUnit.Y, edgeUnit.X, 0);

            // --- 2. Rotate polygon points into edge-aligned coordinates ---
            // edgeStart is origin
            List<Vector3D> localPts = polygon.Points.Select(p =>
            {
                Vector3D v = new Vector3D(p.X - edgeStart.X, p.Y - edgeStart.Y, 0);
                return new Vector3D(Vector3D.DotProduct(v, edgeUnit), Vector3D.DotProduct(v, perpUnit), 0);
            }).ToList();

            // --- 3. Compute bounding box in rotated coordinates ---
            double minAlongEdge = localPts.Min(v => v.X);
            double maxAlongEdge = localPts.Max(v => v.X);
            double minPerp = localPts.Min(v => v.Y);
            double maxPerp = localPts.Max(v => v.Y);

            // --- 4. Determine number of rafters along the edge ---
            double totalLength = maxAlongEdge - minAlongEdge;
            int numLines = (int)Math.Floor(totalLength / spacing) + 1;
            double startOffset = minAlongEdge + (totalLength - (numLines - 1) * spacing) / 2;

            // --- 5. Generate rafters in rotated coordinates ---
            for (int i = 0; i < numLines; i++)
            {
                double offsetAlongEdge = startOffset + i * spacing;

                // Base point along edge
                Vector3D baseLocal = new Vector3D(offsetAlongEdge, 0, 0);

                // Line start/end along perpendicular, clipped to bounding box
                Vector3D lineStartLocal = new Vector3D(baseLocal.X, minPerp, 0);
                Vector3D lineEndLocal = new Vector3D(baseLocal.X, maxPerp, 0);

                // Convert back to world coordinates
                Vector3D startWorld = edgeStart.ToVector() + edgeUnit * lineStartLocal.X + perpUnit * lineStartLocal.Y;
                Vector3D endWorld = edgeStart.ToVector() + edgeUnit * lineEndLocal.X + perpUnit * lineEndLocal.Y;

                rafters.Add((startWorld.ToPoint3D(), endWorld.ToPoint3D()));
            }

            rafters = GeometryHelper.TrimLinesToPolygon(rafters, polygon);
            return rafters;
        }

        public static List<(Point3D Start, Point3D End)> CreateParallelRafters(
                Polygon polygon, Point3D edgeStart, Point3D edgeEnd, double spacing = 16)
        {
            if (polygon == null || polygon.Points.Count < 2) return null;

            List<(Point3D Start, Point3D End)> rafters = new List<(Point3D Start, Point3D End)>();

            // --- 1. Edge vector and perpendicular ---
            Vector3D edgeVec = new Vector3D(edgeEnd.X - edgeStart.X, edgeEnd.Y - edgeStart.Y, 0);
            Vector3D edgeUnit = MathHelpers.Normalize(edgeVec);
            Vector3D perpUnit = new Vector3D(-edgeUnit.Y, edgeUnit.X, 0);

            // --- 2. Rotate polygon points into edge-aligned coordinates ---
            List<Vector3D> localPts = polygon.Points.Select(p =>
            {
                Vector3D v = new Vector3D(p.X - edgeStart.X, p.Y - edgeStart.Y, 0);
                return new Vector3D(Vector3D.DotProduct(v, edgeUnit), Vector3D.DotProduct(v, perpUnit), 0);
            }).ToList();

            // --- 3. Compute bounding box in rotated coordinates ---
            double minAlongEdge = localPts.Min(v => v.X);
            double maxAlongEdge = localPts.Max(v => v.X);
            double minPerp = localPts.Min(v => v.Y);
            double maxPerp = localPts.Max(v => v.Y);

            // --- 4. Determine number of lines across perpendicular direction ---
            double totalWidth = maxPerp - minPerp;
            int numLines = (int)Math.Floor(totalWidth / spacing) + 1;
            double startOffset = minPerp + (totalWidth - (numLines - 1) * spacing) / 2;

            // --- 5. Generate parallel lines (parallel to the edge) ---
            for (int i = 0; i < numLines; i++)
            {
                double offsetPerp = startOffset + i * spacing;

                // Base line offset from edge, parallel to edge
                Vector3D baseLocal = new Vector3D(0, offsetPerp, 0);

                // Line start and end along the edge direction, clipped to bounding box
                Vector3D lineStartLocal = new Vector3D(minAlongEdge, baseLocal.Y, 0);
                Vector3D lineEndLocal = new Vector3D(maxAlongEdge, baseLocal.Y, 0);

                // Convert back to world coordinates
                Vector3D startWorld = edgeStart.ToVector() + edgeUnit * lineStartLocal.X + perpUnit * lineStartLocal.Y;
                Vector3D endWorld = edgeStart.ToVector() + edgeUnit * lineEndLocal.X + perpUnit * lineEndLocal.Y;

                rafters.Add((startWorld.ToPoint3D(), endWorld.ToPoint3D()));
            }

            rafters = GeometryHelper.TrimLinesToPolygon(rafters, polygon);
            return rafters;
        }

        public static List<(Point3D Start, Point3D End)> CreateParallelRaftersCentered(
    Polygon polygon, Point3D edgeStart, Point3D edgeEnd, double spacing = 16)
        {
            if (polygon == null || polygon.Points.Count < 2) return null;

            var rafters = new List<(Point3D Start, Point3D End)>();

            // --- 1. Edge vector and perpendicular ---
            Vector3D edgeVec = new Vector3D(edgeEnd.X - edgeStart.X, edgeEnd.Y - edgeStart.Y, 0);
            Vector3D edgeUnit = MathHelpers.Normalize(edgeVec);
            Vector3D perpUnit = new Vector3D(-edgeUnit.Y, edgeUnit.X, 0);

            // --- 2. Transform polygon into local edge-aligned coordinates ---
            var localPts = polygon.Points.Select(p =>
            {
                Vector3D v = new Vector3D(p.X - edgeStart.X, p.Y - edgeStart.Y, 0);
                return new Vector3D(Vector3D.DotProduct(v, edgeUnit),
                                    Vector3D.DotProduct(v, perpUnit),
                                    0);
            }).ToList();

            // --- 3. Bounding box ---
            double minAlongEdge = localPts.Min(v => v.X);
            double maxAlongEdge = localPts.Max(v => v.X);
            double minPerp = localPts.Min(v => v.Y);
            double maxPerp = localPts.Max(v => v.Y);

            // --- 4. Determine centered offsets ---
            double totalWidth = maxPerp - minPerp;
            int numLines = (int)Math.Floor(totalWidth / spacing) + 1;
            double usedWidth = (numLines - 1) * spacing;
            double startOffset = minPerp + (totalWidth - usedWidth) / 2.0;

            // --- 5. Generate parallel lines centered in the box ---
            for (int i = 0; i < numLines; i++)
            {
                double offsetPerp = startOffset + i * spacing;

                Vector3D lineStartLocal = new Vector3D(minAlongEdge, offsetPerp, 0);
                Vector3D lineEndLocal = new Vector3D(maxAlongEdge, offsetPerp, 0);

                // Convert back to world coordinates
                Vector3D startWorld = edgeStart.ToVector() + edgeUnit * lineStartLocal.X + perpUnit * lineStartLocal.Y;
                Vector3D endWorld = edgeStart.ToVector() + edgeUnit * lineEndLocal.X + perpUnit * lineEndLocal.Y;

                rafters.Add((startWorld.ToPoint3D(), endWorld.ToPoint3D()));
            }

            rafters = GeometryHelper.TrimLinesToPolygon(rafters, polygon);
            return rafters;
        }





        // Helper: check if two line segments intersect
        public static bool LineSegmentIntersect(Point p1, Point p2, Point q1, Point q2, out Point intersection)
        {
            intersection = new Point();

            double s1_x = p2.X - p1.X;
            double s1_y = p2.Y - p1.Y;
            double s2_x = q2.X - q1.X;
            double s2_y = q2.Y - q1.Y;

            double s = (-s1_y * (p1.X - q1.X) + s1_x * (p1.Y - q1.Y)) / (-s2_x * s1_y + s1_x * s2_y);
            double t = (s2_x * (p1.Y - q1.Y) - s2_y * (p1.X - q1.X)) / (-s2_x * s1_y + s1_x * s2_y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                intersection = new Point(p1.X + (t * s1_x), p1.Y + (t * s1_y));
                return true;
            }

            return false;
        }

        // Helper: check if point is inside polygon
        public static bool IsPointInsidePolygon(Point pt, List<Point> polyPoints)
        {
            int n = polyPoints.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var pi = polyPoints[i];
                var pj = polyPoints[j];

                if (((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                    (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        // Simple distance helper
        public static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Conversion helpers
        public static Point ToPoint(this Point3D p) => new Point(p.X, p.Y);
        public static Point3D ToPoint3D(this Point p) => new Point3D(p.X, p.Y, 0);


        // Helper conversion methods
        public static Vector3D ToVector(this Point3D p) => new Vector3D(p.X, p.Y, p.Z);
        public static Point3D ToPoint3D(this Vector3D v) => new Point3D(v.X, v.Y, v.Z);
    }
}
