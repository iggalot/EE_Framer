using StructuralPlanner.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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

        private Brush GetMemberColor(MemberType type)
        {
            switch (type)
            {
                case MemberType.Beam:
                    return Brushes.Magenta;
                case MemberType.Column:
                    return Brushes.Red;
                case MemberType.Rafter:
                    return Brushes.Blue;
                case MemberType.FloorJoist:
                    return Brushes.Green;
                case MemberType.CeilingJoist:
                    return Brushes.Green;
                case MemberType.Purlin:
                    return Brushes.Cyan;
                case MemberType.Wall:
                    return Brushes.Brown;
                case MemberType.RoofBrace:
                    return Brushes.Purple;
                default:
                    return Brushes.Black;
            }
        }

        private int GetMemberLineThickness(MemberType type)
        {
            switch (type)
            {
                case MemberType.Beam:
                    return 2;
                case MemberType.Column:
                    return 2;
                case MemberType.Rafter:
                    return 1;
                case MemberType.FloorJoist:
                    return 1;
                case MemberType.CeilingJoist:
                    return 1;
                case MemberType.Purlin:
                    return 2;
                case MemberType.Wall:
                    return 4;
                case MemberType.RoofBrace:
                    return 1;
                default:
                    return 1;
            }
        }

        public void DrawMember(Canvas canvas, StructuralMember m, double opacity = 1.0)
        {
            if (m == null || m.StartNode == null || m.EndNode == null) return;

            Brush stroke = GetMemberColor(m.Type);
            double thickness = GetMemberLineThickness(m.Type);

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
        }

        public void DrawMemberLabel(Canvas canvas, StructuralMember m)
        {
            Brush stroke = Brushes.Black;

            var lbl = new TextBlock { Text = m.MemberID, Foreground = stroke, FontSize = 10, FontWeight = FontWeights.Bold, Background = Brushes.Transparent };

            // Compute midpoint
            double centerX = (m.StartNode.Location.X + m.EndNode.Location.X) / 2;
            double centerY = (m.StartNode.Location.Y + m.EndNode.Location.Y) / 2;

            // Force measure to get actual size before layout
            lbl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size size = lbl.DesiredSize;

            // Offset position so text is centered
            Canvas.SetLeft(lbl, centerX - size.Width / 2);
            Canvas.SetTop(lbl, centerY - size.Height / 2);

            // Add to canvas
            canvas.Children.Add(lbl);
        }

        public void DrawNode(Canvas canvas, Node n)
        {
            double size = 6;
            var ellipse = new Ellipse { Width = size, Height = size, Fill = Brushes.Red, Stroke = Brushes.Black, StrokeThickness = 1 };
            Canvas.SetLeft(ellipse, n.Location.X - size / 2);
            Canvas.SetTop(ellipse, n.Location.Y - size / 2);
            canvas.Children.Add(ellipse);
        }

        public void DrawNodeLabel(Canvas canvas, Node n)
        {
            var lbl = new TextBlock { Text = n.NodeID, Foreground = Brushes.Black, FontSize = 10, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(lbl, n.Location.X + 4);
            Canvas.SetTop(lbl, n.Location.Y - 4);
            canvas.Children.Add(lbl);
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

        public Ellipse DrawSnapCircle(Canvas cnv, Point snapPos, double snapTolerance, Brush fill, Brush stroke, double strokeThickness = 1, double opacity = 1.0)
        {
            // Draw the snapping circle
            Ellipse snapCircle = new Ellipse
            {
                Width = snapTolerance * 2,
                Height = snapTolerance * 2,
                Stroke = Brushes.Orange,
                StrokeThickness = 1
            };
            Canvas.SetLeft(snapCircle, snapPos.X - snapTolerance);
            Canvas.SetTop(snapCircle, snapPos.Y - snapTolerance);
            cnv.Children.Add(snapCircle);

            return snapCircle;
        }

        public void DrawMemberReactions(Canvas cnv, StructuralMember m, Node n)
        {
            if (m == null || n == null) return;

            string text = "—";

            var tg = new TransformGroup();

            Vector direction = m.EndNode.Location - m.StartNode.Location;
            direction.Normalize(); // now it has length 1

            double offset = 20; // move 10 units along the member line
            int sign = 1;

            if (IsSameNode(n, m.StartNode))
            {
                text = $"{m.ReactionFactored_Start_lbf:F0}";

            }
            else if (IsSameNode(n, m.EndNode))
            {
                text = $"{m.ReactionFactored_End_lbf:F0}";
                sign = -1;
            }

            Point shifted = n.Location + sign * direction * offset;

            var lbl = new TextBlock
            {
                Text = text,
                Foreground = Brushes.DarkRed,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Background = Brushes.Yellow // for visibility while testing
            };

            lbl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size size = lbl.DesiredSize;

            double angle = m.Angle;
            tg.Children.Add(new RotateTransform(angle, size.Width / 2, size.Height / 2));

            // Translate so the label is centered at shifted point
            tg.Children.Add(new TranslateTransform(shifted.X - size.Width / 2, shifted.Y - size.Height / 2));


            lbl.RenderTransform = tg;
            //Canvas.SetLeft(lbl, n.Location.X + 8);
            //Canvas.SetTop(lbl, n.Location.Y - 8);
            cnv.Children.Add(lbl);
        }

        private bool IsSameNode(Node a, Node b, double tol = 1e-3)
        {
            if (a == null || b == null) return false;
            return (Math.Abs(a.Location.X - b.Location.X) < tol &&
                    Math.Abs(a.Location.Y - b.Location.Y) < tol);
        }

    }
}