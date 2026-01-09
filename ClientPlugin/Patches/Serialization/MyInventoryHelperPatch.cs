using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Nrbf;
using System.IO;
using System.Linq;
using HarmonyLib;
using VRage.GameServices;

namespace ClientPlugin.Patches.Serialization;

// ReSharper disable once UnusedType.Global
[HarmonyPatch(typeof(MyInventoryHelper))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyInventoryHelperPatch
{
    // ReSharper disable once UnusedMember.Local
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
}