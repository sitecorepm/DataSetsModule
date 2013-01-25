using System;
using System.Collections;
using scs = System.Collections.Specialized;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.WebControls;
using Irony.Parsing;
using System.Diagnostics;
using log4net;
using Sitecore.SharedSource.Text.Pipelines.ProcessTextFunction;
using Sitecore.Pipelines;
using Sitecore.SharedSource.Dataset;

namespace Sitecore.SharedSource.Text
{
    /// <summary>
    /// This class is used to parse & interpret a function in a text string. It handles replacing the 
    /// function placeholders such as, @replace("sometex","x","xt"), with the function result.
    /// </summary>
    public class FunctionInterpreter
    {
        static FunctionInterpreter()
        {
            Log = LogManager.GetLogger(typeof(FunctionInterpreter));
        }

        private static ILog Log { get; set; }
        private static Regex _rxVariable = new Regex(@"@\{[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}\}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _rxInlineFuntion = new Regex(@"(?<escapefunction>\\)?@\w+\((?>[^()]+|\((?<number>)|\)(?<-number>))*(?(number)(?!))\)", RegexOptions.Compiled | RegexOptions.Singleline);
        //private static FunctionGrammer _FxGrammer = new FunctionGrammer();
        private static Dictionary<string, ParseTree> _ParseTreeMemCache = new Dictionary<string, ParseTree>();
        private static KeyLockManager _lockMgr = new KeyLockManager();

        private ITextFunctionHandler[] _fxCustomHandlers = null;
        private Dictionary<string, BaseFunctionProcessor> _fxHandlerLookup = new Dictionary<string, BaseFunctionProcessor>();
        private Dictionary<string, string> _variables = null;

        public static string Process(string text)
        {
            return Process(text, null, null);
        }

        public static string Process(string text, ITextFunctionHandler[] fxHandlers, Dictionary<string, string> variables)
        {
            var result = text;

            var parser = new FunctionInterpreter()
            {
                _fxCustomHandlers = fxHandlers ?? new ITextFunctionHandler[] { },
                _variables = variables ?? new Dictionary<string, string>()
            };
            result = parser.ProcessInternal(text);

            return result;
        }

        private string ProcessInternal(string text)
        {
            text = _rxInlineFuntion.Replace(text, delegate(Match m)
            {
                var result = m.Value;

                if (m.Groups["escapefunction"].Success)
                    return result.TrimStart('\\');

                try
                {
                    ParseTree pTree = null;

                    // Use previously created ParseTree if one exists...
                    if (_ParseTreeMemCache.ContainsKey(m.Value))
                        pTree = _ParseTreeMemCache[m.Value];
                    else
                    {
                        lock (_lockMgr.AcquireKeyLock(m.Value))
                        {
                            if (_ParseTreeMemCache.ContainsKey(m.Value))
                                pTree = _ParseTreeMemCache[m.Value];
                            else
                            {
                                // Create a new FunctionParser since it is a stateful object.
                                var parser = FunctionGrammer.FunctionParser();
                                pTree = parser.Parse(m.Value);
                                if (!pTree.HasErrors())
                                    _ParseTreeMemCache.Add(m.Value, pTree);
                            }
                        }
                    }

                    if (!pTree.HasErrors())
                        result = ProcessNode(pTree.Root, null) as string;
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("FunctionParser error: [fx: {0}]", m.Value), ex);
                }
                return result;
            });

            return text;
        }


        private object ProcessNode(ParseTreeNode node, string fxName)
        {
            object result = null;

            switch (node.Term.Name)
            {
                case "function_expression":
                    fxName = node.ChildNodes[0].FindTokenAndGetText().TrimStart('@');
                    var fxParams = (List<object>)ProcessNode(node.ChildNodes[1], fxName);
                    var args = new TextFunctionPipelineArgs()
                    {
                        FunctionName = fxName,
                        Args = fxParams.ToArray(),
                        CustomFunctionHandlers = _fxCustomHandlers
                    };
                    if (_fxHandlerLookup.ContainsKey(fxName))
                    {
                        var fx = _fxHandlerLookup[fxName];
                        fx.ProcessFunction(args);
                    }
                    else
                    {
                        // Send to the ProcessTextFunction pipeline....
                        CorePipeline.Run("ProcessTextFunction", args);

                        // Save reference to the handling processor...
                        if (args.FunctionHandled && args.HandledBy != null)
                            _fxHandlerLookup.Add(fxName, args.HandledBy);
                    }
                    if (args.FunctionHandled)
                        result = args.Result;
                    else
                        result = "'@" + fxName + "' function is not recognized.";
                    break;

                case "argument_list_par":
                case "argument_list_opt":
                    if (node.ChildNodes.Count == 0)
                        return new List<object>();
                    else
                        result = ProcessNode(node.FirstChild, fxName);
                    break;

                case "argument_list":
                    var parameters = new List<object>();
                    foreach (var p in node.ChildNodes)
                    {
                        var pValue = ProcessNode(p, fxName);
                        if (pValue != null)
                            parameters.Add(pValue);
                    }
                    result = parameters;
                    break;

                case "bin_op_expression":
                    var b_operand1 = ProcessNode(node.ChildNodes[0], fxName);
                    var b_operand2 = ProcessNode(node.ChildNodes[2], fxName);
                    var b_op = node.ChildNodes[1].FindTokenAndGetText();
                    result = EvaluateOperation(b_operand1, b_op, b_operand2);
                    break;

                case "parenthesized_expression":
                    result = ProcessNode(node.ChildNodes[0], fxName);
                    break;

                case "unary_expression":
                    var u_op = node.FindTokenAndGetText();
                    var u_operand = ProcessNode(node.ChildNodes[1], fxName);
                    result = EvaluateUnaryOperation(u_op, u_operand);
                    break;

                case "StringLiteral":
                    // Remove the surrounding quotes (")
                    var s = node.FindTokenAndGetText();
                    s = ResolveExpressionVariables(s); // resolve embedded field variables
                    result = s.Length > 2 ? s.Substring(1, s.Length - 2) : string.Empty;
                    break;

                case "guid_variable":
                    result = _variables[node.FindTokenAndGetText()];
                    break;

                case "Number":
                    var n = node.FindTokenAndGetText();
                    if (n.Contains("."))
                        result = decimal.Parse(n);
                    else
                        result = int.Parse(n);
                    break;

                case "true":
                case "True":
                    result = true;
                    break;
                case "false":
                case "False":
                    result = false;
                    break;
                case "null":
                    result = null;
                    break;
                default:
                    throw new Exception(string.Format("Unhandled argument type: [{0}][{1}]", node.Term.Name, node.FindTokenAndGetText()));
            }

            return result;
        }


        public string ResolveExpressionVariables(string text)
        {
            if (_variables.Count > 0)
                return _rxVariable.Replace(text, x => _variables.ContainsKey(x.Value) ? _variables[x.Value] : string.Empty);
            return text;
        }

        public static object EvaluateOperation(object operand1, string op, object operand2)
        {
            var result = (object)null;

            if (op == "==" || op == "!=")
            {
                switch (op)
                {
                    case "==":
                        result = operand1.ToString() == operand2.ToString();
                        break;
                    case "!=":
                        result = operand1.ToString() != operand2.ToString();
                        break;
                }
            }
            else if (operand1 is bool && operand2 is bool)
            {
                switch (op)
                {
                    case "||":
                        result = (bool)operand1 || (bool)operand2;
                        break;
                    case "&&":
                        result = (bool)operand1 && (bool)operand2;
                        break;
                    case "|":
                        result = (bool)operand1 | (bool)operand2;
                        break;
                    case "&":
                        result = (bool)operand1 & (bool)operand2;
                        break;

                }
            }
            else if ((operand1 is int && operand2 is int)
                  || (operand1 is decimal && operand2 is decimal))
            {
                var d1 = System.Convert.ToDecimal(operand1);
                var d2 = System.Convert.ToDecimal(operand2);
                switch (op)
                {
                    case "<":
                        result = d1 < d2;
                        break;
                    case ">":
                        result = d1 > d2;
                        break;
                    case "<=":
                        result = d1 <= d2;
                        break;
                    case ">=":
                        result = d1 >= d2;
                        break;
                    case "+":
                        result = d1 + d2;
                        break;
                    case "-":
                        result = d1 - d2;
                        break;
                    case "*":
                        result = d1 * d2;
                        break;
                    case "/":
                        result = d1 / d2;
                        break;
                    case "%":
                        result = d1 % d2;
                        break;
                }

                if (operand1 is int && operand2 is int && result is decimal)
                    result = System.Convert.ToInt32(result);

            }
            else if (operand1 is string && operand2 is string)
            {
                switch (op)
                {
                    case "+":
                        result = (string)operand1 + (string)operand2;
                        break;
                }
            }
            else
            {
                throw new Exception("Unhandled operation: " + operand1.ToString() + " " + op + " " + operand2.ToString());
            }

            return result;
        }

        public static object EvaluateUnaryOperation(string op, object operand)
        {
            var result = (object)null;

            if (op == "!" && operand is bool)
                result = !(bool)operand;
            else if (op == "-")
            {
                if (operand is int)
                    result = -1 * (int)operand;
                else if (operand is decimal)
                    result = -1 * (decimal)operand;
            }

            return result;
        }
    }
}
