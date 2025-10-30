using System.Windows;

namespace StructuralPlanner.Models
{
    public class StructuralMember
    {
        public string ID { get; set; }
        public MemberType Type { get; set; }
        public Node StartNode { get; set; }
        public Node EndNode { get; set; }

        public int Floor => StartNode?.Floor ?? EndNode?.Floor ?? 0;

        public double Length
        {
            get
            {
                if (StartNode == null || EndNode == null) return 0;
                return (EndNode.Location - StartNode.Location).Length;
            }
        }

        public StructuralMember() { }

        public StructuralMember(string id, MemberType type, Node start, Node end)
        {
            ID = id;
            Type = type;
            StartNode = start;
            EndNode = end;
        }

        public bool ContainsPoint(Point p, double tolerance = 1.0)
        {
            if (StartNode == null || EndNode == null) return false;

            var a = StartNode.Location;
            var b = EndNode.Location;
            var proj = StructuralPlanner.Services.GeometryHelper.ProjectPointOntoSegment(p, a, b);
            return StructuralPlanner.Services.GeometryHelper.Distance(p, proj) <= tolerance;
        }
    }
}