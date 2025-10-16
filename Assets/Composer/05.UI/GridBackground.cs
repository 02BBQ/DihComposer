using UnityEngine;
using UnityEngine.UIElements;

namespace VFXComposer.UI
{
    public class GridBackground : VisualElement
    {
        private float baseGridSpacing = 20f;
        private float baseThickLineSpacing = 100f;
        
        private float currentGridSpacing;
        private float currentThickSpacing;
        
        private Color lineColor = new Color(1, 1, 1, 0.05f);
        private Color thickLineColor = new Color(1, 1, 1, 0.1f);
        
        private Vector2 offset = Vector2.zero;
        private float zoom = 1f;
        
        public GridBackground()
        {
            generateVisualContent += OnGenerateVisualContent;
            AddToClassList("grid-background");
            
            UpdateGridSpacing();
        }
        
        public void SetOffset(Vector2 newOffset)
        {
            offset = newOffset;
            MarkDirtyRepaint();
        }
        
        public void SetZoom(float newZoom)
        {
            zoom = newZoom;
            UpdateGridSpacing();
            MarkDirtyRepaint();
        }
        
        private void UpdateGridSpacing()
        {
            if (zoom >= 1f)
            {
                currentGridSpacing = baseGridSpacing * zoom;
                currentThickSpacing = baseThickLineSpacing * zoom;
            }
            else if (zoom >= 0.5f)
            {
                currentGridSpacing = baseGridSpacing * zoom;
                currentThickSpacing = baseThickLineSpacing * zoom;
            }
            else if (zoom >= 0.25f)
            {
                currentGridSpacing = baseThickLineSpacing * zoom;
                currentThickSpacing = baseThickLineSpacing * 5f * zoom;
            }
            else
            {
                currentGridSpacing = baseThickLineSpacing * 5f * zoom;
                currentThickSpacing = baseThickLineSpacing * 10f * zoom;
            }
            
            currentGridSpacing = Mathf.Max(currentGridSpacing, 5f);
            currentThickSpacing = Mathf.Max(currentThickSpacing, 20f);
        }
        
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            
            float width = contentRect.width;
            float height = contentRect.height;
            
            if (currentGridSpacing < 10f)
            {
                return;
            }
            
            painter.lineWidth = 1.0f;
            painter.strokeColor = lineColor;
            
            float startX = offset.x % currentGridSpacing;
            float startY = offset.y % currentGridSpacing;
            
            for (float x = startX; x < width; x += currentGridSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
            
            for (float y = startY; y < height; y += currentGridSpacing)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(width, y));
                painter.Stroke();
            }
            
            if (currentThickSpacing >= currentGridSpacing * 2f)
            {
                float thickStartX = offset.x % currentThickSpacing;
                float thickStartY = offset.y % currentThickSpacing;
                
                painter.strokeColor = thickLineColor;
                painter.lineWidth = 2.0f;
                
                for (float x = thickStartX; x < width; x += currentThickSpacing)
                {
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, 0));
                    painter.LineTo(new Vector2(x, height));
                    painter.Stroke();
                }
                
                for (float y = thickStartY; y < height; y += currentThickSpacing)
                {
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(0, y));
                    painter.LineTo(new Vector2(width, y));
                    painter.Stroke();
                }
            }
        }
    }
}