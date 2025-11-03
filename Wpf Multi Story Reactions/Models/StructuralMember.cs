using System.Windows;
using System.Windows.Media.Media3D;

namespace StructuralPlanner.Models
{
    public class StructuralMember
    {
        private static int _memberCounter = 0;
        public string MemberID { get; set; }
        public MemberType Type { get; set; }
        public Node StartNode { get; set; }
        public Node EndNode { get; set; }
        public double Angle { get => GetAngleOfMember(); }

        public int Floor => StartNode?.Floor ?? EndNode?.Floor ?? 0;

        public double Length
        {
            get
            {
                if (StartNode == null || EndNode == null) return 0;
                return (EndNode.Location - StartNode.Location).Length;
            }
        }

        public double Area_DL_psf { get; set; } = 10;   // psf
        public double Area_LL_psf { get; set; } = 20;   // psf

        public double ReactionUnfactored_Start_lbf { get => ComputeReactionUnfactored(); }   // lbf
        public double ReactionUnfactored_End_lbf { get => ComputeReactionUnfactored(); }   // lbf

        public double ReactionFactored_Start_lbf { get => ComputeReactionFactored(); }   // lbf
        public double ReactionFactored_End_lbf { get => ComputeReactionFactored(); }   // lbf

        public virtual double ComputeReactionUnfactored() { return 0; }

        public virtual double ComputeReactionFactored() { return 0; }

        public StructuralMember() 
        {
            InitializeMember();
        }

        public StructuralMember(MemberType type, Node start, Node end)
        {
            if (type == MemberType.Column)
            {
                if (start.Floor > end.Floor)
                {
                    StartNode = start;
                    EndNode = end;
                }
                else if (start.Floor < end.Floor)
                {
                    StartNode = end;
                    EndNode = start;
                }
                else
                {
                    throw new Exception("Start and end nodes for a column cannot be on the same floor.");
                }
            }
            else
            {
                // left most is start.
                if (start.Location.X < end.Location.X)
                {
                    StartNode = start;
                    EndNode = end;
                }
                else if (start.Location.X > end.Location.X)
                {
                    StartNode = end;
                    EndNode = start;
                }
                else
                {
                    // left most is start.
                    if (start.Location.Y < end.Location.Y)
                    {
                        StartNode = start;
                        EndNode = end;
                    }
                    else if (start.Location.Y > end.Location.Y)
                    {
                        StartNode = end;
                        EndNode = start;
                    }
                    else
                    {
                        throw new Exception("Start and end nodes are the same.");
                    }
                }
            }

            Type = type;

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
                case MemberType.FloorJoist:
                    prefix = "FJ";
                    break;
                case MemberType.CeilingJoist:
                    prefix = "CJ";
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

        private double GetAngleOfMember()
        {
            if (StartNode == null || EndNode == null) return 0;
            if (StartNode.Location.X == EndNode.Location.X && StartNode.Location.Y == EndNode.Location.Y) return 0;

            return Math.Atan2(
                EndNode.Location.Y - StartNode.Location.Y,
                EndNode.Location.X - StartNode.Location.X
            ) * 180 / Math.PI;
        }
    }
}