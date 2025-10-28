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
        // --- Data Models ---
        public enum MemberType { Beam, Column }

        public class StructuralMember
        {
            public MemberType Type { get; set; }
            public Point Start { get; set; }
            public Point End { get; set; }
            public int Floor { get; set; }    // 0 = Floor1, 1 = Floor2, 2 = Roof
        }

        private List<StructuralMember> Members = new List<StructuralMember>();
        private int currentFloor = 0; // 0=Floor1, 1=Floor2, 2=Roof

        // --- Drawing state ---
        private Point? pendingStartPoint = null;
        private bool addingBeam = false;
        private bool addingColumn = false;

        // --- Constants ---
        private const double floorHeight = 200; // Visual vertical spacing between floors

        public MainWindow()
        {
            InitializeComponent();
            RedrawAll();
        }

        // ==============================================================
        // FLOOR SWITCHING
        // ==============================================================

        private void Floor1Button_Click(object sender, RoutedEventArgs e)
        {
            currentFloor = 0;
            RedrawAll();
        }

        private void Floor2Button_Click(object sender, RoutedEventArgs e)
        {
            currentFloor = 1;
            RedrawAll();
        }

        private void RoofButton_Click(object sender, RoutedEventArgs e)
        {
            currentFloor = 2;
            RedrawAll();
        }

        // ==============================================================
        // ADDING MEMBERS
        // ==============================================================

        private void AddBeamButton_Click(object sender, RoutedEventArgs e)
        {
            addingBeam = true;
            addingColumn = false;
            pendingStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            addingBeam = false;
            addingColumn = true;
            pendingStartPoint = null;
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Members.Clear();
            pendingStartPoint = null;
            RedrawAll();
        }

        private void ComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder. (Future implementation)");
        }

        private void ShowColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            // Simply toggle a redraw (in future can toggle visibility state)
            RedrawAll();
        }

        // ==============================================================
        // CANVAS INTERACTION
        // ==============================================================

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(MainCanvas);

            if (addingBeam)
            {
                HandleAddBeam(click);
            }
            else if (addingColumn)
            {
                HandleAddColumn(click);
            }
        }

        private void HandleAddBeam(Point click)
        {
            if (pendingStartPoint == null)
            {
                pendingStartPoint = click;
            }
            else
            {
                var newMember = new StructuralMember
                {
                    Type = MemberType.Beam,
                    Start = pendingStartPoint.Value,
                    End = click,
                    Floor = currentFloor
                };
                Members.Add(newMember);
                pendingStartPoint = null;
                addingBeam = false;
                Mouse.OverrideCursor = null;
                RedrawAll();
            }
        }

        private void HandleAddColumn(Point click)
        {
            if (currentFloor < 0 || currentFloor >= 3)
            {
                MessageBox.Show("Invalid floor selection.");
                return;
            }

            // Determine the lower floor — columns always extend DOWN one level
            int lowerFloor = currentFloor - 1;

            if (lowerFloor < 0)
            {
                MessageBox.Show("No floor below to connect a column to.");
                addingColumn = false;
                Mouse.OverrideCursor = null;
                return;
            }

            double verticalShift = floorHeight; // Vertical distance between levels
            var top = click;
            var bottom = new Point(click.X, click.Y + verticalShift);

            var col = new StructuralMember
            {
                Type = MemberType.Column,
                Start = top,
                End = bottom,
                Floor = lowerFloor  // ✅ belongs to the floor owning its *top*
            };

            Members.Add(col);
            addingColumn = false;
            Mouse.OverrideCursor = null;
            RedrawAll();
        }


        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (pendingStartPoint != null)
            {
                RedrawAll();
                Point current = e.GetPosition(MainCanvas);
                DrawTempLine(pendingStartPoint.Value, current);
            }
        }

        private void DrawTempLine(Point start, Point end)
        {
            Line l = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            MainCanvas.Children.Add(l);
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // (optional zoom feature placeholder)
        }

        // ==============================================================
        // DRAWING ROUTINES
        // ==============================================================

        private void RedrawAll()
        {
            MainCanvas.Children.Clear();

            DrawGridLines();
            DrawMembersForFloor(currentFloor);
            DrawLowerFloorGhosts(currentFloor);
        }

        private void DrawGridLines()
        {
            double spacing = 20;
            double width = MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : MainCanvas.Width;
            double height = MainCanvas.ActualHeight > 0 ? MainCanvas.ActualHeight : MainCanvas.Height;

            for (double x = 0; x < width; x += spacing)
            {
                Line grid = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    StrokeThickness = 1
                };
                MainCanvas.Children.Add(grid);
            }
            for (double y = 0; y < height; y += spacing)
            {
                Line grid = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    StrokeThickness = 1
                };
                MainCanvas.Children.Add(grid);
            }
        }

        private void DrawMembersForFloor(int floor)
        {
            foreach (var m in Members.Where(m => m.Floor == floor))
            {
                DrawMember(m, 1.0);
            }
        }

        private void DrawLowerFloorGhosts(int floor)
        {
            // Show members from the floor below with low opacity (if applicable)
            if (floor == 0) return;
            foreach (var m in Members.Where(m => m.Floor == floor - 1))
            {
                DrawMember(m, 0.3);
            }
        }

        private void DrawMember(StructuralMember m, double opacity)
        {
            Brush stroke;
            double thickness = 3;

            switch (m.Type)
            {
                case MemberType.Beam:
                    stroke = Brushes.SteelBlue;
                    DrawLine(MainCanvas, m, opacity, stroke, thickness);
                    break;
                case MemberType.Column:
                    stroke = Brushes.Gray;
                    thickness = 4;
                    DrawSquareCentered(MainCanvas, m, 10.0, stroke, thickness);
                    break;
                default:
                    stroke = Brushes.Black;
                    DrawLine(MainCanvas, m, opacity, stroke, thickness);
                    break;
            }


        }

        private void DrawLine(Canvas cnv, StructuralMember m, double opacity, Brush stroke, double thickness)
        {
            Line line = new Line
            {
                X1 = m.Start.X,
                Y1 = m.Start.Y,
                X2 = m.End.X,
                Y2 = m.End.Y,
                Stroke = stroke,
                StrokeThickness = thickness,
                Opacity = opacity
            };
            cnv.Children.Add(line);
        }

        private void DrawSquareCentered(Canvas cnv, StructuralMember m, double h, Brush stroke, double thickness, Brush fill = null)
        {
            Rectangle square = new Rectangle
            {
                Width = h,
                Height = h,
                Stroke = stroke,
                StrokeThickness = thickness,
                Fill = fill ?? Brushes.Transparent
            };

            // Position so the square is centered on (m.Start.X, m.Start.Y)
            Canvas.SetLeft(square, m.Start.X - h / 2);
            Canvas.SetTop(square, m.Start.Y - h / 2);

            cnv.Children.Add(square);
        }

    }
}
