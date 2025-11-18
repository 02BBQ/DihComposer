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
                Debug.Log($"[FrameCaptureSystem] Created reusable texture: {exportWidth}x{exportHeight}");
            }

            int totalFrames = endFrame - startFrame + 1;
            int currentFrameIndex = 0;

            Debug.Log($"[FrameCaptureSystem] Starting capture: frames {startFrame} to {endFrame} (total: {totalFrames})");

            for (int frame = startFrame; frame <= endFrame; frame++)
            {
                timeline.SetFrame(frame);

                var executor = new NodeExecutor(graph);
                executor.Execute();

                RenderTexture outputRT = GetOutputRenderTexture();

                if (outputRT != null)
                {
                    Debug.Log($"[FrameCaptureSystem] Frame {frame}: Got RenderTexture {outputRT.width}x{outputRT.height}");
                    RenderTextureToTexture2D(outputRT, reusableTexture);
                    Debug.Log($"[FrameCaptureSystem] Frame {frame}: Converted to Texture2D, invoking callback");
                    onFrameCaptured?.Invoke(reusableTexture, frame);
                }
                else
                {
                    Debug.LogWarning($"[FrameCaptureSystem] No output RenderTexture at frame {frame}");
                }

                currentFrameIndex++;
                float progress = (float)currentFrameIndex / totalFrames;
                onProgress?.Invoke(progress);

                yield return null;
            }

            Debug.Log("[FrameCaptureSystem] Capture complete");
            onComplete?.Invoke();
        }

        private RenderTexture GetOutputRenderTexture()
        {
            var outputNodes = graph.GetOutputNodes();
            Debug.Log($"[FrameCaptureSystem] Found {outputNodes.Count} output nodes");

            if (outputNodes.Count > 0)
            {
                var outputNode = outputNodes[0] as OutputNode;
                if (outputNode != null)
                {
                    Debug.Log($"[FrameCaptureSystem] OutputNode texture: {(outputNode.outputTexture != null ? $"{outputNode.outputTexture.width}x{outputNode.outputTexture.height}" : "null")}");
                    return outputNode.outputTexture;
                }
            }

            Debug.LogWarning("[FrameCaptureSystem] No valid RenderTexture found in output node");
            return null;
        }

        private void RenderTextureToTexture2D(RenderTexture source, Texture2D destination)
        {
            RenderTexture previous = RenderTexture.active;

            // 소스와 목표 크기가 다르면 리사이즈 필요
            if (source.width != exportWidth || source.height != exportHeight)
            {
                Debug.Log($"[FrameCaptureSystem] Resizing from {source.width}x{source.height} to {exportWidth}x{exportHeight}");

                // 임시 RenderTexture 생성 (목표 크기)
                RenderTexture resized = RenderTexture.GetTemporary(exportWidth, exportHeight, 0, RenderTextureFormat.ARGB32);

                // Graphics.Blit으로 리사이즈
                Graphics.Blit(source, resized);

                // 리사이즈된 RenderTexture를 Texture2D로 복사
                RenderTexture.active = resized;
                destination.ReadPixels(new Rect(0, 0, exportWidth, exportHeight), 0, 0);
                destination.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(resized);
            }
            else
            {
                // 크기가 같으면 직접 복사
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
                RenderTexture.active = previous;
            }
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
