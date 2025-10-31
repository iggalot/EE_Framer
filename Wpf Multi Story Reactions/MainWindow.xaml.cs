using StructuralPlanner.Managers;
using StructuralPlanner.Models;
using StructuralPlanner.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace StructuralPlanner
{
    public partial class MainWindow : Window
    {
        // State
        private List<Node> Nodes = new List<Node>();
        private List<StructuralMember> Members = new List<StructuralMember>();
        private List<Node> polygonNodes = new List<Node>();
        private List<Polygon> finalizedPolygons = new List<Polygon>();

        private int currentFloor = 0;
        private bool addingBeam = false;
        private bool addingColumn = false;
        private bool addingPolygon = false;
        private bool addingParallelLine = false;

        private Point? pendingStartPoint = null;
        private Polygon previewPolygon = null;
        private Ellipse snapCircle = null;
        private Line tempLineToMouse = null;  // For polygon preview line following mouse

        private enum ParallelLineMode { None, EdgePerp, Vertical, Horizontal }
        private ParallelLineMode currentParallelLineMode = ParallelLineMode.None;
        private Point? parallelStartPoint = null;   // first click
        private Line tempPolygonLine = null;

        // Services
        private readonly DrawingService drawingService = new DrawingService();
        private readonly MemberDetectionService memberDetectionService = new MemberDetectionService();
        private readonly SnappingService snappingService = new SnappingService();
        private readonly CanvasManager canvasManager;

        private const double snapTolerance = 15;

        // Polygon parallel line preview
        private Polygon currentPolygonForLines = null;
        private List<Line> parallelLinePreview = new List<Line>();


        public MainWindow()
        {
            InitializeComponent();
            canvasManager = new CanvasManager(drawingService);

            drawingService.DrawGridLines(GridLayer);
            RedrawMembers();

            ResetUI();
        }

        // Restore the UI and application to initial state
        private void ResetUI()
        {
            spParallelLinesButtons.Visibility = Visibility.Collapsed;

            MemberLayer.MouseLeftButtonDown -= MemberLayer_MouseLeftButtonDown_Polygon;


            addingBeam = false;
            addingColumn = false;
            addingPolygon = false;
            addingParallelLine = false;
            parallelStartPoint = null;
            parallelLinePreview.Clear();
            currentPolygonForLines = null;
            parallelLinePreview.Clear();
            previewPolygon = null;
            snapCircle = null;
            tempLineToMouse = null;  // For polygon preview line following mouse
            pendingStartPoint = null;
            polygonNodes.Clear();

            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        // Helper function to call into our canvas manager
        private void RedrawMembers() { canvasManager.RedrawMembers(MemberLayer, OverlayLayer, Members, Nodes, finalizedPolygons, previewPolygon, tempLineToMouse, currentFloor); }

        // ==================== Member Buttons ====================
        #region UI Button Clicks
        private void btnFloor1Button_Click(object sender, RoutedEventArgs e) { currentFloor = 0; RedrawMembers(); }
        private void btnFloor2Button_Click(object sender, RoutedEventArgs e) { currentFloor = 1; RedrawMembers(); }
        private void btnRoofButton_Click(object sender, RoutedEventArgs e) { currentFloor = 2; RedrawMembers(); }


        private void btnAddBeamButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            addingBeam = true; addingColumn = false; pendingStartPoint = null; Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnAddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            addingBeam = false; addingColumn = true; pendingStartPoint = null; Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnAddPolygonButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            addingPolygon = true;

            if (previewPolygon != null) OverlayLayer.Children.Remove(previewPolygon);

            previewPolygon = new Polygon { Stroke = Brushes.Green, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 }, Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)) };
            OverlayLayer.Children.Add(previewPolygon);

            MemberLayer.MouseLeftButtonDown += MemberLayer_MouseLeftButtonDown_Polygon;
        }

        private void btnAddParallelLineButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUI();
            spParallelLinesButtons.Visibility = Visibility.Visible;
            addingParallelLine = true;
            pendingStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnClearButton_Click(object sender, RoutedEventArgs e)
        {
            Members.Clear();
            Nodes.Clear();
            finalizedPolygons.Clear();

            ResetUI();
        }

        private void btnComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder.");
        }

        // ==================== Polygon Alignment Buttons ====================
        private void btnEdgePerp_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.EdgePerp;
            parallelStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnVertical_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.Vertical;
            parallelStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnHorizontal_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.Horizontal;
            parallelStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }
        private void btnCreateRoofButton_Click(object sender, RoutedEventArgs e)
        {
            CreateTestRoof();
        }

        #endregion

        // ==================== Canvas Events ====================
        #region Canvas Events
        // ==================== Canvas Events ====================
        private void MemberLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(MemberLayer);

            if (addingBeam) HandleAddBeam(click);
            else if (addingColumn) HandleAddColumn(click);
            else if (addingPolygon) HandleAddPolygon();
            else if (addingParallelLine) HandleParallelLineCreation(click);
        }

        private void MemberLayer_MouseLeftButtonDown_Polygon(object sender, MouseButtonEventArgs e)
        {
            if (!addingPolygon) return;

            Point click = e.GetPosition(MemberLayer);
            Node node = snappingService.GetNearbyNode(click, Nodes, currentFloor, snapTolerance) ?? CreateNode(click, currentFloor);

            if (!polygonNodes.Contains(node))
                polygonNodes.Add(node);

            if (previewPolygon != null)
            {
                previewPolygon.Points.Clear();
                foreach (var n in polygonNodes)
                {
                    previewPolygon.Points.Add(n.Location);
                }

                if (polygonNodes.Count >= 2)
                {
                    previewPolygon.Points.Add(polygonNodes[0].Location);
                }
            }

            HandleAddPolygon();
        }

        private void MemberLayer_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(MemberLayer);

            // --- Remove old snap circle if exists ---
            if (snapCircle != null)
                OverlayLayer.Children.Remove(snapCircle);

            // --- Draw snap circle ---
            if (addingBeam || addingBeam || addingPolygon)
            {
                // --- Determine closest node ---
                Node closestNode = Nodes
                    .Where(n => n.Floor == currentFloor)
                    .OrderBy(n => GeometryHelper.Distance(mousePos, n.Location))
                    .FirstOrDefault();

                Point snapPos = mousePos;

                if (closestNode != null && GeometryHelper.Distance(mousePos, closestNode.Location) <= snapTolerance)
                    snapPos = closestNode.Location;

                // Draw the snapping circle
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

                // --- Add Beam preview line to mouse ---
                if (addingBeam && pendingStartPoint.HasValue)
                {
                    if (tempLineToMouse != null)
                        OverlayLayer.Children.Remove(tempLineToMouse);

                    tempLineToMouse = drawingService.DrawTempLine(OverlayLayer, tempLineToMouse, pendingStartPoint.Value, mousePos);

                    if (OverlayLayer.Children.Contains(tempLineToMouse) is false)
                    {
                        OverlayLayer.Children.Add(tempLineToMouse);
                    }
                }

                // --- Polygon preview line to mouse ---
                if (addingPolygon && polygonNodes.Count > 0)
                {
                    Point lastPoint = polygonNodes.Last().Location;

                    if (tempLineToMouse != null)
                        OverlayLayer.Children.Remove(tempLineToMouse);

                    tempLineToMouse = drawingService.DrawTempLine(OverlayLayer, tempLineToMouse, lastPoint, snapPos);

                    if (OverlayLayer.Children.Contains(tempLineToMouse) is false)
                    {
                        OverlayLayer.Children.Add(tempLineToMouse);
                    }
                }
            }
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e) { /* Optional zoom */ }
        #endregion




        //private void UpdatePolygonPreview()
        //{
        //    if (previewPolygon == null) return;

        //    previewPolygon.Points.Clear();
        //    foreach (var n in polygonNodes) previewPolygon.Points.Add(n.Location);
        //    if (polygonNodes.Count >= 2)
        //        previewPolygon.Points.Add(polygonNodes[0].Location);
        //}















        private void HandleAddBeam(Point click)
        {
            var clickedNode = snappingService.GetNearbyNode(click, Nodes, currentFloor, snapTolerance) ?? CreateNode(click, currentFloor);

            if (pendingStartPoint == null)
            {
                pendingStartPoint = clickedNode.Location;
            }
            else
            {
                var startNode = snappingService.GetNearbyNode(pendingStartPoint.Value, Nodes, currentFloor, snapTolerance) ?? CreateNode(pendingStartPoint.Value, currentFloor);
                CreateBeam(startNode, clickedNode);
                
                ResetUI();
                addingBeam = true;
            }
        }

        private void HandleAddColumn(Point click)
        {
            if (currentFloor == 0)
            {
                MessageBox.Show("No floor below to connect a column to.");
                return;
            }

            var clickedNode = snappingService.GetNearbyNode(click, Nodes, currentFloor, snapTolerance) ?? CreateNode(click, currentFloor);

            var topNode = snappingService.GetNearbyNode(click, Nodes, currentFloor, snapTolerance) ?? CreateNode(click, currentFloor);
            var bottomNode = snappingService.GetNearbyNode(new Point(click.X, click.Y), Nodes, currentFloor - 1, snapTolerance) ?? CreateNode(new Point(click.X, click.Y), currentFloor - 1);
            CreateColumn(topNode, bottomNode);

            ResetUI();
            addingColumn = true;
        }

        private void HandleParallelLineCreation(Point click)
        {
            // --- Parallel line handling ---
            if (currentParallelLineMode != ParallelLineMode.None)
            {
                parallelStartPoint = click;

                Point p1 = parallelStartPoint.Value;
                Point p2 = click;
                StructuralMember nearestEdge = null;

                Point? nearestPoint = MemberDetectionService.FindNearestPointOnMember(p1, Members, out nearestEdge);

                if (nearestPoint.HasValue is false || nearestEdge is null) return;

                p1 = nearestPoint.Value;

                // Reset the polygon color and then Find the first polygon that contains the point and highlight it red
                foreach (Polygon p in finalizedPolygons)
                {
                    p.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
                }

                Polygon poly = GeometryHelper.GetPolygonContainingPoint(click, finalizedPolygons);
                if (poly != null)
                {
                    poly.Fill = Brushes.Red;
                }

                List<(Point3D start, Point3D end)> parallelLines = new List<(Point3D start, Point3D end)>();

                switch (currentParallelLineMode)
                {
                    case ParallelLineMode.Horizontal:
                        p2.X = nearestPoint.Value.X + 100;
                        parallelLines = MemberLayoutService.CreateHorizontalRafters(poly, 16);
                        break;
                    case ParallelLineMode.Vertical:
                        p2.Y = nearestPoint.Value.Y + 100;
                        parallelLines = MemberLayoutService.CreateVerticalRafters(poly, 16);
                        break;
                    case ParallelLineMode.EdgePerp:
                        p2 = nearestPoint.Value;
                        Point3D start = new Point3D(nearestEdge.StartNode.Location.X, nearestEdge.StartNode.Location.Y, 0);
                        Point3D end = new Point3D(nearestEdge.EndNode.Location.X, nearestEdge.EndNode.Location.Y, 0);
                        parallelLines = MemberLayoutService.CreatePerpendicularRafters(poly, start, end, 16);
                        break;
                }

                // Draw the rafter lines
                if (parallelLines != null)
                {
                    foreach (var line in parallelLines)
                    {
                        Line ln = new Line
                        {
                            X1 = line.start.X,
                            Y1 = line.start.Y,
                            X2 = line.end.X,
                            Y2 = line.end.Y,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 1,
                            //StrokeDashArray = new DoubleCollection { 4, 2 }
                        };
                        //tempParallelLine = ln;
                        MemberLayer.Children.Add(ln);
                    }
                }

                return; // skip other logic
            }
        }


        private void HandleAddPolygon()
        {
            // if only two points, can't create a polygon yet
            if (polygonNodes.Count < 3)
            {
                return;
            }

            // TODO
            else if (polygonNodes.Count == 3)
            {
                // TODO:  how to determine if we have a triangle or rectangle region?
            }

            // if four points, we can create a polygon for sure.
            else if (polygonNodes.Count == 4)
            {
                // Sort nodes clockwise
                var sortedNodes = GeometryHelper.OrderNodesClockwise(polygonNodes);

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
                CreatePolygon(finalPolygon);

                
                ResetUI();
                addingPolygon = true;
            }
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




        private void CreateTestRoof()
        {
            // For square roof
            //Node n1 = CreateNode(new Point(100, 100), currentFloor);
            //Node n2 = CreateNode(new Point(300, 100), currentFloor);
            //Node n3 = CreateNode(new Point(300, 300), currentFloor);
            //Node n4 = CreateNode(new Point(100, 300), currentFloor);

            // For arbitrary roof
            Node n1 = CreateNode(new Point(50, 50), currentFloor);
            Node n2 = CreateNode(new Point(300, 400), currentFloor);
            Node n3 = CreateNode(new Point(480, 175), currentFloor);
            Node n4 = CreateNode(new Point(220, 25), currentFloor);

            polygonNodes.Add(n1);
            polygonNodes.Add(n4);

            polygonNodes.Add(n2);
            polygonNodes.Add(n3);
            HandleAddPolygon();

            CreateBeam(n1, n2);
            CreateBeam(n2, n3);
            CreateBeam(n3, n4);
            CreateBeam(n4, n1);
        }

        private Node CreateNode(Point p, int floor)
        {
            var node = new Node(p, floor);
            Nodes.Add(node);
            return node;
        }

        private void CreateBeam(Node startNode, Node endNode)
        {
            int beamCount = Members.Count(m => m.Type == MemberType.Beam) + 1;
            var member = new StructuralMember($"B{beamCount}", MemberType.Beam, startNode, endNode);
            Members.Add(member);

            startNode.ConnectedMembers.Add(member);
            endNode.ConnectedMembers.Add(member);
        }

        private void CreateColumn(Node startNode, Node endNode)
        {
            int colCount = Members.Count(m => m.Type == MemberType.Column) + 1;
            var member = new StructuralMember($"C{colCount}", MemberType.Column, startNode, endNode);
            Members.Add(member);

            startNode.ConnectedMembers.Add(member);
            endNode.ConnectedMembers.Add(member);
        }

        private void CreatePolygon(Polygon finalPolygon)
        {
            finalizedPolygons.Add(finalPolygon);
        }
    }
}
