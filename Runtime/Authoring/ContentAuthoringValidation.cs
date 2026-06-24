using System;
using System.Collections.Generic;

namespace Deucarian.Attacks.Authoring
{
    public enum ContentAuthoringValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct ContentAuthoringValidationIssue
    {
        public ContentAuthoringValidationIssue(ContentAuthoringValidationSeverity severity, string message, string path)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Path = path ?? string.Empty;
        }

        public ContentAuthoringValidationSeverity Severity { get; }
        public string Message { get; }
        public string Path { get; }
        public bool IsError => Severity == ContentAuthoringValidationSeverity.Error;

        public static ContentAuthoringValidationIssue Error(string path, string message)
        {
            return new ContentAuthoringValidationIssue(ContentAuthoringValidationSeverity.Error, message, path);
        }

        public static ContentAuthoringValidationIssue Warning(string path, string message)
        {
            return new ContentAuthoringValidationIssue(ContentAuthoringValidationSeverity.Warning, message, path);
        }
    }

    public sealed class ContentAuthoringValidationReport
    {
        private readonly ContentAuthoringValidationIssue[] _issues;

        public ContentAuthoringValidationReport(IReadOnlyList<ContentAuthoringValidationIssue> issues)
        {
            _issues = Copy(issues);
        }

        public IReadOnlyList<ContentAuthoringValidationIssue> Issues => _issues;

        public bool IsValid
        {
            get
            {
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].IsError)
                        return false;
                return true;
            }
        }

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].IsError)
                        count++;
                return count;
            }
        }

        public int WarningCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _issues.Length; i++)
                    if (_issues[i].Severity == ContentAuthoringValidationSeverity.Warning)
                        count++;
                return count;
            }
        }

        private static ContentAuthoringValidationIssue[] Copy(IReadOnlyList<ContentAuthoringValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0) return Array.Empty<ContentAuthoringValidationIssue>();
            var copy = new ContentAuthoringValidationIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++) copy[i] = issues[i];
            return copy;
        }
    }
}
