using System;
using System.Collections.Generic;
using System.Linq;
using ROSNET.Field;
using ROSNET.Type;

namespace ROSNET.ROSMessageParser
{
    public static class MessageDataParser
    {
        public static Dictionary<string, FieldValue> ParseMessageData(byte[] messageData, List<FieldValue> messageDefinition)
        {
            var fieldValueByFieldName = new Dictionary<string, FieldValue>();
            var posBytes = 0;

            foreach (var definitionField in messageDefinition)
            {
                if (definitionField is ArrayFieldValue)
                {
                    if (posBytes != 37)
                    {
                        var a = 1;
                    }
                    (var arrayField, var newPosBytes) = ParseArrayDefinitionField(definitionField as ArrayFieldValue, messageData, posBytes);
                    posBytes = newPosBytes;

                    fieldValueByFieldName.Add(arrayField.Name, arrayField);


                } else
                {
                    (var field, var newPosBytes) = ParseDefinitionField(definitionField, messageData, posBytes);
                    fieldValueByFieldName.Add(field.Name, field);
                    posBytes = newPosBytes;
                }
    

            }

            return fieldValueByFieldName;
        }

        public static (ArrayFieldValue, int) ParseArrayDefinitionField (ArrayFieldValue arrayDefinitionField, byte[] messageData, int posBytes )
        {
            uint arrayLength;
            if (arrayDefinitionField.FixedArrayLength == 0)
            {
                var lengthBytes = messageData.Skip(posBytes).Take(4).ToArray();
                arrayLength = BitConverter.ToUInt32(lengthBytes);
                posBytes += 4;
            }
            else
            {
                arrayLength = arrayDefinitionField.FixedArrayLength;
            }

            int j = 0;
            int arrayDefinitionLength = arrayDefinitionField.ArrayFields.Count;
            var arrayFields = new List<FieldValue>();
            for (uint i = 0; i < arrayLength && posBytes <messageData.Length; i++)
            {
                var definitionFieldValue = arrayDefinitionField.ArrayFields[j];
                if (definitionFieldValue is ArrayFieldValue)
                {
                    (var arrayFieldValue, var newPosBytes) = ParseArrayDefinitionField(definitionFieldValue as ArrayFieldValue, messageData, posBytes);
                    posBytes = newPosBytes;
                } else
                {
                    (var field, var newPosBytes) = ParseDefinitionField(definitionFieldValue, messageData, posBytes);
                    posBytes = newPosBytes;
                    arrayFields.Add(field);


                    j++;

                    if (j == arrayDefinitionLength)
                    {
                        j = 0;
                    }
                }

            }

            var arrayField = new ArrayFieldValue(arrayDefinitionField.Name, arrayFields);

            return (arrayField, posBytes);

        }

        public static (FieldValue, int) ParseDefinitionField(FieldValue definitionField, byte[] messageData, int posBytes)
        {
            var value = messageData.Skip(posBytes).Take(definitionField.GetByteLength()).ToArray();
            var field = new FieldValue(definitionField.Name, definitionField.DataType, value);

            var newPosBytes = posBytes + definitionField.GetByteLength();

            return (field, newPosBytes);
        }


    }
}
