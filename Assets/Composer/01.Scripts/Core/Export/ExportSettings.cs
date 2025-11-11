/*
 * ExportSettings.cs
 *
 * Export configuration data class
 * - Format: PNG sequence (MP4 in Phase 2)
 * - Resolution presets: 512, 1024, 2048
 * - Frame range: full timeline or custom range
 * - File naming prefix
 */

using System;

namespace VFXComposer.Core.Export
{
    [Serializable]
    public class ExportSettings
    {
        public enum ExportFormat
        {
            PNGSequence,
            MP4Video
        }

        public enum ResolutionPreset
        {
            Res512 = 512,
            Res1024 = 1024,
            Res2048 = 2048
        }

        public ExportFormat format = ExportFormat.PNGSequence;
        public ResolutionPreset resolution = ResolutionPreset.Res1024;
        public bool useCustomRange = false;
        public int startFrame = 0;
        public int endFrame = 0;
        public string filePrefix = "frame";

        public int GetResolution()
        {
            return (int)resolution;
        }
    }
}
