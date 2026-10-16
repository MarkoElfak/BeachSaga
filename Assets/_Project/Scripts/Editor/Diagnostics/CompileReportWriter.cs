using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Writes every script compilation result (errors and warnings) to Logs/BeachTycoonCompile.log
    /// so compile health can be checked without opening the Console. Lives in its own assembly so it
    /// keeps working while gameplay assemblies have errors.
    /// </summary>
    [InitializeOnLoad]
    internal static class CompileReportWriter
    {
        private const string LogPath = "Logs/BeachTycoonCompile.log";
        private static readonly StringBuilder Buffer = new StringBuilder();
        private static int _errors;
        private static int _warnings;

        static CompileReportWriter()
        {
            CompilationPipeline.compilationStarted -= OnStarted;
            CompilationPipeline.compilationStarted += OnStarted;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyFinished;
            CompilationPipeline.compilationFinished -= OnFinished;
            CompilationPipeline.compilationFinished += OnFinished;
        }

        private static void OnStarted(object context)
        {
            Buffer.Clear();
            _errors = 0;
            _warnings = 0;
            Buffer.AppendLine("Compilation started " + DateTime.UtcNow.ToString("u"));
        }

        private static void OnAssemblyFinished(string assemblyPath, CompilerMessage[] messages)
        {
            foreach (CompilerMessage message in messages)
            {
                if (message.type == CompilerMessageType.Error) _errors++;
                else if (message.type == CompilerMessageType.Warning) _warnings++;
                else continue;
                Buffer.Append(message.type).Append(" | ").Append(Path.GetFileName(assemblyPath))
                    .Append(" | ").Append(message.file).Append(':').Append(message.line)
                    .Append(" | ").AppendLine(message.message);
            }
        }

        private static void OnFinished(object context)
        {
            Buffer.Append("Compilation finished ").Append(DateTime.UtcNow.ToString("u"))
                .Append(" errors=").Append(_errors).Append(" warnings=").Append(_warnings).AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath, Buffer.ToString());
        }
    }
}
