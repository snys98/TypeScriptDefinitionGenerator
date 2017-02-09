﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TypeScriptDefinitionGenerator
{
    internal static class IntellisenseWriter
    {
        private static readonly Regex _whitespaceTrimmer = new Regex(@"^\s+|\s+$|\s*[\r\n]+\s*", RegexOptions.Compiled);

        public static string WriteTypeScript(IEnumerable<IntellisenseObject> objects)
        {
            var sb = new StringBuilder();

            foreach (var ns in objects.GroupBy(o => o.Namespace))
            {
                sb.AppendFormat("declare module {0} {{\r\n", ns.Key);

                foreach (IntellisenseObject io in ns)
                {
                    if (!string.IsNullOrEmpty(io.Summary))
                        sb.AppendLine("\t/** " + _whitespaceTrimmer.Replace(io.Summary, "") + " */");

                    if (io.IsEnum)
                    {
                        sb.AppendLine("\tconst enum " + CamelCaseClassName(io.Name) + " {");

                        foreach (var p in io.Properties)
                        {
                            WriteTypeScriptComment(p, sb);

                            if (p.InitExpression != null)
                            {
                                sb.AppendLine("\t\t" + CamelCaseEnumValue(p.Name) + " = " + CleanEnumInitValue(p.InitExpression) + ",");
                            }
                            else
                            {
                                sb.AppendLine("\t\t" + CamelCaseEnumValue(p.Name) + ",");
                            }
                        }

                        sb.AppendLine("\t}");
                    }
                    else
                    {
                        sb.Append("\tinterface ").Append(CamelCaseClassName(io.Name)).Append(" ");

                        if (!string.IsNullOrEmpty(io.BaseName))
                        {
                            sb.Append("extends ");

                            if (!string.IsNullOrEmpty(io.BaseNamespace) && io.BaseNamespace != io.Namespace)
                                sb.Append(io.BaseNamespace).Append(".");

                            sb.Append(io.BaseName).Append(" ");
                        }

                        WriteTSInterfaceDefinition(sb, "\t", io.Properties);
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string CamelCaseEnumValue(string name)
        {
            if (DtsPackage.Options.CamelCaseEnumerationValues)
            {
                name = CamelCase(name);
            }
            return name;
        }

        private static string CamelCasePropertyName(string name)
        {
            if (DtsPackage.Options.CamelCasePropertyNames)
            {
                name = CamelCase(name);
            }
            return name;
        }

        private static string CamelCaseClassName(string name)
        {
            if (DtsPackage.Options.CamelCaseTypeNames)
            {
                name = CamelCase(name);
            }
            return name;
        }

        private static string CamelCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return name[0].ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) + name.Substring(1);
        }

        private static string CleanEnumInitValue(string value)
        {
            value = value.TrimEnd('u', 'U', 'l', 'L'); //uint ulong long
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return value;
            var trimedValue = value.TrimStart('0'); // prevent numbers to be parsed as octal in js.
            if (trimedValue.Length > 0) return trimedValue;
            return "0";
        }


        private static void WriteTypeScriptComment(IntellisenseProperty p, StringBuilder sb)
        {
            if (string.IsNullOrEmpty(p.Summary)) return;
            sb.AppendLine("\t\t/** " + _whitespaceTrimmer.Replace(p.Summary, "") + " */");
        }

        private static void WriteTSInterfaceDefinition(StringBuilder sb, string prefix,
            IEnumerable<IntellisenseProperty> props)
        {
            sb.AppendLine("{");

            foreach (var p in props)
            {
                WriteTypeScriptComment(p, sb);
                sb.AppendFormat("{0}\t{1}: ", prefix, CamelCasePropertyName(p.Name));

                if (p.Type.IsKnownType) sb.Append(p.Type.TypeScriptName);
                else
                {
                    if (p.Type.Shape == null) sb.Append("any");
                    else WriteTSInterfaceDefinition(sb, prefix + "\t", p.Type.Shape);
                }
                if (p.Type.IsArray) sb.Append("[]");

                sb.AppendLine(";");
            }

            sb.Append(prefix).Append("}");
        }
    }
}