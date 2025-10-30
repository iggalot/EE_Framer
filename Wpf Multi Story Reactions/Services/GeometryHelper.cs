using StructuralPlanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
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

        public static Polygon GetPolygonContainingPoint(Point click, List<Polygon> finalizedPolygons)
        {
            foreach (var poly in finalizedPolygons)
            {
                // Convert the polygon points to a StreamGeometry
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(poly.Points[0], true, true); // is filled, is closed
                    ctx.PolyLineTo(poly.Points.Skip(1).ToList(), true, true);
                }
                geometry.Freeze(); // optional for performance

                if (geometry.FillContains(click))
                    return poly;
            }

            return null; // No polygon contains the point
        }

        public static  List<Node> OrderNodesClockwise(List<Node> nodes)
        {
            var center = new Point(nodes.Average(n => n.Location.X), nodes.Average(n => n.Location.Y));
            return nodes.OrderBy(n => Math.Atan2(n.Location.Y - center.Y, n.Location.X - center.X)).ToList();
        }
    }
}