﻿using ImproveGame.Common.Systems;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace ImproveGame.Common.ConstructCore
{
    internal class GenerateCore
    {
        private static int _taskProcessed;

        public static void GenerateFromTag(TagCompound tag, Point position)
        {
            CoroutineSystem.GenerateRunner.Run(Generate(tag, position));
        }

        public static IEnumerator Generate(TagCompound tag, Point position)
        {
            var structure = new QoLStructure(tag);

            if (structure.StructureDatas is null || structure.StructureDatas.Count is 0)
            {
                // 此处应有Logger.Warn
                yield break;
            }

            // 添加Origin偏移
            position.X -= structure.OriginX;
            position.Y -= structure.OriginY;

            _taskProcessed = 0;
            var currentTask = CoroutineSystem.GenerateRunner.Run(KillTiles(structure, position));
            while (currentTask.IsRunning)
                yield return null;
            currentTask = CoroutineSystem.GenerateRunner.Run(GenerateOneBlockTiles(structure, position));
            while (currentTask.IsRunning)
                yield return null;
            currentTask = CoroutineSystem.GenerateRunner.Run(GenerateWalls(structure, position));
            while (currentTask.IsRunning)
                yield return null;
            currentTask = CoroutineSystem.GenerateRunner.Run(GenerateMultiTiles(structure, position));
            while (currentTask.IsRunning)
                yield return null;
            CoroutineSystem.GenerateRunner.Run(SquareTiles(structure, position));
        }

        public static IEnumerator KillTiles(QoLStructure structure, Point position)
        {
            int width = structure.Width;
            int height = structure.Height;
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    var placePosition = position + new Point(x, y);
                    Tile tile = Main.tile[placePosition.X, placePosition.Y];
                    if (tile.HasTile && TryKillTile(placePosition.X, placePosition.Y, Main.LocalPlayer))
                    {
                        _taskProcessed++;
                    }
                    if (_taskProcessed >= 5)
                    {
                        _taskProcessed = 0;
                        yield return null;
                    }
                }
            }
        }

        public static IEnumerator GenerateOneBlockTiles(QoLStructure structure, Point position)
        {
            int width = structure.Width;
            int height = structure.Height;
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    int index = y + x * (height + 1);
                    TileDefinition tileData = structure.StructureDatas[index];
                    int tileType = structure.ParseTileType(tileData); // 实际上的Type
                    int tileItemFindType = tileType; // 用于找到合适物品的Type
                    if (tileItemFindType is not -1 && TileID.Sets.Grass[tileItemFindType])
                    {
                        tileItemFindType = TileID.Dirt;
                    }
                    int tileItemType = GetTileItem(tileItemFindType, tileData.TileFrameX, tileData.TileFrameY);
                    if (tileItemType != -1)
                    {
                        var tileObjectData = TileObjectData.GetTileData(tileType, 0);
                        if (tileObjectData is null || (tileObjectData.CoordinateFullWidth <= 18 && tileObjectData.CoordinateFullHeight <= 18))
                        {
                            var placePosition = position + new Point(x, y);
                            var inventory = GetAllInventoryItemsList(Main.LocalPlayer, ignorePortable: true).ToArray();
                            PickItemInInventory(Main.LocalPlayer, inventory, (item) =>
                                    item.type == tileItemType &&
                                TryPlaceTile(placePosition.X, placePosition.Y, item, Main.LocalPlayer, forced: true),
                                true, out _);
                            // 挖掉重来！
                            if (TileID.Sets.Grass[tileType])
                            {
                                Main.tile[placePosition].ResetToType((ushort)tileType);
                                WorldGen.TileFrame(placePosition.X, position.Y, true, false);
                            }
                            if (WorldGen.CanPoundTile(placePosition.X, placePosition.Y))
                            {
                                if (tileData.BlockType is BlockType.HalfBlock)
                                {
                                    WorldGen.SlopeTile(placePosition.X, placePosition.Y, 0);
                                    WorldGen.PoundTile(placePosition.X, placePosition.Y);
                                }
                                else if (tileData.BlockType is not BlockType.Solid)
                                {
                                    WorldGen.SlopeTile(placePosition.X, placePosition.Y, (int)tileData.BlockType - 1);
                                }
                            }
                            _taskProcessed++;
                            if (_taskProcessed >= 6)
                            {
                                _taskProcessed = 0;
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerator GenerateWalls(QoLStructure structure, Point position)
        {
            int width = structure.Width;
            int height = structure.Height;
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    int index = y + x * (height + 1);
                    var placePosition = position + new Point(x, y);
                    TileDefinition tileData = structure.StructureDatas[index];
                    int wallItemType = GetWallItem(structure.ParseTileType(tileData));
                    if (wallItemType != -1)
                    {
                        var inventory = GetAllInventoryItemsList(Main.LocalPlayer, ignorePortable: true).ToArray();
                        PickItemInInventory(Main.LocalPlayer, inventory, (item) =>
                            item.type == wallItemType &&
                            TryPlaceWall(item, placePosition.X, placePosition.Y),
                            true, out _);
                        _taskProcessed++;
                        if (_taskProcessed >= 10)
                        {
                            _taskProcessed = 0;
                            yield return null;
                        }
                    }
                }
            }
        }

        private static bool TryPlaceWall(Item item, int x, int y)
        {
            WorldGen.KillWall(x, y);
            if (Main.tile[x, y].WallType == 0)
            {
                WorldGen.PlaceWall(x, y, item.createWall);
                return true;
            }
            return false;
        }

        public static IEnumerator GenerateMultiTiles(QoLStructure structure, Point position)
        {
            int width = structure.Width;
            int height = structure.Height;
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    int index = y + x * (height + 1);
                    TileDefinition tileData = structure.StructureDatas[index];
                    int tileType = structure.ParseTileType(tileData);
                    if (tileType is -1)
                        continue;
                    var tileObjectData = TileObjectData.GetTileData(tileType, 0);
                    if (tileObjectData is not null && (tileObjectData.CoordinateFullWidth > 18 || tileObjectData.CoordinateFullHeight > 18))
                    {
                        // 转换为帧坐标
                        int subX = (tileData.TileFrameX / tileObjectData.CoordinateFullWidth) * tileObjectData.CoordinateFullWidth;
                        int subY = (tileData.TileFrameY / tileObjectData.CoordinateFullHeight) * tileObjectData.CoordinateFullHeight;
                        int tileItemType = GetTileItem(tileType, subX, subY);
                        if (tileItemType != -1)
                        {
                            subX = tileData.TileFrameX % tileObjectData.CoordinateFullWidth;
                            subY = tileData.TileFrameY % tileObjectData.CoordinateFullHeight;
                            Point16 frame = new(subX / 18, subY / 18);
                            var origin = tileObjectData.Origin.ToPoint();
                            if (tileType is TileID.OpenDoor && tileData.TileFrameX / tileObjectData.CoordinateFullWidth % 2 == 1) // 开启的门实际上OriginX为0，这里对于向左开的要重新定位到他的轴心
                            {
                                origin.X = 1;
                            }
                            if (frame.X == origin.X && frame.Y == origin.Y)
                            {
                                var placePosition = position + new Point(x, y);
                                var inventory = GetAllInventoryItemsList(Main.LocalPlayer, ignorePortable: true).ToArray();
                                PickItemInInventory(Main.LocalPlayer, inventory, (item) =>
                                    item is not null && item.type == tileItemType &&
                                    TryPlaceTile(placePosition.X, placePosition.Y, item, Main.LocalPlayer, forced: true),
                                    true, out _);
                                // 什么怪玩意，还要我特判
                                if (tileType == TileID.Banners)
                                {
                                    int placeStyle = MaterialCore.ItemToPlaceStyle[tileItemType];
                                    short frameX = (short)(placeStyle % 111 * 18);
                                    short frameY = (short)(placeStyle / 111 * 54);
                                    for (int j = 0; j <= 2; j++)
                                    {
                                        var tilePosition = placePosition;
                                        tilePosition.Y += j;

                                        var tile = Main.tile[tilePosition];
                                        var stateData = tile.Get<TileWallWireStateData>(); // 只想替换掉TileType，其他数据先存一存
                                        tile.ResetToType((ushort)tileType);
                                        tile.Get<TileWallWireStateData>() = stateData; // 还原其他数据
                                        tile.HasTile = true;
                                        tile.TileFrameX = frameX;
                                        tile.TileFrameY = (short)(frameY + j * 18);
                                    }
                                }
                                yield return null;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerator SquareTiles(QoLStructure structure, Point position)
        {
            int width = structure.Width;
            int height = structure.Height;
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    var placePosition = position + new Point(x, y);
                    if (Main.tile[placePosition].HasTile)
                    {
                        WorldGen.TileFrame(placePosition.X, position.Y, true, false);
                    }
                    if (Main.tile[placePosition].WallType > 0)
                    {
                        Framing.WallFrame(placePosition.X, position.Y, true);
                    }
                    _taskProcessed++;
                    if (_taskProcessed >= 50)
                    {
                        _taskProcessed = 0;
                        yield return null;
                    }
                }
            }
            if (Main.netMode is NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, position.X - 1, position.Y - 1, width + 2, height + 2);
            }
        }
    }
}
