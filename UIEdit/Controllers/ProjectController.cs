using Ookii.Dialogs.Wpf;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UIEdit.Models;

// By: SpinxDev 2025 xD
namespace UIEdit.Controllers
{
    public class ProjectController
    {
        #region Injection Properties
        public string InterfacesPath { get; set; }
        public string SurfacesPath { get; set; }
        public List<SourceFile> Files { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Set default paths from settings interfaces
        /// </summary>
        public void SetLocationInterfaces()
        {
            var dlg = new VistaFolderBrowserDialog();
            var showDialog = dlg.ShowDialog();
            if (showDialog == null || !((bool)showDialog)) return;
            LoadInterfacesFromPath(dlg.SelectedPath);
            try { UIEdit.Properties.Settings.Default.LastInterfacesPath = InterfacesPath; UIEdit.Properties.Settings.Default.Save(); } catch { }
        }

        /// <summary>
        /// Set default paths from settings surfaces
        /// </summary>
        public void SetLocationSurfaces()
        {
            var dlg = new VistaFolderBrowserDialog();
            var showDialog = dlg.ShowDialog();
            if (showDialog == null || !((bool)showDialog)) return;
            LoadSurfacesFromPath(dlg.SelectedPath);
            try { UIEdit.Properties.Settings.Default.LastSurfacesPath = SurfacesPath; UIEdit.Properties.Settings.Default.Save(); } catch { }
        }

        /// <summary>
        /// Load all XML files from the specified path into the Files list
        /// </summary>
        /// <param name="path"></param>
        public void LoadInterfacesFromPath(string path)
        {
            InterfacesPath = path;
            Files = new List<SourceFile>();
            foreach (var file in Directory.GetFiles(InterfacesPath, "*.xml", SearchOption.AllDirectories))
            {
                Files.Add(new SourceFile { FileName = file, ProjectPath = InterfacesPath });
            }
            try { UIEdit.Properties.Settings.Default.LastInterfacesPath = InterfacesPath; UIEdit.Properties.Settings.Default.Save(); } catch { }
        }

        /// <summary>
        /// Load all XML files from the specified path into the Files list
        /// </summary>
        /// <param name="path"></param>
        public void LoadSurfacesFromPath(string path)
        {
            SurfacesPath = path;
            try { UIEdit.Properties.Settings.Default.LastSurfacesPath = SurfacesPath; UIEdit.Properties.Settings.Default.Save(); } catch { }
        }

        /// <summary>
        /// Reads the content of the specified source file, detects its encoding, and returns the content as a string.
        /// </summary>
        /// <param name="currentSourceFile"></param>
        /// <returns></returns>
        public string GetSourceFileContent(SourceFile currentSourceFile)
        {
            bool hasBom;
            var encoding = DetectEncoding(currentSourceFile.FileName, out hasBom);
            currentSourceFile.PrefixExists = hasBom;
            currentSourceFile.EncodingName = encoding.WebName;

            var bytes = File.ReadAllBytes(currentSourceFile.FileName);
            var bomLen = GetBomLength(bytes);
            var text = encoding.GetString(bytes, bomLen, bytes.Length - bomLen);
            return SafeXml(text);
        }

        /// <summary>
        /// Saves the provided text content to the specified source file, using the appropriate encoding and BOM settings.
        /// </summary>
        /// <param name="currentSourceFile"></param>
        /// <param name="text"></param>
        public void SaveSourceFileContent(SourceFile currentSourceFile, string text)
        {
            var declared = ExtractDeclaredEncoding(text);
            var encodingToWrite = BuildEncodingForSave(currentSourceFile, declared);
            var raw = UnsafeXml(text);
            raw = NormalizeIndentation(raw, 4);
            using (var fs = new FileStream(currentSourceFile.FileName, FileMode.Create, FileAccess.Write))
            {
                var preamble = encodingToWrite.GetPreamble();
                if (preamble != null && preamble.Length > 0) fs.Write(preamble, 0, preamble.Length);
                var data = encodingToWrite.GetBytes(raw);
                fs.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Builds the appropriate Encoding object for saving the file, based on the declared encoding in the XML and the original file's encoding and BOM settings.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="declaredEncoding"></param>
        /// <returns></returns>
        private Encoding BuildEncodingForSave(SourceFile file, string declaredEncoding)
        {
            var web = (declaredEncoding ?? file.EncodingName ?? "utf-8").ToLowerInvariant();
            var withBom = file.PrefixExists;
            if (web == "unicode") web = "utf-16";
            if (web == "utf-16" || web == "utf-16le" || web == "unicode")
            {
                return new UnicodeEncoding(false, true);
            }
            if (web == "utf-16be")
            {
                return new UnicodeEncoding(true, true);
            }
            if (web == "utf-8")
            {
                return new UTF8Encoding(withBom);
            }
            try { return Encoding.GetEncoding(web); } catch { return new UTF8Encoding(false); }
        }

        /// <summary>
        /// Determines the length of the Byte Order Mark (BOM) in the given byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static int GetBomLength(byte[] bytes)
        {
            // This is a fucking AI code snippet, don't judge it too hard
            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0xFF && bytes[1] == 0xFE) return 2;
                if (bytes[0] == 0xFE && bytes[1] == 0xFF) return 2;
            }
            if (bytes.Length >= 3)
            {
                if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return 3;
            }
            if (bytes.Length >= 4)
            {
                if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) return 4;
                if (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) return 4;
            }
            return 0;
        }

        /// <summary>
        /// Detects the encoding of the file at the specified path by examining its BOM and XML declaration.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="hasBom"></param>
        /// <returns></returns>
        private static Encoding DetectEncoding(string path, out bool hasBom)
        {
            var bytes = File.ReadAllBytes(path);
            var bomLen = GetBomLength(bytes);
            hasBom = bomLen > 0;
            if (bomLen == 2)
            {
                if (bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode;
                if (bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode;
            }
            if (bomLen == 3) return Encoding.UTF8;
            if (bomLen == 4)
            {
                if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) return new UTF32Encoding(true, true);
                if (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) return new UTF32Encoding(false, true);
            }
            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0x3C && bytes[1] == 0x00) return Encoding.Unicode;
                if (bytes[0] == 0x00 && bytes[1] == 0x3C) return Encoding.BigEndianUnicode;
            }
            var ascii = Encoding.ASCII.GetString(bytes, 0, System.Math.Min(bytes.Length, 200));
            var m = Regex.Match(ascii, "encoding=\"(?<enc>[^\"]+)\"", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var encName = m.Groups["enc"].Value.ToLowerInvariant();
                if (encName == "unicode") return Encoding.Unicode;
                try { return Encoding.GetEncoding(encName); } catch { }
            }
            return Encoding.Unicode;
        }

        /// <summary>
        /// Extracts the declared encoding from the XML declaration in the provided XML text.
        /// </summary>
        /// <param name="xmlText"></param>
        /// <returns></returns>
        private static string ExtractDeclaredEncoding(string xmlText)
        {
            var m = Regex.Match(xmlText ?? "", @"^\uFEFF?\s*<\?xml[^>]*encoding=""(?<enc>[^""]+)""", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["enc"].Value;
            return null;
        }

        /// <summary>
        /// Replaces angle brackets within quoted strings in the XML with placeholder tokens to prevent parsing issues.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private string SafeXml(string xml)
        {
            foreach (var match in Regex.Matches(xml, '"' + @"\S+<\S+" + '"', RegexOptions.IgnoreCase))
            {
                var safeString = match.ToString().Replace("<", "###LeftArrow###").Replace(">", "###RightArrow###");
                xml = xml.Replace(match.ToString(), safeString);
            }
            return xml;
        }

        /// <summary>
        /// Restores angle brackets in the XML by replacing placeholder tokens with actual '<' and '>' characters.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private string UnsafeXml(string xml)
        {
            return xml.Replace("###LeftArrow###", "<").Replace("###RightArrow###", ">");
        }

        /// <summary>
        /// Normalizes indentation in the XML by replacing tabs with spaces and standardizing line endings.
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="spacesPerIndent"></param>
        /// <returns></returns>
        private static string NormalizeIndentation(string xml, int spacesPerIndent)
        {
            var spaces = new string(' ', spacesPerIndent);
            xml = xml.Replace("\t", spaces);
            xml = xml.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            return xml;
        }
        #endregion
    }
}
