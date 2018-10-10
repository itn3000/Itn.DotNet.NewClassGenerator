using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using RazorLight;
using RazorLight.Caching;


namespace Itn.DotNet.NewClassGenerator
{
    [HelpOption(Description = "generating C# class file from razor template")]
    [VersionOption("0.1.0")]
    class GeneratorApp
    {
        [Option("-t|--template <TEMPLATEFILE>", @"razor template file, '-' means get from standard input, default is following:
-----------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace @Model.Namespace
{
    public class @Model.ClassName
    {
    }
}
-----------------
", CommandOptionType.SingleValue)]
        public string TemplateFile { get; }
        [Option("-a|--properties <NAME>=<VALUE>", "additional properties for razor(can be accessed by @Model.Properties[<NAME>])", CommandOptionType.MultipleValue)]
        public string[] AdditionalProperties { get; }
        [Option("-n|--namespace <NAMESPACE>", "class namespace(default: current directory name)", CommandOptionType.SingleValue)]
        public string Namespace { get; }
        [Option("-o|--output <OUTPUTFILEPATH>", "path to output file(default: standard output)", CommandOptionType.SingleValue)]
        public string OutputFileName { get; }
        [Option("-e|--encoding <ENCODING>", "file content encoding(default: UTF-8)", CommandOptionType.SingleValue)]
        public string FileEncoding { get; }
        [Argument(1, "name of class(required)")]
        public string ClassName { get; }
        public GeneratorApp(IConsole console)
        {
            m_Console = console;
        }
        IConsole m_Console { get; }
        const string DefaultTemplate = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace @Model.Namespace
{
    public class @Model.ClassName
    {
    }
}
";
        Dictionary<string, string> CreateAdditionalProperties()
        {
            if(AdditionalProperties == null)
            {
                return new Dictionary<string, string>();
            }
            return AdditionalProperties.Select(kvString => kvString.Split('='))
                .Where(x => x != null && x.Length >= 2)
                .ToDictionary(x => x[0], x => x[1])
                ;
        }
        async Task InternalExecute(string outputFilePath)
        {
            var eng = new RazorLightEngineBuilder()
                .AddDefaultNamespaces("System")
                .UseCachingProvider(new MemoryCachingProvider())
                .Build();
            var ns = Namespace ?? Path.GetFileName(Directory.GetCurrentDirectory());
            var result = await eng.CompileRenderAsync("NewClassGenerator",
                GetTemplate(),
                new { ClassName, Namespace = ns, Properties = CreateAdditionalProperties() }
                );
            using(var ostm = CreateOutputStream(outputFilePath))
            {
                var bytes = GetFileEncoding().GetBytes(result);
                await ostm.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }
        Encoding GetFileEncoding()
        {
            if(string.IsNullOrEmpty(FileEncoding))
            {
                return Encoding.UTF8;
            }
            else
            {
                return Encoding.GetEncoding(FileEncoding);
            }
        }
        string GetTemplate()
        {
            if(string.IsNullOrEmpty(TemplateFile))
            {
                return DefaultTemplate;
            }
            else if(TemplateFile == "-")
            {
                using(var istm = Console.OpenStandardInput())
                {
                    Span<byte> buf = stackalloc byte[256];
                    var inputBytes = new byte[1024];
                    int offset = 0;
                    while(true)
                    {
                        var bytesread = istm.Read(buf);
                        if(bytesread <= 0)
                        {
                            break;
                        }
                        if(inputBytes.Length < offset + bytesread)
                        {
                            var tmp = new byte[inputBytes.Length * 2];
                            inputBytes.AsSpan(0, offset).CopyTo(tmp.AsSpan());
                            inputBytes = tmp;
                        }
                        buf.Slice(0, bytesread).CopyTo(inputBytes.AsSpan(offset));
                        offset += bytesread;
                    }
                    return GetFileEncoding().GetString(inputBytes, 0, offset);
                }
            }
            else
            {
                return File.ReadAllText(TemplateFile, GetFileEncoding());
            }
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
        public async Task<int> OnExecuteAsync()
        {
            if (string.IsNullOrEmpty(ClassName))
            {
                m_Console.Error.WriteLine($"you must specify class name");
                return (int)ErrorCodes.MissingClassName;
            }

            await InternalExecute(OutputFileName).ConfigureAwait(false);

            return (int)ErrorCodes.Ok;
        }
    }
}