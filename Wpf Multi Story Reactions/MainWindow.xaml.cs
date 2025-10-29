using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StructuralPlanner
{
    public partial class MainWindow : Window
    {
        public enum MemberType { Beam, Column }

        public class Node
        {
            private static int _nodeCount = 0;
            public string NodeID { get; set; }
            public Point Location { get; set; }
            public int Floor { get; set; }

            public List<StructuralMember> ConnectedMembers { get; } = new List<StructuralMember>();

            public Node()
            {
                _nodeCount++;
                NodeID = $"N{_nodeCount}";
            }

            public string ConnectedMemberIDs => string.Join(", ", ConnectedMembers.Select(m => m.ID));
        }

        public class StructuralMember
        {
            public string ID { get; set; }
            public MemberType Type { get; set; }
            public Node StartNode { get; set; }
            public Node EndNode { get; set; }
            public int Floor => StartNode.Floor;
        }

        private readonly List<Node> Nodes = new();
        private readonly List<StructuralMember> Members = new();
        private List<Node> polygonNodes = new List<Node>();
        private readonly List<Polygon> finalizedPolygons = new List<Polygon>();


        private int currentFloor = 0;
        private bool addingBeam = false;
        private bool addingColumn = false;
        private bool addingPolygon = false;
        private bool addingParallelLine = false;

        private Point? pendingStartPoint = null;
        private Line tempLine = null;
        private Polygon previewPolygon = null;
        private Ellipse snapCircle = null;
        private Line tempLineToMouse = null;  // For polygon preview line following mouse

        private enum ParallelLineMode { None, EdgePerp, Vertical, Horizontal }
        private ParallelLineMode currentParallelLineMode = ParallelLineMode.None;
        private Point? parallelStartPoint = null;   // first click
        private Line tempParallelLine = null;       // live preview




        private const double floorHeight = 200;
        private const double snapTolerance = 15;

        private int beamCount = 0;
        private int columnCount = 0;
        private int nodeCount = 0;

        private Line tempPolygonLine = null;


        // Polygon parallel line preview
        private Polygon currentPolygonForLines = null;
        private List<Line> parallelLinePreview = new List<Line>();
        private enum LineMode { PerpendicularEdge, Vertical, Horizontal }
        private LineMode currentLineMode = LineMode.PerpendicularEdge;

        public MainWindow()
        {
            InitializeComponent();
            DrawGridLines();
            RedrawMembers();
        }

        // ==================== Floor Buttons ====================
        private void Floor1Button_Click(object sender, RoutedEventArgs e) { currentFloor = 0; RedrawMembers(); }
        private void Floor2Button_Click(object sender, RoutedEventArgs e) { currentFloor = 1; RedrawMembers(); }
        private void RoofButton_Click(object sender, RoutedEventArgs e) { currentFloor = 2; RedrawMembers(); }

        // ==================== Member Buttons ====================
        private void AddBeamButton_Click(object sender, RoutedEventArgs e)
        {
            addingBeam = true; addingColumn = false; pendingStartPoint = null; Mouse.OverrideCursor = Cursors.Cross;
        }

        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            addingBeam = false; addingColumn = true; pendingStartPoint = null; Mouse.OverrideCursor = Cursors.Cross;
        }

        private void AddPolygonButton_Click(object sender, RoutedEventArgs e)
        {
            StartPolygonSelection();
            MemberLayer.MouseLeftButtonDown += MemberLayer_MouseLeftButtonDown_Polygon;
        }

        private void AddParallelLineButton_Click(object sender, RoutedEventArgs e)
        {
            addingParallelLine = true;
            pendingStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Members.Clear();
            Nodes.Clear();
            pendingStartPoint = null;
            tempLine = null;
            tempPolygonLine = null;
            tempLineToMouse = null;
            beamCount = 0;
            columnCount = 0;
            nodeCount = 0;
            OverlayLayer.Children.Clear();
            MemberLayer.Children.Clear();
            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private void ComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder.");
        }

        // ==================== Polygon Alignment Buttons ====================
        private void BtnEdgePerp_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.EdgePerp;
            parallelStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void BtnVertical_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.Vertical;
            parallelStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void BtnHorizontal_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.Horizontal;
            parallelStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }


        // ==================== Canvas Events ====================
        private void MemberLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(MemberLayer);

            if (addingBeam) HandleAddBeam(click);
            else if (addingColumn) HandleAddColumn(click); 
            else if (addingParallelLine)
            {
                // --- Parallel line handling ---
                if (currentParallelLineMode != ParallelLineMode.None)
                {
                    if (parallelStartPoint == null)
                    {
                        parallelStartPoint = click;
                    }
                    else
                    {
                        Point p1 = parallelStartPoint.Value;
                        Point p2 = click;
                        Point finalEnd = p2;

                        switch (currentParallelLineMode)
                        {
                            case ParallelLineMode.Vertical:
                                finalEnd.X = p1.X; break;
                            case ParallelLineMode.Horizontal:
                                finalEnd.Y = p1.Y; break;
                            case ParallelLineMode.EdgePerp:
                                StructuralMember nearestEdge = FindNearestMember(p1, Members);
                                finalEnd = ProjectPerpendicular(p1, p2, nearestEdge);
                                break;
                        }

                        Line ln = new Line
                        {
                            X1 = p1.X,
                            Y1 = p1.Y,
                            X2 = finalEnd.X,
                            Y2 = finalEnd.Y,
                            Stroke = Brushes.Purple,
                            StrokeThickness = 2
                        };
                        MemberLayer.Children.Add(ln);

                        parallelStartPoint = null;
                        currentParallelLineMode = ParallelLineMode.None;

                        if (tempParallelLine != null)
                        {
                            OverlayLayer.Children.Remove(tempParallelLine);
                            tempParallelLine = null;
                        }

                        Mouse.OverrideCursor = null;
                    }
                    return; // skip other logic
                }

            }
        }

        private void MemberLayer_MouseLeftButtonDown_Polygon(object sender, MouseButtonEventArgs e)
        {
            if (!addingPolygon) return;

            Point click = e.GetPosition(MemberLayer);
            Node node = GetNearbyNode(click, currentFloor) ?? CreateNode(click, currentFloor);

            if (!polygonNodes.Contains(node))
                polygonNodes.Add(node);

            UpdatePolygonPreview();

            // Update temporary line start
            if (polygonNodes.Count >= 1)
            {
                if (tempPolygonLine == null)
                {
                    tempPolygonLine = new Line
                    {
                        Stroke = Brushes.Orange,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };
                    OverlayLayer.Children.Add(tempPolygonLine);
                }
                tempPolygonLine.X1 = node.Location.X;
                tempPolygonLine.Y1 = node.Location.Y;
            }

            if (polygonNodes.Count >= 3)
            {
                currentPolygonForLines = new Polygon();
                foreach (var n in OrderNodesClockwise(polygonNodes))
                    currentPolygonForLines.Points.Add(n.Location);
            }

            if (polygonNodes.Count == 4)
            {
                FinishPolygon();
            }
        }

        private void MemberLayer_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(MemberLayer);

            // --- Remove old snap circle if exists ---
            if (snapCircle != null)
                OverlayLayer.Children.Remove(snapCircle);

            // --- Determine closest node ---
            Node closestNode = Nodes
                .Where(n => n.Floor == currentFloor)
                .OrderBy(n => Distance(mousePos, n.Location))
                .FirstOrDefault();

            Point snapPos = mousePos;

            if (closestNode != null && Distance(mousePos, closestNode.Location) <= snapTolerance)
                snapPos = closestNode.Location;

            // --- Draw snap circle ---
            if (addingBeam || addingBeam || addingPolygon)
            {

                snapCircle = new Ellipse
                {
                    Width = snapTolerance * 2,
                    Height = snapTolerance * 2,
                    Stroke = Brushes.Orange,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(snapCircle, snapPos.X - snapTolerance);
                Canvas.SetTop(snapCircle, snapPos.Y - snapTolerance);
                OverlayLayer.Children.Add(snapCircle);
            }

            // --- Polygon preview line to mouse ---
            if (addingPolygon && polygonNodes.Count > 0)
            {
                Point lastPoint = polygonNodes.Last().Location;

                if (tempLineToMouse != null)
                    OverlayLayer.Children.Remove(tempLineToMouse);

                tempLineToMouse = DrawTempLine(lastPoint, snapPos);

                if(OverlayLayer.Children.Contains(tempLineToMouse) is false)
                {
                    OverlayLayer.Children.Add(tempLineToMouse);
                }
            }

            if (addingBeam && pendingStartPoint.HasValue)
            {
                if(tempLineToMouse != null)
                    OverlayLayer.Children.Remove(tempLineToMouse);

                tempLineToMouse = DrawTempLine(pendingStartPoint.Value, mousePos);

                if (OverlayLayer.Children.Contains(tempLineToMouse) is false)
                {
                    OverlayLayer.Children.Add(tempLineToMouse);
                }
            }

            // --- Parallel line preview ---
            if (currentParallelLineMode != ParallelLineMode.None && parallelStartPoint != null)
            {
                Point p1 = parallelStartPoint.Value;
                Point previewEnd = mousePos;

                switch (currentParallelLineMode)
                {
                    case ParallelLineMode.Vertical:
                        previewEnd.X = p1.X; break;
                    case ParallelLineMode.Horizontal:
                        previewEnd.Y = p1.Y; break;
                    case ParallelLineMode.EdgePerp:
                        StructuralMember nearestEdge = FindNearestMember(p1, Members);
                        previewEnd = ProjectPerpendicular(p1, mousePos, nearestEdge);
                        break;
                }

                if (tempParallelLine != null)
                    OverlayLayer.Children.Remove(tempParallelLine);

                tempParallelLine = new Line
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = previewEnd.X,
                    Y2 = previewEnd.Y,
                    Stroke = Brushes.Purple,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                OverlayLayer.Children.Add(tempParallelLine);
            }



        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e) { /* Optional zoom */ }

        private void CreateRoofButton_Click(object sender, RoutedEventArgs e)
        {
            CreateTestRoof();
        }

        // ==================== Node / Member Methods ====================
        private Node GetNearbyNode(Point p, int floor) => Nodes.FirstOrDefault(n => n.Floor == floor && Distance(n.Location, p) <= snapTolerance);

        private Node CreateNode(Point p, int floor)
        {
            var node = new Node { Location = p, Floor = floor };
            Nodes.Add(node);
            return node;
        }

        private double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        private void HandleAddBeam(Point click)
        {
            Node clickedNode = GetNearbyNode(click, currentFloor) ?? CreateNode(click, currentFloor);

            if (pendingStartPoint == null)
            {
                pendingStartPoint = clickedNode.Location;
            }
            else
            {
                Node startNode = GetNearbyNode(pendingStartPoint.Value, currentFloor) ?? CreateNode(pendingStartPoint.Value, currentFloor);
                CreateBeam(startNode, clickedNode);
                pendingStartPoint = null;
                addingBeam = false;
                Mouse.OverrideCursor = null;
                OverlayLayer.Children.Clear();
            }
        }

        private void HandleAddColumn(Point click)
        {
            if (currentFloor == 0)
            {
                MessageBox.Show("No floor below to connect a column to.");
                addingColumn = false;
                Mouse.OverrideCursor = null;
                return;
            }

            Node topNode = GetNearbyNode(click, currentFloor) ?? CreateNode(click, currentFloor);
            Node bottomNode = GetNearbyNode(new Point(click.X, click.Y), currentFloor - 1) ?? CreateNode(new Point(click.X, click.Y), currentFloor - 1);

            columnCount++;
            var member = new StructuralMember { ID = $"C{columnCount}", Type = MemberType.Column, StartNode = topNode, EndNode = bottomNode };
            Members.Add(member);

            topNode.ConnectedMembers.Add(member);
            bottomNode.ConnectedMembers.Add(member);

            addingColumn = false;
            Mouse.OverrideCursor = null;
            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private void CreateBeam(Node startNode, Node endNode)
        {
            beamCount++;
            var member = new StructuralMember
            {
                ID = $"B{beamCount}",
                Type = MemberType.Beam,
                StartNode = startNode,
                EndNode = endNode
            };

            Members.Add(member);

            startNode.ConnectedMembers.Add(member);
            endNode.ConnectedMembers.Add(member);

            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private List<Node> OrderNodesClockwise(List<Node> nodes)
        {
            var center = new Point(nodes.Average(n => n.Location.X), nodes.Average(n => n.Location.Y));
            return nodes.OrderBy(n => Math.Atan2(n.Location.Y - center.Y, n.Location.X - center.X)).ToList();
        }

        private void DrawGridLines()
        {
            GridLayer.Children.Clear();
            double spacing = 20, width = 1200, height = 800;
            for (double x = 0; x < width; x += spacing) GridLayer.Children.Add(new Line { X1 = x, Y1 = 0, X2 = x, Y2 = height, Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), StrokeThickness = 1 });
            for (double y = 0; y < height; y += spacing) GridLayer.Children.Add(new Line { X1 = 0, Y1 = y, X2 = width, Y2 = y, Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), StrokeThickness = 1 });
        }

        private void RedrawMembers()
        {
            MemberLayer.Children.Clear();
            OverlayLayer.Children.Clear();

            foreach (var m in Members.Where(m => m.Floor == currentFloor))
                DrawMember(MemberLayer, m, 1.0);
            if (currentFloor > 0)
                foreach (var m in Members.Where(m => m.Floor == currentFloor - 1))
                    DrawMember(MemberLayer, m, 0.3);

            foreach (var n in Nodes.Where(n => n.Floor == currentFloor))
                DrawNode(MemberLayer, n);

            // Redraw finalized polygons only for current floor
            foreach (var poly in finalizedPolygons)
            {
                if (poly.Points.Count == 0)
                    continue;

                // Determine floor from first node that matches a polygon point
                var firstPoint = poly.Points[0];
                var node = Nodes.FirstOrDefault(n => n.Location == firstPoint);
                if (node != null && node.Floor == currentFloor)
                {
                    if (!MemberLayer.Children.Contains(poly))
                        MemberLayer.Children.Add(poly);
                }
            }


            // Keep preview polygon and temp line if they exist
            if (previewPolygon != null && !OverlayLayer.Children.Contains(previewPolygon))
                OverlayLayer.Children.Add(previewPolygon);

            if (tempLineToMouse != null && !OverlayLayer.Children.Contains(tempLineToMouse))
                OverlayLayer.Children.Add(tempLineToMouse);


            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private void DrawMember(Canvas cnv, StructuralMember m, double opacity)
        {
            Brush stroke = m.Type == MemberType.Beam ? Brushes.SteelBlue : Brushes.Gray;
            double thickness = m.Type == MemberType.Beam ? 3 : 4;

            cnv.Children.Add(new Line { X1 = m.StartNode.Location.X, Y1 = m.StartNode.Location.Y, X2 = m.EndNode.Location.X, Y2 = m.EndNode.Location.Y, Stroke = stroke, StrokeThickness = thickness, Opacity = opacity });

            TextBlock lbl = new TextBlock { Text = m.ID, Foreground = Brushes.Black, FontWeight = FontWeights.Bold, Background = Brushes.White };
            Canvas.SetLeft(lbl, (m.StartNode.Location.X + m.EndNode.Location.X) / 2 + 2);
            Canvas.SetTop(lbl, (m.StartNode.Location.Y + m.EndNode.Location.Y) / 2 + 2);
            cnv.Children.Add(lbl);
        }

        private void DrawNode(Canvas cnv, Node n)
        {
            double size = 6;
            Ellipse ellipse = new Ellipse { Width = size, Height = size, Fill = Brushes.Red, Stroke = Brushes.Black, StrokeThickness = 1 };
            Canvas.SetLeft(ellipse, n.Location.X - size / 2);
            Canvas.SetTop(ellipse, n.Location.Y - size / 2);
            cnv.Children.Add(ellipse);

            TextBlock lbl = new TextBlock { Text = n.NodeID, Foreground = Brushes.Black, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(lbl, n.Location.X + 4);
            Canvas.SetTop(lbl, n.Location.Y - 4);
            cnv.Children.Add(lbl);
        }

        private void UpdateDataGrid()
        {
            var tableData = Members.Select(m => new
            {
                ID = m.ID,
                Type = m.Type.ToString(),
                StartX = Math.Round(m.StartNode.Location.X, 1),
                StartY = Math.Round(m.StartNode.Location.Y, 1),
                EndX = Math.Round(m.EndNode.Location.X, 1),
                EndY = Math.Round(m.EndNode.Location.Y, 1),
                Floor = m.Floor
            }).ToList();

            BeamDataGrid.ItemsSource = tableData;
        }

        private void UpdateNodeConnectionsDataGrid()
        {
            var data = Nodes.Select(n => new
            {
                NodeID = n.NodeID,
                Floor = n.Floor,
                ConnectedMembers = n.ConnectedMemberIDs
            }).ToList();

            NodeConnectionsDataGrid.ItemsSource = data;
        }

        private Line DrawTempLine(Point start, Point end)
        {
            if (tempLine != null) OverlayLayer.Children.Remove(tempLine);
            tempLine = new Line { X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y, Stroke = Brushes.Orange, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 } };
            OverlayLayer.Children.Add(tempLine);

            return tempLine;
        }

        private void StartPolygonSelection()
        {
            polygonNodes.Clear();
            addingPolygon = true;
            if (previewPolygon != null) OverlayLayer.Children.Remove(previewPolygon);

            previewPolygon = new Polygon { Stroke = Brushes.Green, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 }, Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)) };
            OverlayLayer.Children.Add(previewPolygon);

            MessageBox.Show("Click 3 or 4 distinct nodes to form the polygon. Nodes will snap automatically.");
        }

        private void FinishPolygon()
        {
            if (polygonNodes.Count >= 3)
            {
                // Sort nodes clockwise
                var sortedNodes = OrderNodesClockwise(polygonNodes);

                // Create the final polygon
                Polygon finalPolygon = new Polygon
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0))
                };

                foreach (var n in sortedNodes)
                    finalPolygon.Points.Add(n.Location);

                // Add to overlay and store reference so it persists on redraw
                MemberLayer.Children.Add(finalPolygon);
                finalizedPolygons.Add(finalPolygon);

                // Clear temporary preview
                if (previewPolygon != null)
                {
                    OverlayLayer.Children.Remove(previewPolygon);
                    previewPolygon = null;
                }

                // Clear temp line to mouse
                if (tempLineToMouse != null)
                {
                    OverlayLayer.Children.Remove(tempLineToMouse);
                    tempLineToMouse = null;
                }

                MessageBox.Show($"Polygon created with {polygonNodes.Count} nodes.");
            }

            // Reset polygon state
            polygonNodes.Clear();
            addingPolygon = false;
            Mouse.OverrideCursor = null;
        }

        private void UpdatePolygonPreview()
        {
            if (previewPolygon == null) return;

            previewPolygon.Points.Clear();
            foreach (var n in polygonNodes) previewPolygon.Points.Add(n.Location);
            if (polygonNodes.Count >= 2)
                previewPolygon.Points.Add(polygonNodes[0].Location);
        }

        private void CreateTestRoof()
        {
            Node n1 = CreateNode(new Point(100, 100), currentFloor);
            Node n2 = CreateNode(new Point(300, 100), currentFloor);
            Node n3 = CreateNode(new Point(300, 300), currentFloor);
            Node n4 = CreateNode(new Point(100, 300), currentFloor);

            CreateBeam(n1, n2);
            CreateBeam(n2, n3);
            CreateBeam(n3, n4);
            CreateBeam(n4, n1);
        }

        // ==================== Polygon Parallel Lines ====================
        private void DrawParallelLinesPreview()
        {
            // Remove previous
            parallelLinePreview.ForEach(l => OverlayLayer.Children.Remove(l));
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
                    OverlayLayer.Children.Add(l);
                    parallelLinePreview.Add(l);
                }
            }
            else if (currentLineMode == LineMode.Vertical)
            {
                for (double x = bounds.Left; x <= bounds.Right; x += spacing)
                {
                    Line l = new Line { X1 = x, X2 = x, Y1 = bounds.Top, Y2 = bounds.Bottom, Stroke = Brushes.Purple, StrokeThickness = 1 };
                    OverlayLayer.Children.Add(l);
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
                    OverlayLayer.Children.Add(l);
                    parallelLinePreview.Add(l);
                }
            }
        }

        private StructuralMember FindNearestMember(Point p, List<StructuralMember> members)
        {
            if (members.Count == 0) return null;
            return members.OrderBy(m => Distance(p, MidPoint(m.StartNode.Location, m.EndNode.Location))).First();
        }

        private Point MidPoint(Point a, Point b) => new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);

        private Point ProjectPerpendicular(Point origin, Point mousePos, StructuralMember edge)
        {
            if (edge == null) return mousePos;
            Vector edgeVec = edge.EndNode.Location - edge.StartNode.Location;
            edgeVec.Normalize();
            Vector mouseVec = mousePos - origin;
            Vector perp = new Vector(-edgeVec.Y, edgeVec.X); // perpendicular
            double length = Vector.Multiply(mouseVec, perp);
            return origin + perp * length;
        }

    }
}
