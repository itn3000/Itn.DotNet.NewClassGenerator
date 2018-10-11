using System;
using Xunit;
using McMaster.Extensions.CommandLineUtils;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Itn.DotNet.NewClassGenerator.Test
{
    class MemoryStandardIOFactory : IStandardIOFactory
    {
        Stream m_Stdout;
        Stream m_Stdin;
        public MemoryStandardIOFactory(Stream stdout, Stream stdin)
        {
            m_Stdout = stdout;
            m_Stdin = stdin;
        }
        public Stream OpenStandardInput()
        {
            return m_Stdin;
        }

        public Stream OpenStandardOutput()
        {
            return m_Stdout;
        }
    }
    public class TestGeneratorApp
    {
        public TestGeneratorApp()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        [Fact]
        public void BasicUsageTest()
        {
            using (var stdout = new MemoryStream())
            {
                var iofactory = new MemoryStandardIOFactory(stdout, null);
                var services = new ServiceCollection();
                services.AddSingleton<IStandardIOFactory, MemoryStandardIOFactory>((provider) => iofactory);
                var app = new CommandLineApplication<GeneratorApp>();
                app.Conventions.UseDefaultConventions()
                    .UseConstructorInjection(services.BuildServiceProvider());
                var args = new string[] { "TestClass", "-n", "TestNamespace" };
                var rc = app.Execute(args);
                Assert.Equal(0, rc);
                stdout.Flush();
                var resultString = Encoding.UTF8.GetString(stdout.ToArray());
                Assert.Equal(GeneratorApp.DefaultTemplate.Replace("@Model.ClassName", "TestClass")
                    .Replace("@Model.Namespace", "TestNamespace"), resultString);
            }
        }
        [Fact]
        public void AdditionalPropertiesTest()
        {
            var template = @"@Model.ClassName, @Model.Namespace, @Model.Properties[""A""]";
            using(var stdout = new MemoryStream())
            using(var stdin = new MemoryStream(Encoding.UTF8.GetBytes(template)))
            {
                var iofactory = new MemoryStandardIOFactory(stdout, stdin);
                var services = new ServiceCollection();
                services.AddSingleton<IStandardIOFactory, MemoryStandardIOFactory>((provider) => iofactory);
                var app = new CommandLineApplication<GeneratorApp>();
                app.Conventions.UseDefaultConventions()
                    .UseConstructorInjection(services.BuildServiceProvider());
                var args = new string[]{ "A", "-n", "B", "-a", "A=C", "-t", "-" };
                var rc = app.Execute(args);
                Assert.Equal(0, rc);
                stdout.Flush();
                var resultString = Encoding.UTF8.GetString(stdout.ToArray());
                Assert.Equal("A, B, C", resultString);
            }
        }
        [Fact]
        public void EncodingTest()
        {
            // 0x3042 = 'あ'
            var template = string.Join(',', new string((char)0x3042, 4), "@Model.ClassName");
            // shift-jis
            var enc = Encoding.GetEncoding(932);
            var templateBytes = enc.GetBytes(template);
            using(var stdout = new MemoryStream())
            using(var stdin = new MemoryStream(templateBytes))
            {
                var iofactory = new MemoryStandardIOFactory(stdout, stdin);
                var services = new ServiceCollection();
                services.AddSingleton<IStandardIOFactory, MemoryStandardIOFactory>((provider) => iofactory);
                var app = new CommandLineApplication<GeneratorApp>();
                app.Conventions.UseDefaultConventions()
                    .UseConstructorInjection(services.BuildServiceProvider());
                var args = new string[]{ "A", "-e", "shift_jis", "-t", "-" };
                var rc = app.Execute(args);
                Assert.Equal(0, rc);
                stdout.Flush();
                var resultString = enc.GetString(stdout.ToArray());
                Assert.Equal(template.Replace("@Model.ClassName", "A"), resultString);
            }
        }
    }
}
