using Godot;

namespace Warship.UI;

/// <summary>
/// Centralized UI design system — colors, font sizes, layout constants, and style factories.
/// Every UI file should reference this instead of hardcoding values.
/// Inspired by SNES-era blue menu windows (FF6, Chrono Trigger).
/// </summary>
public static class UITheme
{
    // ─── LAYOUT CONSTANTS ────────────────────────────────────────────
    public const float LeftSidebarWidth = 250f;
    public const float RightSidebarWidth = 250f;
    public const float TopBarHeight = 32f;
    public const float AlertBarHeight = 32f;
    public const float TopBarsTotal = AlertBarHeight + TopBarHeight; // 64
    public const float TabBarHeight = 32f;
    public const float BottomPanelHeight = 200f;
    public const float SidebarGap = 20f; // gap between sidebar edge and floating panels

    // ─── BACKGROUND COLORS ───────────────────────────────────────────
    public static readonly Color BgDarkest   = new(0.03f, 0.03f, 0.05f, 1f);   // deepest bg
    public static readonly Color BgDark      = new(0.06f, 0.06f, 0.09f, 1f);   // panel bg dark
    public static readonly Color BgPanel     = new(0.08f, 0.09f, 0.12f, 1f);   // standard panel bg
    public static readonly Color BgSurface   = new(0.10f, 0.11f, 0.14f, 1f);   // raised surface
    public static readonly Color BgElevated  = new(0.13f, 0.14f, 0.18f, 1f);   // buttons/rows
    public static readonly Color BgHover     = new(0.18f, 0.20f, 0.26f, 1f);   // hover state
    public static readonly Color BgActive    = new(0.14f, 0.22f, 0.42f, 1f);   // active/selected

    // ─── ACCENT COLORS ──────────────────────────────────────────────
    public static readonly Color AccentBlue      = new(0.25f, 0.45f, 0.85f, 1f);
    public static readonly Color AccentBlueBright = new(0.35f, 0.55f, 0.95f, 1f);
    public static readonly Color AccentBlueDim   = new(0.18f, 0.32f, 0.65f, 1f);
    public static readonly Color AccentGold      = new(0.83f, 0.66f, 0.29f, 1f);
    public static readonly Color AccentCrimson   = new(0.85f, 0.15f, 0.15f, 1f);

    // ─── CATEGORY ACCENTS ───────────────────────────────────────────
    public static readonly Color CatDiplomatic   = new(0.30f, 0.55f, 0.95f, 1f);
    public static readonly Color CatMilitary     = new(0.95f, 0.35f, 0.25f, 1f);
    public static readonly Color CatEconomic     = new(0.30f, 0.85f, 0.40f, 1f);
    public static readonly Color CatIntelligence = new(0.75f, 0.55f, 0.95f, 1f);

    // ─── STATUS COLORS ──────────────────────────────────────────────
    public static readonly Color StatusHostile  = new(0.95f, 0.28f, 0.28f, 1f);
    public static readonly Color StatusWary     = new(0.95f, 0.58f, 0.18f, 1f);
    public static readonly Color StatusCool     = new(0.78f, 0.78f, 0.30f, 1f);
    public static readonly Color StatusNeutral  = new(0.58f, 0.58f, 0.68f, 1f);
    public static readonly Color StatusFriendly = new(0.30f, 0.85f, 0.40f, 1f);

    // ─── ALERT COLORS ───────────────────────────────────────────────
    public static readonly Color AlertInfo    = new(0.30f, 0.55f, 0.95f, 1f);
    public static readonly Color AlertWarning = new(0.95f, 0.78f, 0.20f, 1f);
    public static readonly Color AlertDanger  = new(0.95f, 0.28f, 0.28f, 1f);
    public static readonly Color AlertSuccess = new(0.30f, 0.85f, 0.40f, 1f);

    // ─── TEXT COLORS ────────────────────────────────────────────────
    public static readonly Color TextPrimary   = new(0.88f, 0.88f, 0.92f, 1f);
    public static readonly Color TextSecondary = new(0.62f, 0.62f, 0.70f, 1f);
    public static readonly Color TextDim       = new(0.42f, 0.42f, 0.50f, 1f);
    public static readonly Color TextAccent    = new(0.50f, 0.70f, 1.00f, 1f);

    // ─── BORDER COLORS ──────────────────────────────────────────────
    public static readonly Color BorderSubtle  = new(0.16f, 0.18f, 0.22f, 1f);
    public static readonly Color BorderMedium  = new(0.22f, 0.25f, 0.30f, 1f);
    public static readonly Color BorderAccent  = new(0.25f, 0.45f, 0.85f, 1f);

    // ─── FONT SIZES ────────────────────────────────────────────────
    public const int FontTiny   = 10;
    public const int FontSmall  = 12;
    public const int FontBody   = 14;
    public const int FontMedium = 16;
    public const int FontLarge  = 20;
    public const int FontXL     = 24;
    public const int FontTitle  = 32;
    public const int FontHuge   = 48;

    // ─── COMMON DIMENSIONS ──────────────────────────────────────────
    public const int CornerRadius    = 4;
    public const int CornerRadiusLg  = 8;
    public const int PaddingSmall    = 8;
    public const int PaddingMedium   = 16;
    public const int PaddingLarge    = 24;
    public const int BorderThin      = 1;
    public const int BorderMediumW   = 2;
    public const int BorderThick     = 3;
    public const int CategoryBorderW = 4;
    public const int ButtonHeight    = 40;
    public const int ButtonHeightSm  = 32;
    public const int RowHeight       = 36;

    // ═══ STYLE FACTORIES ═══════════════════════════════════════════

    /// <summary>Standard panel background with optional accent border.</summary>
    public static StyleBoxFlat PanelStyle(Color? borderColor = null, int borderWidth = 0)
    {
        var s = new StyleBoxFlat
        {
            BgColor = BgSurface,
            CornerRadiusTopLeft = CornerRadius,
            CornerRadiusTopRight = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            CornerRadiusBottomRight = CornerRadius
        };
        if (borderColor.HasValue && borderWidth > 0)
        {
            s.BorderColor = borderColor.Value;
            s.BorderWidthTop = borderWidth;
            s.BorderWidthBottom = borderWidth;
            s.BorderWidthLeft = borderWidth;
            s.BorderWidthRight = borderWidth;
        }
        return s;
    }

    /// <summary>Sidebar background style with a single border on the inner edge.</summary>
    public static StyleBoxFlat SidebarStyle(bool rightBorder)
    {
        var s = new StyleBoxFlat { BgColor = BgSurface, BorderColor = BorderMedium };
        if (rightBorder)
            s.BorderWidthRight = BorderMediumW;
        else
            s.BorderWidthLeft = BorderMediumW;
        return s;
    }

    /// <summary>Category header (left accent bar + dark bg).</summary>
    public static StyleBoxFlat CategoryHeaderStyle(Color accentColor)
    {
        return new StyleBoxFlat
        {
            BgColor = BgPanel,
            BorderColor = accentColor,
            BorderWidthLeft = CategoryBorderW,
            ContentMarginLeft = 12,
            ContentMarginTop = 7,
            ContentMarginBottom = 7
        };
    }

    /// <summary>Standard row style for list items.</summary>
    public static StyleBoxFlat RowStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = BgElevated,
            BorderColor = BorderSubtle,
            BorderWidthBottom = BorderThin,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
    }

    /// <summary>Standard action button with normal/hover/pressed states.</summary>
    public static void ApplyButtonStyle(Button btn, Color? accentColor = null)
    {
        var accent = accentColor ?? AccentBlue;

        var normal = new StyleBoxFlat
        {
            BgColor = BgElevated,
            BorderColor = BorderSubtle,
            BorderWidthBottom = BorderThin,
            ContentMarginLeft = PaddingMedium,
            ContentMarginRight = PaddingMedium,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            CornerRadiusTopLeft = CornerRadius,
            CornerRadiusTopRight = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            CornerRadiusBottomRight = CornerRadius
        };

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = BgHover;
        hover.BorderColor = accent;
        hover.BorderWidthBottom = BorderMediumW;

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = BgActive;
        pressed.BorderColor = accent;
        pressed.BorderWidthBottom = BorderMediumW;

        var focus = (StyleBoxFlat)hover.Duplicate();

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", pressed);
        btn.AddThemeStyleboxOverride("focus", focus);
        btn.AddThemeFontSizeOverride("font_size", FontBody);
        btn.AddThemeColorOverride("font_color", TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
    }

    /// <summary>Prominent action button (filled accent color).</summary>
    public static void ApplyPrimaryButtonStyle(Button btn, Color? color = null)
    {
        var c = color ?? AccentBlue;

        var normal = new StyleBoxFlat
        {
            BgColor = c * 0.7f,
            BorderColor = c,
            BorderWidthTop = BorderMediumW,
            BorderWidthBottom = BorderMediumW,
            BorderWidthLeft = BorderMediumW,
            BorderWidthRight = BorderMediumW,
            CornerRadiusTopLeft = CornerRadius + 2,
            CornerRadiusTopRight = CornerRadius + 2,
            CornerRadiusBottomLeft = CornerRadius + 2,
            CornerRadiusBottomRight = CornerRadius + 2,
            ContentMarginLeft = PaddingMedium,
            ContentMarginRight = PaddingMedium,
            ContentMarginTop = PaddingSmall,
            ContentMarginBottom = PaddingSmall
        };

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = c * 0.9f;

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = c * 0.5f;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", pressed);
        btn.AddThemeStyleboxOverride("focus", hover);
        btn.AddThemeFontSizeOverride("font_size", FontMedium);
        btn.AddThemeColorOverride("font_color", Colors.White);
    }

    /// <summary>Styled progress bar with consistent look.</summary>
    public static void ApplyBarStyle(ProgressBar bar, Color fillColor)
    {
        var barBg = new StyleBoxFlat
        {
            BgColor = BgDark,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };

        var barFill = new StyleBoxFlat
        {
            BgColor = fillColor,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };

        bar.AddThemeStyleboxOverride("background", barBg);
        bar.AddThemeStyleboxOverride("fill", barFill);
    }

    /// <summary>Modal window style (for crisis, victory, etc).</summary>
    public static StyleBoxFlat ModalWindowStyle(Color borderColor)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(BgPanel.R, BgPanel.G, BgPanel.B, 0.97f),
            BorderColor = borderColor,
            BorderWidthTop = BorderThick,
            BorderWidthBottom = BorderThick,
            BorderWidthLeft = BorderThick,
            BorderWidthRight = BorderThick,
            CornerRadiusTopLeft = CornerRadiusLg,
            CornerRadiusTopRight = CornerRadiusLg,
            CornerRadiusBottomLeft = CornerRadiusLg,
            CornerRadiusBottomRight = CornerRadiusLg,
            ShadowColor = new Color(0, 0, 0, 0.5f),
            ShadowSize = 12,
            ContentMarginLeft = PaddingLarge,
            ContentMarginRight = PaddingLarge,
            ContentMarginTop = PaddingLarge,
            ContentMarginBottom = PaddingLarge
        };
    }

    /// <summary>Toast notification style by type.</summary>
    public static StyleBoxFlat ToastStyle(string type)
    {
        var (bgColor, borderColor) = type switch
        {
            "success" => (new Color(0.08f, 0.22f, 0.12f, 0.92f), AlertSuccess),
            "danger"  => (new Color(0.28f, 0.08f, 0.08f, 0.92f), AlertDanger),
            "warning" => (new Color(0.28f, 0.22f, 0.08f, 0.92f), AlertWarning),
            _         => (new Color(0.10f, 0.15f, 0.25f, 0.92f), AlertInfo)
        };

        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthLeft = CategoryBorderW,
            CornerRadiusTopLeft = CornerRadius + 2,
            CornerRadiusTopRight = CornerRadius + 2,
            CornerRadiusBottomLeft = CornerRadius + 2,
            CornerRadiusBottomRight = CornerRadius + 2,
            ContentMarginLeft = PaddingMedium,
            ContentMarginRight = PaddingMedium,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0, 0, 0, 0.35f),
            ShadowSize = 4
        };
    }
}
