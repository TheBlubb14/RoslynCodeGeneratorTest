using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
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
        internal const string ROOT_NAMESPACE = "ZigBeeNet";
        const string CLUSTER_NAMESPACE = ROOT_NAMESPACE + ".ZCL.Clusters";
        const string PROTOCOL_NAMESPACE = ROOT_NAMESPACE + ".ZCL.Protocol";
        const string FIELD_NAMESPACE = ROOT_NAMESPACE + ".ZCL.Field";
        const string GENERAL_COMMAND = "GENERAL";
        const string ZCL_COMMAND = "ZclCommand";
        const string AUTO_GENERATED_WARNING = "Code is auto-generated. Modifications may be overwritten!";
        const string ZCL_COMMAND_DIRECTION = "ZclCommandDirection";
        const string CLIENT_TO_SERVER = "CLIENT_TO_SERVER";
        const string SERVER_TO_CLIENT = "SERVER_TO_CLIENT";

        private static List<UsingDirectiveSyntax> _usings;

        static void Main(string[] args)
        {
            _usings = new List<UsingDirectiveSyntax>()
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Collections.Generic")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Linq")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System.Text")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(PROTOCOL_NAMESPACE)),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(FIELD_NAMESPACE)),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(CLUSTER_NAMESPACE)),
            };

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

                // command means own class
                // atttributes müssen auch in command to class rein
                // in autocode heissen sie fields
                if (cluster.command != null)
                {
                    foreach (var command in cluster.command)
                    {
                        Console.WriteLine(CommandToClass(command, @namespace, cluster.name.Equals(GENERAL_COMMAND), name, cluster.code, cluster.attribute));
                        return;
                    }
                }


                // Generate enums
                //if (cluster.constant != null)
                //{
                //    foreach (var constant in cluster.constant)
                //    {
                //        Console.WriteLine(ConstantToEnum(constant, @namespace));
                //    }
                //}

                //Console.WriteLine(ClusterToClass(cluster));
                //return;
            }

            Console.WriteLine("Finished generating clusters");
        }

        static string CommandToClass(clusterCommand Command, string @Namespace, bool IsGeneral, string ClusterName, string ClusterId, clusterAttribute[] attributes)
        {
            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(@Namespace))
                .WithUsings(SyntaxFactory.List(_usings));

            var comments = new List<string>();
            comments.Add($"{ClusterName} Cluster. Command is sent from {Command.source}");
            comments.AddRange(Command.description);
            comments.Add("This command is " + (IsGeneral
                ? "a generic command used across the profile."
                : "a specific command used for the " + Command.name + " cluster."));
            comments.Add(AUTO_GENERATED_WARNING);

            // Create class scaffolding
            var @class = SyntaxFactory
                .ClassDeclaration(Command.name.GetValidName())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(ZCL_COMMAND)))
                .AddXmlComment(comments.ToArray());

            // properties
            //Command.field
            foreach (var attribute in attributes)
            {
                @class = @class
                    .AddMembers(
                    SyntaxFactory
                    .PropertyDeclaration(
                        attribute.type.ParseType(),
                        SyntaxFactory.Identifier(attribute.name.GetValidName()))
                    .WithModifiers(CreatePublicModifier())
                    .WithAccessorList(CreateGetSetSyntax()))
                    .AddXmlComment(attribute.description);

                break;
            }

            var constructorBody = new List<StatementSyntax>();
            constructorBody.Add(SyntaxFactory.ParseStatement($"GenericCommand = {IsGeneral.ToKeyword()};\r\n"));
            if (!IsGeneral)
                constructorBody.Add(SyntaxFactory.ParseStatement($"ClusterId = {ClusterId};\r\n"));
            constructorBody.Add(SyntaxFactory.ParseStatement($"CommandId = {Command.code};\r\n"));
            constructorBody.Add(SyntaxFactory.ParseStatement($"CommandDirection = {ZCL_COMMAND_DIRECTION}.{(string.Equals(Command.source, "server") ? SERVER_TO_CLIENT : CLIENT_TO_SERVER)};\r\n"));

            // Add constructor
            @class = @class.AddMembers(
                SyntaxFactory.ConstructorDeclaration(@class.Identifier.ValueText)
                .WithModifiers(CreatePublicModifier())
                .AddXmlComment("Default constructor")
                .WithBody(SyntaxFactory.Block(constructorBody)));

            var toStringBody = new List<StatementSyntax>()
            {
                // var builder = new StringBuilder();
                CreateLocalDeclaration("builder", "StringBuilder"),
                
                // builder.Append("TheCommand [");
                SyntaxFactory
                .ExpressionStatement(
                    CreateInvocation("builder", "Append", StringArgument($"{@class.Identifier.ValueText} ["))),

                // builder.Append(base.ToString());
                SyntaxFactory
                .ExpressionStatement(
                CreateInvocation(
                    "builder", "Append",
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(
                            CreateInvocation(
                                SyntaxFactory.BaseExpression(SyntaxFactory.Token(SyntaxKind.BaseKeyword)),
                                SyntaxFactory.ParseToken("ToString")))
                        })))),

                // builder.Append(']');
                SyntaxFactory
                .ExpressionStatement(
                    CreateInvocation("builder", "Append", CharArgument(']'))),

                // return builder.ToString();
                SyntaxFactory.ReturnStatement(CreateInvocation("builder", "ToString"))
            };

            @class = @class.AddMembers(
                SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    "ToString")
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
                .WithBody(SyntaxFactory.Block(toStringBody))
                );


            //@class = @class.AddMembers(
            //    SyntaxFactory.MethodDeclaration().wit
            //    )
            return @namespace
                .AddMembers(@class)
                .Format()
                .ToFullString();
        }

        static AccessorListSyntax CreateGetSetSyntax()
        {
            return SyntaxFactory.AccessorList(
                SyntaxFactory.List(
                    new AccessorDeclarationSyntax[]
                    {
                        SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(
                            SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(
                            SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    }));
        }

        static SyntaxTokenList CreatePublicModifier()
        {
            return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        static ArgumentListSyntax Argument(ExpressionSyntax Argument)
        {
            return SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory
                    .Argument(Argument)
                }));
        }

        static ArgumentListSyntax StringArgument(string Value)
        {
            return Argument(
                SyntaxFactory
                .LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(Value)));
        }

        static ArgumentListSyntax CharArgument(char Value)
        {
            return Argument(
                SyntaxFactory
                .LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression,
                    SyntaxFactory.Literal(Value)));
        }

        static InvocationExpressionSyntax CreateInvocation(string Variable, string Method, ArgumentListSyntax ArgumentList = null)
        {
            return CreateInvocation(SyntaxFactory.ParseToken(Variable), SyntaxFactory.ParseToken(Method), ArgumentList);
        }

        static InvocationExpressionSyntax CreateInvocation(SyntaxToken Variable, SyntaxToken Method, ArgumentListSyntax ArgumentList = null)
        {
            return CreateInvocation(
                SyntaxFactory.IdentifierName(Variable),
                Method, ArgumentList);
        }

        static InvocationExpressionSyntax CreateInvocation(ExpressionSyntax Variable, SyntaxToken Method, ArgumentListSyntax ArgumentList = null)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    Variable,
                    SyntaxFactory.IdentifierName(Method)),
                ArgumentList ?? SyntaxFactory.ArgumentList());
        }

        static LocalDeclarationStatementSyntax CreateLocalDeclaration(string Variable, string Type, string Identifier = "var")
        {
            return CreateLocalDeclaration(SyntaxFactory.Identifier(Variable), SyntaxFactory.Identifier(Type), Identifier);
        }

        static LocalDeclarationStatementSyntax CreateLocalDeclaration(SyntaxToken Variable, SyntaxToken Type, string Identifier = "var")
        {
            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName(Identifier),
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.VariableDeclarator(
                            Variable,
                            null,
                            SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName(
                                        Type),
                                    SyntaxFactory.ArgumentList(), null)))
                    })));
        }

        static string ClusterToClass(cluster Cluster)
        {
            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(ROOT_NAMESPACE));

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
                .Format()
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
            return ConstantToEnum(@Namespace, Constant.@class, Constant.value.Select(x => (x.name, x.code)), Constant.description);
        }

        static string ConstantToEnum(zigbeeConstant Constant, string @Namespace)
        {
            return ConstantToEnum(@Namespace, Constant.@class, Constant.value.Select(x => (x.name, x.code)), Constant.description);
        }

        static string ConstantToEnum(string @Namespace, string Class, IEnumerable<(string Name, string Value)> Values, params string[] Descriptions)
        {
            var name = @Namespace.Split('.').Last();
            var @namespace = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(@Namespace));

            // Create enum scaffolding
            var @enum = SyntaxFactory
                .EnumDeclaration(Class.GetValidName().TrimStart(name).TrimEnd("Enum"))
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
                .Format()
                .ToFullString();
        }
    }

    class A
    {
        public A()
        {

        }

        public override string ToString()
        {
            var builder = new System.Text.StringBuilder();

            builder.Append("ResetToFactoryDefaultsCommand [");
            builder.Append(base.ToString());
            builder.Append(']');

            return builder.ToString();
        }
    }
}
