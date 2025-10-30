using StructuralPlanner.Models;
using StructuralPlanner.Services;
using System.Windows;

namespace StructuralPlanner.Tests
{
    [TestClass]
    public class MemberDetectionServiceTests
    {
        [TestMethod]
        public void FindNearestMember_ReturnsClosestMember()
        {
            // Arrange: create some nodes
            Node n1 = new Node(new Point(0, 0), 0);
            Node n2 = new Node(new Point(10, 0), 0);
            Node n3 = new Node(new Point(10, 10), 0);

            // Create members connecting nodes
            StructuralMember m1 = new StructuralMember { ID = "M1", StartNode = n1, EndNode = n2, Type = MemberType.Beam };
            StructuralMember m2 = new StructuralMember { ID = "M2", StartNode = n2, EndNode = n3, Type = MemberType.Beam };

            // Connect members to nodes
            n1.ConnectedMembers.Add(m1);
            n2.ConnectedMembers.Add(m1);
            n2.ConnectedMembers.Add(m2);
            n3.ConnectedMembers.Add(m2);

            var members = new List<StructuralMember> { m1, m2 };

            // Act: pick a point near m1
            Point click = new Point(5, 1);
            StructuralMember nearest = MemberDetectionService.FindNearestMember(click, members);

            // Assert: the nearest member should be m1
            Assert.IsNotNull(nearest);
            Assert.AreEqual("M1", nearest.ID);
        }

        [TestMethod]
        public void FindNearestMember_ReturnsNull_WhenNoMembers()
        {
            // Arrange
            var members = new List<StructuralMember>();
            Point click = new Point(0, 0);

            // Act
            StructuralMember nearest = MemberDetectionService.FindNearestMember(click, members);

            // Assert
            Assert.IsNull(nearest);
        }
    }
}
