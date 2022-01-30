using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ROSNET.Type;
using ROSNET.Field;

namespace ROSNET.ROSMessageParser
{
    /// <summary>
    /// Parses messageDefinitionField in the data of connection record
    /// </summary>
    public static class MessageDefinitionParser
    {
        /// <summary>
        /// Parses the messageDefinition from bytes to list of fieldvalues
        /// </summary>
        /// <returns>List of fields in the messagedefinition</returns>
        public static List<FieldValue> ParseMessageDefinition(byte[] messageDefinitionBytes)
        {
            var messageDefinition = System.Text.Encoding.Default.GetString(messageDefinitionBytes);

            //splits the definitions in the messageDefinition
            var definitions = messageDefinition.Split("================================================================================\n");
            var mainDefinition = definitions.First();
            var fieldValuesBySubDefinitionName = new Dictionary<string, List<FieldValue>>();

            //skip maindefinition and reverse to parse the last subdefinitions first
            foreach(var definition in definitions.Skip(1).Reverse())
            {
                (var name, var fields) = ParseSubDefinition(definition, fieldValuesBySubDefinitionName);
                fieldValuesBySubDefinitionName.Add(name, fields);

                //Adds copy of subdefinition to the dictionary with last name of the subdefinition (dictionary contains std_msgs/Header and Header). Some definitions only use last name.
                if (name.Contains("/"))
                {
                    fieldValuesBySubDefinitionName.Add(name.Split("/").Last(), fields);
                }
            }

            return ParseMainDefinition(mainDefinition,fieldValuesBySubDefinitionName);


        }

        /// <summary>
        /// Parses a subdefinition in the messagedefinition
        /// </summary>
        /// <returns>name of subdefinition and list of fields in subdefinition</returns>
        private static (string, List<FieldValue>) ParseSubDefinition(string subDefinition, Dictionary<string, List<FieldValue>> fieldValuesByDefinitionName )
        {
            //gets the definitionname from first line and skips "MSG: " before the name
            var definitionName = subDefinition.Split("\n").First().Split(" ").Last();
            var lines = subDefinition.Split("\n").Skip(1);

            var fieldValues = ParseDefinition(lines, fieldValuesByDefinitionName);

            return (definitionName, fieldValues);
        }

        /// <summary>
        /// Parses the maindefinition using subdefinitions
        /// </summary>
        /// <returns>list of fields in mainmessage</returns>
        private static List<FieldValue> ParseMainDefinition(string mainDefinition, Dictionary<string, List<FieldValue>> fieldValuesByDefinitionName)
        {
            var lines = mainDefinition.Split("\n");

            var fieldValues = ParseDefinition(lines, fieldValuesByDefinitionName);

            return fieldValues;


        }

        /// <summary>
        /// Parses a definition using subdefinitions
        /// </summary>
        /// <returns>list of fields in definition</returns>
        private static List<FieldValue> ParseDefinition(IEnumerable<string> lines, Dictionary<string, List<FieldValue>> fieldValuesByDefinitionName)
        {
            var fields = new List<String>();
            Regex commentRegex = new Regex(@"#.*");
            foreach (var line in lines)
            {
                var tempLine = commentRegex.Replace(line, "").Trim(); //removes comments
                if (!string.IsNullOrWhiteSpace(tempLine)) //removes empty lines
                {
                    fields.Add(tempLine);
                }
            }

            var fieldValues = new List<FieldValue>();
            Regex arrayRegex = new Regex(@".*\[\]");
            Regex fixedLengthArrayRegex = new Regex(@".*\[[0-9]+\]");
            foreach (var field in fields)
            {
                var wordsInLine = field.Split(" ").SelectMany(t => t.Split("=")); //splits field into words
                if (wordsInLine.Count() == 2) //checks if the field is not a constant
                {
                    var name = wordsInLine.Last();
                    var dataTypeString = wordsInLine.First();
                    if (Enum.TryParse(typeof(PrimitiveType), dataTypeString.ToUpper(), out var dataType)) //checks if datatype is primitive
                    {
                        FieldValue fieldValue;
                        if ((PrimitiveType) dataType == PrimitiveType.STRING)
                        {
                            //creates new ArrayFieldValue since string is array of chars (uint8) with variable length
                            fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue("LetterInString", PrimitiveType.CHAR) });
                        } else
                        {
                            fieldValue = new FieldValue(name, (PrimitiveType)dataType);
                        }
                        fieldValues.Add(fieldValue);

                    }
                    else if (arrayRegex.IsMatch(dataTypeString)) //checks if field is array
                    {
                        var arrayDataTypeString = dataTypeString.Split("[]").First(); //finds datatype of array

                        if (Enum.TryParse(typeof(PrimitiveType), arrayDataTypeString.ToUpper(), out var arrayDataType)) //checks if datatype of array is primitive
                        {
                            FieldValue fieldValue;
                            if ((PrimitiveType)arrayDataType == PrimitiveType.STRING)
                            {
                                //creates new array of strings (array of chars (uint8)) 
                                fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new ArrayFieldValue(name, new List<FieldValue> { new FieldValue("LetterInString", PrimitiveType.CHAR) }) });

                            }
                            else {
                                fieldValue = new ArrayFieldValue(name, new List<FieldValue> { new FieldValue(name + "InArray", (PrimitiveType)arrayDataType) });
                            }

                            fieldValues.Add(fieldValue);

                        }
                        else
                        {
                            if (fieldValuesByDefinitionName.TryGetValue(arrayDataTypeString, out var subMessageFieldValues)) //checks if datatype of array is a subdefinition
                            {
                                // adds subDefinitionName to fieldName in fields from subDefinitions
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

                                var fieldValue = new ArrayFieldValue(name, subFieldValuesCopy);

                            }
                            else
                            {
                                throw new KeyNotFoundException($"Could not find {arrayDataTypeString}");
                            }
                        }
                    }
                    else if (fixedLengthArrayRegex.IsMatch(dataTypeString)) //check if field is array with fixed length
                    {
                        Regex lengthRegex = new Regex(@"(?<=\[)([0-9]*?)(?=\])");
                        var arrayType = dataTypeString.Split("[").First();
                        var arrayLength = uint.Parse(lengthRegex.Match(dataTypeString).Value);

                        if (Enum.TryParse(typeof(PrimitiveType), arrayType.ToUpper(), out var arrayDataType)) //check if datatype of array is primitive
                        {
                            FieldValue fieldValue;
                            if ((PrimitiveType)arrayDataType == PrimitiveType.STRING)
                            {
                                //creates new array of strings (array of chars (uint8)) with fixed length
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
                                // adds subDefinitionName to fieldName in fields from subDefinitions
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

                                var fieldValue = new ArrayFieldValue(name, subFieldValuesCopy, arrayLength);

                                fieldValues.Add(fieldValue);

                            }
                            else
                            {
                                throw new KeyNotFoundException($"The dataType of array: {arrayType} is not a primitive type or defined in MessageDefinition");
                            }
                        }
                    }
                    else
                    {
                        if (fieldValuesByDefinitionName.TryGetValue(wordsInLine.First(), out var subFieldValues)) //checks if field points to subdefiniition
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
                            throw new KeyNotFoundException($"The dataType: {wordsInLine.First()} is not a primitive type or defined in MessageDefinition");
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
