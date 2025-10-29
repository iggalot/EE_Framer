using System;
using System.Collections.Generic;
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

            // Keep a list of all connected members
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

        private int currentFloor = 0;
        private bool addingBeam = false;
        private bool addingColumn = false;
        private Point? pendingStartPoint = null;
        private Line tempLine = null;

        private const double floorHeight = 200;
        private const double snapTolerance = 15;

        private int beamCount = 0;
        private int columnCount = 0;
        private int nodeCount = 0;

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

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Members.Clear();
            Nodes.Clear();
            pendingStartPoint = null;
            beamCount = 0;
            columnCount = 0;
            nodeCount = 0;
            OverlayLayer.Children.Clear();
            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private void ComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder.");
        }

        private void ShowColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            RedrawMembers();
        }

        // ==================== Canvas Events ====================
        private void MemberLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(MemberLayer);

            if (addingBeam) HandleAddBeam(click);
            else if (addingColumn) HandleAddColumn(click);
        }

        private void MemberLayer_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(MemberLayer);
            OverlayLayer.Children.Clear();

            if (!addingBeam && !addingColumn) return;

            // Snap circle
            Ellipse snapCircle = new Ellipse
            {
                Width = snapTolerance * 2,
                Height = snapTolerance * 2,
                Stroke = Brushes.Orange,
                StrokeThickness = 1
            };
            Canvas.SetLeft(snapCircle, mousePos.X - snapTolerance);
            Canvas.SetTop(snapCircle, mousePos.Y - snapTolerance);
            OverlayLayer.Children.Add(snapCircle);

            // Highlight closest node
            Node closestNode = Nodes
                .Where(n => n.Floor == currentFloor)
                .OrderBy(n => Distance(mousePos, n.Location))
                .FirstOrDefault();

            if (closestNode != null && Distance(mousePos, closestNode.Location) <= snapTolerance)
            {
                Ellipse highlight = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = Brushes.Yellow,
                    Opacity = 0.5
                };
                Canvas.SetLeft(highlight, closestNode.Location.X - 6);
                Canvas.SetTop(highlight, closestNode.Location.Y - 6);
                OverlayLayer.Children.Add(highlight);
            }

            // Update node connections grid
            UpdateNodeConnectionsDataGrid();

            // Temporary line
            if (pendingStartPoint != null)
                DrawTempLine(pendingStartPoint.Value, mousePos);
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Optional zoom placeholder
        }

        private void CreateRoofButton_Click(object sender, RoutedEventArgs e)
        {
            CreateTestRoof();
        }



        // ==================== Node Management ====================
        private Node GetNearbyNode(Point p, int floor)
        {
            return Nodes.FirstOrDefault(n => n.Floor == floor && Distance(n.Location, p) <= snapTolerance);
        }

        private Node CreateNode(Point p, int floor)
        {
            var node = new Node { Location = p, Floor = floor };
            Nodes.Add(node);
            return node;
        }

        private double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        // ==================== Adding Members ====================
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

                CreateBeam(startNode, clickedNode);  // ✅ clean delegation

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

            // 🔹 Track connection in both nodes
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

            // Track connections in both nodes
            startNode.ConnectedMembers.Add(member);
            endNode.ConnectedMembers.Add(member);

            // Redraw and refresh
            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }



        // ==================== Drawing ====================
        private void DrawGridLines()
        {
            GridLayer.Children.Clear();
            double spacing = 20, width = 1200, height = 800;

            for (double x = 0; x < width; x += spacing)
                GridLayer.Children.Add(new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    StrokeThickness = 1
                });

            for (double y = 0; y < height; y += spacing)
                GridLayer.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    StrokeThickness = 1
                });
        }

        private void RedrawMembers()
        {
            MemberLayer.Children.Clear();
            OverlayLayer.Children.Clear();

            // Draw members on current floor
            foreach (var m in Members.Where(m => m.Floor == currentFloor))
                DrawMember(MemberLayer, m, 1.0);

            // Draw faint members from floor below
            if (currentFloor > 0)
            {
                foreach (var m in Members.Where(m => m.Floor == currentFloor - 1))
                    DrawMember(MemberLayer, m, 0.3);
            }

            // Draw nodes for current floor
            foreach (var n in Nodes.Where(n => n.Floor == currentFloor))
                DrawNode(MemberLayer, n);

            // Update data grids
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private void DrawTempLine(Point start, Point end)
        {
            tempLine = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            OverlayLayer.Children.Add(tempLine);
        }

        private void DrawMember(Canvas cnv, StructuralMember m, double opacity)
        {
            Brush stroke = m.Type == MemberType.Beam ? Brushes.SteelBlue : Brushes.Gray;
            double thickness = m.Type == MemberType.Beam ? 3 : 4;

            cnv.Children.Add(new Line
            {
                X1 = m.StartNode.Location.X,
                Y1 = m.StartNode.Location.Y,
                X2 = m.EndNode.Location.X,
                Y2 = m.EndNode.Location.Y,
                Stroke = stroke,
                StrokeThickness = thickness,
                Opacity = opacity
            });

            // Draw member ID label at midpoint
            TextBlock lbl = new TextBlock
            {
                Text = m.ID,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                Background = Brushes.White
            };
            double midX = (m.StartNode.Location.X + m.EndNode.Location.X) / 2;
            double midY = (m.StartNode.Location.Y + m.EndNode.Location.Y) / 2;
            Canvas.SetLeft(lbl, midX + 2);
            Canvas.SetTop(lbl, midY + 2);
            cnv.Children.Add(lbl);
        }

        private void DrawNode(Canvas cnv, Node n)
        {
            double size = 6;
            Ellipse ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, n.Location.X - size / 2);
            Canvas.SetTop(ellipse, n.Location.Y - size / 2);
            cnv.Children.Add(ellipse);

            // Draw node ID
            TextBlock lbl = new TextBlock
            {
                Text = n.NodeID,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(lbl, n.Location.X + 4);
            Canvas.SetTop(lbl, n.Location.Y - 4);
            cnv.Children.Add(lbl);
        }

        // ==================== Update DataGrids ====================
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

        private void CreateTestRoof()
        {
            if (currentFloor < 0)
            {
                MessageBox.Show("Please select a valid floor first.");
                return;
            }

            // Create 4 nodes in a square
            Node n1 = CreateNode(new Point(100, 100), currentFloor);
            Node n2 = CreateNode(new Point(300, 100), currentFloor);
            Node n3 = CreateNode(new Point(300, 300), currentFloor);
            Node n4 = CreateNode(new Point(100, 300), currentFloor);

            // Connect with 4 beams
            CreateBeam(n1, n2);
            CreateBeam(n2, n3);
            CreateBeam(n3, n4);
            CreateBeam(n4, n1);
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
    }
}
