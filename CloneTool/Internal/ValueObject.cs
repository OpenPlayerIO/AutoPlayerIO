using System;
using System.Collections.Generic;

namespace CloneTool.Internal
{
    public record ValueObject(ValueType ValueType, string String, int Int, uint UInt, long Long, bool Bool, float Float, double Double, byte[] ByteArray, long DateTime, List<ArrayProperty> ArrayProperties, List<ObjectProperty> ObjectProperties)
    {
        public object GetValue()
        {
            switch (ValueType)
            {
                case ValueType.String:
                    return String;

                case ValueType.Bool:
                    return Bool;

                case ValueType.ByteArray:
                    return ByteArray;

                case ValueType.Int:
                    return Int;

                case ValueType.UInt:
                    return UInt;

                case ValueType.DateTime:
                    return DateTime;

                case ValueType.Double:
                    return Double;

                case ValueType.Float:
                    return Float;

                case ValueType.Obj:
                    return ObjectProperties;

                case ValueType.Array:
                    return ArrayProperties;

                default:
                    throw new NotImplementedException($"GetValue() is not implemented for ValueType '{ValueType.ToString()}'.");
            }
        }
    }
}