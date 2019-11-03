using System.Collections.Generic;

namespace RoslynCodeGeneratorTest.TypeMapping
{
    internal static class ZclDataType
    {
        public static readonly Dictionary<string, DataTypeMap> Mapping;

        static ZclDataType()
        {
            Mapping = new Dictionary<string, DataTypeMap>
            {
                { "CHARACTER_STRING", new DataTypeMap("string", 0x42, false) },
                { "IEEE_ADDRESS", new DataTypeMap("IeeeAddress", 0xf0,false},
                //{ "EXTENDED_PANID", new DataTypeMap("Long", 0, 0, false) },
                { "NODE_DESCRIPTOR", new DataTypeMap("NodeDescriptor", 0, false) },
                { "SIMPLE_DESCRIPTOR", new DataTypeMap("SimpleDescriptor", 0, false) },
                { "COMPLEX_DESCRIPTOR", new DataTypeMap("ComplexDescriptor", 0, false) },
                { "POWER_DESCRIPTOR", new DataTypeMap("PowerDescriptor", 0, false) },
                { "USER_DESCRIPTOR", new DataTypeMap("UserDescriptor", 0, false) },
                { "NEIGHBOR_TABLE", new DataTypeMap("NeighborTable", 0, false) },
                { "ROUTING_TABLE", new DataTypeMap("RoutingTable", 0, false) },
                { "NWK_ADDRESS", new DataTypeMap("ushort", 0, false) },
                { "N_X_IEEE_ADDRESS", new DataTypeMap("List<long>", 0, false) },
                { "N_X_NWK_ADDRESS", new DataTypeMap("List<ushort>", 0, false) },
                { "CLUSTERID", new DataTypeMap("ushort", 0, false) },
                { "N_X_CLUSTERID", new DataTypeMap("List<ushort>", 0, false) },
                { "ENDPOINT", new DataTypeMap("byte", 0, false) },
                { "N_X_ENDPOINT", new DataTypeMap("List<byte>", 0, false) },
                { "N_X_EXTENSION_FIELD_SET", new DataTypeMap("List<ExtensionFieldSet>", 0, false) },
                { "N_X_NEIGHBORS_INFORMATION", new DataTypeMap("List<NeighborInformation>", 0, false) },
                { "N_X_UNSIGNED_16_BIT_INTEGER", new DataTypeMap("List<ushort>", 0, false) },
                { "UNSIGNED_8_BIT_INTEGER_ARRAY", new DataTypeMap("byte[]", 0, false) },
                { "X_UNSIGNED_8_BIT_INTEGER", new DataTypeMap("List<byte>", 0, false) },
                { "N_X_UNSIGNED_8_BIT_INTEGER", new DataTypeMap("List<byte>", 0, false) },
                { "N_X_ATTRIBUTE_IDENTIFIER", new DataTypeMap("List<ushort>", 0, false) },
                { "N_X_READ_ATTRIBUTE_STATUS_RECORD", new DataTypeMap("List<ReadAttributeStatusRecord>", 0, false) },
                { "N_X_WRITE_ATTRIBUTE_RECORD", new DataTypeMap("List<WriteAttributeRecord>", 0, false) },
                { "N_X_WRITE_ATTRIBUTE_STATUS_RECORD", new DataTypeMap("List<WriteAttributeStatusRecord>", 0, false) },
                { "N_X_ATTRIBUTE_REPORTING_CONFIGURATION_RECORD", new DataTypeMap("List<AttributeReportingConfigurationRecord>", 0, false) },
                { "N_X_ATTRIBUTE_STATUS_RECORD", new DataTypeMap("List<AttributeStatusRecord>", 0, false) },
                { "N_X_ATTRIBUTE_RECORD", new DataTypeMap("List<AttributeRecord>", 0, false) },
                { "N_X_ATTRIBUTE_REPORT", new DataTypeMap("List<AttributeReport>", 0, false) },
                { "N_X_ATTRIBUTE_INFORMATION", new DataTypeMap("List<AttributeInformation>", 0, false) },
                { "N_X_ATTRIBUTE_SELECTOR", new DataTypeMap("object", 0, false) },
                { "N_X_EXTENDED_ATTRIBUTE_INFORMATION", new DataTypeMap("List<ExtendedAttributeInformation>", 0, false) },
                { "BOOLEAN", new DataTypeMap("bool", 0x10, false) },
                { "SIGNED_8_BIT_INTEGER", new DataTypeMap("sbyte", 0x28, true) },
                { "SIGNED_16_BIT_INTEGER", new DataTypeMap("short", 0x29, true) },
                { "SIGNED_32_BIT_INTEGER", new DataTypeMap("int", 0x2b, true) },
                { "UNSIGNED_8_BIT_INTEGER", new DataTypeMap("byte", 0x20, true) },
                { "UNSIGNED_16_BIT_INTEGER", new DataTypeMap("ushort", 0x21, true) },
                { "UNSIGNED_24_BIT_INTEGER", new DataTypeMap("uint", 0x22, true) },
                { "UNSIGNED_32_BIT_INTEGER", new DataTypeMap("uint", 0x23, true) },
                { "UNSIGNED_40_BIT_INTEGER", new DataTypeMap("ulong", 0x24, true) },
                { "UNSIGNED_48_BIT_INTEGER", new DataTypeMap("ulong", 0x25, true) },
                { "BITMAP_8_BIT", new DataTypeMap("byte", 0x18, false) },
                { "BITMAP_16_BIT", new DataTypeMap("ushort", 0x19, false) },
                { "BITMAP_24_BIT", new DataTypeMap("int", 0x1a, false) },
                { "BITMAP_32_BIT", new DataTypeMap("int", 0x1b, false) },
                { "ENUMERATION_16_BIT", new DataTypeMap("ushort", 0x31, false) },
                { "ENUMERATION_8_BIT", new DataTypeMap("byte", 0x30, false) },
                { "DATA_8_BIT", new DataTypeMap("byte", 0x08, false) },
                { "OCTET_STRING", new DataTypeMap("ByteArray", 0x41, false) },
                { "UTCTIME", new DataTypeMap("DateTime", 0xe2, true) },
                { "ZDO_STATUS", new DataTypeMap("ZdoStatus", 0, false) },
                { "ZCL_STATUS", new DataTypeMap("ZclStatus", 0, false) },
                { "ZIGBEE_DATA_TYPE", new DataTypeMap("ZclDataType", 0, false) },
                { "EXTENDED_PANID", new DataTypeMap("ExtendedPanId", 0, false) },
                { "BINDING_TABLE", new DataTypeMap("BindingTable", 0, false) },
                { "N_X_BINDING_TABLE", new DataTypeMap("List<BindingTable>", 0, false) },
                { "BYTE_ARRAY", new DataTypeMap("ByteArray", 0, false) },
                { "IMAGE_UPGRADE_STATUS", new DataTypeMap("ImageUpgradeStatus", 0, false) }
            };
        }
    }
}
