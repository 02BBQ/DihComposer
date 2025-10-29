using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace VFXComposer.Core
{
    /// <summary>
    /// Cross-platform file picker for mobile and desktop
    /// </summary>
    public static class FilePicker
    {
        private static Action<string> onFileSelected;
        private static Action onCancelled;
        private static VisualElement rootElement;

        /// <summary>
        /// Initialize with root visual element (needed for PC dialog)
        /// </summary>
        public static void Initialize(VisualElement root)
        {
            rootElement = root;
        }

        /// <summary>
        /// Open file picker dialog
        /// </summary>
        /// <param name="filter">File extension filter (e.g., ".vfxc")</param>
        /// <param name="onSelected">Callback when file is selected</param>
        /// <param name="onCancel">Callback when cancelled</param>
        public static void PickFile(string filter, Action<string> onSelected, Action onCancel = null)
        {
            onFileSelected = onSelected;
            onCancelled = onCancel;

#if UNITY_ANDROID && !UNITY_EDITOR
            PickFile_Android(filter);
#else
            PickFile_Standalone(filter);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void PickFile_Android(string filter)
        {
            try
            {
                using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setAction", "android.intent.action.GET_CONTENT");
                    intent.Call<AndroidJavaObject>("setType", "*/*");

                    // Add MIME type filter
                    if (!string.IsNullOrEmpty(filter))
                    {
                        AndroidJavaObject extraMimeTypes = new AndroidJavaObject("java.lang.String[]", 1);
                        extraMimeTypes.Set(0, "application/octet-stream");
                        intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.MIME_TYPES", extraMimeTypes);
                    }

                    currentActivity.Call("startActivityForResult", intent, 1001);
                }

                // Android will call OnFilePickerResult via message
                AndroidFilePickerCallback.Initialize(onFileSelected, onCancelled);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePicker] Android picker failed: {e.Message}");
                onCancelled?.Invoke();
            }
        }
#endif

        private static void PickFile_Standalone(string filter)
        {
            if (rootElement == null)
            {
                Debug.LogError("[FilePicker] Root element not initialized! Call FilePicker.Initialize() first.");
                onCancelled?.Invoke();
                return;
            }

            try
            {
                string path = Application.persistentDataPath;
                var dialog = new VFXComposer.UI.FilePickerDialog(path, filter, onFileSelected, onCancelled);
                rootElement.Add(dialog);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePicker] Standalone picker failed: {e.Message}");
                onCancelled?.Invoke();
            }
        }

        /// <summary>
        /// PC용 파일 선택 다이얼로그 (Windows Forms 사용)
        /// </summary>
        private static void PickFile_Windows(string filter)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                var openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = $"VFX Composer Files (*{filter})|*{filter}";
                openFileDialog.InitialDirectory = Application.persistentDataPath;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    onFileSelected?.Invoke(openFileDialog.FileName);
                }
                else
                {
                    onCancelled?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePicker] Windows picker failed: {e.Message}");
                onCancelled?.Invoke();
            }
#endif
        }

        /// <summary>
        /// Save file picker dialog
        /// </summary>
        public static void SaveFile(string defaultName, string filter, Action<string> onSelected, Action onCancel = null)
        {
            onFileSelected = onSelected;
            onCancelled = onCancel;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Android: Just use default path with filename
            string path = Path.Combine(Application.persistentDataPath, defaultName + filter);
            onFileSelected?.Invoke(path);
#else
            SaveFile_Standalone(defaultName, filter);
#endif
        }

        private static void SaveFile_Standalone(string defaultName, string filter)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, defaultName + filter);
                onFileSelected?.Invoke(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePicker] Save file failed: {e.Message}");
                onCancelled?.Invoke();
            }
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android callback handler for file picker result
    /// </summary>
    public class AndroidFilePickerCallback : MonoBehaviour
    {
        private static Action<string> onSelected;
        private static Action onCancelled;
        private static AndroidFilePickerCallback instance;

        public static void Initialize(Action<string> selected, Action cancelled)
        {
            onSelected = selected;
            onCancelled = cancelled;

            if (instance == null)
            {
                GameObject go = new GameObject("AndroidFilePickerCallback");
                instance = go.AddComponent<AndroidFilePickerCallback>();
                DontDestroyOnLoad(go);
            }
        }

        void OnActivityResult(int requestCode, int resultCode, AndroidJavaObject data)
        {
            if (requestCode == 1001)
            {
                if (resultCode == -1 && data != null) // RESULT_OK
                {
                    try
                    {
                        AndroidJavaObject uri = data.Call<AndroidJavaObject>("getData");
                        string path = GetPathFromUri(uri);

                        if (!string.IsNullOrEmpty(path))
                        {
                            onSelected?.Invoke(path);
                        }
                        else
                        {
                            onCancelled?.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[FilePicker] Failed to get file path: {e.Message}");
                        onCancelled?.Invoke();
                    }
                }
                else
                {
                    onCancelled?.Invoke();
                }
            }
        }

        private string GetPathFromUri(AndroidJavaObject uri)
        {
            // Copy file from content:// URI to persistentDataPath
            try
            {
                using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver"))
                using (AndroidJavaObject inputStream = contentResolver.Call<AndroidJavaObject>("openInputStream", uri))
                {
                    // Read file to byte array
                    AndroidJavaObject available = inputStream.Call<int>("available");
                    byte[] buffer = new byte[(int)available];
                    inputStream.Call<int>("read", buffer);
                    inputStream.Call("close");

                    // Write to persistent path
                    string fileName = "temp_" + System.DateTime.Now.Ticks + ".vfxc";
                    string destPath = Path.Combine(Application.persistentDataPath, fileName);
                    File.WriteAllBytes(destPath, buffer);

                    return destPath;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePicker] GetPathFromUri failed: {e.Message}");
                return null;
            }
        }
    }
#endif
}
