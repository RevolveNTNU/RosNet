using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ROSNET.Type;
using ROSNET.Field;

namespace ROSNET.ROSMessageParser
{
    public static class MessageDefinitionParser
    {

        public static List<FieldValue> ParseMessageDefinition(string messageDefinition)
        {
            var definitions = messageDefinition.Split("================================================================================\n");
            var mainDefinition = definitions.First();

            var fieldValuesByDefinitionName = new Dictionary<string, List<FieldValue>>();
            foreach(var definition in definitions.Skip(1).Reverse())
            {
                (var name, var fields) = ParseSubDefinition(definition, fieldValuesByDefinitionName);
                fieldValuesByDefinitionName.Add(name, fields);
                if (name.Contains("/"))
                {
                    fieldValuesByDefinitionName.Add(name.Split("/").Last(), fields);
                }

            }

            return ParseMainDefinition(mainDefinition,fieldValuesByDefinitionName);


        }

        private static (string name, List<FieldValue> fields) ParseSubDefinition(string suDefinition, Dictionary<string, List<FieldValue>> fieldValuesByDefinitionName )
        {

            var definitionName = suDefinition.Split("\n").First().Split(" ").Last();
            var lines = suDefinition.Split("\n").Skip(1);


            var fieldValues = ParseDefinition(lines, fieldValuesByDefinitionName);


            return (definitionName, fieldValues.ToList());

      
        }

        private static List<FieldValue> ParseMainDefinition(string mainDefinition, Dictionary<string, List<FieldValue>> fieldValuesByDefinitionName)
        {
            var lines = mainDefinition.Split("\n");

            var fieldValues = ParseDefinition(lines, fieldValuesByDefinitionName);

            return fieldValues.ToList();


        }

        private static List<FieldValue> ParseDefinition(IEnumerable<string> lines, Dictionary<string, List<FieldValue>> fieldValuesByDefinitionName)
        {
            var validLines = new List<String>();
            Regex commentRegex = new Regex(@"#.*");
            foreach (var line in lines)
            {
                var tempLine = commentRegex.Replace(line, "").Trim();
                if (!string.IsNullOrWhiteSpace(tempLine))
                {
                    validLines.Add(tempLine);
                }
            }

            var validLinesList = validLines.Where(l => l.FirstOrDefault() != '#' && !string.IsNullOrWhiteSpace(l));

            var fieldValues = new List<FieldValue>();
            Regex arrayRegex = new Regex(@".*\[\]");
            Regex fixedLengthArrayRegex = new Regex(@".*\[[0-9]+\]");
            foreach (var line in validLinesList)
            {
                var fields = line.Split(" ").SelectMany(t => t.Split("="));
                if (fields.Count() == 2)
                {
                    var name = fields.Last();
                    var dataTypeString = fields.First();
                    if (Enum.TryParse(typeof(PrimitiveType), dataTypeString.ToUpper(), out var dataType))
                    {
                        var fieldValue = new FieldValue(name, (PrimitiveType)dataType);
                        Console.WriteLine(fieldValue.toString());
                        fieldValues.Add(fieldValue);

                    }
                    else if (arrayRegex.IsMatch(dataTypeString))
                    {
                        var arrayDataTypeString = dataTypeString.Split("[]").First();

                        if (Enum.TryParse(typeof(PrimitiveType), arrayDataTypeString.ToUpper(), out var arrayDataType))
                        {
                            var arrayFieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "InArray", (PrimitiveType)arrayDataType) });
                            fieldValues.Add(arrayFieldValue);

                        }
                        else
                        {
                            if (fieldValuesByDefinitionName.TryGetValue(arrayDataTypeString, out var subMessageFieldValues))
                            {
                                var arrayFieldValue = new ArrayFieldValue(name, subMessageFieldValues);
                                fieldValues.Add(arrayFieldValue);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Could not find {arrayDataTypeString}");
                            }
                        }
                    }
                    else if (fixedLengthArrayRegex.IsMatch(dataTypeString))
                    {
                        Regex lengthRegex = new Regex(@"(?<=\[)([0-9]*?)(?=\])");
                        var arrayType = dataTypeString.Split("[").First();
                        var arrayLength = int.Parse(lengthRegex.Match(dataTypeString).Value);

                        if (Enum.TryParse(typeof(PrimitiveType), arrayType.ToUpper(), out var arrayDataType))
                        {
                            
                            var arrayFieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "0", (PrimitiveType)arrayDataType) }, arrayLength);
                            fieldValues.Add(arrayFieldValue);

                        }
                        else
                        {
                            if (fieldValuesByDefinitionName.TryGetValue(arrayType, out var subMessageFieldValues))
                            {
                                var arrayFieldValue = new ArrayFieldValue(name, subMessageFieldValues, arrayLength);
                                fieldValues.Add(arrayFieldValue);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"The dataType of array: {arrayType} is not a primitive type or defined in MessageDefinition");
                            }
                        }
                    }
                    else
                    {
                        if (fieldValuesByDefinitionName.TryGetValue(fields.First(), out var subFieldValues))
                        {
                            //adds subDefinitionName to fieldName in fields from subdefinitions
                            var subFieldValuesCopy = new List<FieldValue>();
                            subFieldValuesCopy.AddRange(subFieldValues.Select(f => new FieldValue(name + "." + f.Name, f.DataType)));
                            fieldValues.AddRange(subFieldValuesCopy);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"The dataType: {fields.First()} is not a primitive type or defined in MessageDefinition");
                        }
                    }

                }
                else
                {
                    //parse constants here
                }
            }
            return fieldValues;
        }

    }
}
