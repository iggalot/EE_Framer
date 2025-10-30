using StructuralPlanner.Models;
using StructuralPlanner.Services;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace StructuralPlanner.Managers
{
    public class CanvasManager
    {
        private readonly DrawingService _drawingService;

        public CanvasManager(DrawingService drawingService)
        {
            _drawingService = drawingService;
        }

        public void RedrawMembers(Canvas memberLayer, Canvas overlayLayer, List<StructuralMember> members, List<Node> nodes, List<Polygon> finalizedPolygons, Polygon previewPolygon, Line tempLineToMouse, int currentFloor)
        {
            memberLayer.Children.Clear();
            overlayLayer.Children.Clear();

            foreach (var m in members.Where(m => m.Floor == currentFloor))
                _drawingService.DrawMember(memberLayer, m, 1.0);

            if (currentFloor > 0)
                foreach (var m in members.Where(m => m.Floor == currentFloor - 1))
                    _drawingService.DrawMember(memberLayer, m, 0.3);

            foreach (var n in nodes.Where(n => n.Floor == currentFloor))
                _drawingService.DrawNode(memberLayer, n);

            // Redraw finalized polygons only for current floor
            foreach (var poly in finalizedPolygons)
            {
                bool flowControl = _drawingService.DrawPolygon(memberLayer, poly, nodes, currentFloor);
                if (!flowControl)
                {
                    continue;
                }
            }


            // Keep preview polygon and temp line if they exist
            if (previewPolygon != null && !overlayLayer.Children.Contains(previewPolygon))
                overlayLayer.Children.Add(previewPolygon);

            if (tempLineToMouse != null && !overlayLayer.Children.Contains(tempLineToMouse))
                overlayLayer.Children.Add(tempLineToMouse);
        }
    }
}