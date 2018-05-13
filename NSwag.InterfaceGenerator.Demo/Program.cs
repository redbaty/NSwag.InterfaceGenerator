using System;
using NSwag.InterfaceGenerator.Builders;

namespace NSwag.InterfaceGenerator.Demo
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            new SwaggerInterfaceBuilder().WithUrl("http://localhost:5000/swagger/v1/swagger.json").Build().Wait();
            Console.ReadKey();
        }
    }
}