using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Nrbf;
using System.IO;
using System.Linq;
using HarmonyLib;
using VRage.GameServices;

namespace Shared.Patches.Serialization;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyInventoryHelper))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyInventoryHelperPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyInventoryHelper.GetItemsCheckData))]
    private static bool GetItemsCheckDataPrefix(List<MyGameInventoryItem> items, ref byte[] __result)
    {
        __result = WriteInventoryItems(items ?? []);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyInventoryHelper.GetItemCheckData))]
    private static bool GetItemCheckDataPrefix(MyGameInventoryItem item, ref byte[] __result)
    {
        __result = WriteInventoryItems([item]);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyInventoryHelper.CheckItemData))]
    private static bool ReadInventoryItemsPrefix(byte[] checkData, out bool checkResult, out List<MyGameInventoryItem> __result)
    {
        checkResult = false;
        __result = [];

        if (!NrbfDecoder.StartsWithPayloadHeader(checkData))
            return false;

        if (!TryDecodeInventory(checkData, out var count, out var inventoryItems))
            return false;

        __result.Clear();
        __result.Capacity = count;
        __result.AddRange(inventoryItems.Select(ReadInventoryItem).Where(i => i is not null));

        checkResult = true;
        return false;
    }

    private static bool TryDecodeInventory(byte[] checkData, out int count, out IEnumerable<ClassRecord> inventoryItems)
    {
        count = 0;
        inventoryItems = null;

        var decodedList = NrbfDecoder.DecodeClassRecord(new MemoryStream(checkData));
        if (decodedList.TypeNameMatches(typeof(List<MyGameInventoryItem>)) &&
            decodedList.GetArrayRecord("_items") is ArrayRecord decodedArray &&
            decodedArray.GetArray(typeof(MyGameInventoryItem[]), false) is SerializationRecord[] items)
        {
            count = decodedList.GetInt32("_size");
            inventoryItems = items.Take(count).Where(i => i is ClassRecord).Cast<ClassRecord>();
            return true;
        }

        return false;
    }

    private static MyGameInventoryItem ReadInventoryItem(ClassRecord itemRecord)
    {
        if (!itemRecord.TypeNameMatches(typeof(MyGameInventoryItem)))
            return null;

        var id = itemRecord.GetUInt64(BackingField("ID"));
        var quantity = itemRecord.GetUInt16(BackingField("Quantity"));
        var isStoreFakeItem = itemRecord.GetBoolean(BackingField("IsStoreFakeItem"));
        var isNew = itemRecord.GetBoolean(BackingField("IsNew"));

        var d = itemRecord.GetClassRecord(BackingField("ItemDefinition"));
        if (d?.TypeNameMatches(typeof(MyGameInventoryItemDefinition)) != true)
            return null;

        var qualityRecord = d.GetClassRecord(BackingField("ItemQuality"));
        if (qualityRecord?.TypeNameMatches(typeof(MyGameInventoryItemQuality)) != true)
            return null;

        var definitionTypeRecord = d.GetClassRecord(BackingField("DefinitionType"));
        if (definitionTypeRecord?.TypeNameMatches(typeof(MyGameInventoryItemDefinitionType)) != true)
            return null;

        var itemSlotRecord = d.GetClassRecord(BackingField("ItemSlot"));
        if (itemSlotRecord?.TypeNameMatches(typeof(MyGameInventoryItemSlot)) != true)
            return null;

        var itemDefinition = new MyGameInventoryItemDefinition
        {
            ID = d.GetInt32(BackingField("ID")),
            Name = d.GetString(BackingField("Name")),
            Tradable = d.GetString(BackingField("Tradable")),
            Marketable = d.GetString(BackingField("Marketable")),
            Description = d.GetString(BackingField("Description")),
            IconTexture = d.GetString(BackingField("IconTexture")),
            DisplayType = d.GetString(BackingField("DisplayType")),
            AssetModifierId = d.GetString(BackingField("AssetModifierId")),
            ItemSlot = (MyGameInventoryItemSlot)itemSlotRecord.GetInt32("value__"),
            ToolName = d.GetString(BackingField("ToolName")),
            NameColor = d.GetString(BackingField("NameColor")),
            BackgroundColor = d.GetString(BackingField("BackgroundColor")),
            DefinitionType = (MyGameInventoryItemDefinitionType)definitionTypeRecord.GetInt32("value__"),
            Hidden = d.GetBoolean(BackingField("Hidden")),
            IsStoreHidden = d.GetBoolean(BackingField("IsStoreHidden")),
            CanBePurchased = d.GetBoolean(BackingField("CanBePurchased")),
            ItemQuality = (MyGameInventoryItemQuality)qualityRecord.GetInt32("value__"),
            Exchange = d.GetString(BackingField("Exchange"))
        };

        return new MyGameInventoryItem
        {
            ID = id,
            ItemDefinition = itemDefinition,
            Quantity = quantity,
            UsingCharacters = [],
            IsStoreFakeItem = isStoreFakeItem,
            IsNew = isNew
        };
    }

    private static string BackingField(string name)
    {
        return $"<{name}>k__BackingField";
    }

    private static byte[] WriteInventoryItems(List<MyGameInventoryItem> items)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        new InventoryCheckDataWriter(writer).Write(items);
        return stream.ToArray();
    }

    private sealed class InventoryCheckDataWriter(BinaryWriter writer)
    {
        private const byte SerializedStreamHeader = 0;
        private const byte ClassWithMembersAndTypes = 5;
        private const byte BinaryObjectString = 6;
        private const byte BinaryArray = 7;
        private const byte MemberReference = 9;
        private const byte ObjectNull = 10;
        private const byte MessageEnd = 11;
        private const byte BinaryLibrary = 12;

        private const byte BinaryArrayTypeSingle = 0;

        private const byte BinaryTypePrimitive = 0;
        private const byte BinaryTypeString = 1;
        private const byte BinaryTypeObject = 2;
        private const byte BinaryTypeClass = 4;

        private const byte PrimitiveBoolean = 1;
        private const byte PrimitiveInt32 = 8;
        private const byte PrimitiveUInt16 = 14;
        private const byte PrimitiveUInt64 = 16;

        private const int RootObjectId = 1;
        private const int SystemLibraryId = 2;
        private const int VrageLibraryId = 3;

        private static readonly string SystemAssemblyName = typeof(List<MyGameInventoryItem>).Assembly.FullName;
        private static readonly string VrageAssemblyName = typeof(MyGameInventoryItem).Assembly.FullName;

        private static readonly string ListTypeName = TypeName(typeof(List<MyGameInventoryItem>));
        private static readonly string ItemArrayTypeName = TypeName(typeof(MyGameInventoryItem[]));
        private static readonly string ItemTypeName = TypeName(typeof(MyGameInventoryItem));
        private static readonly string DefinitionTypeName = TypeName(typeof(MyGameInventoryItemDefinition));
        private static readonly string HashSetTypeName = TypeName(typeof(HashSet<long>));
        private static readonly string ItemSlotTypeName = TypeName(typeof(MyGameInventoryItemSlot));
        private static readonly string DefinitionTypeTypeName = TypeName(typeof(MyGameInventoryItemDefinitionType));
        private static readonly string ItemQualityTypeName = TypeName(typeof(MyGameInventoryItemQuality));

        private int m_nextObjectId = 4;

        public void Write(List<MyGameInventoryItem> items)
        {
            WriteSerializedStreamHeader();
            WriteBinaryLibrary(SystemLibraryId, SystemAssemblyName);
            WriteBinaryLibrary(VrageLibraryId, VrageAssemblyName);
            WriteInventoryList(items);
            writer.Write(MessageEnd);
        }

        private void WriteSerializedStreamHeader()
        {
            writer.Write(SerializedStreamHeader);
            writer.Write(RootObjectId);
            writer.Write(-1);
            writer.Write(1);
            writer.Write(0);
        }

        private void WriteBinaryLibrary(int libraryId, string libraryName)
        {
            writer.Write(BinaryLibrary);
            writer.Write(libraryId);
            writer.Write(libraryName);
        }

        private void WriteInventoryList(List<MyGameInventoryItem> items)
        {
            var arrayObjectId = NextObjectId();

            WriteClassWithMembersAndTypes(
                RootObjectId,
                ListTypeName,
                ["_items", "_size", "_version", "_syncRoot"],
                [BinaryTypeClass, BinaryTypePrimitive, BinaryTypePrimitive, BinaryTypeObject],
                [new ClassTypeInfo(ItemArrayTypeName, VrageLibraryId), PrimitiveInt32, PrimitiveInt32, null],
                SystemLibraryId);

            WriteMemberReference(arrayObjectId);
            writer.Write(items.Count);
            writer.Write(items.Count);
            WriteNull();

            WriteItemArray(arrayObjectId, items);
        }

        private void WriteItemArray(int objectId, List<MyGameInventoryItem> items)
        {
            writer.Write(BinaryArray);
            writer.Write(objectId);
            writer.Write(BinaryArrayTypeSingle);
            writer.Write(1);
            writer.Write(items.Count);
            writer.Write(BinaryTypeClass);
            writer.Write(ItemTypeName);
            writer.Write(VrageLibraryId);

            foreach (var item in items)
            {
                if (item is null)
                    WriteNull();
                else
                    WriteInventoryItem(item);
            }
        }

        private void WriteInventoryItem(MyGameInventoryItem item)
        {
            WriteClassWithMembersAndTypes(
                NextObjectId(),
                ItemTypeName,
                [
                    BackingField("ID"),
                    BackingField("ItemDefinition"),
                    BackingField("Quantity"),
                    BackingField("UsingCharacters"),
                    BackingField("IsStoreFakeItem"),
                    BackingField("IsNew")
                ],
                [
                    BinaryTypePrimitive,
                    BinaryTypeClass,
                    BinaryTypePrimitive,
                    BinaryTypeClass,
                    BinaryTypePrimitive,
                    BinaryTypePrimitive
                ],
                [
                    PrimitiveUInt64,
                    new ClassTypeInfo(DefinitionTypeName, VrageLibraryId),
                    PrimitiveUInt16,
                    new ClassTypeInfo(HashSetTypeName, SystemLibraryId),
                    PrimitiveBoolean,
                    PrimitiveBoolean
                ],
                VrageLibraryId);

            writer.Write(item.ID);

            if (item.ItemDefinition is null)
                WriteNull();
            else
                WriteInventoryItemDefinition(item.ItemDefinition);

            writer.Write(item.Quantity);
            WriteNull();
            writer.Write(item.IsStoreFakeItem);
            writer.Write(item.IsNew);
        }

        private void WriteInventoryItemDefinition(MyGameInventoryItemDefinition definition)
        {
            WriteClassWithMembersAndTypes(
                NextObjectId(),
                DefinitionTypeName,
                [
                    BackingField("ID"),
                    BackingField("Name"),
                    BackingField("Tradable"),
                    BackingField("Marketable"),
                    BackingField("Description"),
                    BackingField("DisplayType"),
                    BackingField("IconTexture"),
                    BackingField("AssetModifierId"),
                    BackingField("ItemSlot"),
                    BackingField("ToolName"),
                    BackingField("NameColor"),
                    BackingField("BackgroundColor"),
                    BackingField("DefinitionType"),
                    BackingField("Hidden"),
                    BackingField("IsStoreHidden"),
                    BackingField("CanBePurchased"),
                    BackingField("ItemQuality"),
                    BackingField("Exchange")
                ],
                [
                    BinaryTypePrimitive,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeClass,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeString,
                    BinaryTypeClass,
                    BinaryTypePrimitive,
                    BinaryTypePrimitive,
                    BinaryTypePrimitive,
                    BinaryTypeClass,
                    BinaryTypeString
                ],
                [
                    PrimitiveInt32,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    new ClassTypeInfo(ItemSlotTypeName, VrageLibraryId),
                    null,
                    null,
                    null,
                    new ClassTypeInfo(DefinitionTypeTypeName, VrageLibraryId),
                    PrimitiveBoolean,
                    PrimitiveBoolean,
                    PrimitiveBoolean,
                    new ClassTypeInfo(ItemQualityTypeName, VrageLibraryId),
                    null
                ],
                VrageLibraryId);

            writer.Write(definition.ID);
            WriteString(definition.Name);
            WriteString(definition.Tradable);
            WriteString(definition.Marketable);
            WriteString(definition.Description);
            WriteString(definition.DisplayType);
            WriteString(definition.IconTexture);
            WriteString(definition.AssetModifierId);
            WriteEnum(ItemSlotTypeName, (int)definition.ItemSlot);
            WriteString(definition.ToolName);
            WriteString(definition.NameColor);
            WriteString(definition.BackgroundColor);
            WriteEnum(DefinitionTypeTypeName, (int)definition.DefinitionType);
            writer.Write(definition.Hidden);
            writer.Write(definition.IsStoreHidden);
            writer.Write(definition.CanBePurchased);
            WriteEnum(ItemQualityTypeName, (int)definition.ItemQuality);
            WriteString(definition.Exchange);
        }

        private void WriteEnum(string typeName, int value)
        {
            WriteClassWithMembersAndTypes(
                NextObjectId(),
                typeName,
                ["value__"],
                [BinaryTypePrimitive],
                [PrimitiveInt32],
                VrageLibraryId);

            writer.Write(value);
        }

        private void WriteClassWithMembersAndTypes(
            int objectId,
            string className,
            string[] memberNames,
            byte[] binaryTypes,
            object[] additionalInfos,
            int libraryId)
        {
            writer.Write(ClassWithMembersAndTypes);
            writer.Write(objectId);
            writer.Write(className);
            writer.Write(memberNames.Length);

            foreach (var memberName in memberNames)
                writer.Write(memberName);

            foreach (var binaryType in binaryTypes)
                writer.Write(binaryType);

            foreach (var (binaryType, additionalInfo) in binaryTypes.Zip(additionalInfos))
                WriteAdditionalTypeInfo(binaryType, additionalInfo);

            writer.Write(libraryId);
        }

        private void WriteAdditionalTypeInfo(byte binaryType, object additionalInfo)
        {
            switch (binaryType)
            {
                case BinaryTypePrimitive:
                    writer.Write((byte)additionalInfo);
                    break;

                case BinaryTypeClass:
                    var classTypeInfo = (ClassTypeInfo)additionalInfo;
                    writer.Write(classTypeInfo.TypeName);
                    writer.Write(classTypeInfo.LibraryId);
                    break;
            }
        }

        private void WriteString(string value)
        {
            if (value is null)
            {
                WriteNull();
                return;
            }

            writer.Write(BinaryObjectString);
            writer.Write(NextObjectId());
            writer.Write(value);
        }

        private void WriteMemberReference(int objectId)
        {
            writer.Write(MemberReference);
            writer.Write(objectId);
        }

        private void WriteNull()
        {
            writer.Write(ObjectNull);
        }

        private int NextObjectId()
        {
            return m_nextObjectId++;
        }

        private static string TypeName(Type type)
        {
            return type.FullName ?? type.Name;
        }

        private readonly record struct ClassTypeInfo(string TypeName, int LibraryId);
    }
}
