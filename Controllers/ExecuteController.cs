using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CodeEditorBackend.Controllers
{
    public class CodeRequest
    {
        public string? SourceCode { get; set; }
        public string? Language { get; set; }
    }

    public class CodeResponse
    {
        public string? Output { get; set; }
        public bool Error { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ExecuteController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Execute([FromBody] CodeRequest request)
        {
            if (string.IsNullOrEmpty(request.SourceCode) || string.IsNullOrEmpty(request.Language))
            {
                return BadRequest("Source code and language are required.");
            }

            string? tempFile = null;
            string? outputFile = null;
            string? csprojFile = null;
            ProcessStartInfo? processInfo = null;
            Process? process = null;
            string? output = null;
            string? error = null;

            try
            {
                switch (request.Language.ToLower())
                {
                    case "javascript":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.js");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "node",
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "typescript":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ts");
                        string jsFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.js");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "tsc",
                            Arguments = $"\"{tempFile}\" --outFile \"{jsFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        using (var compileProcess = new Process { StartInfo = processInfo })
                        {
                            compileProcess.Start();
                            var compileOutput = await compileProcess.StandardOutput.ReadToEndAsync();
                            var compileError = await compileProcess.StandardError.ReadToEndAsync();
                            await compileProcess.WaitForExitAsync();
                            if (compileProcess.ExitCode != 0)
                            {
                                return Ok(new CodeResponse { Output = $"Compilation failed: {compileError}\nOutput: {compileOutput}", Error = true });
                            }
                        }
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "node",
                            Arguments = $"\"{jsFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        outputFile = jsFile;
                        break;

                    case "python":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.py");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "python",
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "java":
                        string? className = ExtractJavaClassName(request.SourceCode) ?? "Main";
                        tempFile = Path.Combine(Path.GetTempPath(), $"{className}.java");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        outputFile = Path.Combine(Path.GetTempPath(), className);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "javac",
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        using (var compileProcess = new Process { StartInfo = processInfo })
                        {
                            compileProcess.Start();
                            var compileOutput = await compileProcess.StandardOutput.ReadToEndAsync();
                            var compileError = await compileProcess.StandardError.ReadToEndAsync();
                            await compileProcess.WaitForExitAsync();
                            if (compileProcess.ExitCode != 0)
                            {
                                return Ok(new CodeResponse { Output = $"Compilation failed: {compileError}\nOutput: {compileOutput}", Error = true });
                            }
                        }
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "java",
                            Arguments = $"-cp . {className}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "c":
                    case "cpp":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{(request.Language.ToLower() == "c" ? "c" : "cpp")}");
                        outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.exe");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = request.Language.ToLower() == "c" ? "gcc" : "g++",
                            Arguments = $"\"{tempFile}\" -o \"{outputFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        using (var compileProcess = new Process { StartInfo = processInfo })
                        {
                            compileProcess.Start();
                            var compileOutput = await compileProcess.StandardOutput.ReadToEndAsync();
                            var compileError = await compileProcess.StandardError.ReadToEndAsync();
                            await compileProcess.WaitForExitAsync();
                            if (compileProcess.ExitCode != 0)
                            {
                                return Ok(new CodeResponse { Output = $"Compilation failed: {compileError}\nOutput: {compileOutput}", Error = true });
                            }
                        }
                        processInfo = new ProcessStartInfo
                        {
                            FileName = outputFile,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "csharp":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cs");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
                          <PropertyGroup>
                            <OutputType>Exe</OutputType>
                            <TargetFramework>net9.0</TargetFramework>
                            <AssemblyName>Program</AssemblyName>
                            <OutputPath>bin</OutputPath>
                          </PropertyGroup>
                        </Project>";
                        csprojFile = Path.Combine(Path.GetTempPath(), "TempProject.csproj");
                        await System.IO.File.WriteAllTextAsync(csprojFile, csprojContent);
                        // Step 1: Build the project
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"build \"{csprojFile}\" --no-restore -c Release",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        string buildOutputLog = "";
                        using (var buildProcess = new Process { StartInfo = processInfo })
                        {
                            buildProcess.Start();
                            var buildOutput = await buildProcess.StandardOutput.ReadToEndAsync();
                            var buildError = await buildProcess.StandardError.ReadToEndAsync();
                            await buildProcess.WaitForExitAsync();
                            buildOutputLog = $"Build Output: {buildOutput}\nBuild Error: {buildError}";
                            if (buildProcess.ExitCode != 0)
                            {
                                return Ok(new CodeResponse { Output = $"Build failed: {buildError}\nOutput: {buildOutput}", Error = true });
                            }
                        }
                        // Step 2: Execute the compiled DLL
                        string dllPath = Path.Combine(Path.GetTempPath(), "bin", "net9.0", "Program.dll");
                        if (!System.IO.File.Exists(dllPath))
                        {
                            return Ok(new CodeResponse { Output = $"DLL not found at {dllPath}. Build log: {buildOutputLog}", Error = true });
                        }
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"\"{dllPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        using (var runProcess = new Process { StartInfo = processInfo })
                        {
                            runProcess.Start();
                            var runOutput = await runProcess.StandardOutput.ReadToEndAsync();
                            var runError = await runProcess.StandardError.ReadToEndAsync();
                            await runProcess.WaitForExitAsync();
                            if (runProcess.ExitCode != 0)
                            {
                                return Ok(new CodeResponse { Output = $"Execution failed: {runError}\nOutput: {runOutput}", Error = true });
                            }
                            output = runOutput;
                            error = runError;
                        }
                        outputFile = dllPath; // For cleanup
                        break;

                    case "php":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.php");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "php",
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "ruby":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.rb");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "ruby",
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "go":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.go");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "go",
                            Arguments = $"run \"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    case "rust":
                        tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.rs");
                        outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.exe");
                        await System.IO.File.WriteAllTextAsync(tempFile, request.SourceCode);
                        processInfo = new ProcessStartInfo
                        {
                            FileName = "rustc",
                            Arguments = $"\"{tempFile}\" -o \"{outputFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        using (var compileProcess = new Process { StartInfo = processInfo })
                        {
                            compileProcess.Start();
                            var compileOutput = await compileProcess.StandardOutput.ReadToEndAsync();
                            var compileError = await compileProcess.StandardError.ReadToEndAsync();
                            await compileProcess.WaitForExitAsync();
                            if (compileProcess.ExitCode != 0)
                            {
                                return Ok(new CodeResponse { Output = $"Compilation failed: {compileError}\nOutput: {compileOutput}", Error = true });
                            }
                        }
                        processInfo = new ProcessStartInfo
                        {
                            FileName = outputFile,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
                        break;

                    default:
                        return BadRequest("Unsupported language.");
                }

                using (process = new Process { StartInfo = processInfo! })
                {
                    process.Start();
                    output = await process.StandardOutput.ReadToEndAsync();
                    error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                }

                return Ok(new CodeResponse
                {
                    Output = string.IsNullOrEmpty(error) ? output : error,
                    Error = !string.IsNullOrEmpty(error)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CodeResponse
                {
                    Output = $"Execution failed: {ex.Message}",
                    Error = true
                });
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                    process.Dispose();
                }

                if (tempFile != null)
                {
                    await TryDeleteFileWithRetryAsync(tempFile);
                }
                if (outputFile != null)
                {
                    // For C#, clean up the entire bin directory
                    if (request.Language.ToLower() == "csharp")
                    {
                        string binDir = Path.Combine(Path.GetTempPath(), "bin");
                        if (Directory.Exists(binDir))
                        {
                            try
                            {
                                Directory.Delete(binDir, true);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error deleting bin directory {binDir}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        await TryDeleteFileWithRetryAsync(outputFile);
                    }
                }
                if (request.Language.ToLower() == "csharp" && csprojFile != null)
                {
                    await TryDeleteFileWithRetryAsync(csprojFile);
                }
            }
        }

        private async Task TryDeleteFileWithRetryAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return;
            }

            const int maxRetries = 3;
            const int delayMs = 500;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    System.IO.File.Delete(filePath);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    if (i == maxRetries - 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete file {filePath} after {maxRetries} attempts.");
                        break;
                    }
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                    break;
                }
            }
        }

        private string? ExtractJavaClassName(string sourceCode)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sourceCode, @"public\s+class\s+(\w+)");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}