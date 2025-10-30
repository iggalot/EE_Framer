using StructuralPlanner.Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Shapes;

namespace StructuralPlanner.Services
{
    public class MemberDetectionService
    {
        public static StructuralMember FindNearestMember(Point click, List<StructuralMember> members, double tolerance = 10.0)
        {
            if (members == null || members.Count == 0) return null;

            double minDist = double.MaxValue;
            StructuralMember nearest = null;

            foreach (var m in members)
            {
                var proj = GeometryHelper.ProjectPointOntoSegment(click, m.StartNode.Location, m.EndNode.Location);
                double d = GeometryHelper.Distance(click, proj);
                if (d < minDist && d <= tolerance)
                {
                    minDist = d;
                    nearest = m;
                }
            }

            return nearest;
        }

        public static Point? FindNearestPointOnMember(Point click, List<StructuralMember> members, out StructuralMember nearestMember)
        {
            nearestMember = null;
            if (members == null || members.Count == 0) return null;

            double minDist = double.MaxValue;
            Point closestPoint = new Point();

            foreach (var m in members)
            {
                Point a = m.StartNode.Location;
                Point b = m.EndNode.Location;

                Point projected = GeometryHelper.ProjectPointOntoSegment(click, a, b);
                double dist = GeometryHelper.Distance(click, projected);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestPoint = projected;
                    nearestMember = m;
                }
            }

            return closestPoint;
        }

        public static bool EdgeHasMember(Point a, Point b, List<StructuralMember> members, double tolerance = 0.5)
        {
            foreach (var m in members)
            {
                Point mA = m.StartNode.Location;
                Point mB = m.EndNode.Location;

                bool match = (GeometryHelper.Distance(a, mA) < tolerance && GeometryHelper.Distance(b, mB) < tolerance) ||
                             (GeometryHelper.Distance(a, mB) < tolerance && GeometryHelper.Distance(b, mA) < tolerance);

                if (match) return true;
            }
            return false;
        }

        public static List<(Point Start, Point End)> GetPolygonEdges(Polygon polygon)
        {
            var edges = new List<(Point Start, Point End)>();
            var pts = polygon.Points;

            if (pts.Count < 2)
                return edges;

            for (int i = 0; i < pts.Count; i++)
            {
                Point start = pts[i];
                Point end = (i == pts.Count - 1) ? pts[0] : pts[i + 1]; // wrap around
                edges.Add((start, end));
            }

            return edges;
        }

        private void CheckPolygonEdgesForMembers(Polygon polygon, List<StructuralMember> Members, int currentFloor)
        {
            var edges = MemberDetectionService.GetPolygonEdges(polygon);

            foreach (var edge in edges)
            {
                var floorMembers = Members.Where(m => m.Floor == currentFloor).ToList();
                bool hasMember = MemberDetectionService.EdgeHasMember(edge.Start, edge.End, floorMembers);

                Debug.WriteLine($"Edge {edge.Start} → {edge.End} has member: {hasMember}");
            }
        }
    }
}