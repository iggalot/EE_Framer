using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MultiStoryReactions
{
    public enum MemberType
    {
        Beam,
        Girder,
        Wall,
        Purlin,
        Column
    }

    /// <summary>
    /// Represents a structural member in plan view.
    /// </summary>
    public class StructuralMember
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 5);

        /// <summary>
        /// Type of member (beam, girder, wall, column, etc.)
        /// </summary>
        public MemberType Type { get; set; }

        /// <summary>
        /// True if the member is a cantilever.
        /// </summary>
        public bool IsCantilever { get; set; }

        /// <summary>
        /// Uniform load on member (per unit length)
        /// </summary>
        public double UniformLoad { get; set; }

        /// <summary>
        /// Floor or roof this member belongs to. Horizontal members are assigned to a single floor.
        /// </summary>
        public int Floor { get; set; } = 0; // 0 = ground, 1 = floor1, etc.

        /// <summary>
        /// X,Y start point in plan coordinates.
        /// </summary>
        public Point StartNodePos { get; set; }

        /// <summary>
        /// X,Y end point in plan coordinates.
        /// </summary>
        public Point EndNodePos { get; set; }

        /// <summary>
        /// True if this member is vertical (column or wall) connecting floors.
        /// </summary>
        public bool IsVerticalConnector => Type == MemberType.Column || Type == MemberType.Wall;

        /// <summary>
        /// Length of member in XY plane.
        /// </summary>
        public double Span
        {
            get
            {
                double dx = EndNodePos.X - StartNodePos.X;
                double dy = EndNodePos.Y - StartNodePos.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// Reaction at start node (can be extended for load path tracing)
        /// </summary>
        public double ReactionA { get; set; }

        /// <summary>
        /// Reaction at end node (can be extended for load path tracing)
        /// </summary>
        public double ReactionB { get; set; }

        /// <summary>
        /// Total reaction (start + end)
        /// </summary>
        public double TotalReaction => ReactionA + ReactionB;

        /// <summary>
        /// Compute simple reactions for uniform load.
        /// Placeholder for future load path propagation.
        /// </summary>
        public void ComputeReactions()
        {
            double L = Span;

            if (IsCantilever)
            {
                ReactionA = UniformLoad * L;
                ReactionB = 0;
            }
            else
            {
                ReactionA = UniformLoad * L / 2.0;
                ReactionB = UniformLoad * L / 2.0;
            }
        }
    }

    /// <summary>
    /// Represents the entire building model.
    /// </summary>
    public class BuildingModel
    {
        public List<StructuralMember> Members { get; private set; } = new List<StructuralMember>();

        /// <summary>
        /// Compute reactions for all members.
        /// </summary>
        public void ComputeReactions()
        {
            foreach (var m in Members)
            {
                m.ComputeReactions();
            }
        }

        /// <summary>
        /// Returns members relevant for a given floor.
        /// Horizontal members assigned to the floor, vertical connectors appear on all floors.
        /// </summary>
        public IEnumerable<StructuralMember> GetMembersForFloor(int floor)
        {
            return Members.Where(m => m.Floor == floor || m.IsVerticalConnector);
        }

        /// <summary>
        /// Returns a text summary of all members and their reactions.
        /// </summary>
        public string GetReactionSummary()
        {
            if (Members.Count == 0)
                return "No members defined.";

            return string.Join(Environment.NewLine,
                Members.Select(m =>
                    $"{m.Id} ({m.Type}) Floor={m.Floor} RA={m.ReactionA:F2} RB={m.ReactionB:F2}"));
        }
    }
}
