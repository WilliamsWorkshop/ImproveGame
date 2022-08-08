﻿using ImproveGame.Common.Animations;
using ImproveGame.Common.GlobalItems;
using ImproveGame.Common.ModHooks;
using System.Collections.Generic;
using Terraria.GameContent.UI.Chat;
using Terraria.UI.Chat;

namespace ImproveGame.Interface.UIElements
{
    /// <summary>
    /// 用于操作数组中的物品，初始化的时候必须给定数组 + 下标。
    /// </summary>
    public class ArrayItemSlot : UIElement
    {
        private int mode;
        private List<Item> items2;
        private Item[] items;
        public Item[] Items => items;
        public Texture2D Texture
        {
            get
            {
                if (Item.favorited)
                {
                    return TextureAssets.InventoryBack10.Value;
                }
                return TextureAssets.InventoryBack.Value;
            }
        }
        public int index;
        public Item Item
        {
            get
            {
                if (mode == 0)
                    return items[index];
                else
                {
                    return items2[index];
                }
            }
            set
            {
                if (mode == 0)
                    items[index] = value;
                else
                    items2[index] = value;
            }
        }
        private int RightMouseTimer = -1;

        public ArrayItemSlot(Item[] items, int index)
        {
            mode = 0;
            this.items = items;
            this.index = index;
            Width.Set(TextureAssets.InventoryBack.Value.Width, 0f);
            Height.Set(TextureAssets.InventoryBack.Value.Height, 0f);

            UIText text = new(string.Empty, 0.75f)
            {
                VAlign = 0.8f
            };
            text.Left.Set(0, 0.2f);
            text.OnUpdate += (uie) =>
            {
                text.SetText(Item.IsAir || Item.stack <= 1 ? string.Empty : Item.stack.ToString());
                text.Recalculate();
            };
            Append(text);

            UIText text2 = new((index + 1).ToString(), 0.75f)
            {
                VAlign = 0.15f
            };
            text2.Left.Set(0, 0.15f);
            Append(text2);
        }

        public ArrayItemSlot(List<Item> items, int index)
        {
            mode = 1;
            this.items2 = items;
            this.index = index;
            Width.Set(TextureAssets.InventoryBack.Value.Width, 0f);
            Height.Set(TextureAssets.InventoryBack.Value.Height, 0f);

            UIText text = new(string.Empty, 0.75f)
            {
                VAlign = 0.8f
            };
            text.Left.Set(0, 0.2f);
            text.OnUpdate += (uie) =>
            {
                text.SetText(Item.IsAir || Item.stack <= 1 ? string.Empty : Item.stack.ToString());
                text.Recalculate();
            };
            Append(text);

            UIText text2 = new((index + 1).ToString(), 0.75f)
            {
                VAlign = 0.15f
            };
            text2.Left.Set(0, 0.15f);
            Append(text2);
        }

        public override void MouseDown(UIMouseEvent evt)
        {
            SetCursorOverride();
            MouseClickSlot();
            if (mode == 1)
            {
                if (Item.IsAir)
                {
                    items2.RemoveAt(index);
                    Parent.RemoveChild(Parent.Children.ToList()[^1]);
                }
            }
            base.MouseDown(evt);
        }

        public override void RightMouseDown(UIMouseEvent evt)
        {
            if (!Item.IsAir && !ItemID.Sets.BossBag[Item.type] && !ItemID.Sets.IsFishingCrate[Item.type])
            {
                RightMouseTimer = 0;
                TakeSlotItemToMouseItem();
            }

            if (ItemID.Sets.BossBag[Item.type] || ItemID.Sets.IsFishingCrate[Item.type])
            {
                if (ItemID.Sets.BossBag[Item.type] || ItemID.Sets.BossBag[Item.type])
                    Main.LocalPlayer.OpenBossBag(Item.type);

                if (ItemID.Sets.IsFishingCrate[Item.type])
                    Main.LocalPlayer.OpenFishingCrate(Item.type);

                if (ItemLoader.ConsumeItem(Item, Main.LocalPlayer))
                    Item.stack--;

                if (Item.stack == 0)
                    Item.SetDefaults();

                SoundEngine.PlaySound(SoundID.Grab);
                Main.stackSplit = 30;
                Main.mouseRightRelease = false;
                Recipe.FindRecipes();
                return;
            }
            if (ItemLoader.CanRightClick(Item))
            {
                Main.mouseRightRelease = true;
                ItemLoader.RightClick(Item, Main.LocalPlayer);
                Main.mouseRightRelease = false;
                return;
            }
            if (mode == 1)
            {
                if (Item.IsAir)
                {
                    items2.RemoveAt(index);
                    Parent.RemoveChild(Parent.Children.ToList()[^1]);
                }
            }
            base.MouseDown(evt);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // 右键长按物品持续拿出
            if (Main.mouseRight && IsMouseHovering && !Item.IsAir && !ItemID.Sets.BossBag[Item.type] && !ItemID.Sets.IsFishingCrate[Item.type])
            {
                if (RightMouseTimer >= 60)
                {
                    TakeSlotItemToMouseItem();
                }
                else if (RightMouseTimer >= 30 && RightMouseTimer % 3 == 0)
                {
                    TakeSlotItemToMouseItem();
                }
                else if (RightMouseTimer >= 15 && RightMouseTimer % 6 == 0)
                {
                    TakeSlotItemToMouseItem();
                }
                RightMouseTimer++;
            }
        }

        private readonly Color FavoritedBorderColor = new(233, 176, 0, 200);
        private readonly Color NotFavoritedBorderColor = new(18, 18, 38, 200);
        private readonly Color FavoritedBackground = new(83, 88, 151, 200);
        private readonly Color NotFavoritedBackground = new(63, 65, 151, 200);
        protected override void DrawSelf(SpriteBatch sb)
        {
            CalculatedStyle dimensions = GetDimensions();

            // 按下 Ctrl 改变鼠标指针外观
            if (IsMouseHovering && !Item.IsAir)
            {
                Main.hoverItemName = Item.Name;
                Main.HoverItem = Item.Clone();
                SetCursorOverride();
            }

            Color borderColor = Item.favorited ? FavoritedBorderColor : NotFavoritedBorderColor;
            Color background = Item.favorited ? FavoritedBackground : NotFavoritedBackground;
            // 绘制背景框
            PixelShader.DrawBox(Main.UIScaleMatrix, dimensions.Position(), dimensions.Size(), 12, 3,
                borderColor, background);

            // 原来的边框
            // sb.Draw(Texture, dimensions.Position(), null, Color.White * 0.8f, 0f, Vector2.Zero, 1f, 0, 0f);

            DrawHasGlowItem(sb, Item, dimensions);
        }

        /// <summary>
        /// 拿物品槽内物品到鼠标物品上
        /// </summary>
        public void TakeSlotItemToMouseItem()
        {
            bool CanPlaySound = false;
            if (Main.mouseItem.type == Item.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
            {
                Main.mouseItem.stack++;
                Item.stack--;
                if (Item.IsAir)
                    Item.SetDefaults();
                CanPlaySound = true;
            }
            else if (Main.mouseItem.IsAir && !Item.IsAir && Item.maxStack > 1)
            {
                Main.mouseItem = new Item(Item.type, 1);
                Item.stack--;
                if (Item.IsAir)
                    Item.SetDefaults();
                CanPlaySound = true;
            }
            if (CanPlaySound)
                SoundEngine.PlaySound(SoundID.MenuTick);
        }

        /// <summary>
        /// 改原版的 <see cref="Main.cursorOverride"/>
        /// </summary>
        private void SetCursorOverride()
        {
            if (!Item.IsAir)
            {
                if (!Item.favorited && ItemSlot.ShiftInUse)
                {
                    Main.cursorOverride = CursorOverrideID.ChestToInventory; // 快捷放回物品栏图标
                }
                if (Main.keyState.IsKeyDown(Main.FavoriteKey))
                {
                    Main.cursorOverride = CursorOverrideID.FavoriteStar; // 收藏图标
                    if (Main.drawingPlayerChat)
                    {
                        Main.cursorOverride = CursorOverrideID.Magnifiers; // 放大镜图标 - 输入到聊天框
                    }
                }
                void TryTrashCursorOverride()
                {
                    if (!Item.favorited)
                    {
                        if (Main.npcShop <= 0)
                            Main.cursorOverride = CursorOverrideID.TrashCan; // 垃圾箱图标
                        else
                            Main.cursorOverride = CursorOverrideID.QuickSell;
                    }
                }
                if (ItemSlot.ControlInUse && ItemSlot.Options.DisableLeftShiftTrashCan && !ItemSlot.ShiftForcedOn)
                {
                    TryTrashCursorOverride();
                }
                // 如果左Shift快速丢弃打开了，按原版物品栏的物品应该是丢弃，但是我们这应该算箱子物品，所以不丢弃
                //if (!ItemSlot.Options.DisableLeftShiftTrashCan && ItemSlot.ShiftInUse) {
                //    TryTrashCursorOverride();
                //}
            }
        }

        /// <summary>
        /// 左键点击物品
        /// </summary>
        private void MouseClickSlot()
        {
            if (Main.LocalPlayer.ItemAnimationActive)
                return;

            bool result = false;
            if (Item.ModItem is IItemOverrideLeftClick)
                result |= (Item.ModItem as IItemOverrideLeftClick).OverrideLeftClick(items, 114514, index);
            if (result)
                return;

            // 放大镜图标 - 输入到聊天框
            if (Main.cursorOverride == CursorOverrideID.Magnifiers)
            {
                if (ChatManager.AddChatText(FontAssets.MouseText.Value, ItemTagHandler.GenerateTag(Item), Vector2.One))
                    SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            // 收藏图标
            if (Main.cursorOverride == CursorOverrideID.FavoriteStar)
            {
                Item.favorited = !Item.favorited;
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            // 垃圾箱图标
            if (Main.cursorOverride == CursorOverrideID.TrashCan || Main.cursorOverride == CursorOverrideID.QuickSell)
            {
                // 假装自己是一个物品栏物品
                var temp = new Item[1];
                temp[0] = Item;
                ItemSlot.SellOrTrash(temp, ItemSlot.Context.InventoryItem, 0);
                return;
            }

            // 放回物品栏图标
            if (Main.cursorOverride == CursorOverrideID.ChestToInventory)
            {
                Item = Main.player[Main.myPlayer].GetItem(Main.myPlayer, Item, GetItemSettings.InventoryEntityToPlayerInventorySettings);
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }

            if (ItemSlot.ShiftInUse)
                return;

            // 常规单点
            if (Item.IsAir)
            {
                if (!Main.mouseItem.IsAir)
                {
                    Item = Main.mouseItem;
                    Main.mouseItem = new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
            else
            {
                if (Main.mouseItem.IsAir)
                {
                    Main.mouseItem = Item;
                    Item = new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else
                {
                    // 同种物品
                    if (Main.mouseItem.type == Item.type)
                    {
                        if (Item.stack < Item.maxStack)
                        {
                            if (Item.stack + Main.mouseItem.stack <= Item.maxStack)
                            {
                                Item.stack += Main.mouseItem.stack;
                                Main.mouseItem.SetDefaults(0);
                            }
                            else
                            {
                                Main.mouseItem.stack -= Item.maxStack - Item.stack;
                                Item.stack = Item.maxStack;
                            }
                        }
                        else if (Main.mouseItem.stack < Main.mouseItem.maxStack)
                        {
                            if (Main.mouseItem.stack + Item.stack <= Main.mouseItem.maxStack)
                            {
                                Main.mouseItem.stack += Item.stack;
                                Item.SetDefaults(0);
                            }
                            else
                            {
                                Item.stack -= Main.mouseItem.maxStack - Main.mouseItem.stack;
                                Main.mouseItem.stack = Main.mouseItem.maxStack;
                            }
                        }
                    }
                    else
                    {
                        (Item, Main.mouseItem) = (Main.mouseItem, Item);
                    }
                    SoundEngine.PlaySound(SoundID.Grab);
                }
            }
        }

        public static void DrawHasGlowItem(SpriteBatch sb, Item Item, CalculatedStyle dimensions, float ItemSize = 30f)
        {
            // 绘制物品
            if (!Item.IsAir)
            {
                ApplyBuffItem.UpdateInventoryGlow(Item);

                if (Item.GetGlobalItem<GlobalItemData>().InventoryGlow)
                {
                    OpenItemGlow(sb, Item);
                }

                DrawItem(sb, Item, Color.White, dimensions, ItemSize);

                if (Item.GetGlobalItem<GlobalItemData>().InventoryGlow)
                {
                    CloseItemGlow(sb);
                }
            }
        }

        public static void DrawItem(SpriteBatch sb, Item Item, Color lightColor, CalculatedStyle dimensions, float ItemSize = 30f)
        {
            Main.instance.LoadItem(Item.type);
            var ItemTexture2D = TextureAssets.Item[Item.type];

            Rectangle rectangle;
            if (Main.itemAnimations[Item.type] is null)
                rectangle = ItemTexture2D.Frame(1, 1, 0, 0);
            else
                rectangle = Main.itemAnimations[Item.type].GetFrame(ItemTexture2D.Value);

            float size = rectangle.Width > ItemSize || rectangle.Height > ItemSize ?
                rectangle.Width > rectangle.Height ? ItemSize / rectangle.Width : ItemSize / rectangle.Height :
                1f;

            sb.Draw(ItemTexture2D.Value, dimensions.Center() - rectangle.Size() * size / 2f,
                new Rectangle?(rectangle), Item.GetAlpha(lightColor), 0f, Vector2.Zero, size,
                SpriteEffects.None, 0f);
            sb.Draw(ItemTexture2D.Value, dimensions.Center() - rectangle.Size() * size / 2f,
                new Rectangle?(rectangle), Item.GetColor(lightColor), 0f, Vector2.Zero, size,
                SpriteEffects.None, 0f);
        }

        public static void OpenItemGlow(SpriteBatch sb, Item item)
        {
            Main.instance.LoadItem(item.type);
            Effect effect = ModAssets.ItemEffect.Value;

            Color lerpColor;
            int milliSeconds = (int)Main.gameTimeCache.TotalGameTime.TotalMilliseconds;
            float time = milliSeconds * 0.05f;
            if (time % 60f < 30)
            {
                lerpColor = Color.Lerp(Color.White * 0.25f, Color.Transparent, (float)(time % 60f % 30 / 29));
            }
            else
            {
                lerpColor = Color.Lerp(Color.Transparent, Color.White * 0.25f, (float)(time % 60f % 30 / 29));
            }
            effect.Parameters["uColor"].SetValue(lerpColor.ToVector4());
            effect.CurrentTechnique.Passes["ColorPut"].Apply();
            sb.Begin(effect, Main.UIScaleMatrix);
        }

        public static void CloseItemGlow(SpriteBatch sb)
        {
            sb.Begin(null, Main.UIScaleMatrix);
        }
    }
}
