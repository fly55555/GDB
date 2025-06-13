using GDB.Core;
using GDB.Core.Disassembly;
using GDB.Core.Register;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

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
            
            LoadSymbolsButton.IsEnabled = isHalted && !isBusy;
            LoadSymbolsMenuItem.IsEnabled = isHalted && !isBusy;

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

        private async void LoadSymbolsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_controlCenter.State != DebuggerState.Halted)
            {
                MessageBox.Show("调试器必须处于暂停状态才能加载符号。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // 禁用按钮，防止重复点击
            LoadSymbolsButton.IsEnabled = false;
            LoadSymbolsMenuItem.IsEnabled = false;
            
            try
            {
                // 显示加载进度
                StatusTextBlock.Text = "正在准备加载符号...";
                
                // 定义符号加载状态处理程序
                Action<string> symbolStatusHandler = status => 
                {
                    // 确保在UI线程上更新状态
                    Dispatcher.Invoke(() => 
                    {
                        StatusTextBlock.Text = status;
                    });
                };
                
                // 订阅符号加载状态事件
                _controlCenter.OnSymbolLoadStatusChanged += symbolStatusHandler;
                
                try
                {
                    // 关键修改：将整个符号加载操作包裹在Task.Run中，在真正的后台线程执行
                    // 这样可以避免UI线程被阻塞，同时避免异步方法被错误地同步等待
                    var loadTask = Task.Run(async () => 
                    {
                        // 在后台线程中执行符号加载
                        return await _controlCenter.LoadSymbols();
                    });
                    
                    // 使用ContinueWith在操作完成后更新UI，而不是同步等待
                    await loadTask.ContinueWith(t => 
                    {
                        // 这部分代码将在UI线程中执行
                        Dispatcher.Invoke(() =>
                        {
                            var result = t.Result;
                            if (result)
                            {
                                // 刷新反汇编视图以显示新的符号信息
                                DisassemblyView.RefreshView();
                                StatusTextBlock.Text = "符号加载完成";
                            }
                            else
                            {
                                StatusTextBlock.Text = "符号加载失败";
                            }
                            
                            // 延迟恢复状态显示
                            Task.Delay(1000).ContinueWith(_ => 
                            {
                                Dispatcher.Invoke(() => UpdateUiForState(_controlCenter.State));
                            });
                        });
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                finally
                {
                    // 取消订阅事件
                    _controlCenter.OnSymbolLoadStatusChanged -= symbolStatusHandler;
                }
            }
            finally
            {
                // 重新启用按钮
                LoadSymbolsButton.IsEnabled = true;
                LoadSymbolsMenuItem.IsEnabled = true;
            }
        }
    }
} 