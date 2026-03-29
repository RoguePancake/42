using Godot;
using Warship.Core;
using Warship.Data;

namespace Warship.UI.HUD;

/// <summary>
/// A single hot zone minimap — a small focused view of a specific map region.
/// Renders terrain ownership and unit positions as colored pixels on a TextureRect.
/// Can be pinned to any area and given a label.
/// </summary>
public partial class HotZoneMap : Control
{
    private TextureRect _mapImage = null!;
    private Label _titleLabel = null!;
    private Label _coordLabel = null!;
    private Button _closeBtn = null!;
    private Image _img = null!;

    private int _centerTileX;
    private int _centerTileY;
    private string _zoneLabel = "HOT ZONE";
    private int _slotIndex;
    private bool _isPinned = false;

    // How many tiles to show in each direction from center
    private const int ViewRadius = 12;
    private const int ImgSize = 120; // Pixel size of the rendered minimap

    // Colors for terrain
    private static readonly Color[] TerrainColors = {
        new(0.05f, 0.1f, 0.3f),   // DeepWater
        new(0.1f, 0.2f, 0.5f),    // Water
        new(0.8f, 0.75f, 0.5f),   // Sand
        new(0.2f, 0.5f, 0.2f),    // Grass
        new(0.1f, 0.35f, 0.1f),   // Forest
        new(0.4f, 0.35f, 0.25f),  // Hills
        new(0.5f, 0.5f, 0.55f),   // Mountain
        new(0.85f, 0.9f, 0.95f),  // Snow
    };

    public int SlotIndex => _slotIndex;
    public bool IsPinned => _isPinned;

    public void Setup(int slotIndex)
    {
        _slotIndex = slotIndex;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(140, 160);
        Size = new Vector2(140, 160);
        MouseFilter = MouseFilterEnum.Stop;

        // Background
        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.09f, 0.95f),
            BorderColor = new Color(0.3f, 0.5f, 0.9f, 0.8f),
            BorderWidthTop = 2, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Header row
        var headerRow = new HBoxContainer();
        headerRow.OffsetLeft = 4;
        headerRow.OffsetRight = -4;
        headerRow.OffsetTop = 2;
        headerRow.CustomMinimumSize = new Vector2(0, 20);
        AddChild(headerRow);

        _titleLabel = new Label
        {
            Text = $"ZONE {_slotIndex + 1}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 10);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.7f, 1f));
        headerRow.AddChild(_titleLabel);

        _closeBtn = new Button
        {
            Text = "x",
            CustomMinimumSize = new Vector2(18, 18)
        };
        _closeBtn.AddThemeFontSizeOverride("font_size", 10);
        _closeBtn.Pressed += OnClose;
        headerRow.AddChild(_closeBtn);

        // Map image
        _img = Image.CreateEmpty(ImgSize, ImgSize, false, Image.Format.Rgb8);
        _img.Fill(new Color(0.05f, 0.05f, 0.08f));

        _mapImage = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        _mapImage.OffsetLeft = 10;
        _mapImage.OffsetTop = 24;
        _mapImage.Size = new Vector2(ImgSize, ImgSize);
        _mapImage.Texture = ImageTexture.CreateFromImage(_img);
        AddChild(_mapImage);

        // Coord label
        _coordLabel = new Label
        {
            Text = "---",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _coordLabel.OffsetTop = 148;
        _coordLabel.OffsetLeft = 4;
        _coordLabel.Size = new Vector2(132, 14);
        _coordLabel.AddThemeFontSizeOverride("font_size", 9);
        _coordLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        AddChild(_coordLabel);

        Visible = false; // Hidden until pinned
    }

    public void PinToLocation(int tileX, int tileY, string label)
    {
        _centerTileX = tileX;
        _centerTileY = tileY;
        _zoneLabel = label;
        _isPinned = true;
        _titleLabel.Text = label.ToUpper();
        _coordLabel.Text = $"({tileX}, {tileY})";
        Visible = true;
        Redraw();
    }

    public void ClearPin()
    {
        _isPinned = false;
        Visible = false;
    }

    private void OnClose()
    {
        ClearPin();
        EventBus.Instance?.Publish(new Events.HotZoneClearEvent(_slotIndex));
    }

    public void Redraw()
    {
        if (!_isPinned) return;

        var data = WorldStateManager.Instance?.Data;
        if (data?.TerrainMap == null || data.OwnershipMap == null) return;

        int viewDiameter = ViewRadius * 2;
        float scale = (float)ImgSize / viewDiameter;

        for (int dy = 0; dy < viewDiameter; dy++)
        {
            for (int dx = 0; dx < viewDiameter; dx++)
            {
                int tx = _centerTileX - ViewRadius + dx;
                int ty = _centerTileY - ViewRadius + dy;

                Color color;
                if (tx < 0 || tx >= data.MapWidth || ty < 0 || ty >= data.MapHeight)
                {
                    color = new Color(0.02f, 0.02f, 0.04f);
                }
                else
                {
                    int terrain = data.TerrainMap[tx, ty];
                    int owner = data.OwnershipMap[tx, ty];

                    color = terrain >= 0 && terrain < TerrainColors.Length
                        ? TerrainColors[terrain]
                        : new Color(0.1f, 0.1f, 0.1f);

                    // Tint by owner color
                    if (owner >= 0 && owner < data.Nations.Count)
                    {
                        var nationColor = data.Nations[owner].NationColor;
                        color = color.Lerp(nationColor, 0.35f);
                    }
                }

                // Fill scaled pixels
                int px = (int)(dx * scale);
                int py = (int)(dy * scale);
                int pxEnd = (int)((dx + 1) * scale);
                int pyEnd = (int)((dy + 1) * scale);
                for (int iy = py; iy < pyEnd && iy < ImgSize; iy++)
                    for (int ix = px; ix < pxEnd && ix < ImgSize; ix++)
                        _img.SetPixel(ix, iy, color);
            }
        }

        // Draw armies as bright dots
        foreach (var army in data.Armies)
        {
            if (!army.IsAlive) continue;
            int rx = army.TileX - (_centerTileX - ViewRadius);
            int ry = army.TileY - (_centerTileY - ViewRadius);
            if (rx < 0 || rx >= viewDiameter || ry < 0 || ry >= viewDiameter) continue;

            int owner = -1;
            for (int i = 0; i < data.Nations.Count; i++)
                if (data.Nations[i].Id == army.NationId) { owner = i; break; }

            Color unitColor = owner >= 0 ? data.Nations[owner].NationColor : Colors.White;
            int upx = (int)(rx * scale + scale / 2);
            int upy = (int)(ry * scale + scale / 2);
            // Draw a 3x3 bright dot
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int fx = upx + ox, fy = upy + oy;
                    if (fx >= 0 && fx < ImgSize && fy >= 0 && fy < ImgSize)
                        _img.SetPixel(fx, fy, unitColor);
                }
        }

        // Cities as white/gold squares
        foreach (var city in data.Cities)
        {
            int rx = city.TileX - (_centerTileX - ViewRadius);
            int ry = city.TileY - (_centerTileY - ViewRadius);
            if (rx < 0 || rx >= viewDiameter || ry < 0 || ry >= viewDiameter) continue;

            Color cityColor = city.IsCapital ? Colors.Gold : Colors.White;
            int cpx = (int)(rx * scale + scale / 2);
            int cpy = (int)(ry * scale + scale / 2);
            int sz = city.IsCapital ? 2 : 1;
            for (int oy = -sz; oy <= sz; oy++)
                for (int ox = -sz; ox <= sz; ox++)
                {
                    int fx = cpx + ox, fy = cpy + oy;
                    if (fx >= 0 && fx < ImgSize && fy >= 0 && fy < ImgSize)
                        _img.SetPixel(fx, fy, cityColor);
                }
        }

        _mapImage.Texture = ImageTexture.CreateFromImage(_img);
    }
}
