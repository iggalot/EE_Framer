using System.Windows;

namespace StructuralPlanner.Extensions
{
    public static class PointExtensions
    {
        public static double DistanceTo(this Point a, Point b) => System.Math.Sqrt(System.Math.Pow(a.X - b.X, 2) + System.Math.Pow(a.Y - b.Y, 2));
        public static Point MidpointTo(this Point a, Point b) => new Point((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);
    }
}