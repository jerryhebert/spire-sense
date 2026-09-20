using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: Sts2Decompiler <assembly.dll> <output-dir>");
    return 2;
}

var assemblyPath = Path.GetFullPath(args[0]);
var outputDir = Path.GetFullPath(args[1]);

using var module = new PEFile(assemblyPath);
var resolver = new UniversalAssemblyResolver(assemblyPath, throwOnError: false, module.Metadata.DetectTargetFrameworkId());
resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);

var settings = new DecompilerSettings(LanguageVersion.Latest)
{
    ThrowOnAssemblyResolveErrors = false,
    UseNestedDirectoriesForNamespaces = true,
    ShowXmlDocumentation = true,
};

Directory.CreateDirectory(outputDir);
var decompiler = new WholeProjectDecompiler(settings, resolver, projectWriter: null, assemblyReferenceClassifier: null, debugInfoProvider: null);
decompiler.DecompileProject(module, outputDir);

Console.WriteLine($"Decompiled {Path.GetFileName(assemblyPath)} -> {outputDir}");
return 0;
