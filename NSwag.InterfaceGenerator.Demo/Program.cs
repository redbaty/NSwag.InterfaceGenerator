using System;
using NSwag.InterfaceGenerator.Builders;

namespace NSwag.InterfaceGenerator.Demo
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            new SwaggerInterfaceBuilder().WithUrl("http://petstore.swagger.io/v2/swagger.json").Build().Wait();
            Console.ReadKey();
        }
    }
}