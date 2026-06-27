using Blazing.Mvvm.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace ToolBox.Components.Pages;

public sealed class CodeRunnerVM : ViewModelBase
{
    private string _code = """
using System;
using System.Linq;

var numbers = Enumerable.Range(1, 5).ToArray();
Console.WriteLine(string.Join(", ", numbers));

numbers.Sum();
""";
    private string _output = string.Empty;
    private string? _errorMessage;
    private bool _isRunning;
    private bool _clearOutputBeforeRun = true;
    private string _referenceInput = "System.Text.Json";
    private string _referencePaths = string.Empty;
    private IReadOnlyList<string> _activeReferences = Array.Empty<string>();

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Output
    {
        get => _output;
        private set => SetProperty(ref _output, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public bool ClearOutputBeforeRun
    {
        get => _clearOutputBeforeRun;
        set => SetProperty(ref _clearOutputBeforeRun, value);
    }

    public string ReferenceInput
    {
        get => _referenceInput;
        set => SetProperty(ref _referenceInput, value);
    }

    public string ReferencePaths
    {
        get => _referencePaths;
        set => SetProperty(ref _referencePaths, value);
    }

    public IReadOnlyList<string> ActiveReferences
    {
        get => _activeReferences;
        private set => SetProperty(ref _activeReferences, value);
    }

    public async Task RunAsync()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        ErrorMessage = null;

        if (ClearOutputBeforeRun)
        {
            Output = string.Empty;
        }

        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var references = new List<Assembly>
            {
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(Console).Assembly,
                typeof(System.Text.Json.JsonSerializer).Assembly,
                typeof(CodeRunnerVM).Assembly
            };

            var referenceNames = ParseReferenceNames(ReferenceInput);
            foreach (var name in referenceNames)
            {
                var assembly = ResolveAssembly(name);
                if (assembly != null)
                {
                    references.Add(assembly);
                }
            }

            var referencePaths = ParseReferencePaths(ReferencePaths);
            foreach (var path in referencePaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    references.Add(Assembly.LoadFrom(path));
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Failed to load: {path}{Environment.NewLine}{ex.Message}";
                    return;
                }
            }

            ActiveReferences = references
                .Distinct()
                .Select(a => a.FullName ?? a.GetName().Name ?? a.ToString())
                .ToArray();

            var options = ScriptOptions.Default
                .WithImports(
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Text",
                    "System.Text.Json")
                .WithReferences(references);

            var result = await CSharpScript.EvaluateAsync(Code ?? string.Empty, options).ConfigureAwait(false);
            var consoleOutput = writer.ToString();
            var resultOutput = result is null ? string.Empty : result.ToString();

            if (!string.IsNullOrWhiteSpace(consoleOutput) && !string.IsNullOrWhiteSpace(resultOutput))
            {
                Output = $"{consoleOutput}Result: {resultOutput}";
            }
            else if (!string.IsNullOrWhiteSpace(consoleOutput))
            {
                Output = consoleOutput;
            }
            else if (!string.IsNullOrWhiteSpace(resultOutput))
            {
                Output = $"Result: {resultOutput}";
            }
            else
            {
                Output = "No output.";
            }
        }
        catch (CompilationErrorException ex)
        {
            ErrorMessage = string.Join(Environment.NewLine, ex.Diagnostics);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
            IsRunning = false;
        }
    }

    public void ClearOutput()
    {
        Output = string.Empty;
        ErrorMessage = null;
    }

    private static IReadOnlyList<string> ParseReferenceNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    private static IReadOnlyList<string> ParseReferencePaths(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
    }

    private static Assembly? ResolveAssembly(string name)
    {
        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch
        {
            return null;
        }
    }
}
