﻿//using LinqKit;
using Microsoft.AspNetCore.Components;
using Tabler.Components.Tables.Components;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tabler.Components.Tables;
using LinqKit;

namespace Tabler.Components
{
    public class ColumnBase<Item> : ComponentBase, IColumn<Item> //SbtComponent, IColumn<Item>
    {
        [Inject] protected TableFilterService FilterService { get; set; }

        private string _title;

        [Parameter]
        public string Title
        {
            get { return _title ?? Property.GetPropertyMemberInfo()?.Name; }
            set { _title = value; }
        }

        [CascadingParameter(Name = "Table")] public ITable<Item> Table { get; set; }
        [Parameter] public string Width { get; set; }
        [Parameter] public bool Sortable { get; set; }
        [Parameter] public bool Searchable { get; set; }
        [Parameter] public bool Groupable { get; set; }
        [Parameter] public string CssClass { get; set; }
        [Parameter] public bool Visible { get; set; } = true;
        [Parameter] public RenderFragment<Item> Template { get; set; }
        [Parameter] public RenderFragment<Item> EditorTemplate { get; set; }
        [Parameter] public RenderFragment<TableResult<object, Item>> GroupingTemplate { get; set; }
        [Parameter] public Expression<Func<Item, object>> Property { get; set; }
        [Parameter] public Expression<Func<Item, string, bool>> SearchExpression { get; set; }
        [Parameter] public bool SortColumn { get; set; }
        [Parameter] public bool Group { get; set; }
        public bool SortDescending { get; set; }
        public Type Type { get; private set; }
        public bool GroupBy { get; set; }
     

        public void Dispose()
        {
            Table.RemoveColumn(this);
        }

        protected override void OnInitialized()
        {
            GroupBy = Group;
            Table.AddColumn(this);
        }

        protected override void OnParametersSet()
        {
            if ((Sortable && Property == null) || (Searchable && Property == null))
            {
                throw new InvalidOperationException($"Column {Title} Property parameter is null");
            }

            if (Title == null && Property == null)
            {
                throw new InvalidOperationException("A Column has both Title and Property parameters null");
            }

            Type = Property?.GetPropertyMemberInfo().GetMemberUnderlyingType();
        }

        public Expression<Func<Item, bool>> GetFilter(ITableState state)
        {
            if ((Searchable || SearchExpression != null) && Property != null && !string.IsNullOrEmpty(state.SearchText))
            {
                var filter = FilterService.GetFilter(this, state.SearchText);
                if (filter == null)
                {
                    return null;
                }



                return PredicateBuilder
                    .New<Item>()
                    .And(NotNull())
                    .And(filter);
            }

            return null;
        }

        private Expression<Func<Item, bool>> NotNull()
        {
            return Expression.Lambda<Func<Item, bool>>(
                Expression.NotEqual(Property.Body, Expression.Constant(null)),
                Property.Parameters.ToArray()
            );
        }

        public async Task GroupByMeAsync()
        {
            if (Groupable)
            {
                if (GroupBy)
                {
                    GroupBy = false;
                    Visible = true;
                }
                else
                {
                    foreach (var column in Table.Columns.Where(e => e.GroupBy))
                    {
                        column.GroupBy = false;
                        column.Visible = true;
                    }
                    GroupBy = true;
                    Visible = false;
                }

                await Table.Update();
            }
        }

        public async Task SortByAsync()
        {
            if (Sortable)
            {
                if (SortColumn)
                {
                    SortDescending = !SortDescending;
                }

                Table.Columns.ForEach(x => x.SortColumn = false);
                SortColumn = true;
                await Table.Update();
            }
        }

        public object GetValue(Item item)
        {
            try
            {
                return Property.Compile().Invoke(item);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }
}