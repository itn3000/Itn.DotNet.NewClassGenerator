using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
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
        MissingClassName = 2
    }
    class Program
    {
        static CommandLineApplication<GeneratorApp> CreateApplication()
        {
            var services = new ServiceCollection();
            services.AddTransient<IStandardIOFactory, ConsoleStandardIOFactory>();
            var app = new CommandLineApplication<GeneratorApp>();
            app.Conventions.UseDefaultConventions()
                .UseConstructorInjection(services.BuildServiceProvider())
                ;
            return app;
        }
        static void Main(string[] args)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
