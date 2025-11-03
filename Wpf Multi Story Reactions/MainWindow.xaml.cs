using StructuralPlanner.Managers;
using StructuralPlanner.Models;
using StructuralPlanner.Services;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Wpf_Multi_Story_Reactions.Models;

namespace StructuralPlanner
{
    public partial class MainWindow : Window
    {
        // State
        private List<Node> Nodes = new List<Node>();
        private List<StructuralMember> Members = new List<StructuralMember>();
        private List<Region> PolygonRegions = new List<Region>();

        private List<Node> polygonNodes = new List<Node>();
        private List<Node> tempNodes = new List<Node>();

        private FramingLayer currentFloor = FramingLayer.FloorLevel1;
        private bool addingBeam = false;
        private bool addingColumn = false;
        private bool addingRafter = false;
        private bool addingFloorJoist = false;
        private bool addingCeilingJoist = false;
        private bool addingPurlin = false;
        private bool addingWall = false;
        private bool addingRoofBrace = false;

        private bool addingPolygon = false;

        private bool showLabels = false;
        private bool showReactions = false;


        private Point? pendingStartPoint = null;
        private Polygon previewPolygon = null;
        private Ellipse snapCircle = null;
        private Line tempLineToMouse = null;  // For polygon preview line following mouse

        private ParallelLineMode currentParallelLineMode = ParallelLineMode.PerpendicularEdge;
        private Point? parallelStartPoint = null;   // first click
        private Line tempPolygonLine = null;

        // Services
        private readonly DrawingService drawingService = new DrawingService();
        private readonly MemberDetectionService memberDetectionService = new MemberDetectionService();
        private readonly SnappingService snappingService = new SnappingService();
        private readonly CanvasManager canvasManager;

        private const double snapTolerance = 15;
        private double default_rafter_spacing = 16;
        private double default_joist_spacing = 24;

        public MainWindow()
        {
            InitializeComponent();
            canvasManager = new CanvasManager(drawingService);

            drawingService.DrawGridLines(GridLayer);
            RedrawMembers();

            ResetUIAddMemberButtons();
            ResetUILayerButtons();
            ResetUIMainApp();

            // Initial states
            btnFloor1Button.Background = Brushes.PaleGreen;

            addingBeam = true;
            btnAddBeamButton.Background = Brushes.Pink;
        }

        // Restore the UI and application to initial state
        private void ResetUIMainApp()
        {
            MemberLayer.MouseLeftButtonDown -= MemberLayer_MouseLeftButtonDown_Polygon;


            snapCircle = null;  // clear the snapping circle 

            //previewPolygon = null;


            //polygonNodes.Clear();

            Mouse.OverrideCursor = Cursors.Arrow;

            RedrawMembers();
            UpdateDataGrid();
            UpdateNodeConnectionsDataGrid();
        }

        private void ResetUILayerButtons()
        {
            btnFoundationButton.Background = Brushes.LightGray;
            btnFloor1Button.Background = Brushes.LightGray;
            btnFloor2Button.Background = Brushes.LightGray;
            btnFloor3Button.Background = Brushes.LightGray;
            btnRoofButton.Background = Brushes.LightGray;

            spParallelLinesButtons.Visibility = Visibility.Collapsed;
        }

        private void ResetUIAddMemberButtons()
        {
            addingBeam = false;
            addingColumn = false;
            addingRafter = false;
            addingFloorJoist = false;
            addingCeilingJoist = false;

            addingPurlin = false;
            addingWall = false;
            addingRoofBrace = false;
            
            btnAddBeamButton.Background = Brushes.LightGray;
            btnAddColumnButton.Background = Brushes.LightGray;
            btnAddRafterButton.Background = Brushes.LightGray;
            btnAddFloorJoistButton.Background = Brushes.LightGray;
            btnAddCeilingJoistButton.Background = Brushes.LightGray;

            btnAddPolygonButton.Background = Brushes.LightGray;
        }

        // Helper function to call into our canvas manager
        private void RedrawMembers()
        {
            if (canvasManager != null)
            {
                canvasManager.RedrawMembers(MemberLayer, OverlayLayer, Members, Nodes, PolygonRegions, previewPolygon, tempLineToMouse, currentFloor, showLabels);
            }
        }

        // ==================== Member Buttons ====================
        #region UI Button Clicks
        private void btnFoundationButton_Click(object sender, RoutedEventArgs e) 
        { 
            currentFloor = FramingLayer.Foundation;
            ResetUIMainApp();
            ResetUILayerButtons();
            btnFoundationButton.Background = Brushes.PaleGreen;
        }

        private void btnFloor1Button_Click(object sender, RoutedEventArgs e) 
        { 
            currentFloor = FramingLayer.FloorLevel1;
            ResetUIMainApp();
            ResetUILayerButtons();
            btnFloor1Button.Background = Brushes.PaleGreen;
        }
        private void btnFloor2Button_Click(object sender, RoutedEventArgs e) 
        { 
            currentFloor = FramingLayer.FloorLevel2;
            ResetUIMainApp();
            ResetUILayerButtons();
            btnFloor2Button.Background = Brushes.PaleGreen;
        }
        private void btnFloor3Button_Click(object sender, RoutedEventArgs e) 
        { 
            currentFloor = FramingLayer.FloorLevel3;
            ResetUIMainApp();
            ResetUILayerButtons();
            btnFloor3Button.Background = Brushes.PaleGreen;
        }
        private void btnRoofButton_Click(object sender, RoutedEventArgs e) 
        { 
            currentFloor = FramingLayer.Roof;
            ResetUIMainApp();
            ResetUILayerButtons();
            btnRoofButton.Background = Brushes.PaleGreen;
        }


        private void btnAddBeamButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUIMainApp();
            ResetUIAddMemberButtons();
            btnAddBeamButton.Background = Brushes.Pink;
            addingBeam = true; 
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnAddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUIMainApp();
            ResetUIAddMemberButtons();
            btnAddColumnButton.Background = Brushes.Pink;
            addingColumn = true; 
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnAddPolygonButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUIMainApp();
            ResetUIAddMemberButtons();
            btnAddPolygonButton.Background = Brushes.Pink;
            addingPolygon = true;

            if (previewPolygon != null) OverlayLayer.Children.Remove(previewPolygon);

            previewPolygon = new Polygon { Stroke = Brushes.Green, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 }, Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)) };
            OverlayLayer.Children.Add(previewPolygon);

            MemberLayer.MouseLeftButtonDown += MemberLayer_MouseLeftButtonDown_Polygon;
        }

        private void btnAddRafterButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUIMainApp();
            ResetUIAddMemberButtons();
            spParallelLinesButtons.Visibility = Visibility.Visible;
            btnAddRafterButton.Background = Brushes.Pink;
            addingRafter = true;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnAddFloorJoistButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUIMainApp();
            ResetUIAddMemberButtons();
            spParallelLinesButtons.Visibility = Visibility.Visible;
            btnAddFloorJoistButton.Background = Brushes.Pink;
            addingFloorJoist = true;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnAddCeilingJoistButton_Click(object sender, RoutedEventArgs e)
        {
            ResetUIMainApp();
            ResetUIAddMemberButtons();
            spParallelLinesButtons.Visibility = Visibility.Visible;
            btnAddCeilingJoistButton.Background = Brushes.Pink;
            addingCeilingJoist = true;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void btnClearButton_Click(object sender, RoutedEventArgs e)
        {
            Members.Clear();
            Nodes.Clear();
            PolygonRegions.Clear();

            ResetUIMainApp();
        }

        private void btnComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder.");
        }

        // ==================== Polygon Alignment Buttons ====================
        private void btnEdgePerp_Click(object sender, RoutedEventArgs e)
        {
            currentParallelLineMode = ParallelLineMode.PerpendicularEdge;
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
        #region Window Events
        // ==================== Canvas Events ====================
        private void MemberLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(MemberLayer);

            if (addingBeam)
            {
                HandleAddBeam(click, MemberType.Beam);
                addingBeam = true;
            }
            else if (addingColumn)
            {
                HandleAddColumn(click, MemberType.Column);
                addingColumn = true;
            }
            else if (addingPolygon)
            {
                HandleAddPolygon();
                addingPolygon = true;
            }
            else if (addingRafter)
            {
                HandleParallelLineCreation(click, MemberType.Rafter);
                addingRafter = true;
            }
            else if (addingCeilingJoist)
            {
                HandleParallelLineCreation(click, MemberType.CeilingJoist);
                addingCeilingJoist = true;
            }
            else if (addingFloorJoist)
            {
                HandleParallelLineCreation(click, MemberType.FloorJoist);
                addingFloorJoist = true;
            }
        }

        private void MemberLayer_MouseLeftButtonDown_Polygon(object sender, MouseButtonEventArgs e)
        {
            if (!addingPolygon) return;

            Point click = e.GetPosition(MemberLayer);
            Node node = snappingService.GetNearbyNode(click, Nodes, (int)currentFloor, snapTolerance) ?? CreateNode(click, (int)currentFloor);

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

            RedrawMembers();
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
                //Node closestNode = snappingService.GetNearbyNode(mousePos, Nodes, (int)currentFloor, snapTolerance) ?? CreateNode(mousePos, (int)currentFloor);
                Node closestNode = null;


                Point snapPos = mousePos;

                if (closestNode != null && GeometryHelper.Distance(mousePos, closestNode.Location) <= snapTolerance)
                    snapPos = closestNode.Location;


                snapCircle = drawingService.DrawSnapCircle(OverlayLayer, snapPos, snapTolerance, Brushes.Orange, Brushes.Orange, 1);


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

                    bool flowControl = drawingService.DrawPolygon(OverlayLayer, previewPolygon, polygonNodes, (int)currentFloor);
                }
            }
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e) { /* Optional zoom */ }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetUIMainApp();
                e.Handled = true; // optional — prevents event bubbling
            }
        }

        #endregion

        #region UI Update functions
        private void UpdateDataGrid()
        {
            var tableData = Members.Select(m => new
            {
                ID = m.MemberID,
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
        #endregion







        private void HandleAddBeam(Point click, MemberType type)
        {
            var clickedNode = snappingService.GetNearbyNode(click, Nodes, (int)currentFloor, snapTolerance) ?? CreateNode(click, (int)currentFloor);

            if (pendingStartPoint == null)
            {
                pendingStartPoint = clickedNode.Location;
            }
            else
            {
                var startNode = snappingService.GetNearbyNode(pendingStartPoint.Value, Nodes, (int)currentFloor, snapTolerance) ?? CreateNode(pendingStartPoint.Value, (int)currentFloor);
                CreateMember(startNode, clickedNode, type);

                pendingStartPoint = null; // clear the first point
                tempLineToMouse = null;  // clear the preview line
            }

            RedrawMembers();

        }

        private void HandleAddColumn(Point click, MemberType type)
        {
            if (currentFloor == 0)
            {
                MessageBox.Show("No floor below to connect a column to.");
                return;
            }

            var topNode = snappingService.GetNearbyNode(click, Nodes, (int)currentFloor, snapTolerance) ?? CreateNode(click, (int)currentFloor);
            var bottomNode = snappingService.GetNearbyNode(new Point(click.X, click.Y), Nodes, (int)currentFloor - 1, snapTolerance) ?? CreateNode(new Point(click.X, click.Y), (int)currentFloor - 1);
            CreateColumn(topNode, bottomNode);

            pendingStartPoint = null;  // clear the first point if there is one

            RedrawMembers();
        }

        private void HandleParallelLineCreation(Point click, MemberType type)
        {
            // --- Parallel line handling ---
            parallelStartPoint = click;

            Point p1 = parallelStartPoint.Value;
            Point p2 = click;
            StructuralMember nearestEdge = null;

            Point? nearestPoint = MemberDetectionService.FindNearestPointOnMember(p1, Members, out nearestEdge);

            if (nearestPoint.HasValue is false || nearestEdge is null) return;

            p1 = nearestPoint.Value;

            Region region = GeometryHelper.GetPolygonContainingPoint(click, PolygonRegions);

            //// Reset the polygon color and then Find the first polygon that contains the point and highlight it red
            //foreach (Polygon p in PolygonRegions)
            //{
            //    p.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
            //}


            //if (poly != null)
            //{
            //    poly.Fill = Brushes.Red;
            //    poly.Opacity = 0.5;
            //}

            List<(Point3D start, Point3D end)> parallelLines = new List<(Point3D start, Point3D end)>();

            double spacing = (type == MemberType.Rafter ? default_rafter_spacing : default_joist_spacing);
            switch (currentParallelLineMode)
            {
                case ParallelLineMode.Horizontal:
                    parallelLines = MemberLayoutService.CreateHorizontalRafters(region.Poly, spacing);
                    break;
                case ParallelLineMode.Vertical:
                    parallelLines = MemberLayoutService.CreateVerticalRafters(region.Poly, spacing);
                    break;
                case ParallelLineMode.PerpendicularEdge:
                    Point3D start = new Point3D(nearestEdge.StartNode.Location.X, nearestEdge.StartNode.Location.Y, 0);
                    Point3D end = new Point3D(nearestEdge.EndNode.Location.X, nearestEdge.EndNode.Location.Y, 0);
                    parallelLines = MemberLayoutService.CreatePerpendicularRafters(region.Poly, start, end, spacing);
                    break;
            }

            if (parallelLines != null)
            {
                foreach (var line in parallelLines)
                {
                    Node start = CreateNode(line.start.ToPoint(), (int)currentFloor);
                    Node end = CreateNode(line.end.ToPoint(), (int)currentFloor);
                    CreateMember(start, end, type);
                }
            }
            RedrawMembers();

        }


        private void HandleAddPolygon()
        { 
            // if only two points, can't create a polygon yet
            if (polygonNodes.Count < 3)
            {
                return;
            }

            // if four points, we can create a polygon for sure.
            else if (polygonNodes.Count == 4)
            {
                // Add to overlay and store reference so it persists on redraw
                CreatePolygon(polygonNodes);

                polygonNodes.Clear();  // clear our list of polygon points
                tempLineToMouse = null; // clear the temp line

                RedrawMembers();
            }
        }






        private void CreateTestRoof()
        {
            // For square roof
            //Node n1 = CreateNode(new Point(100, 100), currentFloor);
            //Node n2 = CreateNode(new Point(300, 100), currentFloor);
            //Node n3 = CreateNode(new Point(300, 300), currentFloor);
            //Node n4 = CreateNode(new Point(100, 300), currentFloor);

            // For arbitrary roof
            Node n1 = CreateNode(new Point(50, 50), (int)currentFloor);
            Node n2 = CreateNode(new Point(300, 400), (int)currentFloor);
            Node n3 = CreateNode(new Point(480, 175), (int)currentFloor);
            Node n4 = CreateNode(new Point(220, 25), (int)currentFloor);

            polygonNodes.Add(n1);
            polygonNodes.Add(n4);

            polygonNodes.Add(n2);
            polygonNodes.Add(n3);
            HandleAddPolygon();

            CreateMember(n1, n2, MemberType.Beam);
            CreateMember(n2, n3, MemberType.Beam);
            CreateMember(n3, n4, MemberType.Beam);
            CreateMember(n4, n1, MemberType.Beam);
        }

        private Node CreateNode(Point p, int floor)
        {
            var node = new Node(p, floor);
            return node;
        }

        private void CreateMember(Node startNode, Node endNode, MemberType type)
        {
            StructuralMember member;
            if(type == MemberType.Rafter || type == MemberType.FloorJoist || type == MemberType.CeilingJoist)
            {
                member = new RafterJoistModel(type, startNode, endNode);
            } else
            {
                member = new StructuralMember(type, startNode, endNode);
            }
            Members.Add(member);

            if (!Nodes.Contains(startNode))
            {
                Nodes.Add(startNode);
            }
            if (!Nodes.Contains(endNode))
            {
                Nodes.Add(endNode);
            }


            startNode.ConnectedMembers.Add(member);
            endNode.ConnectedMembers.Add(member);
        }

        private void CreateColumn(Node startNode, Node endNode)
        {
            var member = new StructuralMember(MemberType.Column, startNode, endNode);
            Members.Add(member);
            if (!Nodes.Contains(startNode))
            {
                Nodes.Add(startNode);
            }
            if (!Nodes.Contains(endNode))
            {
                Nodes.Add(endNode);
            }

            startNode.ConnectedMembers.Add(member);
            endNode.ConnectedMembers.Add(member);
        }

        private Region CreatePolygon(List<Node> nodes)
        {
            // Sort nodes clockwise
            var sortedNodes = GeometryHelper.OrderNodesClockwise(nodes);

            // Create the final polygon
            Polygon finalPolygon = new Polygon
            {
                Stroke = Brushes.Green,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0))
            };

            foreach (var n in sortedNodes)
                finalPolygon.Points.Add(n.Location);

            // Add the polygon region to the list
            Region region = new Region(finalPolygon, (int)currentFloor);
            PolygonRegions.Add(region);

            // Add the nodes for our polygon
            foreach (var n in nodes)
            {
                if (!Nodes.Contains(n))
                {
                    Nodes.Add(n);
                }
            }

            return region;
        }

        private void ShowLabelsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showLabels = true;
            RedrawMembers(); // optional – refresh display to show labels
        }

        private void ShowLabelsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showLabels = false;
            RedrawMembers(); // optional – refresh display to hide labels
        }

        private void ShowReactionssCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showReactions = true;
            RedrawMembers(); // optional – refresh display to show labels
        }

        private void ShowReactionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showReactions = false;
            RedrawMembers(); // optional – refresh display to hide labels
        }

    }
}
