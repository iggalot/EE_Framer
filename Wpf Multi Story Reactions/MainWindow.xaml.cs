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
            public int Floor { get; set; } // 0 = Floor1, 1 = Floor2, 2 = Roof
        }

        private readonly List<StructuralMember> Members = new();
        private int currentFloor = 0;

        // --- Drawing state ---
        private Point? pendingStartPoint = null;
        private bool addingBeam = false;
        private bool addingColumn = false;
        private Line tempLine = null;

        // --- Constants ---
        private const double floorHeight = 200;

        // ==============================================================
        // INITIALIZATION
        // ==============================================================
        public MainWindow()
        {
            InitializeComponent();
            DrawGridLines();   // static background grid
            RedrawMembers();   // draw members for default floor
        }

        // ==============================================================
        // FLOOR SWITCHING
        // ==============================================================
        private void Floor1Button_Click(object sender, RoutedEventArgs e)
        {
            currentFloor = 0;
            RedrawMembers();
        }

        private void Floor2Button_Click(object sender, RoutedEventArgs e)
        {
            currentFloor = 1;
            RedrawMembers();
        }

        private void RoofButton_Click(object sender, RoutedEventArgs e)
        {
            currentFloor = 2;
            RedrawMembers();
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
            OverlayLayer.Children.Clear();
            RedrawMembers();
        }

        private void ComputeReactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reaction computation placeholder. (Future implementation)");
        }

        private void ShowColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            RedrawMembers(); // placeholder toggle for visibility
        }

        // ==============================================================
        // CANVAS INTERACTION (new layered version)
        // ==============================================================
        private void MemberLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(MemberLayer);

            if (addingBeam)
                HandleAddBeam(click);
            else if (addingColumn)
                HandleAddColumn(click);
        }

        private void MemberLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (pendingStartPoint != null)
            {
                Point current = e.GetPosition(MemberLayer);
                DrawTempLine(pendingStartPoint.Value, current);
            }
        }

        private void HandleAddBeam(Point click)
        {
            if (pendingStartPoint == null)
            {
                pendingStartPoint = click;
                MemberLayer.CaptureMouse(); // keeps focus even after redraw
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
                MemberLayer.ReleaseMouseCapture();

                OverlayLayer.Children.Clear();
                RedrawMembers();
            }
        }

        private void HandleAddColumn(Point click)
        {
            if (currentFloor < 0 || currentFloor >= 3)
            {
                MessageBox.Show("Invalid floor selection.");
                return;
            }

            int lowerFloor = currentFloor - 1;
            if (lowerFloor < 0)
            {
                MessageBox.Show("No floor below to connect a column to.");
                addingColumn = false;
                Mouse.OverrideCursor = null;
                return;
            }

            double verticalShift = floorHeight;
            var top = click;
            var bottom = new Point(click.X, click.Y + verticalShift);

            var col = new StructuralMember
            {
                Type = MemberType.Column,
                Start = top,
                End = bottom,
                Floor = lowerFloor
            };

            Members.Add(col);
            addingColumn = false;
            Mouse.OverrideCursor = null;
            RedrawMembers();
        }

        private void MainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // optional zoom placeholder
        }

        // ==============================================================
        // DRAWING ROUTINES
        // ==============================================================
        private void DrawGridLines()
        {
            GridLayer.Children.Clear();
            double spacing = 20;
            double width = 1200;  // adjust as needed or bind to ActualWidth
            double height = 800;

            for (double x = 0; x < width; x += spacing)
            {
                GridLayer.Children.Add(new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
                    StrokeThickness = 1
                });
            }
            for (double y = 0; y < height; y += spacing)
            {
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
        }

        private void RedrawMembers()
        {
            MemberLayer.Children.Clear();

            // Draw members for current floor
            foreach (var m in Members.Where(m => m.Floor == currentFloor))
                DrawMember(MemberLayer, m, 1.0);

            // Draw faint “ghosts” of lower floor
            if (currentFloor > 0)
            {
                foreach (var m in Members.Where(m => m.Floor == currentFloor - 1))
                    DrawMember(MemberLayer, m, 0.3);
            }
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
            Brush stroke;
            double thickness = 3;

            switch (m.Type)
            {
                case MemberType.Beam:
                    stroke = Brushes.SteelBlue;
                    DrawLine(cnv, m, opacity, stroke, thickness);
                    break;
                case MemberType.Column:
                    stroke = Brushes.Gray;
                    thickness = 4;
                    DrawSquareCentered(cnv, m, 10.0, stroke, thickness);
                    break;
                default:
                    stroke = Brushes.Black;
                    DrawLine(cnv, m, opacity, stroke, thickness);
                    break;
            }
        }

        private void DrawLine(Canvas cnv, StructuralMember m, double opacity, Brush stroke, double thickness)
        {
            cnv.Children.Add(new Line
            {
                X1 = m.Start.X,
                Y1 = m.Start.Y,
                X2 = m.End.X,
                Y2 = m.End.Y,
                Stroke = stroke,
                StrokeThickness = thickness,
                Opacity = opacity
            });
        }

        private void DrawSquareCentered(Canvas cnv, StructuralMember m, double h, Brush stroke, double thickness, Brush fill = null)
        {
            var square = new Rectangle
            {
                Width = h,
                Height = h,
                Stroke = stroke,
                StrokeThickness = thickness,
                Fill = fill ?? Brushes.Transparent
            };

            Canvas.SetLeft(square, m.Start.X - h / 2);
            Canvas.SetTop(square, m.Start.Y - h / 2);

            cnv.Children.Add(square);
        }
    }
}
