using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ROSNET;
using ROSNET.Enum;

namespace ROSbag_ReadWrite.ROSMessageParser
{
    public static class MessageDefinitionParser
    {

        public static List<FieldValue> ParseMessageDefinition(string OMessageDefinition)
        {
            var messages = OMessageDefinition.Split("================================================================================\n");
            var mainMessageDefinition = messages.First();

            var fieldValuesByMessageName = new Dictionary<string, List<FieldValue>>();
            foreach(var messageDefinition in messages.Skip(1).Reverse())
            {
                (var name, var fields) = ParseSubMessage(messageDefinition, fieldValuesByMessageName);
                fieldValuesByMessageName.Add(name, fields);
                if (name.Contains("/"))
                {
                    fieldValuesByMessageName.Add(name.Split("/").Last(), fields);
                }

            }

            return ParseMainMessage(mainMessageDefinition,fieldValuesByMessageName);


        }

        private static (string name, List<FieldValue> fields) ParseSubMessage(string subMessageDefinition, Dictionary<string, List<FieldValue>> fieldValuesByMessageName )
        {
            
            var messageName = subMessageDefinition.Split("\n").First().Split(" ").Last();
            var lines = subMessageDefinition.Split("\n").Skip(1);


            var fieldValues = ParseMessage(lines, fieldValuesByMessageName);


            return (messageName, fieldValues.ToList());

      
        }

        private static List<FieldValue> ParseMainMessage(string mainMessageDefinition, Dictionary<string, List<FieldValue>> fieldValuesByMessageName)
        {
            var lines = mainMessageDefinition.Split("\n");

            var fieldValues = ParseMessage(lines, fieldValuesByMessageName);


            return fieldValues.ToList();


        }

        private static List<FieldValue> ParseMessage(IEnumerable<string> lines, Dictionary<string, List<FieldValue>> fieldValuesByMessageName)
        {
            var validLines = new List<String>();
            Regex r = new Regex(@"#.*");
            foreach (var line in lines)
            {
                var tempLine = r.Replace(line, "").Trim();
                if (!string.IsNullOrWhiteSpace(tempLine))
                {
                    validLines.Add(tempLine);
                }
            }

            var fieldDefinitions = validLines.Where(l => l.FirstOrDefault() != '#' && !string.IsNullOrWhiteSpace(l));

            var fieldValues = new List<FieldValue>();
            Regex arrayRegex = new Regex(@".*\[\]");
            Regex fixedLengthArrayRegex = new Regex(@".*\[[0-9]+\]");
            foreach (var fieldDefinition in fieldDefinitions)
            {
                var tokens = fieldDefinition.Split(" ").SelectMany(t => t.Split("="));
                if (tokens.Count() == 2)
                {
                    var name = tokens.Last();
                    var dataDefinition = tokens.First();
                    if (Enum.TryParse(typeof(PrimitiveType), dataDefinition.ToUpper(), out var dataType))
                    {
                        var fieldValue = new FieldValue(name, (PrimitiveType)dataType);
                        fieldValues.Add(fieldValue);

                    }
                    else if (arrayRegex.IsMatch(dataDefinition))
                    {
                        var arrayType = dataDefinition.Split("[]").First();

                        if (Enum.TryParse(typeof(PrimitiveType), arrayType.ToUpper(), out var arrayDataType))
                        {
                            var arrayFieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "0", (PrimitiveType)arrayDataType) });
                            fieldValues.Add(arrayFieldValue);

                        }
                        else
                        {
                            if (fieldValuesByMessageName.TryGetValue(arrayType, out var subMessageFieldValues))
                            {
                                var arrayFieldValue = new ArrayFieldValue(name, subMessageFieldValues);
                                fieldValues.Add(arrayFieldValue);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Could not find {arrayType}");
                            }
                        }
                    }
                    else if (fixedLengthArrayRegex.IsMatch(dataDefinition))
                    {
                        Regex lengthRegex = new Regex(@"(?<=\[)([0-9]*?)(?=\])");
                        var arrayType = dataDefinition.Split("[").First();
                        var arrayLength = int.Parse(lengthRegex.Match(dataDefinition).Value);

                        if (Enum.TryParse(typeof(PrimitiveType), arrayType.ToUpper(), out var arrayDataType))
                        {
                            
                            var arrayFieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "0", (PrimitiveType)arrayDataType) }, arrayLength);
                            fieldValues.Add(arrayFieldValue);

                        }
                        else
                        {
                            if (fieldValuesByMessageName.TryGetValue(arrayType, out var subMessageFieldValues))
                            {
                                var arrayFieldValue = new ArrayFieldValue(name, subMessageFieldValues, arrayLength);
                                fieldValues.Add(arrayFieldValue);
                            }
                            else
                            {
                                throw new KeyNotFoundException($"Could not find {arrayType}");
                            }
                        }
                    }
                    else
                    {
                        if (fieldValuesByMessageName.TryGetValue(tokens.First(), out var subMessageFieldValues))
                        {
                            fieldValues.AddRange(subMessageFieldValues);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Could not find {tokens.First()}");
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
