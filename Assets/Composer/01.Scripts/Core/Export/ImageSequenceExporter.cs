/*
 * ImageSequenceExporter.cs
 *
 * Exports frames as PNG image sequence
 * - Saves PNG files with frame numbers (frame_0001.png, frame_0002.png...)
 * - Uses NativeFilePicker for folder selection on mobile/desktop
 * - Async coroutine-based export with progress tracking
 * - Memory efficient with texture reuse
 */

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VFXComposer.Core.Animation;

namespace VFXComposer.Core.Export
{
    public class ImageSequenceExporter
    {
        private FrameCaptureSystem captureSystem;
        private string exportPath;
        private string fileNamePrefix;

        public ImageSequenceExporter(NodeGraph graph, TimelineController timeline, int width, int height)
        {
            captureSystem = new FrameCaptureSystem(graph, timeline, width, height);
        }

        public void SetExportPath(string path, string prefix = "frame")
        {
            exportPath = path;
            fileNamePrefix = prefix;
        }

        public IEnumerator ExportSequence(int startFrame, int endFrame, Action<float> onProgress, Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                Debug.LogError("[ImageSequenceExporter] Export path not set!");
                onComplete?.Invoke(false);
                yield break;
            }

            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            bool success = true;

            yield return captureSystem.CaptureFrames(
                startFrame,
                endFrame,
                (texture, frameNumber) =>
                {
                    try
                    {
                        Debug.Log($"[ImageSequenceExporter] Encoding frame {frameNumber} to PNG");
                        byte[] pngData = texture.EncodeToPNG();
                        Debug.Log($"[ImageSequenceExporter] PNG data size: {pngData?.Length ?? 0} bytes");

                        if (pngData == null || pngData.Length == 0)
                        {
                            Debug.LogError($"[ImageSequenceExporter] PNG encoding failed for frame {frameNumber}");
                            success = false;
                            return;
                        }

                        string fileName = $"{frameNumber:D4}.png";
                        string filePath = Path.Combine(exportPath, fileName);
                        Debug.Log($"[ImageSequenceExporter] Writing to: {filePath}");
                        File.WriteAllBytes(filePath, pngData);
                        Debug.Log($"[ImageSequenceExporter] Successfully saved frame {frameNumber}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ImageSequenceExporter] Failed to save frame {frameNumber}: {e.Message}\nStack: {e.StackTrace}");
                        success = false;
                    }
                },
                onProgress,
                () =>
                {
                    Debug.Log("[ImageSequenceExporter] Export sequence complete");
                    captureSystem.Cleanup();
                    onComplete?.Invoke(success);
                }
            );
        }

        public void Cleanup()
        {
            captureSystem.Cleanup();
        }
    }
}
