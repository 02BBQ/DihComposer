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
                        byte[] pngData = texture.EncodeToPNG();
                        string fileName = $"{frameNumber:D4}.png";
                        string filePath = Path.Combine(exportPath, fileName);
                        File.WriteAllBytes(filePath, pngData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[ImageSequenceExporter] Failed to save frame {frameNumber}: {e.Message}");
                        success = false;
                    }
                },
                onProgress,
                () =>
                {
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
