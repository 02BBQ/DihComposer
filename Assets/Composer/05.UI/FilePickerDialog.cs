using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Linq;

namespace VFXComposer.UI
{
    /// <summary>
    /// Simple file picker dialog using UI Toolkit
    /// </summary>
    public class FilePickerDialog : VisualElement
    {
        private Action<string> onFileSelected;
        private Action onCancelled;
        private VisualElement fileListContainer;
        private Label pathLabel;
        private string currentPath;
        private string fileFilter;

        public FilePickerDialog(string initialPath, string filter, Action<string> onSelected, Action onCancel = null)
        {
            this.currentPath = initialPath;
            this.fileFilter = filter;
            this.onFileSelected = onSelected;
            this.onCancelled = onCancel;

            // Full screen overlay
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.backgroundColor = new Color(0, 0, 0, 0.7f);
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            // Dialog container
            var dialog = new VisualElement();
            dialog.AddToClassList("file-picker-dialog");
            dialog.style.width = 600;
            dialog.style.maxHeight = 500;
            dialog.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            dialog.style.borderTopLeftRadius = 8;
            dialog.style.borderTopRightRadius = 8;
            dialog.style.borderBottomLeftRadius = 8;
            dialog.style.borderBottomRightRadius = 8;
            dialog.style.borderTopWidth = 2;
            dialog.style.borderBottomWidth = 2;
            dialog.style.borderLeftWidth = 2;
            dialog.style.borderRightWidth = 2;
            dialog.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            dialog.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            Add(dialog);

            // Title
            var title = new Label("Select File");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.paddingTop = 16;
            title.style.paddingBottom = 16;
            title.style.paddingLeft = 16;
            title.style.paddingRight = 16;
            title.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            dialog.Add(title);

            // Path label
            pathLabel = new Label(currentPath);
            pathLabel.style.fontSize = 12;
            pathLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            pathLabel.style.paddingTop = 8;
            pathLabel.style.paddingBottom = 8;
            pathLabel.style.paddingLeft = 16;
            pathLabel.style.paddingRight = 16;
            pathLabel.style.whiteSpace = WhiteSpace.Normal;
            dialog.Add(pathLabel);

            // File list scroll view
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.maxHeight = 300;
            dialog.Add(scrollView);

            fileListContainer = new VisualElement();
            fileListContainer.style.paddingTop = 8;
            fileListContainer.style.paddingBottom = 8;
            scrollView.Add(fileListContainer);

            // Buttons
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.FlexEnd;
            buttonContainer.style.paddingTop = 16;
            buttonContainer.style.paddingBottom = 16;
            buttonContainer.style.paddingLeft = 16;
            buttonContainer.style.paddingRight = 16;
            dialog.Add(buttonContainer);

            var cancelButton = new Button(() => Close(false));
            cancelButton.text = "Cancel";
            cancelButton.style.width = 100;
            cancelButton.style.height = 35;
            cancelButton.style.marginRight = 8;
            buttonContainer.Add(cancelButton);

            // Populate file list
            RefreshFileList();
        }

        private void RefreshFileList()
        {
            fileListContainer.Clear();

            try
            {
                string[] files = Directory.GetFiles(currentPath, $"*{fileFilter}");

                if (files.Length == 0)
                {
                    var emptyLabel = new Label($"No {fileFilter} files found");
                    emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                    emptyLabel.style.paddingLeft = 16;
                    emptyLabel.style.paddingTop = 20;
                    emptyLabel.style.fontSize = 14;
                    fileListContainer.Add(emptyLabel);
                    return;
                }

                // Sort by modification time (newest first)
                var sortedFiles = files.OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();

                foreach (var filePath in sortedFiles)
                {
                    var fileButton = CreateFileButton(filePath);
                    fileListContainer.Add(fileButton);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePickerDialog] Failed to list files: {e.Message}");
            }
        }

        private Button CreateFileButton(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            DateTime modTime = File.GetLastWriteTime(filePath);
            string timeStr = modTime.ToString("yyyy-MM-dd HH:mm");

            var button = new Button(() => SelectFile(filePath));
            button.style.height = 40;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.marginBottom = 4;
            button.style.paddingLeft = 12;
            button.style.paddingRight = 12;
            button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            button.style.borderTopWidth = 1;
            button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = 1;
            button.style.borderRightWidth = 1;
            button.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            button.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            button.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            button.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
            button.style.alignItems = Align.FlexStart;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;

            // Create label with file info
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.width = Length.Percent(100);
            container.pickingMode = PickingMode.Ignore;

            var nameLabel = new Label(fileName);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 13;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.pickingMode = PickingMode.Ignore;
            container.Add(nameLabel);

            var timeLabel = new Label(timeStr);
            timeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            timeLabel.style.fontSize = 11;
            timeLabel.pickingMode = PickingMode.Ignore;
            container.Add(timeLabel);

            button.Add(container);

            // Hover effect
            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                button.style.backgroundColor = new Color(0.35f, 0.5f, 0.7f);
            });

            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            });

            return button;
        }

        private void SelectFile(string filePath)
        {
            Close(true);
            onFileSelected?.Invoke(filePath);
        }

        private void Close(bool selected)
        {
            if (!selected)
            {
                onCancelled?.Invoke();
            }

            RemoveFromHierarchy();
        }
    }
}
