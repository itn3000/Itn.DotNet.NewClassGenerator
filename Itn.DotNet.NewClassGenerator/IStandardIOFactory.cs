using System.IO;
using System;

namespace Itn.DotNet.NewClassGenerator
{
    interface IStandardIOFactory
    {
        Stream OpenStandardOutput();
        Stream OpenStandardInput();
    }
    public class ConsoleStandardIOFactory : IStandardIOFactory
    {
        public Stream OpenStandardInput()
        {
            return Console.OpenStandardInput();
        }

        public Stream OpenStandardOutput()
        {
            return Console.OpenStandardOutput();
        }
    }
}