/*
 * FilePicker.cs
 *
 * Cross-platform file picker wrapper using NativeFilePicker plugin
 * - Supports mobile (Android/iOS) and desktop (Windows/Mac/Linux)
 * - Load: Shows native file picker dialog
 * - Save: Shows native save location picker using ExportFile
 * - Recent projects UI shown through RecentProjectsDialog
 */

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace VFXComposer.Core
{
    public static class FilePicker
    {
        private static VisualElement rootElement;

        public static void Initialize(VisualElement root)
        {
            rootElement = root;
        }

        public static void PickFile(string extension, Action<string> onSelected, Action onCancel = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            NativeFilePicker.PickFile((path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        onSelected?.Invoke(path);
                    }
                    else
                    {
                        Debug.LogWarning($"[FilePicker] Selected file does not have {extension} extension: {path}");
                        onCancel?.Invoke();
                    }
                }
                else
                {
                    onCancel?.Invoke();
                }
            }, "application/octet-stream");
#else
            string cleanExtension = extension.TrimStart('.');

            NativeFilePicker.PickFile((path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    onSelected?.Invoke(path);
                }
                else
                {
                    onCancel?.Invoke();
                }
            }, cleanExtension);
#endif
        }

        public static void SaveFile(string sourcePath, Action<bool> onComplete = null)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[FilePicker] Source file not found: {sourcePath}");
                onComplete?.Invoke(false);
                return;
            }

            NativeFilePicker.ExportFile(sourcePath, (success) =>
            {
                onComplete?.Invoke(success);
            });
        }

        public static void ShowRecentProjects(Action<string> onSelected, Action onCancel = null)
        {
            if (rootElement == null)
            {
                Debug.LogError("[FilePicker] Root element not initialized!");
                onCancel?.Invoke();
                return;
            }

            var dialog = new VFXComposer.UI.RecentProjectsDialog(onSelected, onCancel);
            rootElement.Add(dialog);
        }
    }
}
