using System.Windows;
using System.Collections.Generic;

namespace StructuralPlanner.Models
{
    public class Node
    {
        private static int _nodeCounter = 0;
        public string NodeID { get; set; }
        public Point Location { get; set; }
        public int Floor { get; set; }

        public List<StructuralMember> ConnectedMembers { get; } = new List<StructuralMember>();

        public Node()
        {
            _nodeCounter++;
            NodeID = $"N{_nodeCounter}";
        }

        public Node(Point location, int floor) : this()
        {
            Location = location;
            Floor = floor;
        }

        public string ConnectedMemberIDs => string.Join(", ", ConnectedMembers.ConvertAll(m => m.MemberID));
    }
}