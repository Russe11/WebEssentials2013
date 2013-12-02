using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace MadsKristensen.EditorExtensions
{
    internal class IntellisenseWriter
    {
        public void Write(List<IntellisenseObject> objects, string file)
        {
            StringBuilder sb = new StringBuilder();

            if (Path.GetExtension(file).Equals(".ts", StringComparison.OrdinalIgnoreCase))
                WriteTypeScript(objects, sb);
            else
                WriteJavaScript(objects, sb);

            WriteFileToDisk(file, sb);
        }

        private static void WriteJavaScript(List<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("var server = server || {};");

            foreach (IntellisenseObject io in objects)
            {
                sb.AppendLine("server." + io.Name + " = function()  {");

                foreach (var p in io.Properties)
                {
                    string value = GetJavascriptValue(p.Type);
                    string comment = p.Summary ?? "The " + p.Name + " property as defined in " + io.FullName;
                    comment = Regex.Replace(comment, @"\s*[\r\n]+\s*", " ").Trim();
                    sb.AppendLine("\t/// <field name=\"" + p.Name + "\" type=\"" + value + "\">" +
                                  SecurityElement.Escape(comment) + "</field>");
                    sb.AppendLine("\tthis." + p.Name + " = new " + value + "();");
                }

                sb.AppendLine("};");
                sb.AppendLine();
            }
        }

        private static void WriteTypeScript(List<IntellisenseObject> objects, StringBuilder sb)
        {
            sb.AppendLine("declare module server {");
            sb.AppendLine();

            foreach (IntellisenseObject io in objects)
            {
                // Check to see if the class type is generic<>
                var genericType = Regex.Match(io.FullName, @"<(?<genericType>.*?)>\Z");

                if (genericType.Success)
                {
                    var t = genericType.Groups["genericType"].Value;

                    var typeCheck = ConvertTypeScriptType(t);

                    if (typeCheck != null)
                    {
                        t = typeCheck;
                    }


                    sb.AppendLine("\tinterface " + io.Name + "<" + t + "> {");
                }
                else
                {
                    sb.AppendLine("\tinterface " + io.Name + "{");
                }

                foreach (var p in io.Properties)
                {
                    string value = GetTypeScriptValue(p.Type);
                    sb.AppendLine("\t\t" + p.Name + ": " + value + ";");
                }

                sb.AppendLine("}");
            }

            sb.AppendLine("}");
        }

        private static void WriteFileToDisk(string fileName, StringBuilder sb)
        {
            //string current = string.Empty;
            //if (File.Exists(fileName))
            //{
            //    current = File.ReadAllText(fileName);
            //}

            //if (current != sb.ToString())
            //{
            File.WriteAllText(fileName, sb.ToString());
            //}
        }

        public static string GetJavascriptValue(string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "int":
                case "int32":
                case "int64":
                case "long":
                case "double":
                case "float":
                case "decimal":
                    return "Number";

                case "system.datetime":
                    return "Date";

                case "string":
                    return "String";

                case "bool":
                case "boolean":
                    return "Boolean";
            }

            if (type.Contains("System.Collections") || type.Contains("[]") || type.Contains("Array"))
                return "Array";

            return "Object";
        }

        public static string GetTypeScriptValue(string type)
        {
            // Remove ? from nullable field names
            type = type.Replace("?", "");

            // First check if the type is a primative type. If so convert to TS type and return.
            var basicConvert = ConvertTypeScriptType(type);
            if (basicConvert != null)
            {
                return basicConvert;
;           }

            // Next is it a collection or Array 
            if (type.Contains("System.Collections") || type.Contains("Array"))
            {
                var match = Regex.Match(type, @"\.(?<name>[a-zA-Z]{1,}?)<(?<type>.+?)>\Z");

                if (match.Success)
                {
                    var typeMatch = match.Groups["type"].Value;

                    var typeCheck = ConvertTypeScriptType(typeMatch);

                    if (typeCheck != null)
                    {
                        return "Array<" + typeCheck + ">";
                    }
                    else
                    {
                        var split = match.Groups["type"].Value.Split('.');
                        type = split[split.Length-1];
                        return "Array<" + type + ">";
                    }
                }
            }

            // Is it a [] array
            if (type.Contains("[]"))
            {
                var match = Regex.Match(type, @"(?<type>[a-zA-Z]{1,}?)\[\]\Z").Groups["type"].Value;
                return "Array<" + match + ">";
            }

            // Is the type a generic class reference
            var genericMatch = Regex.Match(type, @"(?<name>[a-zA-Z]{1,}?)<(?<type>.+?)>\Z");
            if (genericMatch.Success)
            {
                var t = genericMatch.Groups["type"].Value;

                var convertedType = ConvertTypeScriptType(t);

                if (convertedType == null)
                {
                    return genericMatch.Value;
                }
                else
                {
                    return genericMatch.Groups["name"].Value + "<" + convertedType + ">";
                }
            }


            return Regex.Match(type, @"(?<type>[a-zA-Z]{1,}?)\Z").Groups["type"].Value;
        }

        public static string ConvertTypeScriptType(string type)
        {
            switch (
            type.ToLowerInvariant())
            {
                case "int":
                case "int32":
                case "int64":
                case "long":
                case "double":
                case "float":
                case "decimal":
                    return "Number"
                ;

                case "system.datetime":
                    return "Date";

                case "string":
                    return "String";

                case "bool":
                case "boolean":
                    return "Boolean";
            }

            return null;
        }
    }



    public class IntellisenseObject
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public List<IntellisenseProperty> Properties { get; set; }
    }

    public class IntellisenseProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Summary { get; set; }
    }
}
