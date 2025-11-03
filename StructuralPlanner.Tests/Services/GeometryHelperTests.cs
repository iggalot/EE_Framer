using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using StructuralPlanner.Services;

namespace StructuralPlanner.Tests.Services
{
    [TestClass]
    public class GeometryHelperTests
    {
        [TestMethod]
        public void Distance_ShouldReturnCorrectValue()
        {
            Point a = new Point(0, 0);
            Point b = new Point(3, 4);
            double result = GeometryHelper.Distance(a, b);

            Assert.AreEqual(5.0, result, 0.001, "Distance between (0,0) and (3,4) should be 5");
        }

        [TestMethod]
        public void ProjectPerpendicular_ShouldReturnPerpendicularPoint()
        {
            Point origin = new Point(0, 0);
            Point mouse = new Point(3, 0);

            var edge = new StructuralPlanner.Models.StructuralMember
            {
                StartNode = new StructuralPlanner.Models.Node { Floor = 0, Location = new Point(0, 0) },
                EndNode = new StructuralPlanner.Models.Node { Floor = 0, Location = new Point(0, 10) }
            };

            Point result = GeometryHelper.ProjectPerpendicular(origin, mouse, edge);

            // Edge is vertical, so perpendicular projection should have X ≈ 3 and Y ≈ 0
            Assert.AreEqual(3, result.X, 0.001);
            Assert.AreEqual(0, result.Y, 0.001);
        }

        [TestMethod]
        public void GetPolygonEdges_ShouldReturnClosedEdges()
        {
            StaTestHelper.Run(() =>
            {
                var polygon = new System.Windows.Shapes.Polygon();
                polygon.Points.Add(new System.Windows.Point(0, 0));
                polygon.Points.Add(new System.Windows.Point(10, 0));
                polygon.Points.Add(new System.Windows.Point(10, 10));

                var edges = GeometryHelper.GetPolygonEdges(polygon);

                Assert.AreEqual(3, edges.Count, "Triangle should have 3 edges");
                AssertPointsAreEqual(edges[0].Start, new System.Windows.Point(0, 0));
                AssertPointsAreEqual(edges[2].End, new System.Windows.Point(0, 0));
            });
        }


        private void AssertPointsAreEqual(Point expected, Point actual, double tolerance = 0.0001)
        {
            Assert.IsTrue(Math.Abs(expected.X - actual.X) < tolerance && Math.Abs(expected.Y - actual.Y) < tolerance,
                $"Expected {expected}, got {actual}");
        }



    }
}