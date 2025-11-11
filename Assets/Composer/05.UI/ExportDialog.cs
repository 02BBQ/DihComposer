/*
 * ExportDialog.cs
 *
 * Export dialog UI using UI Toolkit
 * - Format selection (PNG sequence)
 * - Resolution dropdown (512/1024/2048)
 * - Frame range options (full/custom)
 * - Export button with progress bar
 * - Cancel button
 */

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Collections;
using VFXComposer.Core;
using VFXComposer.Core.Export;
using VFXComposer.Core.Animation;

namespace VFXComposer.UI
{
    public class ExportDialog : VisualElement
    {
        private NodeGraph graph;
        private TimelineController timeline;
        private ExportSettings settings;
        private MonoBehaviour coroutineRunner;
        private string projectName;

        private DropdownField resolutionDropdown;
        private Toggle customRangeToggle;
        private IntegerField startFrameField;
        private IntegerField endFrameField;
        private Button exportButton;
        private Button cancelButton;
        private ProgressBar progressBar;
        private Label statusLabel;

        private ImageSequenceExporter exporter;
        private bool isExporting = false;

        public ExportDialog(NodeGraph graph, TimelineController timeline, MonoBehaviour runner, string projectPath)
        {
            this.graph = graph;
            this.timeline = timeline;
            this.coroutineRunner = runner;
            this.settings = new ExportSettings();

            if (!string.IsNullOrEmpty(projectPath))
            {
                this.projectName = Path.GetFileNameWithoutExtension(projectPath);
            }
            else
            {
                this.projectName = $"VFXExport_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            }

            AddToClassList("export-dialog__overlay");

            var dialog = new VisualElement();
            dialog.AddToClassList("export-dialog__container");
            Add(dialog);

            var title = new Label("Export Settings");
            title.AddToClassList("export-dialog__title");
            dialog.Add(title);

            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("export-dialog__content");
            dialog.Add(contentContainer);

            resolutionDropdown = new DropdownField("Resolution",
                new System.Collections.Generic.List<string> { "512x512", "1024x1024", "2048x2048" }, 1);
            resolutionDropdown.AddToClassList("export-dialog__field");
            contentContainer.Add(resolutionDropdown);

            customRangeToggle = new Toggle("Custom Frame Range");
            customRangeToggle.value = false;
            customRangeToggle.AddToClassList("export-dialog__field");
            customRangeToggle.RegisterValueChangedCallback(evt => UpdateFrameRangeVisibility(evt.newValue));
            contentContainer.Add(customRangeToggle);

            var rangeContainer = new VisualElement();
            rangeContainer.name = "rangeContainer";
            rangeContainer.AddToClassList("export-dialog__field");
            rangeContainer.style.display = DisplayStyle.None;
            contentContainer.Add(rangeContainer);

            startFrameField = new IntegerField("Start Frame");
            startFrameField.value = 0;
            rangeContainer.Add(startFrameField);

            int totalFrames = Mathf.CeilToInt(timeline.duration * timeline.fps);
            endFrameField = new IntegerField("End Frame");
            endFrameField.value = totalFrames;
            rangeContainer.Add(endFrameField);

            var projectLabel = new Label($"Export to folder: {projectName}");
            projectLabel.AddToClassList("export-dialog__project-label");
            contentContainer.Add(projectLabel);

            progressBar = new ProgressBar();
            progressBar.AddToClassList("export-dialog__progress");
            progressBar.style.display = DisplayStyle.None;
            contentContainer.Add(progressBar);

            statusLabel = new Label("");
            statusLabel.AddToClassList("export-dialog__status");
            statusLabel.style.display = DisplayStyle.None;
            contentContainer.Add(statusLabel);

            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("export-dialog__buttons");
            dialog.Add(buttonContainer);

            exportButton = new Button(OnExportClicked);
            exportButton.text = "📁 Export...";
            exportButton.AddToClassList("recent-dialog-button");
            exportButton.AddToClassList("recent-dialog-button--primary");
            buttonContainer.Add(exportButton);

            cancelButton = new Button(OnCancelClicked);
            cancelButton.text = "Cancel";
            cancelButton.AddToClassList("recent-dialog-button");
            cancelButton.AddToClassList("recent-dialog-button--cancel");
            buttonContainer.Add(cancelButton);
        }

        private void UpdateFrameRangeVisibility(bool visible)
        {
            var rangeContainer = this.Q("rangeContainer");
            if (rangeContainer != null)
            {
                rangeContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void OnExportClicked()
        {
            if (isExporting) return;

            settings.resolution = (ExportSettings.ResolutionPreset)(512 * (1 << resolutionDropdown.index));
            settings.useCustomRange = customRangeToggle.value;
            settings.startFrame = startFrameField.value;
            settings.endFrame = endFrameField.value;

            if (!settings.useCustomRange)
            {
                settings.startFrame = 0;
                settings.endFrame = Mathf.CeilToInt(timeline.duration * timeline.fps);
            }

#if UNITY_EDITOR
            string selectedFolder = UnityEditor.EditorUtility.OpenFolderPanel("Select Export Folder", Application.persistentDataPath, "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                string projectFolder = Path.Combine(selectedFolder, projectName);
                StartExport(projectFolder);
            }
#elif UNITY_ANDROID
            string projectFolder = Path.Combine(Application.persistentDataPath, projectName);
            StartExport(projectFolder);
#else
            string projectFolder = Path.Combine(Application.persistentDataPath, projectName);
            StartExport(projectFolder);
#endif
        }

        private void StartExport(string exportPath)
        {
            isExporting = true;
            exportButton.SetEnabled(false);

            progressBar.style.display = DisplayStyle.Flex;
            statusLabel.style.display = DisplayStyle.Flex;
            statusLabel.text = "Exporting...";

            int resolution = settings.GetResolution();
            exporter = new ImageSequenceExporter(graph, timeline, resolution, resolution);
            exporter.SetExportPath(exportPath, "");

            coroutineRunner.StartCoroutine(exporter.ExportSequence(
                settings.startFrame,
                settings.endFrame,
                (progress) =>
                {
                    progressBar.value = progress * 100f;
                    int currentFrame = Mathf.RoundToInt(progress * (settings.endFrame - settings.startFrame + 1));
                    statusLabel.text = $"Exporting frame {currentFrame}/{settings.endFrame - settings.startFrame + 1}";
                },
                (success) =>
                {
                    isExporting = false;
                    exportButton.SetEnabled(true);

                    if (success)
                    {
                        statusLabel.text = $"Export complete! Saved to: {exportPath}";
                        statusLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
                    }
                    else
                    {
                        statusLabel.text = "Export failed!";
                        statusLabel.style.color = new Color(0.8f, 0.3f, 0.3f);
                    }
                }
            ));
        }

        private void OnCancelClicked()
        {
            if (exporter != null)
            {
                exporter.Cleanup();
            }
            RemoveFromHierarchy();
        }
    }
}
