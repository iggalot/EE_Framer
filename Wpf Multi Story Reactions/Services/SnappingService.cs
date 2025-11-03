using StructuralPlanner.Models;
using System.Windows;

namespace StructuralPlanner.Services
{
    public class SnappingService
    {
        public Node GetNearbyNode(Point p, List<Node> nodes, int floor, double snapTolerance)
        {
            if (nodes == null || nodes.Count == 0) return null;
            return nodes.Where(n => n.Floor == floor).OrderBy(n => GeometryHelper.Distance(p, n.Location)).FirstOrDefault(n => GeometryHelper.Distance(p, n.Location) <= snapTolerance);
        }

        public Point SnapToGrid(Point p, double spacing)
        {
            double x = Math.Round(p.X / spacing) * spacing;
            double y = Math.Round(p.Y / spacing) * spacing;
            return new Point(x, y);
        }
    }
}