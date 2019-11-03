using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RoslynCodeGeneratorTest
{
    internal static class Extension
    {
        private readonly static Regex _invalidNameCharactersPattern = new Regex(@"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]");
        private const string _defaultReplacementCharacter = "";
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        private static Workspace Workspace = CreateWorkspace();

        // Taken from: https://github.com/RicoSuter/NJsonSchema/blob/1a2ce00b3e1e22e78303d1dfeff84b73a2a25392/src/NJsonSchema.CodeGeneration/DefaultEnumNameGenerator.cs#L16
        internal static string GetValidName(this string name)
        {
            return _invalidNameCharactersPattern.Replace(name.Replace(@"""", ""), _defaultReplacementCharacter);
        }

        internal static string TrimStart(this string source, string value)
        {
            var index = source.IndexOf(value, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
                return source;

            return source[(index + value.Length)..];
        }

        internal static string TrimEnd(this string source, string value)
        {
            var index = source.LastIndexOf(value, StringComparison.OrdinalIgnoreCase);

            if (index < 1)
                return source;

            return source[..index];
        }

        internal static TypeSyntax ParseType(this string type)
        {
            switch (type)
            {
                case "BOOLEAN":
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));
                case "UNSIGNED_8_BIT_INTEGER":
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword));
                case "UNSIGNED_16_BIT_INTEGER":
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword));
                case "CHARACTER_STRING":
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
                default:
                    Debugger.Break();
                    return null;
            }
        }

        /// <summary>
        /// Returns the bool as the language keyword
        /// </summary>
        /// <param name="b">The boolean</param>
        /// <returns></returns>
        internal static string ToKeyword(this bool b)
        {
            return SyntaxFactory.Token(b ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword).ToString();
        }

        internal static T Format<T>(this T node) where T : SyntaxNode
        {
            return Formatter.Format(node, node.FullSpan, Workspace) as T;
        }

        private static Workspace CreateWorkspace()
        {
            var workspace = new AdhocWorkspace();
            var projectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(Program.ROOT_NAMESPACE),
                VersionStamp.Create(),
                Program.ROOT_NAMESPACE,
                Program.ROOT_NAMESPACE,
                LanguageNames.CSharp,
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                metadataReferences: ImmutableList.Create(
                    CorlibReference, SystemCoreReference)
                );

            workspace.AddProject(projectInfo);

            //            string a =
            //                @"namespace ZigBeeNet.ZCL.Protocol
            //{
            //    public enum ZclCommandDirection : byte
            //    {
            //        CLIENT_TO_SERVER = 0x00,
            //        SERVER_TO_CLIENT = 0x01
            //    }
            //}";
            //workspace.AddDocument(projectInfo.Id, "ZclCommandDirection", SourceText.From(a));

            return workspace;
        }

        /// <summary>
        /// Adds the given descriptions before the given <see cref="SyntaxNode"/>
        /// </summary>
        /// <typeparam name="T">Has to be based of from <see cref="SyntaxNode"/></typeparam>
        /// <param name="syntax">The <typeparamref name="T"/> where the comment should be added to</param>
        /// <param name="descriptions">The description to be added in front of the given <typeparamref name="T"/></param>
        /// <returns>The modified <typeparamref name="T"/></returns>
        internal static T AddXmlComment<T>(this T syntax, params string[] descriptions) where T : SyntaxNode
        {
            var lines = descriptions?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? Enumerable.Empty<string>();
            if (!lines.Any())
                return syntax;

            var nodes = new List<XmlNodeSyntax>();
            foreach (var line in lines)
            {
                nodes.Add(SyntaxFactory.XmlNewLine("\r\n"));
                nodes.Add(SyntaxFactory.XmlText(line));
            }
            nodes.Add(SyntaxFactory.XmlNewLine("\r\n"));

            var documentationComment = DocumentationComment(
                SyntaxFactory.XmlSummaryElement(nodes.ToArray()));

            return syntax
                .WithLeadingTrivia(SyntaxFactory.Trivia(documentationComment));
        }

        /// <summary>
        /// Taken from roslyn source code
        /// Creates an xml documentation comment that abstracts xml syntax creation.
        /// </summary>
        /// <param name="content">
        /// A list of xml node syntax that will be the content within the xml documentation comment
        /// (e.g. a summary element, a returns element, exception element and so on).
        /// </param>
        private static DocumentationCommentTriviaSyntax DocumentationComment(params XmlNodeSyntax[] content)
        {
            return SyntaxFactory.DocumentationCommentTrivia(
                SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxFactory.List(content))
                .WithLeadingTrivia(SyntaxFactory.DocumentationCommentExterior("/// "))
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")); // EndofLine with \r\n instead of empty string
        }
    }
}
