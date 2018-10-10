using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Buildalyzer;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Itn.DotNet.NewClassGenerator
{
    enum ErrorCodes
    {
        Ok = 0,
        Exception = 1,
        MissingProjectFile = 2,
        MissingClassName = 3
    }
    [HelpOption]
    class CreateNewCommand
    {
        [Required]
        [Option("-t|--template <TEMPLATEFILE>", "razor template file(required)", CommandOptionType.SingleValue)]
        public string TemplateFile { get; }
        [Option("-a|--properties <NAME>=<VALUE>", "additional properties for razor(can be accessed by @Model.Properties[<NAME>]", CommandOptionType.MultipleValue)]
        public string[] AdditionalProperties { get; }
        [Option("-n|--namespace <NAMESPACE>", "class namespace(default: auto detected)", CommandOptionType.SingleValue)]
        public string Namespace { get; }
        [Option("-u|--using <USING>", "',' separated list of using(default: System)", CommandOptionType.MultipleValue)]
        public string[] Usings { get; }
        [Option("-p|--project <CSPROJ>", "path to project file(default: auto detected(*.csproj))", CommandOptionType.SingleValue)]
        public string ProjectFile { get; }
        [Option("-o|--output <OUTPUTFILEPATH>", "path to output file(default: standard output", CommandOptionType.SingleValue)]
        public string OutputFileName { get; }
        [Argument(1, "name of class(required)")]
        public string ClassName { get; }
        [Option("-e|--encoding <ENCODING>", "output encoding(default: UTF-8 with BOM)", CommandOptionType.SingleValue)]
        public string OutputEncoding { get; }
        public CreateNewCommand(IConsole console)
        {
            m_Console = console;
        }
        IConsole m_Console { get; }
        string FindProjectFilePath()
        {
            var curdir = Directory.GetCurrentDirectory();
            while (curdir != null)
            {
                var csprojList = Directory.GetFiles(curdir, "*.csproj");
                if (csprojList != null && csprojList.Length != 0)
                {
                    return Path.Combine(curdir, csprojList[0]);
                }
                var nextDir = Path.GetDirectoryName(curdir);
                if (nextDir == curdir)
                {
                    return null;
                }
            }
            throw new InvalidOperationException();
        }
        string TemplateCs { get; } = @"
namespace TemplateNamespace
{
    public class Class1
    {

    }
}
";
        void InternalExecute(string projectFilePath, string outputFilePath)
        {
            string ns = Namespace;
            if (string.IsNullOrEmpty(Namespace))
            {
                string rootns;
                var relpath = Path.GetRelativePath(Path.GetDirectoryName(projectFilePath), Directory.GetCurrentDirectory());
                relpath = string.Join(".", relpath.Split(Path.DirectorySeparatorChar).Where(x => x != ".." && x != "."));
                var aopts = new AnalyzerManagerOptions();
                aopts.LogWriter = m_Console.Error;
                var analyzer = new AnalyzerManager();
                var proj = analyzer.GetProject(projectFilePath);
                var firstResult = proj.Build().Results.FirstOrDefault();
                rootns = firstResult.GetProperty("RootNamespace");
                if (string.IsNullOrEmpty(rootns))
                {
                    rootns = Path.GetFileNameWithoutExtension(firstResult.ProjectFilePath);
                }
                ns = string.Join(".", rootns, relpath);
            }
            var usingList = Usings;
            if (usingList == null || usingList.Length == 0)
            {
                usingList = new string[] { "System", "System.Text" };
            }
            usingList = usingList.SelectMany(x => x.Split(',').Select(y => y.Trim())).ToArray();
            var template = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(TemplateCs).GetRoot();
            var nsNodes = usingList.Select(us =>
            {
                m_Console.WriteLine($"usname = {us}");
                var usname = SyntaxFactory.IdentifierName(us);
                usname = usname.InsertTriviaAfter(usname.GetLeadingTrivia().First(), new SyntaxTrivia[] { SyntaxFactory.Space });
                var usdirective = SyntaxFactory.UsingDirective(usname);
                m_Console.WriteLine($"usdirective1 = {usdirective}");
                usdirective = usdirective.InsertTriviaAfter(usdirective.GetTrailingTrivia().Last(), new SyntaxTrivia[] { SyntaxFactory.EndOfLine(Environment.NewLine) });
                m_Console.WriteLine($"usdirective3 = {usdirective}");
                return usdirective;
            }).ToArray();
            template = template.AddUsings(nsNodes);
            var classes = template.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(x => x.Identifier.ToString() == "Class1");
            var newclass = classes.ReplaceToken(classes.Identifier, SyntaxFactory.Identifier(ClassName));
            template = template.ReplaceNode(classes, classes.WithIdentifier(SyntaxFactory.Identifier(ClassName).WithTrailingTrivia(classes.Identifier.TrailingTrivia)));
            var oldns = template.DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault(x => x.Name.ToString() == "TemplateNamespace");
            template = template.ReplaceNode(oldns,
                oldns.WithName(SyntaxFactory.IdentifierName(ns)
                    .WithTrailingTrivia(oldns.Name.GetTrailingTrivia())
                ));
            using (var ostm = CreateOutputStream(outputFilePath))
            {
                var enc = !string.IsNullOrEmpty(OutputEncoding) ? Encoding.GetEncoding(OutputEncoding) : Encoding.UTF8;
                var bytes = enc.GetBytes(template.ToString());
                if (enc.Preamble.Length != 0)
                {
                    m_Console.Error.WriteLine($"output preamble");
                    ostm.Write(enc.Preamble);
                }
                ostm.Write(bytes, 0, bytes.Length);
            }
            // var firstDirective = template.DescendantNodes().First();
            // // firstDirective.InsertNodesBefore(firstDirective.DescendantNodes().First(), nsNodes);
            // template = template.InsertNodesAfter(firstDirective, nsNodes);

        }
        Stream CreateOutputStream(string outputFilePath)
        {
            if (string.IsNullOrEmpty(outputFilePath))
            {
                return Console.OpenStandardOutput();
            }
            else
            {
                return File.Create(outputFilePath);
            }
        }
        public int OnExecute()
        {
            string projectFilePath = ProjectFile;
            if (string.IsNullOrEmpty(ProjectFile))
            {
                projectFilePath = FindProjectFilePath();
            }
            if (string.IsNullOrEmpty(projectFilePath))
            {
                m_Console.Error.WriteLine($"cannot find project file");
                return (int)ErrorCodes.MissingProjectFile;
            }
            if (string.IsNullOrEmpty(ClassName))
            {
                m_Console.Error.WriteLine($"you must specify class name");
                return (int)ErrorCodes.MissingClassName;
            }

            InternalExecute(projectFilePath, OutputFileName);

            return (int)ErrorCodes.Ok;
        }
    }
    class Program
    {
        static CommandLineApplication<CreateNewCommand> CreateApplication()
        {
            var app = new CommandLineApplication<CreateNewCommand>();
            app.Conventions.UseDefaultConventions();
            return app;
        }
        static void Main(string[] args)
        {
            try
            {
                var app = CreateApplication();
                var rc = app.Execute(args);
                Environment.ExitCode = rc;
                return;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"exception: {e}");
                Environment.ExitCode = (int)ErrorCodes.Exception;
            }
        }
    }
}
