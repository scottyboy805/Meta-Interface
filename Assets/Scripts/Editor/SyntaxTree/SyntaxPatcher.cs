using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace MetaInterface.SyntaxTree
{
    public class SyntaxPatcher
    {
        // Methods
        //public static bool IsDeclarationExposed(SyntaxNode syntax)//, SemanticModel model)
        //{
        //    // Get symbol for node
        //    ISymbol symbol = model.GetDeclaredSymbol(syntax);

        //    // Check for public or protcted
        //    if(symbol != null)
        //    {
        //        // Get accessibility
        //        Accessibility access = symbol.DeclaredAccessibility;

        //        switch(access)
        //        {
        //            case Accessibility.Public:
        //            case Accessibility.Protected:
        //            case Accessibility.ProtectedAndFriend:
        //            case Accessibility.ProtectedOrFriend:
        //                {
        //                    // Declaration is exposed
        //                    return true;
        //                }
        //        }
        //    }
        //    return false;
        //}

        public static bool IsClassDeclarationExposed(ClassDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsStructDeclarationExposed(StructDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsInterfaceDeclarationExposed(InterfaceDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsEnumDeclarationExposed(EnumDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsEventDeclarationExposed(EventDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsFieldDeclarationExposed(FieldDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsPropertyDeclarationExposed(PropertyDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsMethodDeclarationExposed(MethodDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        private static bool IsModifierListExposed(SyntaxTokenList modifiers)
        {
            return modifiers
                .Where(m => m.Kind() == SyntaxKind.PublicKeyword
                || m.Kind() == SyntaxKind.ProtectedKeyword)
                .Any();
        }

        public static MethodDeclarationSyntax PatchMethodBody(MethodDeclarationSyntax syntax)
        {
            // Get the new method body
            BlockSyntax replaceBody = GetMethodBodyReplacementSyntax();

            // get the current method body
            BlockSyntax body = syntax.Body;

            // Replace the method body
            return syntax.ReplaceNode(body, replaceBody);
        }

        public static BlockSyntax GetMethodBodyReplacementSyntax()
        {
            return SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(
                    @"throw new System.Exception(""API method provides no functionality!"");"));
        }
    }
}
