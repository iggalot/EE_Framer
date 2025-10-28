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
            public Point Location { get; set; }
            public int Floor { get; set; }
            public int ID { get; set; } // Assigned automatically
        }

        public class StructuralMember
        {
            public MemberType Type { get; set; }
            public Node StartNode { get; set; }
            public Node EndNode { get; set; }
            public int Floor => StartNode.Floor;
            public string ID { get; set; } // e.g., "B1", "C3"
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

        private int beamCounter = 1;
        private int columnCounter = 1;

        public MainWindow()
        {
            InitializeComponent();
            DrawGridLines();
            RedrawMembers();
        }

        // ==================== Floor Buttons ====================
        private void Floor1Button_Click(object sender, RoutedEventArgs e) { currentFloor = 0; RedrawMembers(); UpdateDataGrid(); }
        private void Floor2Button_Click(object sender, RoutedEventArgs e) { currentFloor = 1; RedrawMembers(); UpdateDataGrid(); }
        private void RoofButton_Click(object sender, RoutedEventArgs e) { currentFloor = 2; RedrawMembers(); UpdateDataGrid(); }

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
            OverlayLayer.Children.Clear();
            beamCounter = 1;
            columnCounter = 1;
            RedrawMembers();
            UpdateDataGrid();
            UpdateConnectionGrid();
        }

        private void ComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder.");
        }

        private void ShowColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            RedrawMembers();
            UpdateDataGrid();
            UpdateConnectionGrid();
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
            if (pendingStartPoint != null)
            {
                DrawTempLine(pendingStartPoint.Value, e.GetPosition(MemberLayer));
            }
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Optional zoom placeholder
        }

        // ==================== Node Management ====================
        private Node GetNearbyNode(Point p, int floor)
        {
            return Nodes.FirstOrDefault(n => n.Floor == floor && Distance(n.Location, p) <= snapTolerance);
        }

        private Node CreateNode(Point p, int floor)
        {
            var node = new Node { Location = p, Floor = floor, ID = Nodes.Count + 1 };
            Nodes.Add(node);
            return node;
        }

        private double Distance(Point a, Point b) =>
            Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

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

                Members.Add(new StructuralMember
                {
                    Type = MemberType.Beam,
                    StartNode = startNode,
                    EndNode = clickedNode,
                    ID = $"B{beamCounter++}"
                });

                pendingStartPoint = null;
                addingBeam = false;
                Mouse.OverrideCursor = null;
                OverlayLayer.Children.Clear();
                RedrawMembers();
                UpdateDataGrid();
                UpdateConnectionGrid();
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
            Node bottomNode = GetNearbyNode(new Point(click.X, click.Y + floorHeight), currentFloor - 1) ??
                              CreateNode(new Point(click.X, click.Y + floorHeight), currentFloor - 1);

            Members.Add(new StructuralMember
            {
                Type = MemberType.Column,
                StartNode = topNode,
                EndNode = bottomNode,
                ID = $"C{columnCounter++}"
            });

            addingColumn = false;
            Mouse.OverrideCursor = null;
            RedrawMembers();
            UpdateDataGrid();
            UpdateConnectionGrid();
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
        }

        private void DrawTempLine(Point start, Point end)
        {
            OverlayLayer.Children.Clear();
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

            // Draw node ID label
            TextBlock label = new TextBlock
            {
                Text = $"N{n.ID}",
                Foreground = Brushes.DarkRed,
                FontWeight = FontWeights.Bold,
                FontSize = 10
            };
            Canvas.SetLeft(label, n.Location.X + 6);
            Canvas.SetTop(label, n.Location.Y - 8);
            cnv.Children.Add(label);
        }

        // ==================== Update DataGrid ====================
        private void UpdateDataGrid()
        {
            var tableData = Members.Select(m => new
            {
                ID = m.ID,
                Type = m.Type.ToString(),
                StartNode = $"N{m.StartNode.ID}",
                EndNode = $"N{m.EndNode.ID}",
                StartX = Math.Round(m.StartNode.Location.X, 1),
                StartY = Math.Round(m.StartNode.Location.Y, 1),
                EndX = Math.Round(m.EndNode.Location.X, 1),
                EndY = Math.Round(m.EndNode.Location.Y, 1),
                Floor = m.Floor
            }).ToList();

            BeamDataGrid.ItemsSource = tableData;
        }

        // ==================== Connection Data ====================
        private void UpdateConnectionGrid()
        {
            var connectionData = Nodes.Select(n => new
            {
                Node = $"N{n.ID}",
                X = Math.Round(n.Location.X, 1),
                Y = Math.Round(n.Location.Y, 1),
                Floor = n.Floor,
                ConnectedMembers = string.Join(", ",
                    Members.Where(m => m.StartNode == n || m.EndNode == n)
                           .Select(m => m.ID))
            }).ToList();

            ConnectionDataGrid.ItemsSource = connectionData;
        }
    }
}
