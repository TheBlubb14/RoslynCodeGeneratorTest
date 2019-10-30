using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace RoslynCodeGeneratorTest
{
    static class Program
    {
        const string ROOT_NAMESPACE = "ZigBeeNet";
        const string CLUSTER_NAMESPACE = ROOT_NAMESPACE + ".ZCL.Clusters";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ProcessCluster(@"..\..\..\Resources\", @"..\..\..\Generated\Cluster\");
            //ReadConstants(@"..\..\..\Resources\zigbee_constants.xml", @"..\..\..\Generated\Constant\");
            Console.WriteLine();
            Console.ReadLine();
        }

        static void ProcessCluster(string resourcesPath, string destinationPath)
        {
            Console.WriteLine("Start generating clusters");
            var serializer = new XmlSerializer(typeof(cluster));

            foreach (var file in Directory.EnumerateFiles(resourcesPath, "*.xml", SearchOption.TopDirectoryOnly).Except(new[] { resourcesPath + "zigbee_constants.xml" }))
            {
                using var streamReader = new StreamReader(file);
                var cluster = serializer.Deserialize(streamReader) as cluster;

                var name = cluster.name.GetValidName();
                var @namespace = $"{CLUSTER_NAMESPACE}.{name}";
                var folder = name;

                if (cluster.constant != null)
                {
                    foreach (var constant in cluster.constant)
                    {
                        Console.WriteLine(ConstantToEnum(constant, @namespace));
                    }
                }

                //Console.WriteLine(ClusterToClass(cluster));
                //return;
            }

            Console.WriteLine("Finished generating clusters");
        }
        static string ClusterToClass(cluster Cluster)
        {
            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(ROOT_NAMESPACE))
                .NormalizeWhitespace();

            // Create class scaffolding
            var @class = SyntaxFactory
                .ClassDeclaration(Cluster.name.GetValidName())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddXmlComment(Cluster.description);

            var attributes = Cluster.attribute;
            var constAttributes = attributes.Where(x => x.writable == "false");

            //@class = @class
            //    .AddMembers(
            //    constAttributes.Select(x =>
            //    SyntaxFactory.FieldDeclaration(x.type.ParseType())));

            // So funktioniert const, aber ich brauche eine methode wo ich per if mir das attribute anschauen, weil wenn string muss da was anderen in equals stehen als by byte...
            @class = @class
                .AddMembers(
                constAttributes.Select(x =>
               SyntaxFactory.FieldDeclaration(
               SyntaxFactory.List<AttributeListSyntax>(),
               SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ConstKeyword)),
               SyntaxFactory.VariableDeclaration(
                   x.type.ParseType(),
                   SyntaxFactory.SingletonSeparatedList(
                       SyntaxFactory.VariableDeclarator(
                           SyntaxFactory.Identifier(x.name.GetValidName()),
                           argumentList: null,
                           initializer: SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(x.code))))))).ToArray());

            ;

            foreach (var attribute in Cluster.attribute)
            {
                switch (attribute.type)
                {

                    default:
                        break;
                }
            }

            return @namespace
                .AddMembers(@class)
                .NormalizeWhitespace()
                .FixXmlCommentEndOfLine()
                .ToFullString();
        }

        static void ReadConstants(string ResourcesPath, string DestinationPath)
        {
            Console.WriteLine("Start generating constants");
            var serializer = new XmlSerializer(typeof(zigbee));
            using var streamReader = new StreamReader(ResourcesPath);
            var zigbee = serializer.Deserialize(streamReader) as zigbee;

            foreach (var constant in zigbee.constant)
            {
                File.WriteAllText(Path.Combine(DestinationPath, $"{constant.@class}.cs"), ConstantToEnum(constant, ROOT_NAMESPACE));
            }
            Console.WriteLine("Finished generating constants");
        }

        static string ConstantToEnum(clusterConstant Constant, string @Namespace)
        {
            return ConstantToEnum(@Namespace, Constant.@class.TrimEnd("Enum"), Constant.value.Select(x => (x.name, x.code)), Constant.description);
        }

        static string ConstantToEnum(zigbeeConstant Constant, string @Namespace)
        {
            return ConstantToEnum(@Namespace, Constant.@class, Constant.value.Select(x => (x.name, x.code)), Constant.description);
        }

        static string ConstantToEnum(string @Namespace, string Class, IEnumerable<(string Name, string Value)> Values, params string[] Descriptions)
        {
            var name = @Namespace.Split('.').Last();
            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(@Namespace))
                .NormalizeWhitespace();

            // Create enum scaffolding
            var @enum = SyntaxFactory
                .EnumDeclaration(Class.GetValidName().TrimStart(name))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // Add values to enum
            @enum = @enum.AddMembers(
                Values
                .Select(x =>
                SyntaxFactory
                .EnumMemberDeclaration(x.Name.GetValidName())
                .WithEqualsValue(
                    SyntaxFactory
                    .EqualsValueClause(
                        SyntaxFactory
                        .ParseExpression(x.Value)))
                )
                .ToArray())
                .AddXmlComment(Descriptions);

            return @namespace
                .AddMembers(@enum)
                .NormalizeWhitespace()
                .FixXmlCommentEndOfLine()
                .ToFullString();
        }
    }
}
