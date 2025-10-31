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
        private Line tempLine = null;
        private Polygon previewPolygon = null;
        private Ellipse snapCircle = null;
        private Line tempLineToMouse = null;  // For polygon preview line following mouse

        private enum ParallelLineMode { None, EdgePerp, Vertical, Horizontal }
        private ParallelLineMode currentParallelLineMode = ParallelLineMode.None;
        private Point? parallelStartPoint = null;   // first click
        private Line tempParallelLine = null;       // live preview

        // Services
        private readonly DrawingService drawingService = new DrawingService();
        private readonly MemberDetectionService memberDetectionService = new MemberDetectionService();
        private readonly SnappingService snappingService = new SnappingService();
        private readonly CanvasManager canvasManager;

        private const double floorHeight = 200;
        private const double snapTolerance = 15;

        private int beamCount = 0;
        private int columnCount = 0;
        private int nodeCount = 0;

        private Line tempPolygonLine = null;


        // Polygon parallel line preview
        private Polygon currentPolygonForLines = null;
        private List<Line> parallelLinePreview = new List<Line>();
        private LineMode currentLineMode = LineMode.PerpendicularEdge;


        public MainWindow()
        {
            InitializeComponent();
            canvasManager = new CanvasManager(drawingService);

            drawingService.DrawGridLines(GridLayer);
            RedrawMembers();
        }

        // Helper function to call into our canvas manager
        private void RedrawMembers() { canvasManager.RedrawMembers(MemberLayer, OverlayLayer, Members, Nodes, finalizedPolygons, previewPolygon, tempLineToMouse, currentFloor); }

        // ==================== Member Buttons ====================
        #region UI Button Clicks
        private void Floor1Button_Click(object sender, RoutedEventArgs e) { currentFloor = 0; RedrawMembers(); }
        private void Floor2Button_Click(object sender, RoutedEventArgs e) { currentFloor = 1; RedrawMembers(); }
        private void RoofButton_Click(object sender, RoutedEventArgs e) { currentFloor = 2; RedrawMembers(); }


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

        #endregion

        // ==================== Canvas Events ====================
        #region Canvas Events
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

                    // Trim or extend the rafters to the polygon edge


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

                    // Trim the rafter lines to the polygon


                    //if (currentParallelLineMode == ParallelLineMode.EdgePerp)
                    //{
                    //    drawingService.DrawPerpendicularFromClick(MemberLayer, p2, nearestEdge);
                    //}
                    //else
                    //{

                    //}


                    //parallelStartPoint = null;
                    ////currentParallelLineMode = ParallelLineMode.None;

                    //if (tempParallelLine != null)
                    //{
                    //    OverlayLayer.Children.Remove(tempParallelLine);
                    //    tempParallelLine = null;
                    //}

                    Mouse.OverrideCursor = null;

                    return; // skip other logic
                }

            }
        }

        private void MemberLayer_MouseLeftButtonDown_Polygon(object sender, MouseButtonEventArgs e)
        {
            if (!addingPolygon) return;

            Point click = e.GetPosition(MemberLayer);
            Node node = snappingService.GetNearbyNode(click, Nodes, currentFloor, snapTolerance) ?? CreateNode(click, currentFloor);

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
                foreach (var n in GeometryHelper.OrderNodesClockwise(polygonNodes))
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
                .OrderBy(n => GeometryHelper.Distance(mousePos, n.Location))
                .FirstOrDefault();

            Point snapPos = mousePos;

            if (closestNode != null && GeometryHelper.Distance(mousePos, closestNode.Location) <= snapTolerance)
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

                tempLineToMouse = drawingService.DrawTempLine(OverlayLayer, tempLineToMouse, lastPoint, snapPos);

                if (OverlayLayer.Children.Contains(tempLineToMouse) is false)
                {
                    OverlayLayer.Children.Add(tempLineToMouse);
                }
            }

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

            //// --- Parallel line preview ---
            //if (currentParallelLineMode != ParallelLineMode.None && parallelStartPoint != null)
            //{
            //    drawingService.DrawParallelLinesPreview(OverlayLayer, parallelLinePreview, currentPolygonForLines, currentLineMode);
            //}



        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e) { /* Optional zoom */ }

        private void CreateRoofButton_Click(object sender, RoutedEventArgs e)
        {
            CreateTestRoof();
        }
        #endregion



        private void StartPolygonSelection()
        {
            polygonNodes.Clear();
            addingPolygon = true;
            if (previewPolygon != null) OverlayLayer.Children.Remove(previewPolygon);

            previewPolygon = new Polygon { Stroke = Brushes.Green, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 }, Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)) };
            OverlayLayer.Children.Add(previewPolygon);

           // MessageBox.Show("Click 3 or 4 distinct nodes to form the polygon. Nodes will snap automatically.");
        }

        private void FinishPolygon()
        {
            if (polygonNodes.Count >= 3)
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

                //MessageBox.Show($"Polygon created with {polygonNodes.Count} nodes.");
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
                pendingStartPoint = null;
                addingBeam = false;
                Mouse.OverrideCursor = null;
                canvasManager.RedrawMembers(MemberLayer, OverlayLayer, Members, Nodes, finalizedPolygons, previewPolygon, tempLineToMouse, currentFloor);
            }
            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
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

            var topNode = snappingService.GetNearbyNode(click, Nodes, currentFloor, snapTolerance) ?? CreateNode(click, currentFloor);
            var bottomNode = snappingService.GetNearbyNode(new Point(click.X, click.Y), Nodes, currentFloor - 1, snapTolerance) ?? CreateNode(new Point(click.X, click.Y), currentFloor - 1);

            int columnCount = Members.Count(m => m.Type == MemberType.Column) + 1;
            var member = new StructuralMember($"C{columnCount}", MemberType.Column, topNode, bottomNode);
            Members.Add(member);

            topNode.ConnectedMembers.Add(member);
            bottomNode.ConnectedMembers.Add(member);

            addingColumn = false;
            Mouse.OverrideCursor = null;
            canvasManager.RedrawMembers(MemberLayer, OverlayLayer, Members, Nodes, finalizedPolygons, previewPolygon, tempLineToMouse, currentFloor);
            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
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
            FinishPolygon();

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

            canvasManager.RedrawMembers(MemberLayer, OverlayLayer, Members, Nodes, finalizedPolygons, previewPolygon, tempLineToMouse, currentFloor);
        }
    }
}
