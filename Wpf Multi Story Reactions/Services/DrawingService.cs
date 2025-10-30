using StructuralPlanner.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace StructuralPlanner.Services
{
    public class DrawingService
    {
        public bool DrawPolygon(Canvas cnv, Polygon poly, List<Node> nodes, int currentFloor)
        {
            if (poly.Points.Count == 0)
                return false;

            // Determine floor from first node that matches a polygon point
            var firstPoint = poly.Points[0];
            var node = nodes.FirstOrDefault(n => n.Location == firstPoint);
            if (node != null && node.Floor == currentFloor)
            {
                if (!cnv.Children.Contains(poly))
                    cnv.Children.Add(poly);
            }

            return true;
        }

        public void DrawMember(Canvas canvas, StructuralMember m, double opacity = 1.0)
        {
            if (m == null || m.StartNode == null || m.EndNode == null) return;

            Brush stroke = m.Type == MemberType.Beam ? Brushes.SteelBlue : Brushes.Gray;
            double thickness = m.Type == MemberType.Beam ? 3 : 4;

            var line = new Line
            {
                X1 = m.StartNode.Location.X,
                Y1 = m.StartNode.Location.Y,
                X2 = m.EndNode.Location.X,
                Y2 = m.EndNode.Location.Y,
                Stroke = stroke,
                StrokeThickness = thickness,
                Opacity = opacity
            };

            canvas.Children.Add(line);

            var lbl = new TextBlock { Text = m.ID, Foreground = Brushes.Black, FontWeight = FontWeights.Bold, Background = Brushes.White };
            Canvas.SetLeft(lbl, (m.StartNode.Location.X + m.EndNode.Location.X) / 2 + 2);
            Canvas.SetTop(lbl, (m.StartNode.Location.Y + m.EndNode.Location.Y) / 2 + 2);
            canvas.Children.Add(lbl);
        }

        public void DrawNode(Canvas canvas, Node n)
        {
            double size = 6;
            var ellipse = new Ellipse { Width = size, Height = size, Fill = Brushes.Red, Stroke = Brushes.Black, StrokeThickness = 1 };
            Canvas.SetLeft(ellipse, n.Location.X - size / 2);
            Canvas.SetTop(ellipse, n.Location.Y - size / 2);
            canvas.Children.Add(ellipse);

            var lbl = new TextBlock { Text = n.NodeID, Foreground = Brushes.Black, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(lbl, n.Location.X + 4);
            Canvas.SetTop(lbl, n.Location.Y - 4);
            canvas.Children.Add(lbl);
        }

        public void DrawPerpendicularLine(Canvas overlay, Point pt, StructuralMember edge, double length = 100)
        {
            if (edge == null) return;
            Vector memberVec = edge.EndNode.Location - edge.StartNode.Location;
            if (memberVec.Length == 0) return;
            Vector perp = new Vector(-memberVec.Y, memberVec.X);
            perp.Normalize();

            Point p1 = pt + perp * (length / 2);
            Point p2 = pt - perp * (length / 2);

            Line perpLine = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };

            overlay.Children.Add(perpLine);
        }

        public Line CreateTempLine(Point start, Point end)
        {
            return new Line { X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y, Stroke = Brushes.Orange, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 } };
        }

        public Line DrawTempLine(Canvas cnv, Line tempLine, Point start, Point end)
        {
            if (tempLine != null) cnv.Children.Remove(tempLine);
            tempLine = new Line { X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y, Stroke = Brushes.Orange, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 } };
            cnv.Children.Add(tempLine);

            return tempLine;
        }

        public void DrawGridLines(Canvas cnv)
        {
            cnv.Children.Clear();
            double spacing = 20, width = 1200, height = 800;
            for (double x = 0; x < width; x += spacing) cnv.Children.Add(new Line { X1 = x, Y1 = 0, X2 = x, Y2 = height, Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), StrokeThickness = 1 });
            for (double y = 0; y < height; y += spacing) cnv.Children.Add(new Line { X1 = 0, Y1 = y, X2 = width, Y2 = y, Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), StrokeThickness = 1 });
        }

        public void DrawParallelLinesPreview(Canvas cnv, List<Line> parallelLinePreview, Polygon currentPolygonForLines, LineMode currentLineMode)
        {
            // Remove previous
            parallelLinePreview.ForEach(l => cnv.Children.Remove(l));
            parallelLinePreview.Clear();
            if (currentPolygonForLines == null) return;

            List<Point> pts = currentPolygonForLines.Points.Select(p => new Point(p.X, p.Y)).ToList();

            if (pts.Count < 3) return;

            Rect bounds = new Rect(pts.Min(p => p.X), pts.Min(p => p.Y), pts.Max(p => p.X) - pts.Min(p => p.X), pts.Max(p => p.Y) - pts.Min(p => p.Y));
            double spacing = 15;

            if (currentLineMode == LineMode.Horizontal)
            {
                for (double y = bounds.Top; y <= bounds.Bottom; y += spacing)
                {
                    Line l = new Line { X1 = bounds.Left, X2 = bounds.Right, Y1 = y, Y2 = y, Stroke = Brushes.Purple, StrokeThickness = 1 };
                    cnv.Children.Add(l);
                    parallelLinePreview.Add(l);
                }
            }
            else if (currentLineMode == LineMode.Vertical)
            {
                for (double x = bounds.Left; x <= bounds.Right; x += spacing)
                {
                    Line l = new Line { X1 = x, X2 = x, Y1 = bounds.Top, Y2 = bounds.Bottom, Stroke = Brushes.Purple, StrokeThickness = 1 };
                    cnv.Children.Add(l);
                    parallelLinePreview.Add(l);
                }
            }
            else if (currentLineMode == LineMode.PerpendicularEdge)
            {
                // Simple demo: perpendicular to first edge
                Point a = pts[0], b = pts[1];
                Vector edge = b - a;
                Vector perp = new Vector(-edge.Y, edge.X);
                perp.Normalize();
                double len = edge.Length;
                int lines = (int)(len / spacing);

                for (int i = 0; i < lines; i++)
                {
                    Vector offset = perp * spacing * i;
                    Line l = new Line { X1 = a.X + offset.X, Y1 = a.Y + offset.Y, X2 = b.X + offset.X, Y2 = b.Y + offset.Y, Stroke = Brushes.Purple, StrokeThickness = 1 };
                    cnv.Children.Add(l);
                    parallelLinePreview.Add(l);
                }
            }
        }

        // Call this from your ⊥ edge button click or mode
        public void DrawPerpendicularFromClick(Canvas cnv, Point pt, StructuralMember edge)
        {
            if (edge == null) return;

            // Project click onto the member line
            Point a = edge.StartNode.Location;
            Point b = edge.EndNode.Location;

            // Calculate perpendicular direction
            Vector memberVec = b - a;
            Vector perp = new Vector(-memberVec.Y, memberVec.X); // rotate 90°
            perp.Normalize();

            // Define perpendicular line length (e.g., 100 pixels)
            double length = 100;
            Point p1 = pt + perp * (length / 2);
            Point p2 = pt - perp * (length / 2);

            // Draw line
            Line perpLine = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };

            cnv.Children.Add(perpLine);
        }

    }
}