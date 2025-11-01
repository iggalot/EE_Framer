using StructuralPlanner.Models;
using StructuralPlanner.Services;
using System.Windows.Controls;
using System.Windows.Shapes;
using Wpf_Multi_Story_Reactions.Models;

namespace StructuralPlanner.Managers
{
    public class CanvasManager
    {
        private readonly DrawingService _drawingService;

        public CanvasManager(DrawingService drawingService)
        {
            _drawingService = drawingService;
        }

        public void RedrawMembers(Canvas memberLayer, Canvas overlayLayer, List<StructuralMember> members, List<Node> nodes, List<Polygon> finalizedPolygons, Polygon previewPolygon, Line tempLineToMouse, FramingLayer currentFloor)
        {
            memberLayer.Children.Clear();
            overlayLayer.Children.Clear();

            // Redraw finalized polygons only for current floor -- This should be the first layer drawn so that other objects are drawin over the top
            foreach (var poly in finalizedPolygons)
            {
                bool flowControl = _drawingService.DrawPolygon(memberLayer, poly, nodes, (int)currentFloor);
                if (!flowControl)
                {
                    continue;
                }
            }

            foreach (var m in members.Where(m => m.Floor == (int)currentFloor))
                _drawingService.DrawMember(memberLayer, m, 1.0);

            if (currentFloor > 0)
                foreach (var m in members.Where(m => m.Floor == (int)currentFloor - 1))
                    _drawingService.DrawMember(memberLayer, m, 0.3);

            foreach (var n in nodes.Where(n => n.Floor == (int)currentFloor))
                _drawingService.DrawNode(memberLayer, n);




            // Keep preview polygon and temp line if they exist
            if (previewPolygon != null && !overlayLayer.Children.Contains(previewPolygon))
                overlayLayer.Children.Add(previewPolygon);

            if (tempLineToMouse != null && !overlayLayer.Children.Contains(tempLineToMouse))
                overlayLayer.Children.Add(tempLineToMouse);
        }
    }
}