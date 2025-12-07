using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Subjects;
using System.Threading;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.TreeDataGridTests.Models
{
    public class TextCellTests
    {
        [AvaloniaFact]
        public void Value_Is_Initially_Read_From_String()
        {
            var binding = new BehaviorSubject<BindingValue<string>>("initial");
            var isReadOnly = new BehaviorSubject<BindingValue<bool>>(true);
            var target = new TextCell<string>(binding, binding, isReadOnly);

            Assert.Equal("initial", target.Text);
            Assert.Equal("initial", target.Value);
        }

        [AvaloniaFact]
        public void Modified_Value_Is_Written_To_Binding()
        {
            var binding = new BehaviorSubject<BindingValue<string>>("initial");
            var isReadOnly = new BehaviorSubject<BindingValue<bool>>(false);
            var target = new TextCell<string>(binding, binding, isReadOnly);
            var result = new List<string>();

            binding.Subscribe(x => result.Add(x.Value));
            target.Value = "new";

            Assert.Equal(new[] { "initial", "new" }, result);
        }

        [AvaloniaFact]
        public void Modified_Text_Is_Written_To_Binding()
        {
            var binding = new BehaviorSubject<BindingValue<string>>("initial");
            var isReadOnly = new BehaviorSubject<BindingValue<bool>>(false);
            var target = new TextCell<string>(binding, binding, isReadOnly);
            var result = new List<string>();

            binding.Subscribe(x => result.Add(x.Value));
            target.Text = "new";

            Assert.Equal(new[] { "initial", "new" }, result);
        }

        [AvaloniaFact]
        public void Modified_Value_Is_Written_To_Binding_On_EndEdit()
        {
            var binding = new BehaviorSubject<BindingValue<string>>("initial");
            var isReadOnly = new BehaviorSubject<BindingValue<bool>>(false);
            var target = new TextCell<string>(binding, binding, isReadOnly);
            var result = new List<string>();

            binding.Subscribe(x => result.Add(x.Value));

            target.BeginEdit();
            target.Text = "new";

            Assert.Equal("new", target.Text);
            Assert.Equal("initial", target.Value);
            Assert.Equal(new[] { "initial"}, result);

            target.EndEdit();

            Assert.Equal("new", target.Text);
            Assert.Equal("new", target.Value);
            Assert.Equal(new[] { "initial", "new" }, result);
        }

        [AvaloniaFact]
        public void Modified_Value_Is_Not_Written_To_Binding_On_CancelEdit()
        {
            var binding = new BehaviorSubject<BindingValue<string>>("initial");
            var isReadOnly = new BehaviorSubject<BindingValue<bool>>(false);
            var target = new TextCell<string>(binding, binding, isReadOnly);
            var result = new List<string>();

            binding.Subscribe(x => result.Add(x.Value));

            target.BeginEdit();
            target.Text = "new";

            Assert.Equal("new", target.Text);
            Assert.Equal("initial", target.Value);
            Assert.Equal(new[] { "initial" }, result);

            target.CancelEdit();

            Assert.Equal("initial", target.Text);
            Assert.Equal("initial", target.Value);
            Assert.Equal(new[] { "initial" }, result);
        }

        [AvaloniaFact]
        public void Unchanged_Value_Clears_On_CommitEdit_Without_Changes()
        {
            var binding = new BehaviorSubject<BindingValue<string>>("initial");
            var isReadOnly = new BehaviorSubject<BindingValue<bool>>(false);
            var target = new TextCell<string>(binding, binding, isReadOnly);

            target.BeginEdit();
            target.EndEdit();

            Assert.Equal("initial", target.Text);
            Assert.Equal("initial", target.Value);
        }

        public class StringFormat
        {
            public StringFormat()
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            }

            [AvaloniaFact]
            public void Initial_Int_Value_Is_Formatted()
            {
                var binding = new BehaviorSubject<BindingValue<int>>(42);
                var isReadOnly = new BehaviorSubject<BindingValue<bool>>(true);
                var target = new TextCell<int>(binding, binding, isReadOnly, GetOptions());

                Assert.Equal("42.00", target.Text);
                Assert.Equal(42, target.Value);
            }

            [AvaloniaFact]
            public void Int_Value_Is_Formatted_After_Editing()
            {
                var binding = new BehaviorSubject<BindingValue<int>>(42);
                var isReadOnly = new BehaviorSubject<BindingValue<bool>>(false);
                var target = new TextCell<int>(binding, binding, isReadOnly, GetOptions());
                var result = new List<int>();

                binding.Subscribe(x => result.Add(x.Value));

                target.BeginEdit();
                target.Text = "43";

                Assert.Equal("43", target.Text);
                Assert.Equal(42, target.Value);
                Assert.Equal(new[] { 42 }, result);

                target.EndEdit();

                Assert.Equal("43.00", target.Text);
                Assert.Equal(43, target.Value);
                Assert.Equal(new[] { 42, 43 }, result);
            }

            private ITextCellOptions? GetOptions(string format = "{0:n2}")
            {
                return new TextColumnOptions<int> { StringFormat = format };
            }
        }
    }
}
