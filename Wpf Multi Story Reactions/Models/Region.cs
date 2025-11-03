using System.Windows.Shapes;
using Wpf_Multi_Story_Reactions.Models;

namespace StructuralPlanner.Models
{
    public class Region
    {
        private static int _regionCounter = 0;
        public string RegionID { get; set; }
        public Polygon Poly { get; set; }
        public int Floor { get; set; }

        public Region(Polygon poly, int floor)
        {
            _regionCounter++;
            RegionID = $"r{_regionCounter}";
            Poly = poly;
            Floor = floor;
        }
    }
}