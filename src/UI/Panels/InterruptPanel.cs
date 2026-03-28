using Godot;
using System.Collections.Generic;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.Panels;

/// <summary>
/// "The Phone Rings" UI panel. Slides in from the right with a countdown bar,
/// title, description, and choice buttons. Auto-resolves on timeout.
/// CRITICAL = red border, URGENT = orange, ROUTINE = blue.
/// </summary>
public partial class InterruptPanel : Control
{
    // Panel elements
    private PanelContainer _window = null!;
    private Label _titleLabel = null!;
    private Label _descLabel = null!;
    private Label _timerLabel = null!;
    private ProgressBar _timerBar = null!;
    private VBoxContainer _choiceBox = null!;
    private ColorRect _dimBg = null!;

    // State
    private string _currentId = "";
    private int _defaultChoice;
    private float _timeRemaining;
    private float _totalTime;
    private bool _active;
    private InterruptPriority _currentPriority;

    // Slide animation
    private float _slideProgress;
    private const float SlideSpeed = 5f;
    private const float PanelWidth = 480f;

    // Priority colors
    private static readonly Color CriticalColor = new(0.9f, 0.15f, 0.15f);
    private static readonly Color UrgentColor = new(0.95f, 0.6f, 0.1f);
    private static readonly Color RoutineColor = new(0.2f, 0.45f, 0.9f);

    // Interrupt queue
    private readonly Queue<InterruptTriggeredEvent> _queue = new();

    public override void _Ready()
    {
        // Full screen overlay — transparent until active
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Dim background (only visible for CRITICAL)
        _dimBg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        _dimBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_dimBg);

        // The sliding panel — anchored to the right
        _window = new PanelContainer
        {
            CustomMinimumSize = new Vector2(PanelWidth, 0)
        };
        _window.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        _window.OffsetLeft = PanelWidth; // Start off-screen to the right
        _window.OffsetRight = PanelWidth + PanelWidth;
        _window.OffsetTop = 70;    // Below TopBar
        _window.OffsetBottom = -210; // Above BottomPanel
        AddChild(_window);

        // Default style (will be overridden per priority)
        ApplyPanelStyle(RoutineColor);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        _window.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        // Title
        _titleLabel = new Label
        {
            Text = "INTERRUPT",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(_titleLabel);

        // Timer bar
        _timerBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 20),
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false
        };
        var barStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f),
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        };
        _timerBar.AddThemeStyleboxOverride("background", barStyle);
        vbox.AddChild(_timerBar);

        // Timer text
        _timerLabel = new Label
        {
            Text = "60s remaining",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _timerLabel.AddThemeFontSizeOverride("font_size", 14);
        _timerLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        vbox.AddChild(_timerLabel);

        vbox.AddChild(new HSeparator());

        // Description
        _descLabel = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(PanelWidth - 60, 80)
        };
        _descLabel.AddThemeFontSizeOverride("font_size", 15);
        _descLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        vbox.AddChild(_descLabel);

        // Choices container
        _choiceBox = new VBoxContainer();
        _choiceBox.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_choiceBox);

        // Subscribe
        EventBus.Instance?.Subscribe<InterruptTriggeredEvent>(OnInterruptTriggered);

        _active = false;
        _slideProgress = 0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Slide animation
        float targetSlide = _active ? 1f : 0f;
        _slideProgress = Mathf.MoveToward(_slideProgress, targetSlide, SlideSpeed * dt);
        float offset = Mathf.Lerp(PanelWidth, -PanelWidth, _slideProgress);
        _window.OffsetLeft = offset;
        _window.OffsetRight = offset + PanelWidth;

        // Block input when panel is visible
        MouseFilter = _active ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        _dimBg.MouseFilter = _active ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        // Dim background for CRITICAL
        if (_active && _currentPriority == InterruptPriority.Critical)
            _dimBg.Color = new Color(0, 0, 0, 0.5f * _slideProgress);
        else
            _dimBg.Color = new Color(0, 0, 0, 0);

        if (!_active) return;

        // Countdown
        _timeRemaining -= dt;
        if (_timeRemaining <= 0)
        {
            _timeRemaining = 0;
            Resolve(_defaultChoice, wasTimeout: true);
            return;
        }

        // Update timer display
        float pct = (_timeRemaining / _totalTime) * 100f;
        _timerBar.Value = pct;
        _timerLabel.Text = $"{_timeRemaining:0.0}s remaining";

        // Flash timer bar when low
        if (_timeRemaining < 5f)
        {
            float flash = Mathf.Abs(Mathf.Sin(_timeRemaining * 4f));
            var fillStyle = new StyleBoxFlat
            {
                BgColor = new Color(Mathf.Lerp(0.9f, 0.3f, flash), Mathf.Lerp(0.1f, 0.1f, flash), 0.1f),
                CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
                CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
            };
            _timerBar.AddThemeStyleboxOverride("fill", fillStyle);
        }
    }

    private void OnInterruptTriggered(InterruptTriggeredEvent ev)
    {
        if (_active)
        {
            _queue.Enqueue(ev);
            return;
        }
        CallDeferred(nameof(ShowInterrupt), ev.Id, ev.Title, ev.Description,
            ev.TimerSeconds, ev.DefaultChoiceIndex, (int)ev.Priority,
            SerializeChoices(ev.Choices));
    }

    private void ShowInterrupt(string id, string title, string desc,
        float timer, int defaultChoice, int priority, string choicesJson)
    {
        _currentId = id;
        _defaultChoice = defaultChoice;
        _timeRemaining = timer;
        _totalTime = timer;
        _currentPriority = (InterruptPriority)priority;

        _titleLabel.Text = title;
        _descLabel.Text = desc;

        // Style per priority
        Color borderColor = _currentPriority switch
        {
            InterruptPriority.Critical => CriticalColor,
            InterruptPriority.Urgent => UrgentColor,
            _ => RoutineColor
        };
        ApplyPanelStyle(borderColor);
        _titleLabel.AddThemeColorOverride("font_color", borderColor);

        // Timer bar fill color
        var fillStyle = new StyleBoxFlat
        {
            BgColor = borderColor,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        };
        _timerBar.AddThemeStyleboxOverride("fill", fillStyle);
        _timerBar.Value = 100;

        // Build choice buttons
        foreach (var child in _choiceBox.GetChildren())
            child.QueueFree();

        var choices = DeserializeChoices(choicesJson);
        for (int i = 0; i < choices.Length; i++)
        {
            int idx = i;
            var choice = choices[i];

            var btn = new Button
            {
                Text = choice.Label,
                CustomMinimumSize = new Vector2(0, 40),
                TooltipText = choice.EffectDescription
            };
            btn.AddThemeFontSizeOverride("font_size", 15);

            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.12f, 0.12f, 0.18f),
                BorderColor = borderColor * 0.6f,
                BorderWidthBottom = 2, BorderWidthTop = 1,
                BorderWidthLeft = 1, BorderWidthRight = 1,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
                ContentMarginLeft = 12, ContentMarginRight = 12,
                ContentMarginTop = 6, ContentMarginBottom = 6
            };
            btn.AddThemeStyleboxOverride("normal", style);

            var hover = (StyleBoxFlat)style.Duplicate();
            hover.BgColor = new Color(borderColor.R * 0.3f, borderColor.G * 0.3f, borderColor.B * 0.3f);
            btn.AddThemeStyleboxOverride("hover", hover);

            // Mark default choice
            if (idx == defaultChoice)
                btn.Text += " (default)";

            btn.Pressed += () => Resolve(idx, wasTimeout: false);
            _choiceBox.AddChild(btn);
        }

        // Add effect descriptions as subtle labels under buttons
        for (int i = 0; i < choices.Length; i++)
        {
            var effectLabel = new Label
            {
                Text = $"  → {choices[i].EffectDescription}",
                CustomMinimumSize = new Vector2(0, 16)
            };
            effectLabel.AddThemeFontSizeOverride("font_size", 11);
            effectLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
            _choiceBox.AddChild(effectLabel);
        }

        _active = true;
    }

    private void Resolve(int choiceIndex, bool wasTimeout)
    {
        _active = false;
        EventBus.Instance?.Publish(new InterruptResolvedEvent(_currentId, choiceIndex, wasTimeout));

        // Process next in queue
        if (_queue.Count > 0)
        {
            var next = _queue.Dequeue();
            CallDeferred(nameof(ShowInterrupt), next.Id, next.Title, next.Description,
                next.TimerSeconds, next.DefaultChoiceIndex, (int)next.Priority,
                SerializeChoices(next.Choices));
        }
    }

    private void ApplyPanelStyle(Color borderColor)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.1f, 0.95f),
            BorderColor = borderColor,
            BorderWidthLeft = 4, BorderWidthTop = 4,
            BorderWidthRight = 4, BorderWidthBottom = 4,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ShadowColor = new Color(borderColor.R * 0.3f, borderColor.G * 0.3f, borderColor.B * 0.3f, 0.6f),
            ShadowSize = 8
        };
        _window.AddThemeStyleboxOverride("panel", style);
    }

    // Simple choice serialization to pass through CallDeferred (which needs Variant-compatible args)
    private static string SerializeChoices(InterruptChoice[] choices)
    {
        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < choices.Length; i++)
        {
            if (i > 0) parts.Append("||");
            parts.Append(choices[i].Label);
            parts.Append("|");
            parts.Append(choices[i].EffectDescription);
        }
        return parts.ToString();
    }

    private static InterruptChoice[] DeserializeChoices(string data)
    {
        var entries = data.Split("||");
        var result = new InterruptChoice[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            var parts = entries[i].Split("|", 2);
            result[i] = new InterruptChoice
            {
                Label = parts[0],
                EffectDescription = parts.Length > 1 ? parts[1] : ""
            };
        }
        return result;
    }
}
