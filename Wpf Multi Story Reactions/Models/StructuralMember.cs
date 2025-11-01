using System.Windows;

namespace StructuralPlanner.Models
{
    public class StructuralMember
    {
        private static int _memberCounter = 0;
        public string MemberID { get; set; }
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

        public StructuralMember() 
        {
            InitializeMember();
        }

        public StructuralMember(MemberType type, Node start, Node end)
        {
            Type = type;
            StartNode = start;
            EndNode = end;
            InitializeMember();
        }

        private void InitializeMember()
        {
            _memberCounter++;
            MemberID = $"{GetIDPrefix(Type)}{_memberCounter}";
        }

        private string GetIDPrefix(MemberType type)
        {
            string prefix = "M";

            switch (type)
            {
                case MemberType.Beam:
                    prefix = "B";
                    break;
                case MemberType.Column:
                    prefix = "C";
                    break;
                case MemberType.Rafter:
                    prefix = "R";
                    break;
                case MemberType.Joist:
                    prefix = "J";
                    break;
                case MemberType.Purlin:
                    prefix = "P";
                    break;
                case MemberType.Wall:
                    prefix = "W";
                    break;
                case MemberType.RoofBrace:
                    prefix = "RB";
                    break;
                default:
                    prefix = "M";
                break;
            }
            return prefix;
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