using System.Collections.Generic;
using System.Linq;
using ROSNET.Field;

namespace ROSNET.ROSMessageParser
{
    public static class MessageDataParser
    {
        internal static Dictionary<string, FieldValue> ParseMessageData(byte[] messageData, List<FieldValue> messageDefinition)
        {
            var fieldValueByFieldName = new Dictionary<string, FieldValue>();
            var messageDataPos = 0;
            int fieldSize;

            foreach (var definitionField in messageDefinition)
            {
                if (definitionField is ArrayFieldValue)
                {
                    var arrayField = definitionField as ArrayFieldValue;

                    if (arrayField.FixedArrayLength == 0)
                    {
                        messageDataPos += 32;
                    }

                    foreach (var fieldValue in arrayField.ArrayFields)
                    {
                        fieldSize = fieldValue.GetBitLength();
                        fieldValue.Value = messageData.Skip(messageDataPos).Take(fieldSize).ToArray();

                        messageDataPos += fieldSize;
                    }

                    fieldValueByFieldName.Add(arrayField.Name, arrayField);

                } else
                {
                    definitionField.Value = messageData.Skip(messageDataPos).Take(definitionField.GetBitLength()).ToArray();
                    fieldValueByFieldName.Add(definitionField.Name, definitionField);

                    messageDataPos += definitionField.GetBitLength();
                }
    

            }

            return fieldValueByFieldName;
        }


    }
}
