using Godot;
using Warship.Core;
using Warship.Data;

namespace Warship.UI.HUD;

/// <summary>
/// A single hot zone minimap — a small focused view of a specific map region.
/// Renders terrain ownership and unit positions as colored pixels on a TextureRect.
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

    private const int ViewRadius = 12;
    private const int ImgSize = 120;

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

        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = UITheme.BgDark,
            BorderColor = UITheme.AccentBlueDim,
            BorderWidthTop = UITheme.BorderMediumW,
            BorderWidthBottom = UITheme.BorderThin,
            BorderWidthLeft = UITheme.BorderThin,
            BorderWidthRight = UITheme.BorderThin,
            CornerRadiusTopLeft = UITheme.CornerRadius,
            CornerRadiusTopRight = UITheme.CornerRadius,
            CornerRadiusBottomLeft = UITheme.CornerRadius,
            CornerRadiusBottomRight = UITheme.CornerRadius
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

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
        _titleLabel.AddThemeFontSizeOverride("font_size", UITheme.FontTiny);
        _titleLabel.AddThemeColorOverride("font_color", UITheme.TextAccent);
        headerRow.AddChild(_titleLabel);

        _closeBtn = new Button
        {
            Text = "x",
            CustomMinimumSize = new Vector2(18, 18)
        };
        _closeBtn.AddThemeFontSizeOverride("font_size", UITheme.FontTiny);
        _closeBtn.Pressed += OnClose;
        headerRow.AddChild(_closeBtn);

        _img = Image.CreateEmpty(ImgSize, ImgSize, false, Image.Format.Rgb8);
        _img.Fill(UITheme.BgDarkest);

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

        _coordLabel = new Label
        {
            Text = "---",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _coordLabel.OffsetTop = 148;
        _coordLabel.OffsetLeft = 4;
        _coordLabel.Size = new Vector2(132, 14);
        _coordLabel.AddThemeFontSizeOverride("font_size", 9);
        _coordLabel.AddThemeColorOverride("font_color", UITheme.TextDim);
        AddChild(_coordLabel);

        Visible = false;
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

                    if (owner >= 0 && owner < data.Nations.Count)
                    {
                        var nationColor = data.Nations[owner].NationColor;
                        color = color.Lerp(nationColor, 0.35f);
                    }
                }

                int px = (int)(dx * scale);
                int py = (int)(dy * scale);
                int pxEnd = (int)((dx + 1) * scale);
                int pyEnd = (int)((dy + 1) * scale);
                for (int iy = py; iy < pyEnd && iy < ImgSize; iy++)
                    for (int ix = px; ix < pxEnd && ix < ImgSize; ix++)
                        _img.SetPixel(ix, iy, color);
            }
        }

        foreach (var unit in data.Units)
        {
            if (!unit.IsAlive) continue;
            int rx = unit.TileX - (_centerTileX - ViewRadius);
            int ry = unit.TileY - (_centerTileY - ViewRadius);
            if (rx < 0 || rx >= viewDiameter || ry < 0 || ry >= viewDiameter) continue;

            int owner = -1;
            for (int i = 0; i < data.Nations.Count; i++)
                if (data.Nations[i].Id == unit.NationId) { owner = i; break; }

            Color unitColor = owner >= 0 ? data.Nations[owner].NationColor : Colors.White;
            int upx = (int)(rx * scale + scale / 2);
            int upy = (int)(ry * scale + scale / 2);
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int fx = upx + ox, fy = upy + oy;
                    if (fx >= 0 && fx < ImgSize && fy >= 0 && fy < ImgSize)
                        _img.SetPixel(fx, fy, unitColor);
                }
        }

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
