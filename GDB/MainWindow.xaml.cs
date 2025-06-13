using GDB.Core;
using GDB.Core.Disassembly;
using GDB.Core.Register;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace GDB
{
    public partial class MainWindow : Window
    {
        private readonly ControlCenter _controlCenter;
        private bool _isProcessingKeyDown = false;
        private DateTime _lastKeyDownTime = DateTime.MinValue;
        private const int KEY_DEBOUNCE_MS = 50; // 按键防抖动间隔（毫秒）

        public MainWindow()
        {
            InitializeComponent();
            _controlCenter = new ControlCenter();
            _controlCenter.OnStateChanged += ControlCenter_OnStateChanged;
            
            // 注册内存读取和反汇编处理器
            DisassemblyView.SetMemoryReadHandler(async (address, size) => 
            {
                if (_controlCenter.State != DebuggerState.Halted) return null;
                return await _controlCenter.ReadVirtual((long)address, size);
            });
            
            DisassemblyView.SetDisassemblyHandler((bytes, startAddress) => 
            {
                if (_controlCenter.State != DebuggerState.Halted) return new List<CommonInstruction>();
                return _controlCenter.DisassemblyBytes(bytes, startAddress);
            });
            
            // 不再需要事件绑定，完全使用内部缓存机制
            // DisassemblyView.RequestData += DisassemblyView_RequestData;
            
            UpdateUiForState(DebuggerState.Disconnected);
            
            // 添加键盘事件处理
            this.KeyDown += MainWindow_KeyDown;
        }

        private void ControlCenter_OnStateChanged(DebuggerState newState)
        {
            // Ensure UI updates are on the UI thread
            Dispatcher.Invoke(() => UpdateUiForState(newState));
        }

        private void UpdateUiForState(DebuggerState newState)
        {
            StatusTextBlock.Text = newState.ToString();

            bool isHalted = newState == DebuggerState.Halted;
            bool isRunning = newState == DebuggerState.Running;
            bool isDisconnected = newState == DebuggerState.Disconnected;
            bool isBusy = newState == DebuggerState.Busy;

            ConnectButton.IsEnabled = isDisconnected;
            ConnectMenuItem.IsEnabled = isDisconnected;

            RunButton.IsEnabled = isHalted && !isBusy;
            RunMenuItem.IsEnabled = isHalted && !isBusy;

            StepButton.IsEnabled = isHalted && !isBusy;
            StepIntoMenuItem.IsEnabled = isHalted && !isBusy;
            
            BreakButton.IsEnabled = isRunning && !isBusy;
            BreakMenuItem.IsEnabled = isRunning && !isBusy;

            if (isHalted)
            {
                UpdateFullContext();
            }
        }

        private async void UpdateFullContext()
        {
            var context = await _controlCenter.GetContext();
            if (context == null) return;

            // 更新寄存器视图
            RegistersView.UpdateRegisters(context);

            // 触发反汇编视图更新
            DisassemblyView.GoToAddress(context.RIP);
        }

        private async void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // In a real app, this would come from a dialog
            var host = "127.0.0.1";
            var port = 8864;
            _controlCenter.SetActiveDebugger(DebugMachineType.Vmware);
            await _controlCenter.ConnectAsync(host, port);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void RunMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await _controlCenter.Continue();
        }

        private async void BreakMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await _controlCenter.Break();
        }

        private async void StepIntoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await _controlCenter.Step();
        }

        private async void CommandInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var command = CommandInputTextBox.Text;
                CommandInputTextBox.Clear();
                CommandOutputTextBox.AppendText($"> {command}\n");
                var result = await _controlCenter.Execute(command);
                CommandOutputTextBox.AppendText($"{result}\n");
                CommandOutputTextBox.ScrollToEnd();
            }
        }

        private void GoToAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if (ulong.TryParse(AddressTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out ulong address))
            {
                DisassemblyView.GoToAddress(address);
            }
            else
            {
                MessageBox.Show("Invalid address format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // 如果正在处理按键事件或者距离上次按键时间太短，则忽略此次按键
            if (_isProcessingKeyDown || (DateTime.Now - _lastKeyDownTime).TotalMilliseconds < KEY_DEBOUNCE_MS)
            {
                e.Handled = true;
                return;
            }

            try
            {
                // F7键触发Step
                if (e.Key == Key.F7 && StepButton.IsEnabled)
                {
                    _isProcessingKeyDown = true;
                    _lastKeyDownTime = DateTime.Now;
                    e.Handled = true;
                    await _controlCenter.Step();
                }
                // F9键触发Run
                else if (e.Key == Key.F9 && RunButton.IsEnabled)
                {
                    _isProcessingKeyDown = true;
                    _lastKeyDownTime = DateTime.Now;
                    e.Handled = true;
                    await _controlCenter.Continue();
                }
            }
            finally
            {
                _isProcessingKeyDown = false;
            }
        }
    }
} 