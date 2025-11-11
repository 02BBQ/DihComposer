/*
 * FrameCaptureSystem.cs
 *
 * Captures each frame from timeline and converts to Texture2D
 * - Iterates through timeline frames
 * - Executes node graph for each frame
 * - Extracts RenderTexture from OutputNode
 * - Converts to Texture2D with memory reuse
 * - Supports frame range selection
 */

using System;
using System.Collections;
using UnityEngine;
using VFXComposer.Core.Animation;

namespace VFXComposer.Core.Export
{
    public class FrameCaptureSystem
    {
        private NodeGraph graph;
        private TimelineController timeline;
        private Texture2D reusableTexture;
        private int exportWidth;
        private int exportHeight;

        public FrameCaptureSystem(NodeGraph graph, TimelineController timeline, int width, int height)
        {
            this.graph = graph;
            this.timeline = timeline;
            this.exportWidth = width;
            this.exportHeight = height;
        }

        public IEnumerator CaptureFrames(int startFrame, int endFrame, Action<Texture2D, int> onFrameCaptured, Action<float> onProgress, Action onComplete)
        {
            if (reusableTexture == null)
            {
                reusableTexture = new Texture2D(exportWidth, exportHeight, TextureFormat.RGBA32, false);
            }

            int totalFrames = endFrame - startFrame + 1;
            int currentFrameIndex = 0;

            for (int frame = startFrame; frame <= endFrame; frame++)
            {
                timeline.SetFrame(frame);

                var executor = new NodeExecutor(graph);
                executor.Execute();

                RenderTexture outputRT = GetOutputRenderTexture();

                if (outputRT != null)
                {
                    RenderTextureToTexture2D(outputRT, reusableTexture);
                    onFrameCaptured?.Invoke(reusableTexture, frame);
                }
                else
                {
                    Debug.LogWarning($"[FrameCaptureSystem] No output at frame {frame}");
                }

                currentFrameIndex++;
                float progress = (float)currentFrameIndex / totalFrames;
                onProgress?.Invoke(progress);

                yield return null;
            }

            onComplete?.Invoke();
        }

        private RenderTexture GetOutputRenderTexture()
        {
            var outputNodes = graph.GetOutputNodes();
            if (outputNodes.Count > 0)
            {
                var outputNode = outputNodes[0];
                if (outputNode.cachedOutputs.TryGetValue("texture_in", out object output))
                {
                    return output as RenderTexture;
                }
                if (outputNode.cachedOutputs.TryGetValue("color_in", out object colorOutput))
                {
                    return colorOutput as RenderTexture;
                }
            }
            return null;
        }

        private void RenderTextureToTexture2D(RenderTexture source, Texture2D destination)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;

            destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            destination.Apply();

            RenderTexture.active = previous;
        }

        public void Cleanup()
        {
            if (reusableTexture != null)
            {
                UnityEngine.Object.Destroy(reusableTexture);
                reusableTexture = null;
            }
        }
    }
}
