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
        private double levelHeight = 100;
        private int numberOfStories = 3;
        private bool showFloorGrid = true;

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

            BuildingCanvas.Height = Math.Max(1000, numberOfStories * levelHeight + 200);
            BuildingCanvas.Width = 2000; // can be fixed or dynamic

            BuildingCanvas.MouseMove += Canvas_MouseMove;
            BuildingCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            BuildingCanvas.MouseLeftButtonDown += BuildingCanvas_MouseLeftButtonDown;
        }

        #region Button Handlers

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            isPlacingMember = true;
            placingMember = new StructuralMember
            {
                Id = Guid.NewGuid().ToString().Substring(0, 5),
                Type = MemberType.Beam,
                IsCantilever = false,
                UniformLoad = 1.0
            };
            TxtResults.Text = "Click on canvas to set START node of the member.";
        }

        private void BtnRemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (MemberGrid.SelectedItem is StructuralMember selected)
            {
                _building.Members.Remove(selected);
                MemberGrid.Items.Refresh();
                DrawBuilding();
            }
        }

        private void BtnCompute_Click(object sender, RoutedEventArgs e)
        {
            _building.ComputeReactions();
            MemberGrid.Items.Refresh(); // force DataGrid to refresh
            TxtResults.Text = _building.GetReactionSummary();
            DrawBuilding();
        }

        #endregion

        #region Canvas Placement & Drag-Drop

        private void BuildingCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPos = e.GetPosition(BuildingCanvas);

            if (isPlacingMember)
            {
                double snappedY = Math.Round(clickPos.Y / levelHeight) * levelHeight;

                if (placingMember.StartNodePos == default(Point))
                {
                    placingMember.StartNodePos = new Point(clickPos.X, snappedY);
                    TxtResults.Text = "Click on canvas to set END node of the member.";
                }
                else
                {
                    placingMember.EndNodePos = new Point(clickPos.X, snappedY);

                    // Compute levels
                    placingMember.StartLevel = (int)Math.Round((BuildingCanvas.Height - placingMember.StartNodePos.Y) / levelHeight);
                    placingMember.EndLevel = (int)Math.Round((BuildingCanvas.Height - placingMember.EndNodePos.Y) / levelHeight);

                    _building.Members.Add(placingMember);
                    MemberGrid.Items.Refresh();
                    DrawBuilding();

                    isPlacingMember = false;
                    placingMember = null;
                    TxtResults.Text = "Member added. Click 'Add Member' to place another.";
                }
            }
        }

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            selectedNode = sender as Ellipse;
            if (selectedNode != null)
            {
                isDragging = true;
                Point mousePos = e.GetPosition(BuildingCanvas);
                mouseOffset = new Point(mousePos.X - Canvas.GetLeft(selectedNode),
                                        mousePos.Y - Canvas.GetTop(selectedNode));
                selectedNode.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedNode != null)
            {
                Point mousePos = e.GetPosition(BuildingCanvas);
                double snappedY = Math.Round(mousePos.Y / levelHeight) * levelHeight;

                Canvas.SetLeft(selectedNode, mousePos.X - mouseOffset.X);
                Canvas.SetTop(selectedNode, snappedY - 5);

                var (member, isStart) = ((StructuralMember, bool))selectedNode.Tag;
                Point newPos = new Point(mousePos.X, snappedY);

                if (isStart)
                    member.StartNodePos = newPos;
                else
                    member.EndNodePos = newPos;

                DrawBuilding();
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

        #endregion

        #region Drawing

        private void DrawBuilding()
        {
            BuildingCanvas.Children.Clear();

            // Ensure canvas height covers all floors
            BuildingCanvas.Height = Math.Max(1000, numberOfStories * levelHeight + 100);
            BuildingCanvas.Width = 2000;

            // 1. Draw floor grid
            if (showFloorGrid)
            {
                for (int i = 0; i <= numberOfStories; i++)
                {
                    double y = BuildingCanvas.Height - i * levelHeight;

                    Line gridLine = new Line
                    {
                        X1 = 0,
                        Y1 = y,
                        X2 = BuildingCanvas.Width,
                        Y2 = y,
                        Stroke = Brushes.LightGray,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };
                    BuildingCanvas.Children.Add(gridLine);

                    // Floor label
                    TextBlock floorLabel = new TextBlock
                    {
                        Text = $"Floor {i}",
                        Foreground = Brushes.Gray,
                        FontSize = 12
                    };
                    Canvas.SetLeft(floorLabel, 5);
                    Canvas.SetTop(floorLabel, y - 15);
                    BuildingCanvas.Children.Add(floorLabel);
                }
            }

            // 2. Draw members on top of grid
            var members = _building.Members.OrderByDescending(m => m.StartLevel).ToList();

            foreach (var m in members)
            {
                DrawMember(m);
            }
        }

        private void DrawMember(StructuralMember m)
        {
            double xStart = m.StartNodePos.X;
            double yStart = m.StartNodePos.Y;
            double xEnd = m.EndNodePos.X;
            double yEnd = m.EndNodePos.Y;

            // Draw member line
            Line line = new Line
            {
                X1 = xStart,
                Y1 = yStart,
                X2 = xEnd,
                Y2 = yEnd,
                Stroke = m.Type switch
                {
                    MemberType.Beam => Brushes.Blue,
                    MemberType.Girder => Brushes.DarkBlue,
                    MemberType.Wall => Brushes.Brown,
                    MemberType.Purlin => Brushes.Green,
                    _ => Brushes.Black
                },
                StrokeThickness = 4,
                StrokeDashArray = m.IsCantilever ? new DoubleCollection { 4, 2 } : null,
                ToolTip = $"{m.Id} ({m.Type}) RA={m.ReactionA:F2}, RB={m.ReactionB:F2}"
            };
            BuildingCanvas.Children.Add(line);

            // Draw start and end nodes
            DrawNode(m.StartNodePos, m, true);
            DrawNode(m.EndNodePos, m, false);

            // Draw reactions
            DrawReactionArrow(xStart, yStart, m.ReactionA);
            DrawReactionArrow(xEnd, yEnd, m.ReactionB);
        }



        private void DrawNode(Point pos, StructuralMember member, bool isStart)
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
            BuildingCanvas.Children.Add(node);
        }

        private void DrawReactionArrow(double x, double y, double reaction)
        {
            double arrowLength = 20 + reaction * 5;
            Line arrow = new Line
            {
                X1 = x,
                Y1 = y,
                X2 = x,
                Y2 = y + arrowLength,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            BuildingCanvas.Children.Add(arrow);
        }

        private void DrawFloorGrid()
        {
            for (int i = 0; i <= numberOfStories; i++)
            {
                double y = BuildingCanvas.Height - i * levelHeight;

                Line gridLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = BuildingCanvas.Width,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                BuildingCanvas.Children.Add(gridLine);

                // Optional: floor label
                TextBlock floorLabel = new TextBlock
                {
                    Text = $"Floor {i}",
                    Foreground = Brushes.Gray,
                    FontSize = 12
                };
                Canvas.SetLeft(floorLabel, 5);
                Canvas.SetTop(floorLabel, y - 15);
                BuildingCanvas.Children.Add(floorLabel);
            }
        }


        #endregion
    }
}
