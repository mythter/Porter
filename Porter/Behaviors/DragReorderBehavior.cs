using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Porter.ViewModels;

namespace Porter.Behaviors;

/// <summary>
/// Attached behavior that allows reordering the items of an <see cref="ItemsControl"/>
/// by dragging them up and down.
/// While dragging, the dragged container follows the pointer and the other containers
/// smoothly slide away to make room for it; the underlying collection is only modified
/// once the pointer is released, by the <see cref="IReorderableViewModel"/> the items
/// control is bound to.
/// Native AOT-compatible: no reflection is used.
/// </summary>
public static class DragReorderBehavior
{
	private const double DragThreshold = 4;

	private const double DimmedOpacity = 0.45;

	private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(150);

	private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(200);

	public static readonly AttachedProperty<bool> EnabledProperty =
		AvaloniaProperty.RegisterAttached<ItemsControl, bool>(
			"Enabled",
			typeof(DragReorderBehavior));

	private static readonly List<Control> _containers = [];

	private static readonly List<double> _centers = [];

	private static ItemsControl? _itemsControl;

	private static Control? _container;

	private static Point _origin;

	private static bool _dragging;

	private static int _startIndex;

	private static int _currentIndex;

	private static double _step;

	static DragReorderBehavior()
	{
		EnabledProperty.Changed.AddClassHandler<ItemsControl>(OnEnabledChanged);
	}

	public static void SetEnabled(ItemsControl control, bool value) => control.SetValue(EnabledProperty, value);

	public static bool GetEnabled(ItemsControl control) => control.GetValue(EnabledProperty);

	private static void OnEnabledChanged(ItemsControl itemsControl, AvaloniaPropertyChangedEventArgs args)
	{
		itemsControl.PointerPressed -= OnPointerPressed;
		itemsControl.PointerMoved -= OnPointerMoved;
		itemsControl.PointerReleased -= OnPointerReleased;
		itemsControl.PointerCaptureLost -= OnPointerCaptureLost;

		if (args.NewValue is true)
		{
			itemsControl.PointerPressed += OnPointerPressed;
			itemsControl.PointerMoved += OnPointerMoved;
			itemsControl.PointerReleased += OnPointerReleased;
			itemsControl.PointerCaptureLost += OnPointerCaptureLost;
		}
	}

	private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		// Interactive children (TextBox, Button, ComboBox, ...) mark the event as handled,
		// so reaching this bubbling handler already means the press happened on a
		// "neutral" part of the item.
		if (sender is not ItemsControl itemsControl ||
			!e.GetCurrentPoint(itemsControl).Properties.IsLeftButtonPressed)
			return;

		var container = GetContainer(itemsControl, e.Source as Visual);

		if (container is null)
			return;

		_itemsControl = itemsControl;
		_container = container;
		_origin = e.GetPosition(itemsControl);
		_dragging = false;
	}

	private static void OnPointerMoved(object? sender, PointerEventArgs e)
	{
		if (_itemsControl is null || _container is null)
			return;

		if (!e.GetCurrentPoint(_itemsControl).Properties.IsLeftButtonPressed)
		{
			EndDrag(false);
			return;
		}

		var offset = e.GetPosition(_itemsControl).Y - _origin.Y;

		if (!_dragging)
		{
			if (Math.Abs(offset) < DragThreshold)
				return;

			if (!BeginDrag())
			{
				Reset();
				return;
			}

			e.Pointer.Capture(_itemsControl);
		}

		// The dragged container follows the pointer without any transition.
		SetTranslation(_container, offset, false);

		UpdateOrder(_centers[_startIndex] + offset);
	}

	private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => EndDrag(true);

	private static void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => EndDrag(false);

	private static bool BeginDrag()
	{
		if (_itemsControl?.ItemsPanelRoot is not { } panel ||
			_container is null ||
			_itemsControl.DataContext is not IReorderableViewModel)
			return false;

		_containers.Clear();
		_centers.Clear();
		_containers.AddRange(panel.Children.OfType<Control>());

		foreach (var container in _containers)
		{
			_centers.Add(container.Bounds.Center.Y);
		}

		_startIndex = _containers.IndexOf(_container);

		if (_startIndex < 0)
			return false;

		_currentIndex = _startIndex;

		var spacing = panel is StackPanel stackPanel ? stackPanel.Spacing : 0;
		_step = _container.Bounds.Height + spacing;

		foreach (var container in _containers)
		{
			if (container == _container)
			{
				// The dragged one must stick to the pointer, so it gets no transitions.
				container.Transitions = null;
				continue;
			}

			// The others animate into place and fade out to highlight the dragged item.
			container.Transitions =
			[
				new TransformOperationsTransition
				{
					Property = Visual.RenderTransformProperty,
					Duration = SlideDuration,
					Easing = new CubicEaseOut(),
				},
				new DoubleTransition
				{
					Property = Visual.OpacityProperty,
					Duration = FadeDuration,
					Easing = new CubicEaseOut(),
				},
			];

			container.Opacity = DimmedOpacity;
		}

		_container.ZIndex = 1;
		_container.Effect = new DropShadowEffect
		{
			BlurRadius = 12,
			OffsetX = 0,
			OffsetY = 2,
			Color = Colors.Black,
			Opacity = 0.35,
		};

		_dragging = true;

		return true;
	}

	private static void UpdateOrder(double draggedCenter)
	{
		var newIndex = _currentIndex;

		while (newIndex > 0 && draggedCenter < _centers[newIndex - 1])
			newIndex--;

		while (newIndex < _containers.Count - 1 && draggedCenter > _centers[newIndex + 1])
			newIndex++;

		if (newIndex == _currentIndex)
			return;

		_currentIndex = newIndex;

		for (var i = 0; i < _containers.Count; i++)
		{
			if (i == _startIndex)
				continue;

			var translation = i switch
			{
				// The dragged item moved down: the items it passed slide up.
				_ when i > _startIndex && i <= _currentIndex => -_step,

				// The dragged item moved up: the items it passed slide down.
				_ when i < _startIndex && i >= _currentIndex => _step,

				_ => 0d,
			};

			SetTranslation(_containers[i], translation, true);
		}
	}

	private static void EndDrag(bool commit)
	{
		if (!_dragging)
		{
			Reset();
			return;
		}

		var itemsControl = _itemsControl;
		var startIndex = _startIndex;
		var currentIndex = _currentIndex;

		// Drop the transform transitions so the containers snap back to their neutral
		// position instead of animating while the collection is being reordered, but keep
		// an opacity transition so the dimming fades out smoothly.
		var restored = _containers.ToArray();

		foreach (var container in restored)
		{
			container.Transitions =
			[
				new DoubleTransition
				{
					Property = Visual.OpacityProperty,
					Duration = FadeDuration,
					Easing = new CubicEaseOut(),
				},
			];

			container.RenderTransform = null;
			container.ZIndex = 0;
			container.Opacity = 1;
			container.Effect = null;
		}

		// Once the fade is over the transitions are no longer needed.
		DispatcherTimer.RunOnce(
			() =>
			{
				foreach (var container in restored)
					container.Transitions = null;
			},
			FadeDuration);

		Reset();

		if (commit &&
			currentIndex != startIndex &&
			itemsControl?.DataContext is IReorderableViewModel reorderable)
		{
			reorderable.MoveItem(startIndex, currentIndex);
		}
	}

	private static void SetTranslation(Control control, double y, bool animated)
	{
		if (!animated)
		{
			control.Transitions = null;
		}

		control.RenderTransform = TransformOperations.Parse(
			string.Create(CultureInfo.InvariantCulture, $"translateY({y}px)"));
	}

	private static void Reset()
	{
		_itemsControl = null;
		_container = null;
		_containers.Clear();
		_centers.Clear();
		_dragging = false;
	}

	private static Control? GetContainer(ItemsControl itemsControl, Visual? source)
	{
		if (itemsControl.ItemsPanelRoot is not { } panel)
			return null;

		var current = source;
		while (current is not null && current != itemsControl)
		{
			if (current.GetVisualParent() == panel && current is Control control)
				return control;

			current = current.GetVisualParent();
		}

		return null;
	}
}
