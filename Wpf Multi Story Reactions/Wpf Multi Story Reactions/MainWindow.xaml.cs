using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MultiStoryReactions
{
    public partial class MainWindow : Window
    {
        private BuildingModel _building = new BuildingModel();
        private int gridSize = 50; // Plan grid snapping

        // Drag-and-drop
        private bool isDragging = false;
        private Ellipse selectedNode = null;
        private Point mouseOffset;

        // Interactive placement
        private bool isPlacingMember = false;
        private StructuralMember placingMember;

        public MainWindow()
        {
            InitializeComponent();
            MemberGrid.ItemsSource = _building.Members;

            // Attach mouse events for all canvases
            Floor1Canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            Floor1Canvas.MouseMove += Canvas_MouseMove;
            Floor1Canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

            Floor2Canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            Floor2Canvas.MouseMove += Canvas_MouseMove;
            Floor2Canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

            RoofCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            RoofCanvas.MouseMove += Canvas_MouseMove;
            RoofCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
        }

        #region Button Handlers

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            isPlacingMember = true;
            placingMember = new StructuralMember
            {
                Type = MemberType.Beam,
                IsCantilever = false,
                UniformLoad = 1.0,
                Floor = FloorTabs.SelectedIndex // Floor determined by active tab
            };
            TxtResults.Text = "Click on canvas to set START node of the member.";
        }

        private void BtnRemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (MemberGrid.SelectedItem is StructuralMember selected)
            {
                _building.Members.Remove(selected);
                MemberGrid.Items.Refresh();
                DrawAllFloors();
            }
        }

        private void BtnCompute_Click(object sender, RoutedEventArgs e)
        {
            _building.ComputeReactions();
            MemberGrid.Items.Refresh();
            TxtResults.Text = _building.GetReactionSummary();
            DrawAllFloors();
        }

        #endregion

        #region Canvas Placement & Drag-Drop

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = sender as Canvas;
            Point clickPos = e.GetPosition(canvas);

            if (isPlacingMember)
            {
                // Snap to grid
                double snappedX = Math.Round(clickPos.X / gridSize) * gridSize;
                double snappedY = Math.Round(clickPos.Y / gridSize) * gridSize;
                Point snappedPoint = new Point(snappedX, snappedY);

                if (placingMember.StartNodePos == default(Point))
                {
                    placingMember.StartNodePos = snappedPoint;
                    TxtResults.Text = "Click on canvas to set END node of the member.";
                }
                else
                {
                    placingMember.EndNodePos = snappedPoint;

                    _building.Members.Add(placingMember);
                    MemberGrid.Items.Refresh();
                    DrawAllFloors();

                    isPlacingMember = false;
                    placingMember = null;
                    TxtResults.Text = "Member added. Select 'Add Member' to place another.";
                }
            }
        }

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedNode = sender as Ellipse;
            if (selectedNode != null)
            {
                isDragging = true;
                Canvas canvas = GetCanvasForNode(selectedNode);
                Point mousePos = e.GetPosition(canvas);
                mouseOffset = new Point(mousePos.X - Canvas.GetLeft(selectedNode),
                                        mousePos.Y - Canvas.GetTop(selectedNode));
                selectedNode.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedNode != null)
            {
                Canvas canvas = GetCanvasForNode(selectedNode);
                Point mousePos = e.GetPosition(canvas);

                double snappedX = Math.Round((mousePos.X - mouseOffset.X + 5) / gridSize) * gridSize;
                double snappedY = Math.Round((mousePos.Y - mouseOffset.Y + 5) / gridSize) * gridSize;

                Canvas.SetLeft(selectedNode, snappedX - 5);
                Canvas.SetTop(selectedNode, snappedY - 5);

                var (member, isStart) = ((StructuralMember, bool))selectedNode.Tag;
                Point newPos = new Point(snappedX, snappedY);

                if (isStart)
                    member.StartNodePos = newPos;
                else
                    member.EndNodePos = newPos;

                DrawAllFloors();
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && selectedNode != null)
            {
                selectedNode.ReleaseMouseCapture();
                isDragging = false;
                selectedNode = null;
            }
        }

        private Canvas GetCanvasForNode(Ellipse node)
        {
            var (member, _) = ((StructuralMember, bool))node.Tag;
            return member.Floor switch
            {
                0 => Floor1Canvas,
                1 => Floor2Canvas,
                2 => RoofCanvas,
                _ => Floor1Canvas
            };
        }

        #endregion

        #region Drawing

        private void DrawAllFloors()
        {
            DrawFloor(0, Floor1Canvas);
            DrawFloor(1, Floor2Canvas);
            DrawFloor(2, RoofCanvas);
        }

        private void DrawFloor(int floor, Canvas canvas)
        {
            canvas.Children.Clear();

            // Draw grid
            for (int i = 0; i <= canvas.Width; i += gridSize)
                canvas.Children.Add(new Line { X1 = i, Y1 = 0, X2 = i, Y2 = canvas.Height, Stroke = Brushes.LightGray, StrokeThickness = 1 });
            for (int j = 0; j <= canvas.Height; j += gridSize)
                canvas.Children.Add(new Line { X1 = 0, Y1 = j, X2 = canvas.Width, Y2 = j, Stroke = Brushes.LightGray, StrokeThickness = 1 });

            // Draw members for this floor
            foreach (var m in _building.GetMembersForFloor(floor))
            {
                DrawMemberOnCanvas(canvas, m);
            }
        }

        private void DrawMemberOnCanvas(Canvas canvas, StructuralMember m)
        {
            Brush color = m.Type switch
            {
                MemberType.Beam => Brushes.Blue,
                MemberType.Girder => Brushes.DarkBlue,
                MemberType.Wall => Brushes.Brown,
                MemberType.Purlin => Brushes.Green,
                MemberType.Column => Brushes.Orange,
                _ => Brushes.Black
            };

            Line line = new Line
            {
                X1 = m.StartNodePos.X,
                Y1 = m.StartNodePos.Y,
                X2 = m.EndNodePos.X,
                Y2 = m.EndNodePos.Y,
                Stroke = color,
                StrokeThickness = 4,
                StrokeDashArray = m.IsCantilever ? new DoubleCollection { 4, 2 } : null,
                ToolTip = $"{m.Id} ({m.Type})"
            };
            canvas.Children.Add(line);

            // Draw nodes
            DrawNode(canvas, m.StartNodePos, m, true);
            DrawNode(canvas, m.EndNodePos, m, false);
        }

        private void DrawNode(Canvas canvas, Point pos, StructuralMember member, bool isStart)
        {
            Ellipse node = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Black,
                Tag = (member, isStart)
            };
            Canvas.SetLeft(node, pos.X - 5);
            Canvas.SetTop(node, pos.Y - 5);
            node.MouseLeftButtonDown += Node_MouseLeftButtonDown;
            canvas.Children.Add(node);
        }

        #endregion
    }
}
