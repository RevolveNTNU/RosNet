using System;
using System.Collections.Generic;
using System.Linq;
using RosNet.Field;

namespace RosNet.RosMessageParser;

/// <summary>
/// Parses data in message record
/// </summary>
internal static class MessageDataParser
{

    /// <summary>
    /// Parses messagedata from bytes to fieldvalues using messageDefinition
    /// </summary>
    /// <returns>Dictionary contatining the fields in the messageData. Key is name of field and value is the fieldValue</returns>
    public static Dictionary<string, FieldValue> ParseMessageData(byte[] messageData, List<FieldValue> messageDefinition)
    {
        var fieldValueByFieldName = new Dictionary<string, FieldValue>();
        int posBytes = 0;

        foreach (var definitionField in messageDefinition)
        {
            if (definitionField is ArrayFieldValue)
            {
                ArrayFieldValue arrayField= ParseArrayDefinitionField(definitionField as ArrayFieldValue, messageData, ref posBytes);
                fieldValueByFieldName.Add(arrayField.Name, arrayField);

            } 
            else
            {
                FieldValue field = ParseDefinitionField(definitionField, messageData, ref posBytes);
                fieldValueByFieldName.Add(field.Name, field);
            }
        }
        return fieldValueByFieldName;
    }

    /// <summary>
    /// Parses an arrayfield in the messagedata on the position posBytes
    /// </summary>
    /// <returns> The parsed arrayfieldValue</returns>
    public static ArrayFieldValue ParseArrayDefinitionField (ArrayFieldValue arrayDefinitionField, byte[] messageData, ref int posBytes )
    {
        uint arrayLength;
        if (arrayDefinitionField.FixedArrayLength == 0)
        {
            byte[] lengthBytes = messageData.Skip(posBytes).Take(4).ToArray();
            arrayLength = BitConverter.ToUInt32(lengthBytes);
            posBytes += 4;
        }
        else
        {
            arrayLength = arrayDefinitionField.FixedArrayLength;
        }

        
        int j = 0; //position in arrayDefinitionField
        int arrayDefinitionLength = arrayDefinitionField.ArrayFields.Count;
        var arrayFields = new List<FieldValue>();
        for (uint i = 0; i < arrayLength; i++)
        {
            var definitionFieldValue = arrayDefinitionField.ArrayFields[j];
            if (definitionFieldValue is ArrayFieldValue)
            {
                ArrayFieldValue arrayFieldValue = ParseArrayDefinitionField(definitionFieldValue as ArrayFieldValue, messageData, ref posBytes);
            }
            else
            {
                FieldValue field = ParseDefinitionField(definitionFieldValue, messageData, ref posBytes);
                arrayFields.Add(field);

                j++;

                if (j == arrayDefinitionLength) //starts at beginning of arrayDefinitionField if it reaches the end
                {
                    j = 0;
                }
            }
        }
        return new ArrayFieldValue(arrayDefinitionField.Name, arrayFields, arrayDefinitionField.DataType);
    }

    /// <summary>
    /// Parses a field in the messageData on the position posBytes
    /// </summary>
    /// <returns> The parsed fieldvalue</returns>
    public static FieldValue ParseDefinitionField(FieldValue definitionField, byte[] messageData, ref int posBytes)
    {
        byte[] value = messageData.Skip(posBytes).Take(definitionField.GetByteLength()).ToArray();
        var field = new FieldValue(definitionField.Name, definitionField.DataType, value);

        posBytes += definitionField.GetByteLength();

        return field;
    }
}
