using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MultiStoryReactions
{
    public enum MemberType
    {
        Beam,
        Girder,
        Wall,
        Purlin
    }

    public class StructuralMember : INotifyPropertyChanged
    {
        private double _span;
        private double _reactionA;
        private double _reactionB;

        public string Id { get; set; }
        public int StartLevel { get; set; }
        public int EndLevel { get; set; }
        public double UniformLoad { get; set; }
        public bool IsCantilever { get; set; }
        public MemberType Type { get; set; }

        private Point _startNodePos;
        public Point StartNodePos
        {
            get => _startNodePos;
            set
            {
                _startNodePos = value;
                OnPropertyChanged(nameof(StartNodePos));
            }
        }

        private Point _endNodePos;
        public Point EndNodePos
        {
            get => _endNodePos;
            set
            {
                _endNodePos = value;
                OnPropertyChanged(nameof(EndNodePos));
            }
        }

        public double Span
        {
            get => _span;
            set
            {
                _span = value;
                OnPropertyChanged(nameof(Span));
            }
        }

        public double ReactionA
        {
            get => _reactionA;
            set
            {
                _reactionA = value;
                OnPropertyChanged(nameof(ReactionA));
            }
        }

        public double ReactionB
        {
            get => _reactionB;
            set
            {
                _reactionB = value;
                OnPropertyChanged(nameof(ReactionB));
            }
        }

        public double TotalReaction => ReactionA + ReactionB;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void ComputeReactions()
        {
            // Compute actual span from node positions
            double dx = EndNodePos.X - StartNodePos.X;
            double dy = EndNodePos.Y - StartNodePos.Y;
            double L = Math.Sqrt(dx * dx + dy * dy);

            Span = L; // update property

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

    public class BuildingModel
    {
        public List<StructuralMember> Members { get; private set; } = new List<StructuralMember>();

        public void ComputeReactions()
        {
            foreach (var member in Members)
                member.ComputeReactions();
        }

        public string GetReactionSummary()
        {
            if (Members.Count == 0)
                return "No members defined.";

            return string.Join(Environment.NewLine,
                Members.Select(m => $"{m.Id} ({m.Type}) StartLevel={m.StartLevel} EndLevel={m.EndLevel} RA={m.ReactionA:F2} RB={m.ReactionB:F2}"));
        }
    }
}
