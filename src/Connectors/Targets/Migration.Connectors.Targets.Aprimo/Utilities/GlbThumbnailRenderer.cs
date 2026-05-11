using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Utilities
{

    public static class GlbThumbnailRenderer
    {
        public sealed class BlenderRenderException : Exception
        {
            public string TempDir { get; }
            public string CommandLine { get; }
            public int ExitCode { get; }
            public string StdOut { get; }
            public string StdErr { get; }

            public BlenderRenderException(string message, string tempDir, string commandLine, int exitCode, string stdout, string stderr)
                : base(message)
            {
                TempDir = tempDir;
                CommandLine = commandLine;
                ExitCode = exitCode;
                StdOut = stdout;
                StdErr = stderr;
            }
        }

        public static async Task<byte[]> RenderGlbThumbnailAsync(
            Stream glbStream,
            string blenderExePath,
            string blenderScriptPath,
            int size = 2000,
            CancellationToken ct = default)
        {
            if (glbStream == null) throw new ArgumentNullException(nameof(glbStream));

            if (string.IsNullOrWhiteSpace(blenderExePath))
                throw new ArgumentNullException(nameof(blenderExePath));
            if (!File.Exists(blenderExePath))
                throw new FileNotFoundException("Blender executable not found.", blenderExePath);

            if (string.IsNullOrWhiteSpace(blenderScriptPath))
                throw new ArgumentNullException(nameof(blenderScriptPath));
            if (!File.Exists(blenderScriptPath))
                throw new FileNotFoundException("Blender python script not found.", blenderScriptPath);

            var tempDir = Path.Combine(Path.GetTempPath(), "glbthumb_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var glbPath = Path.Combine(tempDir, "model.glb");
            var pngPath = Path.Combine(tempDir, "thumb.png");

            // Write GLB to disk
            await using (var fs = File.Create(glbPath))
            {
                if (glbStream.CanSeek) glbStream.Position = 0;
                await glbStream.CopyToAsync(fs, 81920, ct);
            }

            // IMPORTANT: Use absolute paths
            blenderExePath = Path.GetFullPath(blenderExePath);
            blenderScriptPath = Path.GetFullPath(blenderScriptPath);

            // Build args (keep quoting correct)
            var args = $"--background --factory-startup --python \"{blenderScriptPath}\" -- \"{glbPath}\" \"{pngPath}\" {size}";
            var commandLine = $"\"{blenderExePath}\" {args}";

            var psi = new ProcessStartInfo
            {
                FileName = blenderExePath,
                Arguments = args,
                WorkingDirectory = tempDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Read output fully
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = process.ExitCode;

            // If thumbnail wasn't created, throw with diagnostics + keep temp dir
            if (exitCode != 0 || !File.Exists(pngPath))
            {
                throw new BlenderRenderException(
                    message: "Blender did not generate thumbnail.",
                    tempDir: tempDir,
                    commandLine: commandLine,
                    exitCode: exitCode,
                    stdout: stdout,
                    stderr: stderr);
            }

            var bytes = await File.ReadAllBytesAsync(pngPath, ct);

            // Cleanup on success
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore */ }

            return bytes;
        }
    }

}
