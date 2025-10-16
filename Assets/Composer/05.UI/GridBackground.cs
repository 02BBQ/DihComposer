using UnityEngine;
using UnityEngine.UIElements;

namespace VFXComposer.UI
{
    public class GridBackground : VisualElement
    {
        private float gridSpacing = 20f;
        private float thickLineSpacing = 100f;
        private Color lineColor = new Color(1, 1, 1, 0.05f);
        private Color thickLineColor = new Color(1, 1, 1, 0.1f);
        
        private Vector2 offset = Vector2.zero;
        
        public GridBackground()
        {
            generateVisualContent += OnGenerateVisualContent;
            AddToClassList("grid-background");
        }
        
        public void SetOffset(Vector2 newOffset)
        {
            offset = newOffset;
            MarkDirtyRepaint();
        }
        
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            
            float width = contentRect.width;
            float height = contentRect.height;
            
            painter.lineWidth = 1.0f;
            
            float startX = offset.x % gridSpacing;
            float startY = offset.y % gridSpacing;
            
            painter.strokeColor = lineColor;
            for (float x = startX; x < width; x += gridSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
            
            for (float y = startY; y < height; y += gridSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
                painter.Stroke();
            }
            
            float thickStartX = offset.x % thickLineSpacing;
            float thickStartY = offset.y % thickLineSpacing;
            
            painter.strokeColor = thickLineColor;
            painter.lineWidth = 2.0f;
            
            for (float x = thickStartX; x < width; x += thickLineSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
            
            for (float y = thickStartY; y < height; y += thickLineSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
                painter.Stroke();
            }
        }
    }
}