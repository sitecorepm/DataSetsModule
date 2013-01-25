using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using System.Runtime.InteropServices;
using PInvoke;

namespace Sitecore.SharedSource.Text
{
    public class FunctionGrammer : Grammar
    {
        public FunctionGrammer()
        {
            //Terminals
            var StringLiteral = TerminalFactory.CreateCSharpString("StringLiteral");
            var CharLiteral = TerminalFactory.CreateCSharpChar("CharLiteral");
            var Number = TerminalFactory.CreateCSharpNumber("Number");
            var identifier = TerminalFactory.CreateCSharpIdentifier("Identifier");
            //var guid_part_8 = new FixedLengthLiteral("guid_part_8", 8, TypeCode.String);
            //var guid_part_4 = new FixedLengthLiteral("guid_part_4", 4, TypeCode.String);
            //var guid_part_12 = new FixedLengthLiteral("guid_part_12", 12, TypeCode.String);
            var guid_variable = new CustomTerminal("guid_variable", delegate(Terminal terminal, ParsingContext context, ISourceStream source)
                {
                    var start = source.PreviewPosition;
                    if (source.PreviewChar == '@'
                        && source.NextPreviewChar == '{'
                        && source.Text.Length - start >= 39) // There are enough characters left to make a guid + @ symbol
                    {
                        Guid parsed_guid;
                        var guid_candidate = source.Text.Substring(start + 1, 38);
                        if (TryStrToGuid(guid_candidate, out parsed_guid))
                        {
                            source.PreviewPosition = source.PreviewPosition + 39;
                            return source.CreateToken(terminal, source.Text.Substring(start, 39));
                        }
                    }
                    return null;
                });


            //Symbols
            KeyTerm colon = ToTerm(":", "colon");
            KeyTerm semi = ToTerm(";", "semi");
            KeyTerm comma = ToTerm(",", "comma");
            KeyTerm Lbr = ToTerm("{");
            KeyTerm Rbr = ToTerm("}");
            KeyTerm Lpar = ToTerm("(");
            KeyTerm Rpar = ToTerm(")");
            KeyTerm qmark = ToTerm("?", "qmark");
            KeyTerm at = ToTerm("@", "At");
            KeyTerm hyphen = ToTerm("-");

            //Nonterminals
            var argument_list = new NonTerminal("argument_list");
            var argument_list_opt = new NonTerminal("argument_list_opt");
            var argument_list_par = new NonTerminal("argument_list_par");
            var argument_list_par_opt = new NonTerminal("argument_list_par_opt");
            var bin_op = new NonTerminal("bin_op", "operator symbol");
            var bin_op_expression = new NonTerminal("bin_op_expression");
            //var conditional_expression = new NonTerminal("conditional_expression");
            var expression = new NonTerminal("expression");
            var function_expression = new NonTerminal("function_expression");
            //var guid = new NonTerminal("guid");
            //var guid_variable = new NonTerminal("guid_variable");
            var literal = new NonTerminal("literal");
            var parenthesized_expression = new NonTerminal("parenthesized_expression");
            var primary_expression = new NonTerminal("primary_expression");
            //var typecast_expression = new NonTerminal("typecast_expression");
            var unary_expression = new NonTerminal("unary_expression");
            var unary_operator = new NonTerminal("unary_operator");
            var qual_name_with_targs = new NonTerminal("qual_name_with_targs");

            #region From CSharpGrammer

            #region operators, punctuation and delimiters
            RegisterOperators(1, "||");
            RegisterOperators(2, "&&");
            RegisterOperators(3, "|");
            RegisterOperators(4, "^");
            RegisterOperators(5, "&");
            RegisterOperators(6, "==", "!=");
            RegisterOperators(7, "<", ">", "<=", ">=", "is", "as");
            RegisterOperators(8, "<<", ">>");
            RegisterOperators(9, "+", "-");
            RegisterOperators(10, "*", "/", "%");
            //RegisterOperators(11, ".");
            // RegisterOperators(12, "++", "--");

            //The following makes sense, if you think about "?" in context of operator precedence. 
            // What we say here is that "?" has the lowest priority among arithm operators.
            // Therefore, the parser should prefer reduce over shift when input symbol is "?".
            // For ex., when seeing ? in expression "a + b?...", the parser will perform Reduce:
            //  (a + b)->expr
            // and not shift the "?" symbol.  
            // Same goes for ?? symbol
            RegisterOperators(-3, "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=");
            RegisterOperators(-2, "?");
            RegisterOperators(-1, "??");

            this.Delimiters = "{}[](),:;+-*/%&|^!~<>=";
            this.MarkPunctuation(";", ",", "(", ")", "{", "}", "[", "]", ":");
            this.MarkTransient(expression, literal, bin_op, primary_expression, expression);

            this.AddTermsReportGroup("assignment", "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=");
            this.AddTermsReportGroup("typename", "bool", "decimal", "float", "double", "string", "object", "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "char");
            this.AddTermsReportGroup("statement", "if", "switch", "do", "while", "for", "foreach", "continue", "goto", "return", "try", "yield", "break", "throw", "unchecked", "using");
            this.AddTermsReportGroup("type declaration", "public", "private", "protected", "static", "internal", "sealed", "abstract", "partial", "class", "struct", "delegate", "interface", "enum");
            this.AddTermsReportGroup("member declaration", "virtual", "override", "readonly", "volatile", "extern");
            this.AddTermsReportGroup("constant", Number, StringLiteral, CharLiteral);
            this.AddTermsReportGroup("constant", "true", "false", "null");

            this.AddTermsReportGroup("unary operator", "+", "-", "!", "~");

            this.AddToNoReportGroup(comma, semi);
            this.AddToNoReportGroup("var", "const", "new", "++", "--", "this", "base", "checked", "lock", "typeof", "default", "{", "}", "[");

            #endregion
            #endregion

            //Rules
            //guid.Rule = guid_part_8 + hyphen + guid_part_4 + hyphen + guid_part_4 + hyphen + guid_part_4 + hyphen + guid_part_12;
            //guid_variable.Rule = at + Lbr + guid + Rbr;
            bin_op.Rule = ToTerm("<") | "||" | "&&" | "|" | "&" | "==" | "!=" | ">" | "<=" | ">=" | "+" | "-" | "*" | "/" | "%";
            argument_list.Rule = MakePlusRule(argument_list, comma, expression);
            argument_list_opt.Rule = Empty | argument_list;
            argument_list_par.Rule = Lpar + argument_list_opt + Rpar;
            argument_list_par_opt.Rule = Empty | argument_list_par;
            bin_op_expression.Rule = expression + bin_op + expression;
            parenthesized_expression.Rule = Lpar + expression + Rpar;
            //typecast_expression.Rule = parenthesized_expression + primary_expression;
            primary_expression.Rule = literal | unary_expression | parenthesized_expression;
            //conditional_expression.Rule = expression + PreferShiftHere() + qmark + expression + colon + expression;// + ReduceThis();
            unary_expression.Rule = unary_operator + primary_expression;
            expression.Rule = bin_op_expression
                    | primary_expression
                    | function_expression
                    | guid_variable;
                    //| typecast_expression
                    //| conditional_expression;
            literal.Rule = Number | StringLiteral | CharLiteral | "true" | "false" | "True" | "False" | "null";
            unary_operator.Rule = ToTerm("-") | "!"; //ToTerm("+") | "-" | "!" | "~" | "*";
            function_expression.Rule = identifier + argument_list_par;

            //Set grammar root
            this.Root = function_expression;
        }

        /// <summary>
        /// Attempts to convert a string to a guid.
        /// credit: http://stackoverflow.com/questions/104850/c-test-if-string-is-a-guid-without-throwing-exceptions
        /// </summary>
        /// <param name="s">The string to try to convert</param>
        /// <param name="value">Upon return will contain the Guid</param>
        /// <returns>Returns true if successful, otherwise false</returns>
        public static Boolean TryStrToGuid(String s, out Guid value)
        {
            //ClsidFromString returns the empty guid for null strings   
            if ((s == null) || (s == ""))
            {
                value = Guid.Empty;
                return false;
            }

            int hresult = PInvoke.ObjBase.CLSIDFromString(s, out value);
            if (hresult >= 0)
            {
                return true;
            }
            else
            {
                value = Guid.Empty;
                return false;
            }
        }

        public static Parser FunctionParser()
        {
            var g = new FunctionGrammer();
            var p = new Parser(g);
            return p;
        }
    }
}
