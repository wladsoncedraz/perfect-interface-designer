using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using UIEdit.Models;

// By: SpinxDev 2025 xD
namespace UIEdit.Controllers
{
    public static class LayerMetadataService
    {
        #region Injection Properties
        private const string FileSuffix = ".layers.json";
        #endregion

        #region Constructor
        #endregion

        #region Methods
        /// <summary>
        /// Gets the directory path for storing metadata files.
        /// </summary>
        /// <returns></returns>
        private static string GetMetaDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(baseDir, "MetaData");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Gets the full path for the metadata file corresponding to the given source file path.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        private static string GetMetadataPathForSource(string sourceFilePath)
        {
            var fileName = Path.GetFileName(sourceFilePath ?? "");
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "unknown";
            return Path.Combine(GetMetaDirectory(), fileName + FileSuffix);
        }

        /// <summary>
        /// Saves the ZIndex layers of the controls in the given dialog to a metadata file associated with the source file path.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="dialog"></param>
        public static void Save(string sourceFilePath, UIDialog dialog)
        {
            if (dialog == null) return;
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Action<IEnumerable<UIControl>> add = controls =>
            {
                foreach (var c in controls)
                {
                    if (string.IsNullOrWhiteSpace(c.Name)) continue;
                    map[c.Name] = c.ZIndex;
                }
            };
            add(dialog.ImagePictures);
            add(dialog.Scrolls);
            add(dialog.Edits);
            add(dialog.StillImageButtons);
            add(dialog.RadioButtons);
            add(dialog.Labels);

            var payload = new { Layers = map, SavedAt = DateTime.Now.ToString("o") };
            var json = new JavaScriptSerializer().Serialize(payload);
            var path = GetMetadataPathForSource(sourceFilePath);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Tries to load and apply the ZIndex layers from the metadata file associated with the source file path to the controls in the given dialog.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="dialog"></param>
        /// <returns></returns>
        public static bool TryLoadAndApply(string sourceFilePath, UIDialog dialog)
        {
            try
            {
                var path = GetMetadataPathForSource(sourceFilePath);
                if (!File.Exists(path)) return false;
                var json = File.ReadAllText(path);
                var serializer = new JavaScriptSerializer();
                var obj = serializer.Deserialize<Dictionary<string, object>>(json);
                if (obj == null || !obj.ContainsKey("Layers")) return false;
                var layersDict = obj["Layers"] as Dictionary<string, object>;
                if (layersDict == null) return false;
                Func<string, int?> getZ = name =>
                {
                    if (name == null) return null;
                    if (!layersDict.ContainsKey(name)) return null;
                    var v = layersDict[name];
                    if (v == null) return null;
                    int iz;
                    if (v is int) return (int)v;
                    if (int.TryParse(v.ToString(), out iz)) return iz;
                    return null;
                };

                Action<IEnumerable<UIControl>> apply = controls =>
                {
                    foreach (var c in controls)
                    {
                        var z = getZ(c.Name);
                        if (z.HasValue) c.ZIndex = z.Value;
                    }
                };
                apply(dialog.ImagePictures);
                apply(dialog.Scrolls);
                apply(dialog.Edits);
                apply(dialog.StillImageButtons);
                apply(dialog.RadioButtons);
                apply(dialog.Labels);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}


