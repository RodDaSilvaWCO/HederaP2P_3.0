using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WorldComputer.Simulator
{
    static public class AssemblyGenerator
    {

        public static (byte[], CSharpCompilation, SyntaxTree) GenerateAssembly(
                                            bool generateAssembly,
                                            OutputKind outputKind,
                                            bool isReleaseMode,
                                            LanguageVersion languageVersion,
                                            List<MetadataReference>? compilerRefFiles,
                                            bool allowUnsafeCode,
                                            string sourceCode,
                                            string assemblyName,
                                            string snKeyFile,
                                            string preprocessorSymbols,
                                            IEnumerable<ResourceDescription> manifestResources,
                                            MetadataReference[] dynamicReferences)
        {
            byte[] assemblyBuffer = null!;
            CSharpCompilation csCompilation = null!;
            SyntaxTree parsedSyntaxTree = null!;
            try
            {
                assemblyBuffer = GenDllAssembly(generateAssembly, outputKind, isReleaseMode, languageVersion, sourceCode, allowUnsafeCode,
                                    compilerRefFiles, snKeyFile, preprocessorSymbols, assemblyName,
                                    out csCompilation, out parsedSyntaxTree, manifestResources, dynamicReferences);
            }
            catch (Exception ex)
            {
                Debug.Print($"{ex.ToString()}");
            }
            return (assemblyBuffer, csCompilation, parsedSyntaxTree);
        }

        private static byte[] GenDllAssembly(bool generateAssembly, OutputKind outputKind, bool isReleaseMode, LanguageVersion languageVersion, string finalSourceCode,
                                        bool allowUnsafeCode, /*string */ List<MetadataReference>? compilerRefFiles,
                                        string snKeyFile, string preprocessorSymbols, string assemblyName,
                                        out CSharpCompilation csCompilation, out SyntaxTree parsedSyntaxTree,
                                        IEnumerable<ResourceDescription> manifestResources, MetadataReference[] dynamicReferences = null!)
        {
            byte[] assemblyBytes = null!;
            csCompilation = null!;
            parsedSyntaxTree = null!;
            try
            {
                // Compile the source code and 
                csCompilation = CompileCode(outputKind,
                                        (isReleaseMode ? OptimizationLevel.Release : OptimizationLevel.Debug),
                                        languageVersion,
                                        allowUnsafeCode,
                                        finalSourceCode, assemblyName, compilerRefFiles, snKeyFile, preprocessorSymbols,
                                        out parsedSyntaxTree, dynamicReferences);
                if (generateAssembly)
                {
                    // Generated the resultant assembly to a MemoryStream
                    using (var peStream = new MemoryStream())
                    {
                        using (Stream win32resStream = csCompilation.CreateDefaultWin32Resources(
                                                                            versionResource: true, // Important!
                                                                            noManifest: false,
                                                                            manifestContents: null,
                                                                            iconInIcoFormat: null))
                        {
                            EmitResult emitresult = null!;
                            if (manifestResources != null)
                            {
                                emitresult = csCompilation.Emit(
                                                             peStream: peStream,
                                                             win32Resources: win32resStream,
                                                             manifestResources: manifestResources);
                            }
                            else
                            {
                                emitresult = csCompilation.Emit(
                                peStream: peStream,
                                win32Resources: win32resStream);

                            }
                            if (!emitresult.Success)
                            {
                                var failures = emitresult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                                foreach (var diagnostic in failures)
                                {
                                    Debug.Print($"({diagnostic.Location.GetLineSpan().StartLinePosition},{diagnostic.Location.GetLineSpan().EndLinePosition}) {diagnostic.Id}: {diagnostic.GetMessage()}");
                                }
                            }
                            else
                            {
                                peStream.Seek(0, SeekOrigin.Begin);
                                assemblyBytes = peStream.ToArray();

                            }
                        }
                    }
                }
                return assemblyBytes;
            }
            catch (Exception ex)
            {
                Debug.Print($"{ex.ToString()}");
                return null!;
            }
        }

        private static CSharpCompilation CompileCode(OutputKind outputKind, OptimizationLevel optLvl, LanguageVersion languageVersion,
                                                bool allowUnSafeCode, string sourceCode, string assemblyName, /*string*/ List<MetadataReference>? compilerRefFiles,
                                                string snKeyFilePath, string preprocessorSymbols, out SyntaxTree parsedSyntaxTree, MetadataReference[] dynamicReferences = null!)
        {
            var codeString = SourceText.From(sourceCode);
            CSharpParseOptions options = null!;
            if (string.IsNullOrEmpty(preprocessorSymbols))
            {
                options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
            }
            else
            {
                options = CSharpParseOptions.Default.WithLanguageVersion(languageVersion).WithPreprocessorSymbols(preprocessorSymbols.Split(new char[] { ';' }));
            }
            parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
            MetadataReference[] references = new MetadataReference[(dynamicReferences == null ? 0 : dynamicReferences.Length)];
            //string[] compilerReferences = compilerReferences = new string[0]; 
            //string[] compilerReferences = new string[0];
            //if (!string.IsNullOrEmpty(compilerRefFiles))
            //{
            //compilerRefFiles = compilerRefFiles.Replace("|", "");
            //compilerReferences = compilerRefFiles.Split(new char[] { ';' });
            references = new MetadataReference[compilerRefFiles.Count + (dynamicReferences == null ? 0 : dynamicReferences.Length)];
            //}
            for (int i = 0; i < compilerRefFiles.Count; i++)
            {
                //references[i] = MetadataReference.CreateFromFile(compilerReferences[i]);
                references[i] = compilerRefFiles[i];
            }
            // Add any dynamic (i.e.; in memory) references that were passed in
            if (dynamicReferences != null)
            {
                for (int j = 0; j < dynamicReferences.Length; j++)
                {
                    references[compilerRefFiles.Count + j] = dynamicReferences[j];
                }
            }
            CSharpCompilationOptions compileOptions = null!;
            if (string.IsNullOrEmpty(snKeyFilePath))
            {
                compileOptions = new CSharpCompilationOptions(
                                        outputKind,
                                        optimizationLevel: optLvl,
                                        allowUnsafe: allowUnSafeCode,
                                        deterministic: true,
                                        strongNameProvider: new DesktopStrongNameProvider(),
                                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
            }
            else
            {
                compileOptions = new CSharpCompilationOptions(
                                        outputKind,
                                        optimizationLevel: optLvl,
                                        allowUnsafe: allowUnSafeCode,
                                        deterministic: true,
                                        strongNameProvider: new DesktopStrongNameProvider(),
                                        assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default)
                                        .WithCryptoKeyFile(snKeyFilePath);
            }
            return CSharpCompilation.Create(assemblyName,
                new[] { parsedSyntaxTree },
                references: references,
                options: compileOptions);
        }
    }
}
