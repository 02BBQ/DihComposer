using UnityEngine;

namespace VFXComposer.Rendering
{
    public class TextureRenderer
    {
        private static int defaultResolution = 512;
        
        public static RenderTexture CreateRenderTexture(int width = 0, int height = 0)
        {
            if (width <= 0) width = defaultResolution;
            if (height <= 0) height = defaultResolution;
            
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();
            
            return rt;
        }
        
        public static void ClearRenderTexture(RenderTexture rt, Color color)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            GL.Clear(true, true, color);
            
            RenderTexture.active = previous;
        }
        
        public static void BlitWithMaterial(RenderTexture source, RenderTexture dest, Material material)
        {
            Graphics.Blit(source, dest, material);
        }
        
        public static void DrawQuad(RenderTexture target, Material material)
        {
            // Use Graphics.Blit with null source for generator shaders
            // This works outside of rendering callbacks unlike GL.Begin/GL.End
            Graphics.Blit(null, target, material);
        }
        
        public static Texture2D RenderTextureToTexture2D(RenderTexture rt)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            
            RenderTexture.active = previous;
            
            return tex;
        }
    }
}