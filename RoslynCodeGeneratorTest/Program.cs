using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RoslynCodeGeneratorTest
{
    static class Program
    {
        const string NAMESPACE = "ZigBeeNet";
        private readonly static Regex _invalidNameCharactersPattern = new Regex(@"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]");
        private const string _defaultReplacementCharacter = "";

        // Taken from: https://github.com/RicoSuter/NJsonSchema/blob/1a2ce00b3e1e22e78303d1dfeff84b73a2a25392/src/NJsonSchema.CodeGeneration/DefaultEnumNameGenerator.cs#L16
        static string GetValidName(string name)
        {
            return _invalidNameCharactersPattern.Replace(name.Replace(@"""", ""), _defaultReplacementCharacter);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //ProcessCluster(@"..\..\..\Resources\0000_Basic.xml");
            ReadConstants(@"..\..\..\Resources\zigbee_constants.xml", @"..\..\..\Generated\");
            Console.WriteLine();
            Console.ReadLine();
        }

        static void ProcessCluster(string path)
        {
            var s = new XmlSerializer(typeof(cluster));
            using var r = new StreamReader(path);
            var a = s.Deserialize(r);
            ;
        }

        static void ReadConstants(string resourcesPath, string destinationPath)
        {
            Console.WriteLine("Start generating constants");
            var serializer = new XmlSerializer(typeof(zigbee));
            using var streamReader = new StreamReader(resourcesPath);
            var zigbee = serializer.Deserialize(streamReader) as zigbee;

            foreach (var constant in zigbee.constant)
            {
                File.WriteAllText(Path.Combine(destinationPath, $"{constant.@class}.cs"), ConstantToEnum(constant));
            }
            Console.WriteLine("Finished generating constants");
        }

        static string ConstantToEnum(zigbeeConstant constant)
        {
            bool withDescription = !string.IsNullOrWhiteSpace(constant.description);

            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(NAMESPACE))
                .NormalizeWhitespace();

            // Create enum scaffolding
            var @class = SyntaxFactory
                .EnumDeclaration(constant.@class)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // Add values to enum
            @class = @class.AddMembers(
                constant.value
                .Select(x =>
                SyntaxFactory
                .EnumMemberDeclaration(GetValidName(x.name))
                .WithEqualsValue(
                    SyntaxFactory
                    .EqualsValueClause(
                        SyntaxFactory
                        .ParseExpression(x.code)))
                ).ToArray());

            if (withDescription)
            {
                var documentationComment = SyntaxFactory.DocumentationComment(
                    SyntaxFactory.XmlSummaryElement(
                        SyntaxFactory.XmlNewLine("\r\n"),
                        SyntaxFactory.XmlText(constant.description),
                        SyntaxFactory.XmlNewLine("\r\n"),
                        SyntaxFactory.XmlPreliminaryElement()));

                @class
                    .WithLeadingTrivia(SyntaxFactory.Trivia(documentationComment).WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation));
            }

            @namespace = @namespace
                .AddMembers(@class)
                .NormalizeWhitespace();

            // Insert \r\n and 4 spaces for indentation between the last comment and the access modifier
            if (withDescription)
            {
                @namespace = @namespace
                    .InsertTriviaAfter(@namespace
                    .GetAnnotatedTrivia(SyntaxAnnotation.ElasticAnnotation)
                    .First(),
                    new[] { SyntaxFactory.EndOfLine("\r\n    ") });
            }

            return @namespace
                .ToFullString();
        }
    }
}
