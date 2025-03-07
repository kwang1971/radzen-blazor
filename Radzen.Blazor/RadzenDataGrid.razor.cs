﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataGrid component.
    /// </summary>
    /// <typeparam name="TItem">The type of the DataGrid data item.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDataGrid @data=@orders TItem="Order" AllowSorting="true" AllowPaging="true" AllowFiltering="true"&gt;
    ///     &lt;Columns&gt;
    ///         &lt;RadzenDataGridColumn TItem="Order" Property="OrderId" Title="OrderId" /&gt;
    ///         &lt;RadzenDataGridColumn TItem="Order" Property="OrderDate" Title="OrderDate" /&gt;
    ///     &lt;/Columns&gt;
    /// &lt;/RadzenDataGrid&gt;
    /// </code>
    /// </example>
    public partial class RadzenDataGrid<TItem> : PagedDataBoundComponent<TItem>
    {
#if NET5_0_OR_GREATER
        internal void SetAllowVirtualization(bool allowVirtualization)
        {
            AllowVirtualization = allowVirtualization;
        }

        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> virtualize;
        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<GroupResult> groupVirtualize;

        /// <summary>
        /// Gets Virtualize component reference.
        /// </summary>
        public Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> Virtualize
        {
            get
            {
                return virtualize;
            }
        }

        List<TItem> virtualDataItems = new List<TItem>();

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var view = AllowPaging ? PagedView : View;
            var top = request.Count;

            if(top <= 0)
            {
                top = PageSize;
            }

            await InvokeLoadData(request.StartIndex, top);
            
            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();

            virtualDataItems = (LoadData.HasDelegate ? Data : itemToInsert != null ? (new[] { itemToInsert }).Concat(view.Skip(request.StartIndex).Take(top)) : view.Skip(request.StartIndex).Take(top))?.ToList();

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>(virtualDataItems, totalItemsCount);
        }

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<GroupResult>> LoadGroups(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var top = request.Count;

            if(top <= 0)
            {
                top = PageSize;
            }

            var view = AllowPaging ? PagedView : View;
            var query = view.AsQueryable().OrderBy(string.Join(',', Groups.Select(g => $"np({g.Property})")));
            _groupedPagedView = query.GroupByMany(Groups.Select(g => $"np({g.Property})").ToArray()).ToList();

            var totalItemsCount = _groupedPagedView.Count();

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<GroupResult>(_groupedPagedView.Skip(request.StartIndex).Take(top), totalItemsCount);
        }
#endif
        RenderFragment DrawRows(IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            return new RenderFragment(builder =>
            {
#if NET5_0_OR_GREATER
                if (AllowVirtualization)
                {
                    if(AllowGrouping && Groups.Any() && !LoadData.HasDelegate)
                    {
                        builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<GroupResult>));
                        builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<GroupResult>(LoadGroups));

                        builder.AddAttribute(2, "ChildContent", (RenderFragment<GroupResult>)((context) =>
                        {
                            return (RenderFragment)((b) =>
                            {
                                b.OpenComponent(3, typeof(RadzenDataGridGroupRow<TItem>));
                                b.AddAttribute(4, "Columns", visibleColumns);
                                b.AddAttribute(5, "Grid", this);
                                b.AddAttribute(6, "GroupResult", context);
                                b.AddAttribute(7, "Builder", b);
                                b.SetKey(context);
                                b.CloseComponent();
                            });
                        }));

                        builder.AddComponentReferenceCapture(8, c => { groupVirtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<GroupResult>)c; });
                    }
                    else
                    {
                        builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>));
                        builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<TItem>(LoadItems));
                    
                        builder.AddAttribute(2, "ChildContent", (RenderFragment<TItem>)((context) =>
                        {
                            return (RenderFragment)((b) =>
                            {
                                b.OpenComponent<RadzenDataGridRow<TItem>>(3);
                                b.AddAttribute(4, "Columns", visibleColumns);
                                b.AddAttribute(5, "Grid", this);
                                b.AddAttribute(6, "TItem", typeof(TItem));
                                b.AddAttribute(7, "Item", context);
                                b.AddAttribute(8, "InEditMode", IsRowInEditMode(context));
                                b.AddAttribute(9, "Index", virtualDataItems.IndexOf(context));

                                if (editContexts.ContainsKey(context))
                                {
                                    b.AddAttribute(10, nameof(RadzenDataGridRow<TItem>.EditContext), editContexts[context]);
                                }

                                b.SetKey(context);
                                b.CloseComponent();
                            });
                        }));

                        builder.AddComponentReferenceCapture(8, c => { virtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>)c; });

                    }

                    builder.CloseComponent();
                }
                else
                {
                    DrawGroupOrDataRows(builder, visibleColumns);
                }
#else
                DrawGroupOrDataRows(builder, visibleColumns);
#endif
            });
        }

        internal void DrawGroupOrDataRows(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            if (Groups.Any())
            {
                foreach (var group in GroupedPagedView)
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridGroupRow<TItem>));
                    builder.AddAttribute(1, "Columns", visibleColumns);
                    builder.AddAttribute(3, "Grid", this);
                    builder.AddAttribute(5, "GroupResult", group);
                    builder.AddAttribute(6, "Builder", builder);
                    builder.CloseComponent();
                }
            }
            else
            {
                int i = 0;
                foreach (var item in PagedView)
                {
                    builder.OpenComponent<RadzenDataGridRow<TItem>>(0);
                    builder.AddAttribute(1, "Columns", visibleColumns);
                    builder.AddAttribute(2, "Index", i);
                    builder.AddAttribute(3, "Grid", this);
                    builder.AddAttribute(4, "TItem", typeof(TItem));
                    builder.AddAttribute(5, "Item", item);
                    builder.AddAttribute(6, "InEditMode", IsRowInEditMode(item));

                    if (editContexts.ContainsKey(item))
                    {
                        builder.AddAttribute(7, nameof(RadzenDataGridRow<TItem>.EditContext), editContexts[item]);
                    }

                    builder.CloseComponent();
                    i++;
                }
            }
        }

        /// <summary>
        /// Gets or sets the load child data callback.
        /// </summary>
        /// <value>The load child data callback.</value>
        [Parameter]
        public EventCallback<Radzen.DataGridLoadChildDataEventArgs<TItem>> LoadChildData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid data cells will follow the header cells structure in composite columns.
        /// </summary>
        /// <value><c>true</c> if DataGrid data cells will follow the header cells structure in composite columns; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCompositeDataCells { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether DataGrid data body show empty message.
        /// </summary>
        [Parameter]
        public bool ShowEmptyMessage { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether DataGrid is responsive.
        /// </summary>
        /// <value><c>true</c> if DataGrid is Responsive; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Responsive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating DataGrid density.
        /// </summary>
        [Parameter]
        public Density Density { get; set; }

        /// <summary>
        /// The grouped and paged View
        /// </summary>
        IEnumerable<GroupResult> _groupedPagedView;
        /// <summary>
        /// Gets the view grouped and paged.
        /// </summary>
        /// <value>The grouped paged view.</value>
        public IEnumerable<GroupResult> GroupedPagedView
        {
            get
            {
                if (_groupedPagedView == null)
                {
                    var orderBy = GetOrderBy();
                    var query = Groups.Count(g => g.SortOrder == null) == Groups.Count || !string.IsNullOrEmpty(orderBy) ? View : View.OrderBy(string.Join(',', Groups.Select(g => $"np({g.Property}) {(g.SortOrder == null ? "" : g.SortOrder == SortOrder.Ascending ? " asc" : " desc")}")));
                    var v = (AllowPaging && !LoadData.HasDelegate ? query.Skip(skip).Take(PageSize) : query).ToList().AsQueryable();
                    _groupedPagedView = v.GroupByMany(Groups.Select(g => $"np({g.Property})").ToArray()).ToList();
                }
                return _groupedPagedView;
            }
        }

        internal async Task RemoveGroupAsync(GroupDescriptor gd)
        {
            Groups.Remove(gd); 
            _groupedPagedView = null;

            var column = columns.Where(c => c.GetGroupProperty() == gd.Property).FirstOrDefault();
            if (column != null)
            {
                await Group.InvokeAsync(new DataGridColumnGroupEventArgs<TItem>() { Column = column, GroupDescriptor = null });
            }
            if (IsVirtualizationAllowed())
            {
                await InvokeAsync(Reload);
            }
        }

        /// <summary>
        /// Gets or sets the column group callback.
        /// </summary>
        /// <value>The column group callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnGroupEventArgs<TItem>> Group { get; set; }

        internal string getFrozenColumnClass(RadzenDataGridColumn<TItem> column, IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            return column.IsFrozen() ? "rz-frozen-cell" : "";
        }

        internal string getColumnAlignClass(RadzenDataGridColumn<TItem> column)
        {
            return $"rz-text-align-{column.TextAlign.ToString().ToLower()}";
        }

        /// <summary>
        /// The filter operator style for dates.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        protected string DateFilterOperatorStyle(RadzenDataGridColumn<TItem> column, FilterOperator value)
        {
            return column.GetFilterOperator() == value ?
                "rz-listbox-item  rz-state-highlight" :
                "rz-listbox-item ";
        }

        /// <summary>
        /// Called when filter key pressed.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        protected void OnFilterKeyPress(EventArgs args, RadzenDataGridColumn<TItem> column)
        {
            Debounce(() => DebounceFilter(column), FilterDelay);
        }

        async Task DebounceFilter(RadzenDataGridColumn<TItem> column)
        {
            var inputValue = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", getFilterInputId(column));
            if (!object.Equals(column.GetFilterValue(), inputValue))
            {
                await InvokeAsync(() => { OnFilter(new ChangeEventArgs() { Value = inputValue }, column); });
            }
        }

        /// <summary>
        /// Applies the date filter by filter operator.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="filterOperator">The filter operator.</param>
        protected void ApplyDateFilterByFilterOperator(RadzenDataGridColumn<TItem> column, FilterOperator filterOperator)
        {
            column.SetFilterOperator(filterOperator);
            SaveSettings();
        }

        internal IJSRuntime GetJSRuntime()
        {
            return JSRuntime;
        }

        private List<RadzenDataGridColumn<TItem>> columns = new List<RadzenDataGridColumn<TItem>>();
        internal readonly List<RadzenDataGridColumn<TItem>> childColumns = new List<RadzenDataGridColumn<TItem>>();
        internal List<RadzenDataGridColumn<TItem>> allColumns = new List<RadzenDataGridColumn<TItem>>();
        private List<RadzenDataGridColumn<TItem>> allPickableColumns = new List<RadzenDataGridColumn<TItem>>();
        internal object selectedColumns;

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [Parameter]
        public RenderFragment Columns { get; set; }

        internal void UpdateColumnsOrder()
        {
            if (allColumns.Any(c => c.GetOrderIndex().HasValue))
            {
                var columnsWithoutOrderIndex = columns.Where(c => !c.GetOrderIndex().HasValue).ToList();
                for (var i = 0; i < columnsWithoutOrderIndex.Count; i++)
                {
                    columnsWithoutOrderIndex[i].SetOrderIndex(columns.IndexOf(columnsWithoutOrderIndex[i]));
                }

                columns = columns.OrderBy(c => c.GetOrderIndex()).ToList();

                if (AllowColumnPicking)
                {
                    allPickableColumns = allColumns.Where(c => c.Pickable).OrderBy(c => c.GetOrderIndex()).ToList();
                }
            }
        }

        internal void AddColumn(RadzenDataGridColumn<TItem> column)
        {
            if (!columns.Contains(column) && column.Parent == null)
            {
                columns.Add(column);
            }

            if (!childColumns.Contains(column) && column.Parent != null)
            {
                childColumns.Add(column);

                var level = column.GetLevel();
                if (level > deepestChildColumnLevel)
                {
                    deepestChildColumnLevel = level;
                }
            }

            var descriptor = sorts.Where(d => d.Property == column?.GetSortProperty()).FirstOrDefault();
            if (descriptor == null && column.SortOrder.HasValue)
            {
                descriptor = new SortDescriptor() { Property = column.GetSortProperty(), SortOrder = column.SortOrder.Value };
                sorts.Add(descriptor);
            }

            if (!allColumns.Contains(column))
            {
                allColumns.Add(column);
            }

            if (AllowColumnPicking)
            {
                selectedColumns = allColumns.Where(c => c.Pickable && c.GetVisible()).ToList();
                allPickableColumns = allColumns.Where(c => c.Pickable).ToList();
            }

            UpdateColumnsOrder();

            StateHasChanged();
        }

        internal void UpdatePickableColumn(RadzenDataGridColumn<TItem> column, bool visible)
        {
            if (selectedColumns == null)
                return;

            var columnsList = ((IEnumerable<object>)selectedColumns).ToList();
            if (visible)
            {
                if (!columnsList.Contains(column))
                {
                    columnsList.Add(column);
                }
            }
            else 
            {
                if (columnsList.Contains(column))
                {
                    columnsList.Remove(column);
                }
            }

            selectedColumns = columnsList;
        }

        internal void RemoveColumn(RadzenDataGridColumn<TItem> column)
        {
            if (columns.Contains(column))
            {
                columns.Remove(column);
            }

            if (childColumns.Contains(column))
            {
                childColumns.Remove(column);
            }

            if (allColumns.Contains(column))
            {
                allColumns.Remove(column);
            }

            UpdateColumnsOrder();

            if (!disposed)
            {
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        void ToggleColumns()
        {
            var selected = ((IEnumerable<object>)selectedColumns).Cast<RadzenDataGridColumn<TItem>>();

            foreach (var c in allPickableColumns)
            {
                c.SetVisible(selected.Contains(c));
            }

            PickedColumnsChanged.InvokeAsync(new DataGridPickedColumnsChangedEventArgs<TItem>() { Columns = selected });
            SaveSettings();
        }

        /// <summary>
        /// Gets or sets the picked columns changed callback.
        /// </summary>
        /// <value>The picked columns changed callback.</value>
        [Parameter]
        public EventCallback<DataGridPickedColumnsChangedEventArgs<TItem>> PickedColumnsChanged { get; set; }

        string getFilterInputId(RadzenDataGridColumn<TItem> column)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + column.GetFilterProperty();
        }

        internal string getFilterDateFormat(RadzenDataGridColumn<TItem> column)
        {
            if (column != null && !string.IsNullOrEmpty(column.FormatString))
            {
                var formats = column.FormatString.Split(new char[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                if (formats.Length > 0)
                {
                    var format = formats[0].Trim().Split(':');
                    if (format.Length > 1)
                    {
                        return format[1].Trim();
                    }
                }
            }

            return FilterDateFormat;
        }

        internal RenderFragment DrawNumericFilter(RadzenDataGridColumn<TItem> column, bool force = true, bool isFirst = true)
        {
            return new RenderFragment(builder =>
            {
                var type = Nullable.GetUnderlyingType(column.FilterPropertyType) != null ?
                    column.FilterPropertyType : typeof(Nullable<>).MakeGenericType(column.FilterPropertyType);

                var numericType = typeof(RadzenNumeric<>).MakeGenericType(type);

                builder.OpenComponent(0, numericType);

                builder.AddAttribute(1, "Value", isFirst ? column.GetFilterValue() : column.GetSecondFilterValue());
                builder.AddAttribute(2, "Style", "width:100%");

                Action<object> action;
                if (force)
                {
                    action = args => OnFilter(new ChangeEventArgs() { Value = args }, column, isFirst);
                }
                else
                {
                    action = args => { column.SetFilterValue(args, isFirst); SaveSettings(); };
                }

                var eventCallbackGenericCreate = typeof(NumericFilterEventCallback).GetMethod("Create").MakeGenericMethod(type);
                var eventCallbackGenericAction = typeof(NumericFilterEventCallback).GetMethod("Action").MakeGenericMethod(type);

                builder.AddAttribute(3, "Change", eventCallbackGenericCreate.Invoke(this,
                    new object[] { this, eventCallbackGenericAction.Invoke(this, new object[] { action }) }));

                if (FilterMode == FilterMode.Advanced)
                {
                    builder.AddAttribute(4, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, args =>
                    {
                        var value = $"{args.Value}";
                        object filterValue = null;

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            try
                            {
                                filterValue = Convert.ChangeType(value, Nullable.GetUnderlyingType(type));
                            }
                            catch (Exception)
                            {
                                filterValue = null;
                            }
                        }

                        column.SetFilterValue(filterValue, isFirst);
                        SaveSettings();
                    }));
                }
                else if (FilterMode == FilterMode.SimpleWithMenu)
                {
                    builder.AddAttribute(4, "Disabled", column.CanSetFilterValue());
                }

                builder.CloseComponent();
            });
        }

        /// <summary>
        /// Called when filter.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="isFirst">if set to <c>true</c> [is first].</param>
        protected async Task OnFilter(ChangeEventArgs args, RadzenDataGridColumn<TItem> column, bool force = false, bool isFirst = true)
        {
            string property = column.GetFilterProperty();
            if (AllowFiltering && column.Filterable)
            {
                if (!object.Equals(isFirst ? column.GetFilterValue() : column.GetSecondFilterValue(), args.Value) || force)
                {
                    column.SetFilterValue(args.Value, isFirst);
                    skip = 0;
                    CurrentPage = 0;

                    await Filter.InvokeAsync(new DataGridColumnFilterEventArgs<TItem>()
                    {
                        Column = column,
                        FilterValue = column.GetFilterValue(),
                        SecondFilterValue = column.GetSecondFilterValue(),
                        FilterOperator = column.GetFilterOperator(),
                        SecondFilterOperator = column.GetSecondFilterOperator(),
                        LogicalFilterOperator = column.GetLogicalFilterOperator()
                    });

                    SaveSettings();

                    if (LoadData.HasDelegate && IsVirtualizationAllowed())
                    {
                        Data = null;
                    }

                    await InvokeAsync(Reload);

                    if (IsVirtualizationAllowed())
                    {
                        StateHasChanged();
                    }
                }
            }
        }


        /// <summary>
        /// Gets the columns collection.
        /// </summary>
        /// <value>The columns collection.</value>
        public IList<RadzenDataGridColumn<TItem>> ColumnsCollection
        {
            get
            {
                return columns;
            }
        }

        /// <summary>
        /// Gets or sets the column sort callback.
        /// </summary>
        /// <value>The column sort callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnSortEventArgs<TItem>> Sort { get; set; }

        internal void OnSort(EventArgs args, RadzenDataGridColumn<TItem> column)
        {
            if (AllowSorting && column.Sortable)
            {
                var property = column.GetSortProperty();
                if (!string.IsNullOrEmpty(property))
                {
                    OrderBy(property);
                }
                else
                {
                    SetColumnSortOrder(column);

                    Sort.InvokeAsync(new DataGridColumnSortEventArgs<TItem>() { Column = column, SortOrder = column.GetSortOrder() });
                    SaveSettings();

                    if (LoadData.HasDelegate && IsVirtualizationAllowed())
                    {
                        Data = null;
                    }

                    InvokeAsync(Reload);
                }
            }
        }

        /// <summary>
        /// Gets or sets the column filter callback.
        /// </summary>
        /// <value>The column filter callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnFilterEventArgs<TItem>> Filter { get; set; }

        /// <summary>
        /// Gets or sets the column filter cleared callback.
        /// </summary>
        /// <value>The column filter callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnFilterEventArgs<TItem>> FilterCleared { get; set; }

        internal async Task ClearFilter(RadzenDataGridColumn<TItem> column, bool closePopup = false)
        {
            if (closePopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", $"{PopupID}{column.GetFilterProperty()}");
            }

            column.ClearFilters();

            skip = 0;
            CurrentPage = 0;

            SaveSettings();

            await FilterCleared.InvokeAsync(new DataGridColumnFilterEventArgs<TItem>() 
            { 
                Column = column, 
                FilterValue = column.GetFilterValue(),
                SecondFilterValue = column.GetSecondFilterValue(),
                FilterOperator = column.GetFilterOperator(),
                SecondFilterOperator = column.GetSecondFilterOperator(),
                LogicalFilterOperator = column.GetLogicalFilterOperator()
            });

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
            }

            await InvokeAsync(Reload);
        }

        internal async Task ApplyFilter(RadzenDataGridColumn<TItem> column, bool closePopup = false)
        {
            if (closePopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", $"{PopupID}{column.GetFilterProperty()}");
            }
            OnFilter(new ChangeEventArgs() { Value = column.GetFilterValue() }, column, true);
        }

        internal IReadOnlyDictionary<string, object> CellAttributes(TItem item, RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Data = item, Column = column };

            if (CellRender != null)
            {
                CellRender(args);
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        internal IReadOnlyDictionary<string, object> HeaderCellAttributes(RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Column = column };

            if (HeaderCellRender != null)
            {
                HeaderCellRender(args);
            }

            var sortOrder = column.GetSortOrder();
            switch (sortOrder)
            {
                case SortOrder.Ascending:
                    args.Attributes.Add("aria-sort", "ascending");
                    break;
                case SortOrder.Descending:
                    args.Attributes.Add("aria-sort", "descending");
                    break;
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        internal IReadOnlyDictionary<string, object> FooterCellAttributes(RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Column = column };

            if (FooterCellRender != null)
            {
                FooterCellRender(args);
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        internal Dictionary<int, int> rowSpans = new Dictionary<int, int>();

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        /// <value>The logical filter operator.</value>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the filter mode.
        /// </summary>
        /// <value>The filter mode.</value>
        [Parameter]
        public FilterMode FilterMode { get; set; } = FilterMode.Advanced;

        /// <summary>
        /// Gets or sets the expand mode.
        /// </summary>
        /// <value>The expand mode.</value>
        [Parameter]
        public DataGridExpandMode ExpandMode { get; set; } = DataGridExpandMode.Multiple;

        /// <summary>
        /// Gets or sets whether the expandable indicator column is visible.
        /// </summary>
        /// <value>The expandable indicator column visibility.</value>
        [Parameter]
        public bool ShowExpandColumn { get; set; } = true;

        /// <summary>
        /// Gets or sets the edit mode.
        /// </summary>
        /// <value>The edit mode.</value>
        [Parameter]
        public DataGridEditMode EditMode { get; set; } = DataGridEditMode.Multiple;

        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
        /// <value>The filter text.</value>
        [Parameter]
        public string FilterText { get; set; } = "Filter";

        /// <summary>
        /// Gets or sets the enum filter select text.
        /// </summary>
        /// <value>The enum filter select text.</value>
        [Parameter]
        public string EnumFilterSelectText { get; set; } = "Select...";

        /// <summary>
        /// Gets or sets the and operator text.
        /// </summary>
        /// <value>The and operator text.</value>
        [Parameter]
        public string AndOperatorText { get; set; } = "And";

        /// <summary>
        /// Gets or sets the or operator text.
        /// </summary>
        /// <value>The or operator text.</value>
        [Parameter]
        public string OrOperatorText { get; set; } = "Or";

        /// <summary>
        /// Gets or sets the apply filter text.
        /// </summary>
        /// <value>The apply filter text.</value>
        [Parameter]
        public string ApplyFilterText { get; set; } = "Apply";

        /// <summary>
        /// Gets or sets the clear filter text.
        /// </summary>
        /// <value>The clear filter text.</value>
        [Parameter]
        public string ClearFilterText { get; set; } = "Clear";

        /// <summary>
        /// Gets or sets the equals text.
        /// </summary>
        /// <value>The equals text.</value>
        [Parameter]
        public string EqualsText { get; set; } = "Equals";

        /// <summary>
        /// Gets or sets the not equals text.
        /// </summary>
        /// <value>The not equals text.</value>
        [Parameter]
        public string NotEqualsText { get; set; } = "Not equals";

        /// <summary>
        /// Gets or sets the less than text.
        /// </summary>
        /// <value>The less than text.</value>
        [Parameter]
        public string LessThanText { get; set; } = "Less than";

        /// <summary>
        /// Gets or sets the less than or equals text.
        /// </summary>
        /// <value>The less than or equals text.</value>
        [Parameter]
        public string LessThanOrEqualsText { get; set; } = "Less than or equals";

        /// <summary>
        /// Gets or sets the greater than text.
        /// </summary>
        /// <value>The greater than text.</value>
        [Parameter]
        public string GreaterThanText { get; set; } = "Greater than";

        /// <summary>
        /// Gets or sets the greater than or equals text.
        /// </summary>
        /// <value>The greater than or equals text.</value>
        [Parameter]
        public string GreaterThanOrEqualsText { get; set; } = "Greater than or equals";

        /// <summary>
        /// Gets or sets the ends with text.
        /// </summary>
        /// <value>The ends with text.</value>
        [Parameter]
        public string EndsWithText { get; set; } = "Ends with";

        /// <summary>
        /// Gets or sets the contains text.
        /// </summary>
        /// <value>The contains text.</value>
        [Parameter]
        public string ContainsText { get; set; } = "Contains";

        /// <summary>
        /// Gets or sets the does not contain text.
        /// </summary>
        /// <value>The does not contain text.</value>
        [Parameter]
        public string DoesNotContainText { get; set; } = "Does not contain";

        /// <summary>
        /// Gets or sets the starts with text.
        /// </summary>
        /// <value>The starts with text.</value>
        [Parameter]
        public string StartsWithText { get; set; } = "Starts with";

        /// <summary>
        /// Gets or sets the not null text.
        /// </summary>
        /// <value>The not null text.</value>
        [Parameter]
        public string IsNotNullText { get; set; } = "Is not null";

        /// <summary>
        /// Gets or sets the is null text.
        /// </summary>
        /// <value>The null text.</value>
        [Parameter]
        public string IsNullText { get; set; } = "Is null";
        
        /// <summary>
        /// Gets or sets the is empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string IsEmptyText { get; set; } = "Is empty";
        
        /// <summary>
        /// Gets or sets the is not empty text.
        /// </summary>
        /// <value>The not empty text.</value>
        [Parameter]
        public string IsNotEmptyText { get; set; } = "Is not empty";

        internal class NumericFilterEventCallback
        {
            public static EventCallback<T> Create<T>(object receiver, Action<T> action)
            {
                return EventCallback.Factory.Create<T>(receiver, action);
            }

            public static Action<T> Action<T>(Action<object> action)
            {
                return args => action(args);
            }
        }

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        /// <value>The filter case sensitivity.</value>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        /// <summary>
        /// Gets or sets the filter delay.
        /// </summary>
        /// <value>The filter delay.</value>
        [Parameter]
        public int FilterDelay { get; set; } = 500;

        /// <summary>
        /// Gets or sets the filter date format.
        /// </summary>
        /// <value>The filter date format.</value>
        [Parameter]
        public string FilterDateFormat { get; set; }

        /// <summary>
        /// Gets or sets the width of all columns.
        /// </summary>
        /// <value>The width of the columns.</value>
        [Parameter]
        public string ColumnWidth { get; set; }

        private string _emptyText = "No records to display.";
        /// <summary>
        /// Gets or sets the empty text shown when Data is empty collection.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText
        {
            get { return _emptyText; }
            set
            {
                if (value != _emptyText)
                {
                    _emptyText = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the empty template shown when Data is empty collection.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }
#if NET5_0_OR_GREATER
        /// <summary>
        /// Gets or sets a value indicating whether this instance is virtualized.
        /// </summary>
        /// <value><c>true</c> if this instance is virtualized; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowVirtualization { get; set; }
#endif
        /// <summary>
        /// Gets or sets a value indicating whether this instance loading indicator is shown.
        /// </summary>
        /// <value><c>true</c> if this instance loading indicator is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sorting is allowed.
        /// </summary>
        /// <value><c>true</c> if sorting is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multi column sorting is allowed.
        /// </summary>
        /// <value><c>true</c> if multi column sorting is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowMultiColumnSorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multi column sorting index is shown.
        /// </summary>
        /// <value><c>true</c> if multi column sorting index is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowMultiColumnSortingIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether filtering is allowed.
        /// </summary>
        /// <value><c>true</c> if filtering is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether column resizing is allowed.
        /// </summary>
        /// <value><c>true</c> if column resizing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnResize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether column reorder is allowed.
        /// </summary>
        /// <value><c>true</c> if column reorder is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnReorder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether column picking is allowed.
        /// </summary>
        /// <value><c>true</c> if column picking is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnPicking { get; set; }

        /// <summary>
        /// Gets or sets the column picker columns showing text.
        /// </summary>
        /// <value>The column picker columns showing text.</value>
        [Parameter]
        public string ColumnsShowingText { get; set; } = "columns showing";

        /// <summary>
        /// Gets or sets the column picker all columns text.
        /// </summary>
        /// <value>The column picker all columns text.</value>
        [Parameter]
        public string AllColumnsText { get; set; } = "All";

        /// <summary>
        /// Gets or sets the column picker columns text.
        /// </summary>
        /// <value>The column picker columns text.</value>
        [Parameter]
        public string ColumnsText { get; set; } = "Columns";

        /// <summary>
        /// Gets or sets a value indicating whether user can pick all columns in column picker.
        /// </summary>
        /// <value><c>true</c> if pick of all columns is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowPickAllColumns { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether grouping is allowed.
        /// </summary>
        /// <value><c>true</c> if grouping is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowGrouping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether grouped column should be hidden.
        /// </summary>
        /// <value><c>true</c> if grouped columns should be hidden; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool HideGroupedColumn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether group footers are visible even when the group is collapsed.
        /// </summary>
        /// <value><c>true</c> if group footers are visible when the group is collapsed otherwise, <c>false</c>.</value>
        [Parameter]
        public bool GroupFootersAlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the group header template.
        /// </summary>
        /// <value>The group header template.</value>
        [Parameter]
        public RenderFragment<Group> GroupHeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the group panel text.
        /// </summary>
        /// <value>The group panel text.</value>
        [Parameter]
        public string GroupPanelText { get; set; } = "Drag a column header here and drop it to group by that column";

        internal string getColumnResizerId(int columnIndex)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + columnIndex;
        }

        internal async Task StartColumnResize(MouseEventArgs args, int columnIndex)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.startColumnResize", getColumnResizerId(columnIndex), Reference, columnIndex, args.ClientX);
        }

        int? indexOfColumnToReoder;

        internal async Task StartColumnReorder(MouseEventArgs args, int columnIndex)
        {
            indexOfColumnToReoder = columnIndex;
            await JSRuntime.InvokeVoidAsync("Radzen.startColumnReorder", getColumnResizerId(columnIndex));
        }

        internal async Task EndColumnReorder(MouseEventArgs args, int columnIndex)
        {
            if (indexOfColumnToReoder != null)
            {
                var visibleColumns = columns.Where(c => c.GetVisible()).ToList();
                var columnToReorder = visibleColumns.ElementAtOrDefault(indexOfColumnToReoder.Value);
                var columnToReorderTo = visibleColumns.ElementAtOrDefault(columnIndex);

                if (columnToReorder != null && columnToReorderTo != null)
                {
                    var actualColumnIndexFrom = columns.IndexOf(columnToReorder);
                    var actualColumnIndexTo = columns.IndexOf(columnToReorderTo);
                    columns.Remove(columnToReorder);
                    columns.Insert(actualColumnIndexTo, columnToReorder);

                    columnToReorder.SetOrderIndex(columns.IndexOf(columnToReorder));
                    columnToReorderTo.SetOrderIndex(columns.IndexOf(columnToReorderTo));

                    UpdateColumnsOrder();

                    await ColumnReordered.InvokeAsync(new DataGridColumnReorderedEventArgs<TItem>
                    {
                        Column = columnToReorder,
                        OldIndex = actualColumnIndexFrom,
                        NewIndex = actualColumnIndexTo
                    });

                    SaveSettings();

                    StateHasChanged();
                }

                indexOfColumnToReoder = null;
            }
        }

        /// <summary>
        /// Called when column is resized.
        /// </summary>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="value">The value.</param>
        [JSInvokable("RadzenGrid.OnColumnResized")]
        public async Task OnColumnResized(int columnIndex, double value)
        {
            var column = columns.Where(c => c.GetVisible()).ToList()[columnIndex];
            column.SetWidth($"{Math.Round(value)}px");
            await ColumnResized.InvokeAsync(new DataGridColumnResizedEventArgs<TItem>
            {
                Column = column,
                Width = value,
            });
            SaveSettings();
        }

        internal string GetOrderBy()
        {
            return string.Join(",", sorts.Select(d => allColumns.ToList().Where(c => c.GetSortProperty() == d.Property).FirstOrDefault()).Where(c => c != null).Select(c => c.GetSortOrderAsString(IsOData())));
        }

        /// <summary>
        /// Gets or sets the column resized callback.
        /// </summary>
        /// <value>The column resized callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnResizedEventArgs<TItem>> ColumnResized { get; set; }

        /// <summary>
        /// Gets or sets the column reordered callback.
        /// </summary>
        /// <value>The column reordered callback.</value>
        [Parameter]
        public EventCallback<DataGridColumnReorderedEventArgs<TItem>> ColumnReordered { get; set; }

        IQueryable<TItem> GetSelfRefView(IQueryable<TItem> view, string orderBy)
        {
            if (!string.IsNullOrEmpty(orderBy))
            {
                if (typeof(TItem) == typeof(object))
                {
                    var firstItem = view.FirstOrDefault();
                    if (firstItem != null)
                    {
                        view = view.Cast(firstItem.GetType()).AsQueryable().OrderBy(orderBy).Cast<TItem>();
                    }
                }
                else
                {
                    view = view.OrderBy(orderBy);
                }
            }

            var viewList = view.ToList();
            var countWithChildren = viewList.Count + childData.SelectMany(d => d.Value.Data).Count();

            for (int i = 0; i < countWithChildren; i++)
            {
                var item = viewList.ElementAtOrDefault(i);

                if (item != null && childData.ContainsKey(item))
                {
                    var level = 1;
                    var parentChildData = childData[item].ParentChildData;
                    while (parentChildData != null)
                    {
                        parentChildData = parentChildData.ParentChildData;
                        level++;
                    }

                    childData[item].Level = level;

                    var cd = childData[item].Data.AsQueryable();
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        cd = cd.OrderBy(orderBy);
                    }

                    viewList.InsertRange(viewList.IndexOf(item) + 1, cd);
                }
            }

            view = viewList.AsQueryable()
                .Where(i => childData.ContainsKey(i) && childData[i].Data.AsQueryable().Where<TItem>(allColumns).Any()
                    || viewList.AsQueryable().Where<TItem>(allColumns).Contains(i));

            return view;
        }

        /// <summary>
        /// Gets the view - Data with sorting, filtering and paging applied.
        /// </summary>
        /// <value>The view.</value>
        public override IQueryable<TItem> View
        {
            get
            {
                var orderBy = GetOrderBy();

                if (LoadData.HasDelegate)
                {
                    if (childData.Any())
                    {
                        return GetSelfRefView(base.View, orderBy);

                    }
                    else
                    {
                        return base.View;
                    }
                }

                IQueryable<TItem> view;

                if (childData.Any())
                {
                    view = GetSelfRefView(base.View, orderBy);
                }
                else
                {
                    view = base.View.Where<TItem>(allColumns);

                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        if (typeof(TItem) == typeof(object))
                        {
                            var firstItem = view.FirstOrDefault();
                            if (firstItem != null)
                            {
                                view = view.Cast(firstItem.GetType()).AsQueryable().OrderBy(orderBy).Cast<TItem>();
                            }
                        }
                        else
                        {
                            view = view.OrderBy(orderBy);
                        }
                    }
                }

                if (!IsVirtualizationAllowed() || AllowPaging)
                {
                    var count = view.Count();
                    if (count != Count)
                    {
                        Count = count;

                        if (skip >= Count && Count > PageSize)
                        {
                            skip = Count - PageSize;
                        }

                        if (Count <= PageSize)
                        {
                            skip = 0;
                            CurrentPage = 0;
                        }

                        CalculatePager();

                        StateHasChanged();
                    }
                }

                return view;
            }
        }

        internal bool IsVirtualizationAllowed()
        {
    #if NET5_0_OR_GREATER
            return AllowVirtualization;
    #else
            return false;
    #endif
        }

        IList<TItem> _value;

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
        [Parameter]
        public IList<TItem> Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public EventCallback<IList<TItem>> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the row select callback.
        /// </summary>
        /// <value>The row select callback.</value>
        [Parameter]
        public EventCallback<TItem> RowSelect { get; set; }

        /// <summary>
        /// Gets or sets the row deselect callback.
        /// </summary>
        /// <value>The row deselect callback.</value>
        [Parameter]
        public EventCallback<TItem> RowDeselect { get; set; }

        /// <summary>
        /// Gets or sets the row click callback.
        /// </summary>
        /// <value>The row click callback.</value>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowClick { get; set; }

        /// <summary>
        /// Gets or sets the row double click callback.
        /// </summary>
        /// <value>The row double click callback.</value>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowDoubleClick { get; set; }

        /// <summary>
        /// Gets or sets the cell click callback.
        /// </summary>
        /// <value>The cell click callback.</value>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellClick { get; set; }

        /// <summary>
        /// Gets or sets the cell double click callback.
        /// </summary>
        /// <value>The cell double click callback.</value>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellDoubleClick { get; set; }

        /// <summary>
        /// Gets or sets the row click callback.
        /// </summary>
        /// <value>The row click callback.</value>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellContextMenu { get; set; }

        /// <summary>
        /// Gets or sets the row expand callback.
        /// </summary>
        /// <value>The row expand callback.</value>
        [Parameter]
        public EventCallback<TItem> RowExpand { get; set; }

        /// <summary>
        /// Gets or sets the group row expand callback.
        /// </summary>
        /// <value>The group row expand callback.</value>
        [Parameter]
        public EventCallback<Group> GroupRowExpand { get; set; }

        /// <summary>
        /// Gets or sets the row collapse callback.
        /// </summary>
        /// <value>The row collapse callback.</value>
        [Parameter]
        public EventCallback<TItem> RowCollapse { get; set; }

        /// <summary>
        /// Gets or sets the group row collapse callback.
        /// </summary>
        /// <value>The group row collapse callback.</value>
        [Parameter]
        public EventCallback<Group> GroupRowCollapse { get; set; }

        /// <summary>
        /// Gets or sets the row render callback. Use it to set row attributes.
        /// </summary>
        /// <value>The row render callback.</value>
        [Parameter]
        public Action<RowRenderEventArgs<TItem>> RowRender { get; set; }

        /// <summary>
        /// Gets or sets the group row render callback. Use it to set group row attributes.
        /// </summary>
        /// <value>The group row render callback.</value>
        [Parameter]
        public Action<GroupRowRenderEventArgs> GroupRowRender { get; set; }

        /// <summary>
        /// Gets or sets the cell render callback. Use it to set cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> CellRender { get; set; }

        /// <summary>
        /// Gets or sets the header cell render callback. Use it to set header cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> HeaderCellRender { get; set; }

        /// <summary>
        /// Gets or sets the footer cell render callback. Use it to set footer cell attributes.
        /// </summary>
        /// <value>The cell render callback.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> FooterCellRender { get; set; }

        /// <summary>
        /// Gets or sets the render callback.
        /// </summary>
        /// <value>The render callback.</value>
        [Parameter]
        public Action<DataGridRenderEventArgs<TItem>> Render { get; set; }

        /// <summary>
        /// Called when data is changed.
        /// </summary>
        protected override void OnDataChanged()
        {
            Reset(!IsOData() && !LoadData.HasDelegate);

            if (!IsOData() && !LoadData.HasDelegate && !Page.HasDelegate)
            {
                skip = 0;
                CurrentPage = 0;
                CalculatePager();
            }
        }

        /// <summary>
        /// Resets the DataGrid instance to initial state with no sorting, grouping and/or filtering.
        /// </summary>
        /// <param name="resetColumnState">if set to <c>true</c> [reset column state].</param>
        /// <param name="resetRowState">if set to <c>true</c> [reset row state].</param>
        public void Reset(bool resetColumnState = true, bool resetRowState = false)
        {
            _groupedPagedView = null;
            _view = null;
            _value = new List<TItem>();

            if (resetRowState)
            {
                selectedItems.Clear();
                expandedItems.Clear();
                collapsedGroupItems.Clear();
            }

            if (resetColumnState)
            {
                allColumns.ToList().ForEach(c => 
                { 
                    c.ClearFilters(); 
                    c.ResetSortOrder();
                    c.SetOrderIndex(null);
                    c.SetWidth(null);
                });
                sorts.Clear();
           }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async override Task Reload()
        {
            _groupedPagedView = null;
            _view = null;

            if (Data != null && !LoadData.HasDelegate)
            {
                Count = 1;
            }
#if NET5_0_OR_GREATER
            if (AllowVirtualization)
            {
                if(!LoadData.HasDelegate)
                {
                    if(virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                    }

                    if(groupVirtualize != null)
                    {
                        await groupVirtualize.RefreshDataAsync();
                    }
                }
                else
                {
                    Data = null;
                }
            }
#endif
            await InvokeLoadData(skip, PageSize);

            CalculatePager();

            if (!LoadData.HasDelegate)
            {
                StateHasChanged();
            }
            else
            {
#if NET5_0_OR_GREATER
                if (AllowVirtualization)
                {
                    if(virtualize != null)
                    {
                        await virtualize.RefreshDataAsync();
                        await virtualize.RefreshDataAsync();
                    }

                    if(groupVirtualize != null)
                    {
                        await groupVirtualize.RefreshDataAsync();
                    }
                }
#endif
            }
        }

        IEnumerable<FilterDescriptor> filters = Enumerable.Empty<FilterDescriptor>();

        async Task InvokeLoadData(int start, int top)
        {
            var orderBy = GetOrderBy();

            Query.Skip = skip;
            Query.Top = PageSize;
            Query.OrderBy = orderBy;

            var filterString = allColumns.ToList().ToFilterString<TItem>();
            Query.Filter = filterString;

            filters = allColumns.ToList().Where(c => c.Filterable && c.GetVisible() && (c.GetFilterValue() != null
                    || c.GetFilterOperator() == FilterOperator.IsNotNull || c.GetFilterOperator() == FilterOperator.IsNull
                    || c.GetFilterOperator() == FilterOperator.IsEmpty | c.GetFilterOperator() == FilterOperator.IsNotEmpty))
                .Select(c => new FilterDescriptor()
                {
                    Property = c.GetFilterProperty(),
                    FilterValue = c.GetFilterValue(),
                    FilterOperator = c.GetFilterOperator(),
                    SecondFilterValue = c.GetSecondFilterValue(),
                    SecondFilterOperator = c.GetSecondFilterOperator(),
                    LogicalFilterOperator = c.GetLogicalFilterOperator()
                }).ToList();

            if (LoadData.HasDelegate)
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs()
                {
                    Skip = start,
                    Top = top,
                    OrderBy = orderBy,
                    Filter = IsOData() ? allColumns.ToList().ToODataFilterString<TItem>() : filterString,
                    Filters = filters,
                    Sorts = sorts
                });
            }
        }

        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when parameters set asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            if (Visible && !LoadData.HasDelegate && _view == null)
            {
                InvokeAsync(Reload);
            }
            else
            {
                CalculatePager();
            }

            return Task.CompletedTask;
        }

        internal Dictionary<RadzenDataGridGroupRow<TItem>, bool> collapsedGroupItems = new Dictionary<RadzenDataGridGroupRow<TItem>, bool>();

        internal string ExpandedGroupItemStyle(RadzenDataGridGroupRow<TItem> item, bool? expandedOnLoad)
        {
            return collapsedGroupItems.Keys.Contains(item) || expandedOnLoad == false ? "rz-row-toggler rzi-grid-sort rzi-chevron-circle-right" : "rz-row-toggler rzi-grid-sort rzi-chevron-circle-down";
        }

        internal bool IsGroupItemExpanded(RadzenDataGridGroupRow<TItem> item)
        {
            return !collapsedGroupItems.Keys.Contains(item);
        }

        internal async System.Threading.Tasks.Task ExpandGroupItem(RadzenDataGridGroupRow<TItem> item, bool? expandedOnLoad)
        {
            if (expandedOnLoad == true)
                return;

            if (!collapsedGroupItems.Keys.Contains(item))
            {
                await GroupRowCollapse.InvokeAsync(item.Group);
                collapsedGroupItems.Add(item, true);
            }
            else
            {
                await GroupRowExpand.InvokeAsync(item.Group);
                collapsedGroupItems.Remove(item);
            }

            await InvokeAsync(StateHasChanged);
        }

        internal Dictionary<TItem, bool> expandedItems = new Dictionary<TItem, bool>();

        internal string ExpandedItemStyle(TItem item)
        {
            return expandedItems.Keys.Contains(item) ? "rz-row-toggler rzi-chevron-circle-down" : "rz-row-toggler rzi-chevron-circle-right";
        }

        internal Dictionary<TItem, bool> selectedItems = new Dictionary<TItem, bool>();

        internal string RowStyle(TItem item, int index)
        {
            var isInEditMode = IsRowInEditMode(item) ? "rz-datatable-edit" : "";

            return (RowSelect.HasDelegate || ValueChanged.HasDelegate || SelectionMode == DataGridSelectionMode.Multiple) && selectedItems.Keys.Contains(item) ? $"rz-state-highlight rz-data-row {isInEditMode} " : $"rz-data-row {isInEditMode} ";
        }

        internal Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>> RowAttributes(TItem item)
        {
            var args = new Radzen.RowRenderEventArgs<TItem>() { Data = item, Expandable = Template != null || LoadChildData.HasDelegate };

            if (RowRender != null)
            {
                RowRender(args);
            }

            return new Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>>(args, new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes));
        }

        internal Tuple<GroupRowRenderEventArgs, IReadOnlyDictionary<string, object>> GroupRowAttributes(RadzenDataGridGroupRow<TItem> item)
        {
            var args = new Radzen.GroupRowRenderEventArgs() { Group = item.Group, FirstRender = firstRender };

            if (GroupRowRender != null)
            {
                GroupRowRender(args);
            }

            return new Tuple<GroupRowRenderEventArgs, IReadOnlyDictionary<string, object>>(args, new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes));
        }

        private bool visibleChanged = false;
        internal bool firstRender = true;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var emptyTextChanged = parameters.DidParameterChange(nameof(EmptyText), EmptyText);

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            bool valueChanged = parameters.DidParameterChange(nameof(Value), Value);

            await base.SetParametersAsync(parameters);

            if (valueChanged)
            {
                selectedItems.Clear();

                if (Value != null)
                {
                    Value.Where(v => v != null).ToList().ForEach(v => selectedItems.Add(v, true));
                }
            }

            if (emptyTextChanged)
            {
                await ChangeState();
            }

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    Dispose();
                }
            }
        }

        internal override async Task ReloadOnFirstRender()
        {
            if (firstRender && Visible && (LoadData.HasDelegate && Data == null) && IsVirtualizationAllowed())
            {
                await InvokeLoadData(skip, PageSize);
            }
            else
            {
                await base.ReloadOnFirstRender();
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (Visible)
            {
                if (settings != null)
                {
                    await LoadSettings(settings);
                }

                var args = new Radzen.DataGridRenderEventArgs<TItem>() { Grid = this, FirstRender = firstRender };

                if (Render != null)
                {
                    Render(args);
                }
            }

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;
            }
        }

        /// <summary>
        /// Expands the row to show the content defined in Template property.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task ExpandRow(TItem item)
        {
            await ExpandItem(item);
        }

        internal async System.Threading.Tasks.Task ExpandItem(TItem item)
        {
            if (ExpandMode == DataGridExpandMode.Single && expandedItems.Keys.Any() && !LoadChildData.HasDelegate)
            {
                var itemToCollapse = expandedItems.Keys.FirstOrDefault();
                if (itemToCollapse != null)
                {
                    expandedItems.Remove(itemToCollapse);
                    await RowCollapse.InvokeAsync(itemToCollapse);

                    if (object.Equals(item, itemToCollapse))
                    {
                        return;
                    }

                }
            }

            if (!expandedItems.Keys.Contains(item))
            {
                expandedItems.Add(item, true);
                await RowExpand.InvokeAsync(item);

                var args = new DataGridLoadChildDataEventArgs<TItem>() { Item = item };
                await LoadChildData.InvokeAsync(args);

                if (args.Data != null && !childData.ContainsKey(item))
                {
                    childData.Add(item, new DataGridChildData<TItem>() { Data = args.Data, ParentChildData = childData.Where(c => c.Value.Data.Contains(item)).Select(c => c.Value).FirstOrDefault() });
                    _view = null;
                }
            }
            else
            {
                expandedItems.Remove(item);
                await RowCollapse.InvokeAsync(item);

                if (childData.ContainsKey(item))
                {
                    childData.Remove(item);
                    _view = null;
                }
            }

            await InvokeAsync(StateHasChanged);
        }

        internal Dictionary<TItem, DataGridChildData<TItem>> childData = new Dictionary<TItem, DataGridChildData<TItem>>();

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid row can be selected on row click.
        /// </summary>
        /// <value><c>true</c> if DataGrid row can be selected on row click; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowRowSelectOnRowClick { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether DataGrid should use alternating row styles.
        /// </summary>
        /// <value><c>true</c> if DataGrid is using alternating row styles; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowAlternatingRows { get; set; } = true;

        /// <summary>
        /// Gets or sets the grid lines.
        /// </summary>
        /// <value>The grid lines.</value>
        [Parameter]
        public DataGridGridLines GridLines { get; set; } = DataGridGridLines.Default;

        internal bool ShowGridLines(RadzenDataGridColumn<TItem> column)
        {
            return column.Columns != null || column.Parent != null;
        }

        internal string getCompositeCellCSSClass(RadzenDataGridColumn<TItem> column)
        {
            return column.Columns != null || column.Parent != null ? "rz-composite-cell" : "";
        }

        internal string getGridLinesCSSClass()
        {
            if (GridLines == DataGridGridLines.Default)
            {
                return "";
            }

            return $"rz-grid-gridlines-{Enum.GetName(typeof(DataGridGridLines), GridLines).ToLower()}";
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <value>The selection mode.</value>
        [Parameter]
        public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Single;

        internal async Task OnCellContextMenu(DataGridCellMouseEventArgs<TItem> args)
        {
            await CellContextMenu.InvokeAsync(args);
        }

        internal async Task OnCellClick(DataGridCellMouseEventArgs<TItem> args)
        {
            await CellClick.InvokeAsync(args);
        }

        internal async Task OnCellDblClick(DataGridCellMouseEventArgs<TItem> args)
        {
            await CellDoubleClick.InvokeAsync(args);
        }

        internal async Task OnRowClick(DataGridRowMouseEventArgs<TItem> args)
        {
            await RowClick.InvokeAsync(args);
            if (AllowRowSelectOnRowClick)
            {
                await OnRowSelect(args.Data);
            }
        }

        internal async System.Threading.Tasks.Task OnRowSelect(object item, bool raiseChange = true)
        {
            if (SelectionMode == DataGridSelectionMode.Single && item != null && selectedItems.Keys.Contains((TItem)item))
            {
                // Legacy RowSelect raise
                if (raiseChange)
                {
                    await RowSelect.InvokeAsync((TItem)item);
                }
                return;
            }

            if (SelectionMode == DataGridSelectionMode.Single && selectedItems.Keys.Any())
            {
                var itemToDeselect = selectedItems.Keys.FirstOrDefault();
                if (itemToDeselect != null)
                {
                    selectedItems.Remove(itemToDeselect);
                    await RowDeselect.InvokeAsync(itemToDeselect);
                }
            }

            if (item != null)
            {
                if (!selectedItems.Keys.Contains((TItem)item))
                {
                    selectedItems.Add((TItem)item, true);
                    if (raiseChange)
                    {
                        await RowSelect.InvokeAsync((TItem)item);
                    }
                }
                else
                {
                    selectedItems.Remove((TItem)item);
                    await RowDeselect.InvokeAsync((TItem)item);
                }
            }
            else
            {
                if (raiseChange)
                {
                    await RowSelect.InvokeAsync((TItem)item);
                }
            }

            var value = selectedItems.Keys;

            _value = SelectionMode == DataGridSelectionMode.Multiple ? new List<TItem>(value) : new List<TItem>() { value.FirstOrDefault() };

            await ValueChanged.InvokeAsync(_value);

            StateHasChanged();
        }

        /// <summary>
        /// Selects the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task SelectRow(TItem item)
        {
            await OnRowSelect(item, true);
        }

        internal async System.Threading.Tasks.Task OnRowDblClick(DataGridRowMouseEventArgs<TItem> args)
        {
            await RowDoubleClick.InvokeAsync(args);
        }

        /// <summary>
        /// Gets or sets the row edit callback.
        /// </summary>
        /// <value>The row edit callback.</value>
        [Parameter]
        public EventCallback<TItem> RowEdit { get; set; }

        /// <summary>
        /// Gets or sets the row update callback.
        /// </summary>
        /// <value>The row update callback.</value>
        [Parameter]
        public EventCallback<TItem> RowUpdate { get; set; }

        /// <summary>
        /// Gets or sets the row create callback.
        /// </summary>
        /// <value>The row create callback.</value>
        [Parameter]
        public EventCallback<TItem> RowCreate { get; set; }


        internal Dictionary<TItem, bool> editedItems = new Dictionary<TItem, bool>();

        internal Dictionary<TItem, EditContext> editContexts = new Dictionary<TItem, EditContext>();

        /// <summary>
        /// Edits the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task EditRow(TItem item)
        {
            if(itemToInsert != null)
            {
                CancelEditRow(itemToInsert);
            }

            await EditRowInternal(item);
        }

        async System.Threading.Tasks.Task EditRowInternal(TItem item)
        {
            if (EditMode == DataGridEditMode.Single && editedItems.Keys.Any())
            {
                var itemToCancel = editedItems.Keys.FirstOrDefault();
                if (itemToCancel != null)
                {
                    editedItems.Remove(itemToCancel);
                    editContexts.Remove(itemToCancel);
                }
            }

            if (!editedItems.Keys.Contains(item))
            {
                editedItems.Add(item, true);

                var editContext = new EditContext(item);
                editContexts.Add(item, editContext);

                await RowEdit.InvokeAsync(item);

                StateHasChanged();
            }
        }

        /// <summary>
        /// Updates the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task UpdateRow(TItem item)
        {
            if (editedItems.Keys.Contains(item))
            {
                var editContext = editContexts[item];

                if (editContext.Validate())
                {
                    editedItems.Remove(item);
                    editContexts.Remove(item);

                    if (object.Equals(itemToInsert, item))
                    {
                        await RowCreate.InvokeAsync(item);
                        itemToInsert = default(TItem);
                    }
                    else
                    {
                        await RowUpdate.InvokeAsync(item);
                    }
                }

                StateHasChanged();
            }
        }

        /// <summary>
        /// Cancels the edited row.
        /// </summary>
        /// <param name="item">The item.</param>
        public void CancelEditRow(TItem item)
        {
            if (object.Equals(itemToInsert, item))
            {
                if(!IsVirtualizationAllowed())
                {
                    var list = this.PagedView.ToList();
                    list.Remove(item);
                    this._view = list.AsQueryable();
                    this.Count--;
                    itemToInsert = default(TItem);
                    StateHasChanged();
                }
                else
                {
#if NET5_0_OR_GREATER
                    itemToInsert = default(TItem);
                    if(virtualize != null)
                    {
                        virtualize.RefreshDataAsync();
                    }

                    if(groupVirtualize != null)
                    {
                        groupVirtualize.RefreshDataAsync();
                    }
#endif
                }
            }
            else
            {
                int hash = item.GetHashCode();

                if (editedItems.Keys.Contains(item))
                {
                    editedItems.Remove(item);
                    editContexts.Remove(item);

                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Determines whether row in edit mode.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if row in edit mode; otherwise, <c>false</c>.</returns>
        public bool IsRowInEditMode(TItem item)
        {
            return editedItems.Keys.Contains(item);
        }

        TItem itemToInsert;

        /// <summary>
        /// Inserts new row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task InsertRow(TItem item)
        {
            itemToInsert = item;
            if(!IsVirtualizationAllowed())
            {
                var list = this.PagedView.ToList();
                list.Insert(0, item);
                this._view = list.AsQueryable();
                this.Count++;
            }
            else
            {
#if NET5_0_OR_GREATER
                if(virtualize != null)
                {
                    await virtualize.RefreshDataAsync();
                }

                if(groupVirtualize != null)
                {
                    await groupVirtualize.RefreshDataAsync();
                }
#endif
            }

            await EditRowInternal(item);
        }


        bool? isOData;

        internal bool IsOData()
        {
            if(isOData == null && Data != null)
            {
                isOData = typeof(ODataEnumerable<TItem>).IsAssignableFrom(Data.GetType());
            }

            return isOData != null ? isOData.Value : false;
        }

        internal List<SortDescriptor> sorts = new List<SortDescriptor>();

        internal void SetColumnSortOrder(RadzenDataGridColumn<TItem> column)
        {
            if (!AllowMultiColumnSorting)
            {
                foreach (var c in allColumns.ToList().Where(c => c != column))
                {
                    c.SetSortOrderInternal(null);
                }
                sorts.Clear();
            }

            var descriptor = sorts.Where(d => d.Property == column?.GetSortProperty()).FirstOrDefault();
            if (descriptor == null)
            {
                descriptor = new SortDescriptor() { Property = column.GetSortProperty() };
            }

            if (column.GetSortOrder() == null)
            {
                column.SetSortOrderInternal(SortOrder.Ascending);
                descriptor.SortOrder = SortOrder.Ascending;
            }
            else if (column.GetSortOrder() == SortOrder.Ascending)
            {
                column.SetSortOrderInternal(SortOrder.Descending);
                descriptor.SortOrder = SortOrder.Descending;
            }
            else if (column.GetSortOrder() == SortOrder.Descending)
            {
                column.SetSortOrderInternal(null);
                if (sorts.Where(d => d.Property == column?.GetSortProperty()).Any())
                {
                    sorts.Remove(descriptor);
                }
                descriptor = null;
            }

            if (descriptor != null && !sorts.Where(d => d.Property == column?.GetSortProperty()).Any())
            {
                sorts.Add(descriptor);
            }
        }

        void GroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                var column = columns.Where(c => c.GetGroupProperty() == ((GroupDescriptor)args.NewItems[0]).Property).FirstOrDefault();

                if (HideGroupedColumn)
                {
                    column.SetVisible(false);
                    if (!groupedColumns.Contains(column))
                    {
                        groupedColumns.Add(column);
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                var column = columns.Where(c => c.GetGroupProperty() == ((GroupDescriptor)args.OldItems[0]).Property).FirstOrDefault();

                if (HideGroupedColumn)
                {
                    column.SetVisible(true);
                    if (groupedColumns.Contains(column))
                    {
                        groupedColumns.Remove(column);
                    }
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var column in groupedColumns)
                {
                    if (HideGroupedColumn)
                    {
                        column.SetVisible(true);
                    }
                }
            }

            SaveSettings();
        }

        List<RadzenDataGridColumn<TItem>> groupedColumns = new List<RadzenDataGridColumn<TItem>>();
        /// <summary>
        /// Gets or sets the group descriptors.
        /// </summary>
        /// <value>The groups.</value>
        public ObservableCollection<GroupDescriptor> Groups 
        { 
            get
            {
                if (groups == null)
                {
                    groups = new ObservableCollection<GroupDescriptor>();
                    groups.CollectionChanged -= GroupsCollectionChanged;
                    groups.CollectionChanged += GroupsCollectionChanged;
                }

                return groups;
            }
            set
            {
                groups = value;
            }
        }

        ObservableCollection<GroupDescriptor> groups;

        internal async Task EndColumnDropToGroup()
        {
            if(indexOfColumnToReoder != null && AllowGrouping)
            {
                var column = columns.Where(c => c.GetVisible()).ElementAtOrDefault(indexOfColumnToReoder.Value);

                if(column != null && column.Groupable && !string.IsNullOrEmpty(column.GetGroupProperty()))
                {
                    var descriptor = Groups.Where(d => d.Property == column.GetGroupProperty()).FirstOrDefault();
                    if (descriptor == null)
                    {
                        descriptor = new GroupDescriptor() { Property = column.GetGroupProperty(), Title = column.Title, SortOrder = column.GetSortOrder() ?? SortOrder.Ascending  };
                        Groups.Add(descriptor);
                        _groupedPagedView = null;

                        await Group.InvokeAsync(new DataGridColumnGroupEventArgs<TItem>() { Column = column, GroupDescriptor = descriptor });

                        if (IsVirtualizationAllowed())
                        {
                            await Reload();
                        }
                    }
                }

                indexOfColumnToReoder = null;
            }  
        }

        /// <summary>
        /// Orders the DataGrid by property name.
        /// </summary>
        /// <param name="property">The property name.</param>
        public void OrderBy(string property)
        {
            var p = IsOData() ? property.Replace('.', '/') : PropertyAccess.GetProperty(property);

            var column = allColumns.ToList().Where(c => c.GetSortProperty() == property).FirstOrDefault();

            if (column != null)
            {
                SetColumnSortOrder(column);
                Sort.InvokeAsync(new DataGridColumnSortEventArgs<TItem>() { Column = column, SortOrder = column.GetSortOrder() });
                SaveSettings();
            }

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
            }

            if (IsVirtualizationAllowed() && LoadData.HasDelegate)
            {
                Debounce(() => InvokeAsync(Reload), 500);
            }
            else
            {
                InvokeAsync(Reload);
            }
        }

        /// <summary>
        /// Orders descending the DataGrid by property name.
        /// </summary>
        /// <param name="property">The property name.</param>
        public void OrderByDescending(string property)
        {
            var p = IsOData() ? property.Replace('.', '/') : PropertyAccess.GetProperty(property);

            var column = allColumns.ToList().Where(c => c.GetSortProperty() == p).FirstOrDefault();

            if (column != null)
            {
                column.SetSortOrderInternal(SortOrder.Ascending);
                SetColumnSortOrder(column);

                Sort.InvokeAsync(new DataGridColumnSortEventArgs<TItem>() { Column = column, SortOrder = column.GetSortOrder() });
                SaveSettings();
            }

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
            }

            InvokeAsync(Reload);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var additionalClasses = new List<string>();

            if (CurrentStyle.ContainsKey("height"))
            {
                additionalClasses.Add("rz-has-height");
            }

            if (RowSelect.HasDelegate || ValueChanged.HasDelegate || SelectionMode == DataGridSelectionMode.Multiple)
            {
                additionalClasses.Add("rz-selectable");
            }

            if (Responsive)
            {
                additionalClasses.Add("rz-datatable-reflow");
            }

            if (Density == Density.Compact)
            {
                additionalClasses.Add("rz-density-compact");
            }

            return $"rz-has-paginator rz-datatable  rz-datatable-scrollable {String.Join(" ", additionalClasses)}";
        }

        internal string getHeaderStyle()
        {
            var additionalStyle = Style != null && Style.IndexOf("height:") != -1 ? "padding-right: 17px;" : "";
            return $"margin-left:0px;{additionalStyle}";
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>The query.</value>
        public Query Query { get; private set; } = new Query();

        internal string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (groups != null)
            {
                groups.CollectionChanged -= GroupsCollectionChanged;
            }

            if (IsJSRuntimeAvailable)
            {
                foreach (var column in allColumns.ToList().Where(c => c.GetVisible()))
                {
                    JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", $"{PopupID}{column.GetFilterProperty()}");
                }
            }
        }

        internal int deepestChildColumnLevel;

        /// <inheritdoc />
        protected override async Task OnPageSizeChanged(int value)
        {
            pageSize = value;

            SaveSettings();

            await PageSizeChanged.InvokeAsync(value);

            await base.OnPageSizeChanged(value);
        }

        /// <summary>
        /// Gets or sets the page size changed callback.
        /// </summary>
        /// <value>The page size changed callback.</value>
        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        /// <summary>
        /// Gets DataGrid settings as JSON string.
        /// </summary>
        internal void SaveSettings()
        {
            if (SettingsChanged.HasDelegate && canSaveSettings)
            {
                settings = new DataGridSettings()
                {
                    Columns = ColumnsCollection.ToList().Select(c => new DataGridColumnSettings()
                    {
                        Property = c.Property,
                        Width = c.GetWidth(),
                        Visible = c.GetVisible(),
                        OrderIndex = c.GetOrderIndex(),
                        SortOrder = c.GetSortOrder(),
                        FilterValue = c.GetFilterValue(),
                        FilterOperator = c.GetFilterOperator(),
                        SecondFilterValue = c.GetSecondFilterValue(),
                        SecondFilterOperator = c.GetSecondFilterOperator(),
                        LogicalFilterOperator = c.GetLogicalFilterOperator()
                    }).ToList(),
                    CurrentPage = CurrentPage,
                    PageSize = PageSize,
                    Groups = Groups
                };

                SettingsChanged.InvokeAsync(settings);
            }
        }

        /// <summary>
        /// Load DataGrid settings saved from GetSettings() method.
        /// </summary>
        internal async Task LoadSettings(DataGridSettings settings)
        {
            if (SettingsChanged.HasDelegate)
            {
                var shouldUpdateState = false;
                var hasFilter = settings.Columns != null && settings.Columns.Any(c => 
                    c.FilterValue != null || c.SecondFilterValue != null || 
                    c.FilterOperator == FilterOperator.IsNull || c.FilterOperator == FilterOperator.IsNotNull ||
                    c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty ||
                    c.SecondFilterOperator == FilterOperator.IsNull || c.SecondFilterOperator == FilterOperator.IsNotNull ||
                    c.SecondFilterOperator == FilterOperator.IsEmpty || c.SecondFilterOperator == FilterOperator.IsNotEmpty);

                if (settings.Columns != null)
                {
                    foreach (var column in settings.Columns)
                    {
                        var gridColumn = ColumnsCollection.Where(c => c.Property == column.Property).FirstOrDefault();
                        if (gridColumn != null)
                        {
                            // Visibility
                            if (gridColumn.GetVisible() != column.Visible)
                            {
                                gridColumn.SetVisible(column.Visible);
                                shouldUpdateState = true;
                            }

                            // Width
                            if (gridColumn.GetWidth() != column.Width)
                            {
                                gridColumn.SetWidth(column.Width);
                                shouldUpdateState = true;
                            }

                            // OrderIndex
                            if (gridColumn.GetOrderIndex() != column.OrderIndex)
                            {
                                gridColumn.SetOrderIndex(column.OrderIndex);
                                shouldUpdateState = true;
                            }

                            // Sorting
                            if (gridColumn.GetSortOrder() != column.SortOrder)
                            {
                                gridColumn.SetSortOrder(column.SortOrder);
                                shouldUpdateState = true;
                            }

                            // Filtering
                            if (!object.Equals(gridColumn.GetFilterValue(), GetFilterValue(column.FilterValue, gridColumn.FilterPropertyType)))
                            {
                                gridColumn.SetFilterValue(GetFilterValue(column.FilterValue, gridColumn.FilterPropertyType));
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetFilterOperator() != column.FilterOperator)
                            {
                                gridColumn.SetFilterOperator(column.FilterOperator);
                                shouldUpdateState = true;
                            }

                            if (!object.Equals(gridColumn.GetSecondFilterValue(), GetFilterValue(column.SecondFilterValue, gridColumn.FilterPropertyType)))
                            {
                                gridColumn.SetFilterValue(GetFilterValue(column.SecondFilterValue, gridColumn.FilterPropertyType), false);
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetSecondFilterOperator() != column.SecondFilterOperator)
                            {
                                gridColumn.SetSecondFilterOperator(column.SecondFilterOperator);
                                shouldUpdateState = true;
                            }

                            if (gridColumn.GetLogicalFilterOperator() != column.LogicalFilterOperator)
                            {
                                gridColumn.SetLogicalFilterOperator(column.LogicalFilterOperator);
                                shouldUpdateState = true;
                            }
                        }
                    }
                }

                if (settings.Groups != null && !settings.Groups.SequenceEqual(Groups))
                {
                    groups.CollectionChanged -= GroupsCollectionChanged;
                    Groups.Clear();
                    settings.Groups.ToList().ForEach(Groups.Add);
                    shouldUpdateState = true;
                    groups.CollectionChanged += GroupsCollectionChanged;
                }

                if (settings.CurrentPage != null && settings.CurrentPage != CurrentPage)
                {
                    CurrentPage = settings.CurrentPage.Value;
                    shouldUpdateState = true;
                }

                if (settings.PageSize != null && settings.PageSize != PageSize)
                {
                    PageSize = settings.PageSize.Value;
                    shouldUpdateState = true;
                }

                if (shouldUpdateState)
                {
                    skip = CurrentPage * PageSize;

                    if (hasFilter ? skip < View.Count() : true)
                    {
                        CalculatePager();
                        UpdateColumnsOrder();
                        await Reload();
                    }
                }
            }
        }

        object GetFilterValue(object value, Type type)
        {
            if (value != null && value is JsonElement)
            {
                var element = (JsonElement)value;
                if (type == typeof(Int16) || type == typeof(Int16?))
                {
                    return element.GetInt16();
                }
                else if (type == typeof(Int32) || type == typeof(Int32?))
                {
                    return element.GetInt32();
                }
                else if (type == typeof(Int64) || type == typeof(Int64?))
                {
                    return element.GetInt64();
                }
                else if (type == typeof(double) || type == typeof(double?))
                {
                    return element.GetDouble();
                }
                else if (type == typeof(bool) || type == typeof(bool?))
                {
                    return element.GetBoolean();
                }
                else if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    return element.GetDateTime();
                }
                else if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    return element.GetDateTime();
                }
                else if (type.IsEnum)
                {
                    return element.GetInt32();
                }
                else
                {
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        return element.GetDouble();
                    }
                    else if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    {
                        return element.GetBoolean();
                    }
                    else
                    {
                        return element.GetRawText().Replace("\"", "");
                    }
                }
            }
            else
            {
                return value;
            }
        }

        bool canSaveSettings = true;

        DataGridSettings settings;

        /// <summary>
        /// Gets or sets DataGrid settings.
        /// </summary>
        [Parameter]
        public DataGridSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                if (settings != value)
                {
                    settings = value;

                    if (settings == null)
                    {
                        canSaveSettings = false;

                        Groups.Clear();
                        CurrentPage = 0;
                        skip = 0;
                        Reset(true);
                        allColumns.ToList().ForEach(c =>
                        {
                            c.SetVisible(true);
                        });
                        InvokeAsync(Reload);

                        canSaveSettings = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the settings changed callback.
        /// </summary>
        /// <value>The settings callback.</value>
        [Parameter]
        public EventCallback<DataGridSettings> SettingsChanged { get; set; }

        async Task ChangePage(PagerEventArgs args)
        {
            CurrentPage = args.PageIndex;
            SaveSettings();

            await OnPageChanged(args);
        }
    }
}
