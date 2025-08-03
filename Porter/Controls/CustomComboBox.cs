using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Porter.Controls
{
	public class CustomComboBox : ComboBox
	{
		public static readonly StyledProperty<IDataTemplate?> DefaultItemProperty =
			AvaloniaProperty.Register<CustomComboBox, IDataTemplate?>(nameof(DefaultItem));

		public IDataTemplate? DefaultItem
		{
			get => GetValue(DefaultItemProperty);
			set => SetValue(DefaultItemProperty, value);
		}

		public static readonly StyledProperty<bool> IsDefaultItemSelectedProperty =
			AvaloniaProperty.Register<CustomComboBox, bool>(nameof(IsDefaultItemSelected));

		public bool IsDefaultItemSelected
		{
			get => GetValue(IsDefaultItemSelectedProperty);
			private set => SetValue(IsDefaultItemSelectedProperty, value);
		}

		private IDataTemplate? _originalItemTemplate;

		public CustomComboBox()
		{
			PropertyChanged += (sender, e) =>
			{
				if (e.Property == ItemsSourceProperty ||
					e.Property == DefaultItemProperty ||
					e.Property == ItemTemplateProperty)
					UpdateItemsAndTemplate();

				if (e.Property == SelectedItemProperty)
				{
					IsDefaultItemSelected = DefaultItem is not null && SelectedItem is null;
				}

				if (e.Property == ItemTemplateProperty &&
					DefaultItem is not null &&
					e.NewValue is { } value && 
					value is not DefaultItemTemplateSelector)
				{
					_originalItemTemplate = (IDataTemplate)value;
					base.ItemTemplate = new DefaultItemTemplateSelector(this, () => _originalItemTemplate);
				}
			};

			SelectionChanged += (sender, e) =>
			{
				if (IsDefaultItemSelected && ContainerFromIndex(0) is ComboBoxItem container)
				{
					container.IsSelected = false;
				}
			};

			base.ItemTemplate = new DefaultItemTemplateSelector(this, () => _originalItemTemplate);

			IsDefaultItemSelected = SelectedItem == null;
		}

		private IEnumerable? _originalItemsSource;
		private List<object?>? _wrappedItems;
		private bool _updating;

		private void UpdateItemsAndTemplate()
		{
			if (_updating || DefaultItem is null) return;

			try
			{
				_updating = true;

				if (ItemsSource is IEnumerable original && !ReferenceEquals(original, _wrappedItems))
				{
					_originalItemsSource = original;

					var list = new List<object?> { null };
					list.AddRange(original.Cast<object>());
					_wrappedItems = list;

					base.ItemsSource = _wrappedItems;
				}
				else if (_originalItemsSource != null && !ReferenceEquals(base.ItemsSource, _originalItemsSource))
				{
					base.ItemsSource = _originalItemsSource;
				}
			}
			finally
			{
				_updating = false;
			}
		}

		private sealed class DefaultItemTemplateSelector : IDataTemplate
		{
			private readonly CustomComboBox _owner;
			private readonly Func<IDataTemplate?> _getOriginalTemplate;

			public DefaultItemTemplateSelector(CustomComboBox owner, Func<IDataTemplate?> getOriginalTemplate)
			{
				_owner = owner;
				_getOriginalTemplate = getOriginalTemplate;
			}

			public Control? Build(object? param)
			{
				if (param == null && _owner.DefaultItem != null)
					return _owner.DefaultItem.Build(null);

				var originalTemplate = _getOriginalTemplate();

				return originalTemplate?.Build(param);
			}

			public bool Match(object? data) => true;
		}
	}
}
