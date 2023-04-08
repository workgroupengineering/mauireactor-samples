﻿using Marvelous.Models;
using Marvelous.Services;
using MauiReactor;
using MauiReactor.Animations;
using MauiReactor.Canvas;
using MauiReactor.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marvelous.Pages.Components;

class MainCarouselViewState
{
    public WonderType CurrentType { get; set; }
    public DateTime? StartDrag { get; set; }
    public double PanX { get; set; }
    public Size ContainerSize { get; set; }
    public bool IsDragging { get; set; }
}

class MainCarouselView : Component<MainCarouselViewState>
{
    public override VisualNode Render()
    {
        return new Grid()
        {
            Enum.GetValues<WonderType>().Select(RenderNextViewItem),

            new Rectangle()
                .Background(Illustration.Config[State.CurrentType].ForegroundBrush),

            new MainCarouselViewIndicator()
                .CurrentType(State.CurrentType)
        }
        .OnSizeChanged(OnMainContainerSizeChanged)
        .OnPanUpdated(OnPan)
        .Background(Illustration.Config[State.CurrentType].BackgroundBrush)
        ;
    }

    void OnMainContainerSizeChanged(object? sender, EventArgs args)
    {
        var container = (MauiControls.Grid?)sender;
        if (container == null)
        {
            return;
        }

        SetState(s => s.ContainerSize = container.Bounds.Size);
    }

    MainCarouselViewItem? RenderNextViewItem(WonderType wonderType)
    {
        return new MainCarouselViewItem()
            .Type(wonderType)
            .RelativePan(State.PanX)
            .CurrentType(State.CurrentType)
            .ContainerSize(State.ContainerSize);
    }

    void OnPan(object? sender, MauiControls.PanUpdatedEventArgs args)
    {
        var container = (MauiControls.Grid?)sender;
        if (container == null)
        {
            return;
        }

        if (args.StatusType == GestureStatus.Started)
        {
            State.StartDrag = DateTime.Now;
        }

        if (args.StatusType == GestureStatus.Running || args.StatusType == GestureStatus.Started)
        {
            SetState(s =>
            {
                s.PanX = args.TotalX;
                s.ContainerSize = container.Bounds.Size;
                s.IsDragging = true;
            });
        }
        else if (args.StatusType == GestureStatus.Canceled)
        {
            SetState(s =>
            {
                s.PanX = 0;
                s.ContainerSize = container.Bounds.Size;
                s.IsDragging = false;
            });
        }
        else //Completed
        {
            var now = DateTime.Now;

            if (State.StartDrag.HasValue && 
                ((now - State.StartDrag.Value < TimeSpan.FromMilliseconds(200)) || args.TotalX > State.ContainerSize.Width / 3.0))
            {
                SetState(s =>
                {
                    s.ContainerSize = container.Bounds.Size;
                    s.CurrentType = s.PanX < 0 ?
                        State.CurrentType.Next() :
                        State.CurrentType.Previous();
                    s.PanX = 0;
                    s.IsDragging = false;
                });
            }
            else
            {
                SetState(s =>
                {
                    s.PanX = 0;
                    s.ContainerSize = container.Bounds.Size;
                    s.IsDragging = false;
                });
            }
        }
    }
}

class MainCarouselViewItemState
{
    public double TranslationX { get; set; }
}

class MainCarouselViewItem : Component<MainCarouselViewItemState>
{
    private WonderType _type;
    private WonderType _currentType;
    private double _relativePan;
    private Size _containerSize;

    private bool IsCurrent => _currentType == _type;

    public MainCarouselViewItem Type(WonderType type)
    {
        _type = type;
        return this;
    }

    public MainCarouselViewItem CurrentType(WonderType currentType)
    {
        _currentType = currentType;
        return this;
    }

    public MainCarouselViewItem RelativePan(double relativePan)
    {
        _relativePan = relativePan;
        return this;
    }

    public MainCarouselViewItem ContainerSize(Size size)
    {
        _containerSize = size;
        return this;
    }

    public override VisualNode Render()
    {
        var config = Illustration.Config[_type];
        var translationX = 0.0;
        var opacity = 0.0;

        var percOpacity = Easing.CubicIn.Ease(Math.Abs(_relativePan / _containerSize.Width));

        if (IsCurrent)
        {
            translationX = _relativePan;
            opacity = _containerSize.Width > 0 ? 1.0 - percOpacity : 1.0;
        }
        else if (_relativePan < 0 && _type.IsNextOf(_currentType))
        {
            translationX = _containerSize.Width + _relativePan;
            opacity = _containerSize.Width > 0 ? percOpacity : 0.0;
        }
        else if (_relativePan > 0 && _type.IsPreviousOf(_currentType))
        {
            translationX = _relativePan - _containerSize.Width;
            opacity = _containerSize.Width > 0 ? percOpacity : 0.0;
        }
        else if (_type.IsNextOf(_currentType))
        {
            translationX = _containerSize.Width;
        }
        else if (_type.IsPreviousOf(_currentType))
        {
            translationX = - _containerSize.Width;
        }

        return new Grid
        {
            new AbsoluteLayout
            {
                config.BackgroundImages?.Select(RenderIllustrationImage),
            },

            new Image(config.MainObject)
                .TranslationX(translationX)
                .WithAnimation(duration: 400)
                .Opacity(opacity)
                .Margin(config.MarginLeft, config.MarginTop, 0, 0)
                .ScaleX(config.ScaleX)
                .ScaleY(config.ScaleY),

            new AbsoluteLayout
            {
                config.ForegroundImages?.Select(RenderIllustrationImage),
            },
        };
    }

    private Image RenderIllustrationImage(IllustrationImage image, int index)
    {
        return new Image(image.Source)
            .Opacity(IsCurrent ? image.Opacity : 0.0)
            .AbsoluteLayoutBounds(IsCurrent ? 
                image.GetFinalBounds(_containerSize) : 
                image.GetInitialBounds(_containerSize))
            .WithAnimation(duration: 400)
            ;
    }
}

class MainCarouselViewIndicatorState
{
    public WonderType CurrentType { get; set; }

    public float Completion { get; set; }
}

class MainCarouselViewIndicator : Component<MainCarouselViewIndicatorState>
{
    private WonderType _wonderType;

    public MainCarouselViewIndicator CurrentType(WonderType wonderType)
    {
        _wonderType = wonderType;
        return this;
    }

    protected override void OnPropsChanged()
    {
        if (_wonderType != State.CurrentType)
        {
            State.Completion = 0;
        }

        base.OnPropsChanged();
    }

    public override VisualNode Render()
    {
        return null;
    }

}

static class WonderTypeExtensions
{
    public static WonderType Next(this WonderType wonderType)
        => wonderType == WonderType.TajMahal ? WonderType.ChichenItza : (WonderType)((int)wonderType + 1);

    public static WonderType Previous(this WonderType wonderType)
        => wonderType == WonderType.ChichenItza ? WonderType.TajMahal : (WonderType)((int)wonderType - 1);

    public static bool IsPreviousOf(this WonderType wonderType, WonderType nextWonderType)
        => nextWonderType.Previous() == wonderType;

    public static bool IsNextOf(this WonderType wonderType, WonderType prevWonderType)
        => prevWonderType.Next() == wonderType;
}