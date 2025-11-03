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

        public void RedrawMembers(Canvas memberLayer, Canvas overlayLayer, List<StructuralMember> members, List<Node> nodes, List<Region> regions, Polygon previewPolygon, Line tempLineToMouse, FramingLayer currentFloor, bool showLabels = true, bool showReactions = true)
        {
            memberLayer.Children.Clear();
            overlayLayer.Children.Clear();

            // Redraw finalized polygons only for current floor -- This should be the first layer drawn so that other objects are drawin over the top
            foreach (var region in regions.Where(region => region.Floor == (int)currentFloor))
            {
                bool flowControl = _drawingService.DrawPolygon(memberLayer, region.Poly, nodes, (int)currentFloor);
                if (!flowControl)
                {
                    continue;
                }

                if (showLabels)
                {
                    _drawingService.DrawRegionLabel(overlayLayer, region);
                }
            }

            foreach (var m in members.Where(m => m.Floor == (int)currentFloor))
            {
                _drawingService.DrawMember(memberLayer, m, 1.0);

                if (showLabels)
                {
                    _drawingService.DrawMemberLabel(overlayLayer, m);
                }

                if (showReactions)
                {
                    _drawingService.DrawMemberReactions(memberLayer, m, m.StartNode);
                    _drawingService.DrawMemberReactions(memberLayer, m, m.EndNode);
                }
            }

            if (currentFloor > 0)
                foreach (var m in members.Where(m => m.Floor == (int)currentFloor - 1))
                    _drawingService.DrawMember(memberLayer, m, 0.3);

            //// Keep preview polygon and temp line if they exist
            //if (previewPolygon != null && !overlayLayer.Children.Contains(previewPolygon))
            //    overlayLayer.Children.Add(previewPolygon);

            //if (tempLineToMouse != null && !overlayLayer.Children.Contains(tempLineToMouse))
            //    overlayLayer.Children.Add(tempLineToMouse);

            foreach (var n in nodes.Where(n => n.Floor == (int)currentFloor))
            {
                _drawingService.DrawNode(memberLayer, n);

                if (showLabels)
                {
                    _drawingService.DrawNodeLabel(overlayLayer, n);
                }
            }
        }
    }
}