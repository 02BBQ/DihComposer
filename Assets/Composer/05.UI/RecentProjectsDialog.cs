/*
 * RecentProjectsDialog.cs
 *
 * Shows list of recently opened projects with UI Toolkit
 * - Displays up to 10 most recent projects
 * - Shows file name and last modified date
 * - Click to load project
 * - Browse button opens native file picker
 */

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using VFXComposer.Core;

namespace VFXComposer.UI
{
    public class RecentProjectsDialog : VisualElement
    {
        private Action<string> onFileSelected;
        private Action onCancelled;
        private VisualElement fileListContainer;

        public RecentProjectsDialog(Action<string> onSelected, Action onCancel = null)
        {
            this.onFileSelected = onSelected;
            this.onCancelled = onCancel;

            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.backgroundColor = new Color(0, 0, 0, 0.7f);
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

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

            var title = new Label("Recent Projects");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.paddingTop = 16;
            title.style.paddingBottom = 16;
            title.style.paddingLeft = 16;
            title.style.paddingRight = 16;
            title.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            dialog.Add(title);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.maxHeight = 350;
            dialog.Add(scrollView);

            fileListContainer = new VisualElement();
            fileListContainer.style.paddingTop = 8;
            fileListContainer.style.paddingBottom = 8;
            scrollView.Add(fileListContainer);

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.SpaceBetween;
            buttonContainer.style.paddingTop = 16;
            buttonContainer.style.paddingBottom = 16;
            buttonContainer.style.paddingLeft = 16;
            buttonContainer.style.paddingRight = 16;
            dialog.Add(buttonContainer);

            var browseButton = new Button(OnBrowseClicked);
            browseButton.text = "📁 Browse...";
            browseButton.AddToClassList("recent-dialog-button");
            browseButton.AddToClassList("recent-dialog-button--primary");
            buttonContainer.Add(browseButton);

            var cancelButton = new Button(() => Close(false));
            cancelButton.text = "Cancel";
            cancelButton.AddToClassList("recent-dialog-button");
            cancelButton.AddToClassList("recent-dialog-button--cancel");
            buttonContainer.Add(cancelButton);

            RefreshFileList();
        }

        private void RefreshFileList()
        {
            fileListContainer.Clear();

            var recentProjects = RecentProjects.GetRecentProjects();

            if (recentProjects.Count == 0)
            {
                var emptyLabel = new Label("No recent projects");
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.paddingLeft = 16;
                emptyLabel.style.paddingTop = 20;
                emptyLabel.style.fontSize = 14;
                fileListContainer.Add(emptyLabel);
                return;
            }

            foreach (var project in recentProjects)
            {
                var fileButton = CreateFileButton(project);
                fileListContainer.Add(fileButton);
            }
        }

        private Button CreateFileButton(RecentProjectInfo project)
        {
            string timeStr = project.LastModified.ToString("yyyy-MM-dd HH:mm");

            var button = new Button(() => SelectFile(project.FilePath));
            button.AddToClassList("recent-project-item");

            var container = new VisualElement();
            container.AddToClassList("recent-project-item__container");
            container.pickingMode = PickingMode.Ignore;

            var nameLabel = new Label(project.FileName);
            nameLabel.AddToClassList("recent-project-item__name");
            nameLabel.pickingMode = PickingMode.Ignore;
            container.Add(nameLabel);

            var timeLabel = new Label(timeStr);
            timeLabel.AddToClassList("recent-project-item__time");
            timeLabel.pickingMode = PickingMode.Ignore;
            container.Add(timeLabel);

            button.Add(container);

            return button;
        }

        private void OnBrowseClicked()
        {
            Close(false);
            FilePicker.PickFile(".vfxc", onFileSelected, onCancelled);
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
