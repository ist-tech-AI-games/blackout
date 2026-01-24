using System.Collections.Generic;
using UnityEngine;

public class Storage : MapRegion
{
    // 변경 사항을 저장할 구조체. 완전히 성공한다고 판정되기 전에는 병합을 수행하지 않음(Atomic).
    private struct PendingChange
    {
        public MapTile TargetTile;
        public int AmountToAdd;
        public bool IsNewItem;
    }

    public override void OnUnitEnter(Unit unit)
    {
        if (unit.Team != OwnedTeam || unit.HoldingItem == null) return;

        (bool shouldRelease, bool shouldDestory) = TryDistributeItem(unit.HoldingItem);
        if (shouldRelease)
        {
            ItemObject item = unit.RetrieveItem();
            if (shouldDestory)
                item.OnDestroyed();
        }
        else
        {
            // storage is full; nop.
        }
    }

    // 세 가지 결과 존재.
    // 1. 적재 실패 -> 아이템 유지
    // 2. 적재 성공 및 빈 타일 하나가 채워짐 -> 배치는 이루어지므로 유닛이 내려놓기만 함.
    // 3. 적재 성공 및 어느 한 타일에 완전 흡수 -> ItemObject 파괴.
    private (bool shouldRelease, bool shouldDestroy) TryDistributeItem(ItemObject incomingItem)
    {
        List<PendingChange> plan = new();
        int currentRemaining = incomingItem.ItemAmount;
        
        // construct plan
        bool shouldDestory = true;
        foreach (var tile in MapTiles)
        {
            if (currentRemaining <= 0) break;

            // 빈 타일의 경우 null.
            ItemObject itemOnTile = tile.MapObjects.Find(x => x is ItemObject) as ItemObject;

            var result = MergeCalculator.Calculate(incomingItem.ItemData, currentRemaining, itemOnTile);

            if (result.Succeeded && result.AmountToTransfer > 0)
            {
                if (itemOnTile == null) shouldDestory = false;
                plan.Add(new PendingChange
                {
                    TargetTile = tile,
                    AmountToAdd = result.AmountToTransfer,
                    IsNewItem = itemOnTile == null
                });
                currentRemaining = result.RemainingInput;
            }
        }

        // 창고 포화로 인해 적재 불가; 취소.
        if (currentRemaining > 0)
        {
            return (false, false);
        }

        ExecutePlan(plan, incomingItem);
        return (true, shouldDestory);
    }

    private void ExecutePlan(List<PendingChange> plan, ItemObject incomingItem)
    {
        // 전제 1: 현 기획상 아이템의 분리는 일어나지 않음.
        //         따라서, '2개 이상의 빈 타일이 채워지는' 일은 발생하지 않기 때문에
        //         현재는 incomingItem을 새 위치에 배치하도록 작성함.
        bool tileFilled = false;
        foreach (var change in plan)
        {
            if (change.IsNewItem)
            {
                if (tileFilled) Debug.LogError("More than 1 tiles are filled.");
                incomingItem.UpdateAmount(change.AmountToAdd);
                incomingItem.OnDropped(change.TargetTile);
                tileFilled = true;
            }
            else
            {
                ItemObject existingItem = change.TargetTile.MapObjects.Find(x => x is ItemObject) as ItemObject;
                existingItem.UpdateAmount(existingItem.ItemAmount + change.AmountToAdd);
            }
        }
    }
}

public static class MergeCalculator
{
    public readonly struct MergeResult
    {
        public readonly int AmountToTransfer;
        public readonly int FinalTileValue;
        public readonly int RemainingInput;
        public readonly bool Succeeded;

        public MergeResult(int transfer, int tileVal, int remain, bool succeeded)
        {
            AmountToTransfer = transfer;
            FinalTileValue = tileVal;
            RemainingInput = remain;
            Succeeded = succeeded;
        }
    }

    // pure function: should not mutate arguments
    public static MergeResult Calculate(
        ItemData itemType, 
        int incomingValue, 
        ItemObject itemOnTile // null: empty tile
    )
    {
        // 1. empty tile
        if (itemOnTile == null)
        {
            int transfer = Mathf.Min(incomingValue, itemType.MaxItemAmount);
            return new MergeResult(
                transfer, 
                transfer, 
                incomingValue - transfer, 
                true
            );
        }

        // 2. type mismatch
        if (itemOnTile.ItemData != itemType)
        {
            return new MergeResult(0, itemOnTile.ItemAmount, incomingValue, false);
        }

        // 3. same type
        int currentTileVal = itemOnTile.ItemAmount;
        int maxVal = itemOnTile.ItemData.MaxItemAmount;
        
        // 3-1. full
        if (currentTileVal >= maxVal)
        {
            return new MergeResult(0, currentTileVal, incomingValue, true);
        }

        int spaceAvailable = maxVal - currentTileVal;
        int amountToTransfer = Mathf.Min(spaceAvailable, incomingValue);

        return new MergeResult(
            amountToTransfer,
            currentTileVal + amountToTransfer,
            incomingValue - amountToTransfer,
            true
        );
    }
}