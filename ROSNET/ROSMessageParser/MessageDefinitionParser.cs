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

            //skip the first messageDefinition which is the main definition and reverse the rest so we read the last ones first (they can be used by the ones above)
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
                        FieldValue fieldValue;
                        if ((PrimitiveType) dataType == PrimitiveType.STRING)
                        {
                            fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue("LetterInString", PrimitiveType.CHAR) });
                        } else
                        {
                            fieldValue = new FieldValue(name, (PrimitiveType)dataType);
                        }
                        fieldValues.Add(fieldValue);

                    }
                    else if (arrayRegex.IsMatch(dataTypeString))
                    {
                        var arrayDataTypeString = dataTypeString.Split("[]").First();

                        if (Enum.TryParse(typeof(PrimitiveType), arrayDataTypeString.ToUpper(), out var arrayDataType))
                        {
                            FieldValue fieldValue;
                            if ((PrimitiveType)arrayDataType == PrimitiveType.STRING)
                            {

                                fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new ArrayFieldValue(name, new List<FieldValue> { new FieldValue("LetterInString", PrimitiveType.CHAR) }) });

                            }
                            else { 
                            
                                fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "InArray", (PrimitiveType)arrayDataType) });
                            }

                            
                            fieldValues.Add(fieldValue);

                        }
                        else
                        {
                            if (fieldValuesByDefinitionName.TryGetValue(arrayDataTypeString, out var subMessageFieldValues))
                            {
                                var subMessageFieldValuesCopy = new List<FieldValue>();
                                subMessageFieldValuesCopy.AddRange(subMessageFieldValues.Select(f => new FieldValue(name + "." + f.Name + "InArray", f.DataType)));
                                fieldValues.AddRange(subMessageFieldValuesCopy);
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
                        var arrayLength = uint.Parse(lengthRegex.Match(dataTypeString).Value);

                        if (Enum.TryParse(typeof(PrimitiveType), arrayType.ToUpper(), out var arrayDataType))
                        {
                            FieldValue fieldValue;
                            if ((PrimitiveType)arrayDataType == PrimitiveType.STRING)
                            {

                                fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new ArrayFieldValue(name, new List<FieldValue> { new FieldValue("LetterInString", PrimitiveType.CHAR) }) }, arrayLength);

                            }
                            else
                            {

                                fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "InArray", (PrimitiveType)arrayDataType) }, arrayLength);
                            }


                            fieldValues.Add(fieldValue);


                        }
                        else
                        {
                            if (fieldValuesByDefinitionName.TryGetValue(arrayType, out var subMessageFieldValues))
                            {
                                var subFieldValuesCopy = new List<FieldValue>();

                                foreach (var subFieldValue in subMessageFieldValues)
                                {
                                    if (subFieldValue is ArrayFieldValue)
                                    {
                                        var arrayFieldValue = subFieldValue as ArrayFieldValue;
                                        subFieldValuesCopy.Add(new ArrayFieldValue(name + "." + arrayFieldValue.Name, arrayFieldValue.ArrayFields));
                                    }
                                    else
                                    {
                                        subFieldValuesCopy.Add(new FieldValue(name + "." + subFieldValue.Name, subFieldValue.DataType));
                                    }


                                }

                                fieldValues.AddRange(subFieldValuesCopy);

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
                            //adds subDefinitionName to fieldName in fields from subDefinitions
                            var subFieldValuesCopy = new List<FieldValue>();

                            foreach (var subFieldValue in subFieldValues)
                            {
                                if (subFieldValue is ArrayFieldValue)
                                {
                                    var arrayFieldValue = subFieldValue as ArrayFieldValue;
                                    subFieldValuesCopy.Add(new ArrayFieldValue(name + "." + arrayFieldValue.Name, arrayFieldValue.ArrayFields));
                                } else
                                {
                                    subFieldValuesCopy.Add(new FieldValue(name + "." + subFieldValue.Name, subFieldValue.DataType));
                                }
                                    
                                
                            }

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
