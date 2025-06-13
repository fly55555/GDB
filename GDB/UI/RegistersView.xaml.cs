using GDB.Core.Register;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GDB.UI
{
    /// <summary>
    /// RegistersView.xaml 的交互逻辑
    /// </summary>
    public partial class RegistersView : UserControl
    {
        public RegistersView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 更新寄存器视图
        /// </summary>
        /// <param name="context">寄存器上下文</param>
        public void UpdateRegisters(CommonRegister_x64 context)
        {
            if (context == null) return;

            // 获取当前寄存器列表
            var currentRegisters = RegistersListView.ItemsSource as List<RegisterViewModel> ?? new List<RegisterViewModel>();
            
            // 创建新的寄存器列表
            var newRegisters = new List<RegisterViewModel>();
            foreach (PropertyInfo prop in typeof(CommonRegister_x64).GetProperties())
            {
                // 支持所有类型的寄存器，不仅仅是ulong类型
                var value = prop.GetValue(context);
                if (value != null)
                {
                    // 尝试查找现有的寄存器对象
                    var existingReg = currentRegisters.FirstOrDefault(r => r.Name == prop.Name);
                    
                    if (existingReg != null)
                    {
                        // 更新现有对象，这会触发HasChanged属性的计算
                        existingReg.Value = Convert.ToUInt64(value);
                        newRegisters.Add(existingReg);
                    }
                    else
                    {
                        // 创建新对象
                        newRegisters.Add(new RegisterViewModel { Name = prop.Name, Value = Convert.ToUInt64(value) });
                    }
                }
            }
            
            RegistersListView.ItemsSource = newRegisters;
        }
    }

    /// <summary>
    /// 寄存器视图模型
    /// </summary>
    public class RegisterViewModel
    {
        private bool _isFirstSet = true;
        
        public string Name { get; set; }
        private ulong _value;
        public ulong Value 
        { 
            get { return _value; }
            set 
            { 
                PreviousValue = _value;
                _value = value;
                
                // 首次设置值时，不标记为变化
                if (_isFirstSet)
                {
                    _isFirstSet = false;
                    HasChanged = false;
                }
                else
                {
                    HasChanged = PreviousValue != _value;
                }
            }
        }
        public ulong PreviousValue { get; private set; }
        public bool HasChanged { get; private set; }
    }

    /// <summary>
    /// 布尔值到颜色的转换器
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasChanged && hasChanged)
            {
                return Colors.Red;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 