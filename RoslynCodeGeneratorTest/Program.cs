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
            //ProcessCluster(@"..\..\..\Resources\", @"..\..\..\Generated\Cluster\");
            ReadConstants(@"..\..\..\Resources\zigbee_constants.xml", @"..\..\..\Generated\Constant\");
            Console.WriteLine();
            Console.ReadLine();
        }

        static void ProcessCluster(string resourcesPath, string destinationPath)
        {
            Console.WriteLine("Start generating clusters");
            var serializer = new XmlSerializer(typeof(cluster));

            foreach (var file in Directory.EnumerateFiles(resourcesPath, "*.xml", SearchOption.TopDirectoryOnly))
            {
                using var streamReader = new StreamReader(file);
                var cluster = serializer.Deserialize(streamReader) as cluster;

                Console.WriteLine(ClusterToClass(cluster));
                return;
            }

            Console.WriteLine("Finished generating clusters");
        }
        static string ClusterToClass(cluster cluster)
        {
            //bool withDescription = !string.IsNullOrWhiteSpace(cluster.description);

            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(NAMESPACE))
                .NormalizeWhitespace();

            // Create class scaffolding
            var @class = SyntaxFactory
                .ClassDeclaration(GetValidName(cluster.name))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));


            ;
            return @namespace
                .AddMembers(@class)
                .NormalizeWhitespace()
                .ToFullString();
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
            var @enum = SyntaxFactory
                .EnumDeclaration(constant.@class)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // Add values to enum
            @enum = @enum.AddMembers(
                constant.value
                .Select(x =>
                SyntaxFactory
                .EnumMemberDeclaration(GetValidName(x.name))
                .WithEqualsValue(
                    SyntaxFactory
                    .EqualsValueClause(
                        SyntaxFactory
                        .ParseExpression(x.code)))
                )
                .ToArray())
                .AddXmlComment(constant.description);

            return @namespace
                .AddMembers(@enum)
                .NormalizeWhitespace()
                .FixXmlCommentEndOfLine()
                .ToFullString();
        }

        private static T AddXmlComment<T>(this T syntax, string description) where T : SyntaxNode
        {
            if (string.IsNullOrWhiteSpace(description))
                return syntax;

            var documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText(description),
                    SyntaxFactory.XmlNewLine("\r\n")));

            return syntax
                .WithLeadingTrivia(SyntaxFactory.Trivia(documentationComment)
                .WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation));
        }

        private static T FixXmlCommentEndOfLine<T>(this T syntax) where T : SyntaxNode
        {
            // Insert \r\n and 4 spaces for indentation between the last comment and the access modifier
            var endOfLine = new[] { SyntaxFactory.EndOfLine("\r\n    ") };

            foreach (var annotation in syntax.GetAnnotatedTrivia(SyntaxAnnotation.ElasticAnnotation))
            {
                syntax = syntax
                    .InsertTriviaAfter(annotation, endOfLine);
            }

            return syntax;
        }
    }
}
