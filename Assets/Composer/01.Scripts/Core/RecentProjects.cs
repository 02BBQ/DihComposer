/*
 * RecentProjects.cs
 *
 * Manages recent project history with PlayerPrefs persistence
 * - Stores up to 10 most recent project paths
 * - Automatically updates on load/save
 * - Provides list of valid (still existing) recent projects
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VFXComposer.Core
{
    public static class RecentProjects
    {
        private const string PREFS_KEY = "VFXComposer_RecentProjects";
        private const int MAX_RECENT = 10;

        private static List<string> recentPaths = null;

        public static void AddRecentProject(string filePath)
        {
            LoadRecents();

            recentPaths.Remove(filePath);
            recentPaths.Insert(0, filePath);

            if (recentPaths.Count > MAX_RECENT)
            {
                recentPaths.RemoveAt(recentPaths.Count - 1);
            }

            SaveRecents();
        }

        public static List<RecentProjectInfo> GetRecentProjects()
        {
            LoadRecents();

            var result = new List<RecentProjectInfo>();

            foreach (var path in recentPaths)
            {
                if (File.Exists(path))
                {
                    result.Add(new RecentProjectInfo
                    {
                        FilePath = path,
                        FileName = Path.GetFileName(path),
                        LastModified = File.GetLastWriteTime(path)
                    });
                }
            }

            return result;
        }

        public static void RemoveRecentProject(string filePath)
        {
            LoadRecents();
            recentPaths.Remove(filePath);
            SaveRecents();
        }

        public static void ClearRecents()
        {
            recentPaths = new List<string>();
            SaveRecents();
        }

        private static void LoadRecents()
        {
            if (recentPaths != null) return;

            recentPaths = new List<string>();

            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<RecentProjectsData>(json);
                    if (data != null && data.paths != null)
                    {
                        recentPaths = data.paths.ToList();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RecentProjects] Failed to load: {e.Message}");
                }
            }
        }

        private static void SaveRecents()
        {
            var data = new RecentProjectsData { paths = recentPaths.ToArray() };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        [Serializable]
        private class RecentProjectsData
        {
            public string[] paths;
        }
    }

    public struct RecentProjectInfo
    {
        public string FilePath;
        public string FileName;
        public DateTime LastModified;
    }
}
