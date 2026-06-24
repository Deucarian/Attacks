using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed class ContentAssetCreationResult
    {
        public ContentAssetCreationResult(bool succeeded, string message, UnityEngine.Object createdRoot)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            CreatedRoot = createdRoot;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        public UnityEngine.Object CreatedRoot { get; }
    }

    internal static class GameContentAuthoringEditorPaths
    {
        public static string NormalizeAssetFolderPath(string path, string defaultRoot)
        {
            string normalized = string.IsNullOrWhiteSpace(path)
                ? defaultRoot
                : path.Trim().Replace("\\", "/");
            while (normalized.Contains("//")) normalized = normalized.Replace("//", "/");
            return normalized.TrimEnd('/');
        }

        public static bool IsValidAssetFolderPath(string path, string defaultRoot)
        {
            string normalized = NormalizeAssetFolderPath(path, defaultRoot);
            if (string.IsNullOrWhiteSpace(normalized)) return false;
            if (!string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase) && !normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return false;

            string[] parts = normalized.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrWhiteSpace(part) || part == "." || part == ".." || HasInvalidAssetPathChars(part))
                    return false;
            }

            return true;
        }

        public static string EnsureFolder(string folder, string defaultRoot)
        {
            folder = NormalizeAssetFolderPath(folder, defaultRoot);
            if (string.Equals(folder, "Assets", StringComparison.OrdinalIgnoreCase)) return "Assets";
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }

            return folder;
        }

        public static bool FolderContainsAssets(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return false;
            return AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length > 0;
        }

        public static string SanitizePathSegment(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            char[] chars = value.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                bool valid = char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.';
                chars[i] = valid ? c : '-';
            }

            return new string(chars);
        }

        private static bool HasInvalidAssetPathChars(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '<' || c == '>' || c == ':' || c == '"' || c == '|' || c == '?' || c == '*')
                    return true;
            }

            return false;
        }
    }

    internal static class GameContentAuthoringEditorAssets
    {
        public static bool ConfirmExistingFolder(string folder, string contentName)
        {
            return EditorUtility.DisplayDialog(
                "Use Existing " + contentName + " Folder?",
                "The folder already contains assets:\n\n" + folder + "\n\nCreate this " + contentName.ToLowerInvariant() + " root asset in that folder?",
                "Create Here",
                "Cancel");
        }

        public static bool HasDuplicateId<TAsset>(string id, Func<TAsset, string> getId) where TAsset : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(TAsset).Name);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset != null && string.Equals(getId(asset), id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void AddSubAsset(ScriptableObject subAsset, UnityEngine.Object root, string name)
        {
            if (subAsset == null) return;
            subAsset.name = name;
            string path = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrWhiteSpace(path))
                AssetDatabase.AddObjectToAsset(subAsset, root);
            else
                AssetDatabase.AddObjectToAsset(subAsset, path);
            EditorUtility.SetDirty(subAsset);
        }

        public static string[] SplitCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();
            string[] parts = csv.Split(',');
            var values = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string value = parts[i].Trim();
                if (!string.IsNullOrWhiteSpace(value)) values.Add(value);
            }

            return values.ToArray();
        }

        public static void DestroyTransientObject(UnityEngine.Object target)
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
        }

        public static void AddPathIssues(
            List<ContentAuthoringValidationIssue> issues,
            string outputRoot,
            string defaultRoot,
            string folder,
            string rootPath,
            string contentName,
            string pathLabel)
        {
            if (!GameContentAuthoringEditorPaths.IsValidAssetFolderPath(outputRoot, defaultRoot))
            {
                issues.Add(ContentAuthoringValidationIssue.Error(pathLabel, "Output root must be Assets or a folder below Assets, without empty or parent-directory segments."));
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rootPath) != null)
                issues.Add(ContentAuthoringValidationIssue.Error(pathLabel, "An asset already exists at " + rootPath + ". Rename the " + contentName.ToLowerInvariant() + " or edit the existing asset."));
            else if (AssetDatabase.IsValidFolder(folder) && GameContentAuthoringEditorPaths.FolderContainsAssets(folder))
                issues.Add(ContentAuthoringValidationIssue.Warning(pathLabel, "The " + contentName.ToLowerInvariant() + " folder already contains assets. Creation will ask for confirmation before adding this root asset."));
        }
    }
}
