using GDB.Core.Disassembly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Media;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GDB.UI
{
    /// <summary>
    /// InfiniteListView 控件详细说明
    /// 
    /// 功能概述：
    /// InfiniteListView 是一个专为显示反汇编代码设计的自定义控件，支持无限滚动和动态加载数据。
    /// 该控件可以根据需要动态请求并加载内存数据，将其反汇编为指令，并在用户滚动到列表边缘时自动加载更多数据。
    /// 
    /// 核心特性：
    /// 1. 无限滚动：用户可以无限向上或向下滚动，控件会根据需要动态加载更多数据
    /// 2. 动态数据加载：通过委托机制从外部获取内存数据和反汇编结果
    /// 3. 自定义滚动条：实现了带有中心回弹功能的滚动条，可以通过拖动滑块控制滚动速度和方向
    /// 4. 滚动按钮：滚动条上的按钮被重新实现为向上/向下滚动一行的功能，不改变当前选中项
    /// 5. 加载状态指示：在加载数据时显示动画指示器
    /// 
    /// 主要组件：
    /// 1. 数据处理委托：
    ///    - MemoryReadHandler：从指定地址读取内存数据
    ///    - DisassemblyHandler：将内存数据反汇编为指令列表
    ///    - DataRequestEventHandler：当需要加载数据时触发的事件
    /// 
    /// 2. 数据管理：
    ///    - _internalSource：内部数据源，存储当前显示的指令列表
    ///    - ItemsSource：公开的数据源属性，用于绑定和更新UI
    /// 
    /// 3. 滚动控制：
    ///    - ScrollBar：自定义滚动条，用于控制滚动方向和速度
    ///    - _scrollViewer：内部ListView的ScrollViewer，用于控制实际滚动位置
    ///    - _scrollRepeatTimer 和 _autoScrollTimer：用于控制连续滚动和自动滚动的定时器
    /// 
    /// 4. 滚动按钮功能：
    ///    - MoveSelectionUp/Down：向上/向下滚动一行，不改变当前选中项
    ///    - _isProgrammaticScrolling：标记是程序触发的滚动，防止滑块位置变化
    /// 
    /// 工作流程：
    /// 1. 初始化：设置内存读取和反汇编处理器，初始化UI和事件处理
    /// 2. 数据加载：
    ///    - 通过GoToAddress方法跳转到指定地址
    ///    - 或通过滚动触发RequestMoreData方法加载更多数据
    /// 3. 数据处理：
    ///    - LoadInstructionsAsync方法异步读取内存数据并反汇编
    ///    - 根据请求类型(重新加载/请求历史/请求未来)处理结果
    ///    - 更新ItemsSource属性，触发UI更新
    /// 4. 滚动处理：
    ///    - 用户可通过鼠标滚轮、拖动滑块或点击滚动按钮控制滚动
    ///    - 滚动到边缘时自动加载更多数据
    ///    - 滚动按钮被重新实现为向上/向下滚动一行的功能
    /// 
    /// 优化措施：
    /// 1. 滚动节流：使用定时器控制数据加载频率，避免频繁请求
    /// 2. 动态加载：只在需要时加载数据，减少内存占用
    /// 3. 滑块回弹：拖动结束后滑块自动回到中心位置
    /// 4. 程序滚动标记：使用_isProgrammaticScrolling标记区分用户滚动和程序滚动，确保滑块位置不变
    /// </summary>

    /// <summary>
    /// 内存读取委托，用于从指定地址读取指定大小的内存数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="size">要读取的字节数</param>
    /// <returns>读取到的字节数组</returns>
    public delegate Task<byte[]> MemoryReadHandler(ulong address, int size);

    /// <summary>
    /// 反汇编委托，用于将字节数据反汇编为指令列表
    /// </summary>
    /// <param name="bytes">要反汇编的字节数据</param>
    /// <param name="startAddress">字节数据对应的起始地址</param>
    /// <returns>反汇编后的指令列表</returns>
    public delegate List<CommonInstruction> DisassemblyHandler(byte[] bytes, ulong startAddress);

    public partial class InfiniteListView : UserControl
    {
        public delegate void DataRequestEventHandler(object sender, DataRequestEventArgs e);
        public event DataRequestEventHandler RequestData;

        // 内存读取和反汇编处理器
        private MemoryReadHandler _memoryReadHandler;
        private DisassemblyHandler _disassemblyHandler;
        
        // 默认读取字节数
        private const int DEFAULT_BYTES_TO_READ = 512;

        private readonly ObservableCollection<CommonInstruction> _internalSource = new ObservableCollection<CommonInstruction>();
        private ulong _lastSelectedAddress;
        private readonly DispatcherTimer _autoScrollTimer;
        private ScrollViewer _scrollViewer;
        private bool _isPrefetching = false;
        private bool _isScrollingUp = false;
        private bool _isScrollingDown = false;
        private double _currentScrollSpeed = 0; // 当前滚动速度
        private const double MAX_SCROLL_SPEED = 20.0; // 最大滚动速度
        private DataRequestEventArgs.RequestType _lastRequestType = DataRequestEventArgs.RequestType.Reload;
        private bool _isLoading = false;
        private DateTime _lastLoadTime = DateTime.MinValue;
        private bool _isProgrammaticScrolling = false; // 标记是否是程序触发的滚动，而非用户操作

        /// <summary>
        /// 设置内存读取处理器
        /// </summary>
        /// <param name="handler">内存读取回调函数</param>
        public void SetMemoryReadHandler(MemoryReadHandler handler)
        {
            _memoryReadHandler = handler;
        }

        /// <summary>
        /// 设置反汇编处理器
        /// </summary>
        /// <param name="handler">反汇编回调函数</param>
        public void SetDisassemblyHandler(DisassemblyHandler handler)
        {
            _disassemblyHandler = handler;
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<CommonInstruction>), typeof(InfiniteListView), new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable<CommonInstruction> ItemsSource
        {
            get { return (IEnumerable<CommonInstruction>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public InfiniteListView()
        {
            InitializeComponent();
            InternalListView.ItemsSource = _internalSource;

            _autoScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // 约60fps的刷新率
            };
            _autoScrollTimer.Tick += AutoScrollTimer_Tick;

            Loaded += InfiniteListView_Loaded;
        }

        private void InfiniteListView_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindVisualChild<ScrollViewer>(InternalListView);
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0 || _isPrefetching || _isLoading)
            {
                return;
            }
            
            // 如果是程序触发的滚动，我们仍然希望在接近边缘时加载数据，所以注释掉这个判断
            // if (_isProgrammaticScrolling)
            // {
            //     return;
            // }

            // 检查是否有足够的数据
            if (!_internalSource.Any()) return;

            // 计算当前滚动位置相对于总高度的比例
            double scrollPosition = 0;
            if (e.ExtentHeight > e.ViewportHeight) // 防止除以零
            {
                scrollPosition = e.VerticalOffset / (e.ExtentHeight - e.ViewportHeight);
            }
            
            // 定义更合理的阈值，避免频繁触发
            const double UPPER_THRESHOLD = 0.9;  // 接近底部
            const double LOWER_THRESHOLD = 0.1;  // 接近顶部
            
            DataRequestEventArgs.RequestType requestType = DataRequestEventArgs.RequestType.Reload;
            bool needsMoreData = false;
            
            // 滚动到底部区域，检查是否需要加载更多未来数据
            if (scrollPosition >= UPPER_THRESHOLD)
            {
                requestType = DataRequestEventArgs.RequestType.RequestFuture;
                needsMoreData = true;
            }
            // 滚动到顶部区域，检查是否需要加载更多历史数据
            else if (scrollPosition <= LOWER_THRESHOLD)
            {
                requestType = DataRequestEventArgs.RequestType.RequestHistory;
                needsMoreData = true;
            }
            
            // 只有在需要更多数据时才触发加载
            if (needsMoreData)
            {
                RequestMoreData(requestType);
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (InfiniteListView)d;
            control._isPrefetching = false; // Reset prefetching flag when new data arrives
            control._isLoading = false;
            control.LoadingIndicator.Visibility = Visibility.Collapsed;
            
            // 根据上次请求类型决定是否需要恢复选择位置
            bool shouldRestoreSelection = (control._lastRequestType == DataRequestEventArgs.RequestType.Reload);
            control.UpdateInternalSource(e.NewValue as IEnumerable<CommonInstruction>, shouldRestoreSelection);
        }

        private void UpdateInternalSource(IEnumerable<CommonInstruction> newItems, bool restoreSelection = true)
        {
            if (newItems == null)
                return;

            // 获取当前滚动位置
            double scrollPosition = 0;
            if (_scrollViewer != null)
                scrollPosition = _scrollViewer.VerticalOffset;

            _internalSource.Clear();
            foreach (var item in newItems)
            {
                _internalSource.Add(item);
            }

            // 只在需要时恢复选择位置
            if (restoreSelection)
            {
                RestoreSelection(newItems.FirstOrDefault(x => x.IP == _lastSelectedAddress));
            }
            else if (_scrollViewer != null)
            {
                // 否则尝试保持滚动位置
                _scrollViewer.ScrollToVerticalOffset(scrollPosition);
            }
        }

        private void RestoreSelection(CommonInstruction newSelection)
        {
            if (newSelection != null)
            {
                InternalListView.SelectedItem = newSelection;
                InternalListView.ScrollIntoView(newSelection);
            }
        }

        public void GoToAddress(ulong address)
        {
            _lastSelectedAddress = address;
            
            // 检查目标地址是否已经在已加载的数据中
            var targetInstruction = _internalSource.FirstOrDefault(instr => instr.IP == address);
            
            if (targetInstruction != null)
            {
                // 地址已在已加载的数据中，只需滚动到该位置并选中
                InternalListView.SelectedItem = targetInstruction;
                InternalListView.ScrollIntoView(targetInstruction);
                return;
            }
            
            // 如果地址不在已加载的数据中，则需要重新加载
            _lastRequestType = DataRequestEventArgs.RequestType.Reload;
            
            ShowLoading(true);
            
            if (_memoryReadHandler != null && _disassemblyHandler != null)
            {
                // 使用内部处理器加载数据
                _ = LoadInstructionsAsync(address, DataRequestEventArgs.RequestType.Reload);
            }
            else
            {
                // 回退到旧的事件模式
                RequestData?.Invoke(this, new DataRequestEventArgs(address, DataRequestEventArgs.RequestType.Reload));
            }
        }

        private void ListView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_internalSource.Any() || _isLoading) return;
            
            // 根据滚轮方向确定请求类型
            DataRequestEventArgs.RequestType requestType = e.Delta > 0 ? 
                DataRequestEventArgs.RequestType.RequestHistory : 
                DataRequestEventArgs.RequestType.RequestFuture;
            
            RequestMoreData(requestType);
        }

        private void ScrollUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectionUp();
        }

        private void ScrollDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectionDown();
        }

        private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            if (_isLoading) return;
            
            // 处理小增量和小减量事件（按钮点击）
            if (e.ScrollEventType == ScrollEventType.SmallDecrement)
            {
                e.Handled = true; // 阻止默认行为
                MoveSelectionUp();
                return;
            }
            else if (e.ScrollEventType == ScrollEventType.SmallIncrement)
            {
                e.Handled = true; // 阻止默认行为
                MoveSelectionDown();
                return;
            }
            // 处理大增量和大减量事件（点击滑轨空白处）
            else if (e.ScrollEventType == ScrollEventType.LargeDecrement)
            {
                e.Handled = true; // 阻止默认行为
                PageUp();
                return;
            }
            else if (e.ScrollEventType == ScrollEventType.LargeIncrement)
            {
                e.Handled = true; // 阻止默认行为
                PageDown();
                return;
            }
            
            if (e.ScrollEventType == ScrollEventType.ThumbTrack)
            {
                // 计算滑块离中心的距离，用于确定滚动方向和速度
                double centerValue = (ScrollBar.Maximum + ScrollBar.Minimum) / 2;
                double distanceFromCenter = e.NewValue - centerValue;
                double threshold = (ScrollBar.Maximum - ScrollBar.Minimum) * 0.05; // 5%的阈值
                
                // 如果在中心附近，不触发滚动
                if (Math.Abs(distanceFromCenter) <= threshold)
                {
                    _isScrollingUp = _isScrollingDown = false;
                    _autoScrollTimer.Stop();
                    return;
                }
                
                // 根据离中心的距离确定滚动方向和速度
                if (distanceFromCenter > 0) // 向下滚动
                {
                    _isScrollingDown = true;
                    _isScrollingUp = false;
                    // 计算速度：离中心越远，速度越快
                    _currentScrollSpeed = Math.Min(MAX_SCROLL_SPEED, Math.Abs(distanceFromCenter) / 5);
                    
                    if (!_autoScrollTimer.IsEnabled)
                        _autoScrollTimer.Start();
                }
                else // 向上滚动
                {
                    _isScrollingUp = true;
                    _isScrollingDown = false;
                    // 计算速度：离中心越远，速度越快
                    _currentScrollSpeed = Math.Min(MAX_SCROLL_SPEED, Math.Abs(distanceFromCenter) / 5);
                    
                    if (!_autoScrollTimer.IsEnabled)
                        _autoScrollTimer.Start();
                }
            }
            else if (e.ScrollEventType == ScrollEventType.EndScroll)
            {
                // 拖动结束时，停止所有自动操作并回弹滑块
                _isScrollingUp = _isScrollingDown = false;
                _autoScrollTimer.Stop();
                BeginSnapBackAnimation();
            }
        }
        
        private void AutoScrollTimer_Tick(object sender, EventArgs e)
        {
            if (_scrollViewer == null || _isLoading) return;

            // 根据滚动方向和速度移动视图
            if (_isScrollingUp)
            {
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - _currentScrollSpeed);
            }
            else if (_isScrollingDown)
            {
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + _currentScrollSpeed);
            }
        }
        
        private void BeginSnapBackAnimation()
        {
            var animation = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                // After animation completes, set the value and remove the animation
                // to allow user interaction again.
                ScrollBar.Value = 0;
                ScrollBar.BeginAnimation(RangeBase.ValueProperty, null);
            };

            ScrollBar.BeginAnimation(RangeBase.ValueProperty, animation);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is CommonInstruction selectedInstruction)
            {
                _lastSelectedAddress = selectedInstruction.IP;
            }
        }
        
        // Helper to find visual children
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// 显示或隐藏加载指示器
        /// </summary>
        private void ShowLoading(bool show)
        {
            _isLoading = show;
            
            if (show)
            {
                LoadingText.Text = $"加载中...";
                LoadingIndicator.Visibility = Visibility.Visible;
                
                // 启动加载动画
                var loadingAnimation = (Storyboard)FindResource("LoadingAnimation");
                loadingAnimation.Begin();
            }
            else
            {
                // 停止加载动画并隐藏指示器
                var loadingAnimation = (Storyboard)FindResource("LoadingAnimation");
                loadingAnimation.Stop();
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 请求更多数据
        /// </summary>
        private void RequestMoreData(DataRequestEventArgs.RequestType requestType)
        {
            if (_isLoading || _isPrefetching || !_internalSource.Any())
                return;
                
            _isPrefetching = true;
            _lastRequestType = requestType;
            
            ulong address = requestType == DataRequestEventArgs.RequestType.RequestFuture ? 
                _internalSource.Last().IP : _internalSource.First().IP;
            
            ShowLoading(true);
            
            if (_memoryReadHandler != null && _disassemblyHandler != null)
            {
                // 使用内部缓存和处理器加载数据
                _ = LoadInstructionsAsync(address, requestType);
            }
            else
            {
                // 回退到旧的事件模式
                RequestData?.Invoke(this, new DataRequestEventArgs(address, requestType));
            }
        }

        /// <summary>
        /// 异步加载指令数据
        /// </summary>
        private async Task LoadInstructionsAsync(ulong address, DataRequestEventArgs.RequestType requestType)
        {
            if (_memoryReadHandler == null || _disassemblyHandler == null)
            {
                ShowLoading(false);
                _isPrefetching = false;
                return;
            }

            try
            {
                int bytesToRead = DEFAULT_BYTES_TO_READ;
                ulong startAddress = address;
                
                switch (requestType)
                {
                    case DataRequestEventArgs.RequestType.Reload:
                        startAddress = (address > (ulong)(bytesToRead / 2)) ? address - (ulong)(bytesToRead / 2) : 0;
                        break;
                    case DataRequestEventArgs.RequestType.RequestHistory:
                        startAddress = (address > (ulong)bytesToRead) ? address - (ulong)bytesToRead : 0;
                        break;
                    case DataRequestEventArgs.RequestType.RequestFuture:
                        // 从当前地址开始向后读取
                        break;
                }
                
                byte[] memoryData = await _memoryReadHandler(startAddress, bytesToRead);
                
                if (memoryData == null || memoryData.Length == 0)
                {
                    return;
                }

                List<CommonInstruction> newInstructions = _disassemblyHandler(memoryData, startAddress);

                if (!newInstructions.Any())
                {
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    switch (requestType)
                    {
                        case DataRequestEventArgs.RequestType.Reload:
                            _internalSource.Clear();
                            foreach (var item in newInstructions.OrderBy(i => i.IP))
                            {
                                _internalSource.Add(item);
                            }
                            RestoreSelection(_internalSource.FirstOrDefault(x => x.IP == _lastSelectedAddress));
                            break;

                        case DataRequestEventArgs.RequestType.RequestFuture:
                            var existingFutureIPs = new HashSet<ulong>(_internalSource.Select(i => i.IP));
                            var futureItemsToAdd = newInstructions
                                .Where(i => !existingFutureIPs.Contains(i.IP))
                                .GroupBy(i => i.IP)
                                .Select(g => g.First())
                                .OrderBy(i => i.IP);

                            foreach (var item in futureItemsToAdd)
                            {
                                _internalSource.Add(item);
                            }
                            break;

                        case DataRequestEventArgs.RequestType.RequestHistory:
                            var existingHistoryIPs = new HashSet<ulong>(_internalSource.Select(i => i.IP));
                            var historyItemsToInsert = newInstructions
                                .Where(i => !existingHistoryIPs.Contains(i.IP))
                                .GroupBy(i => i.IP)
                                .Select(g => g.First())
                                .OrderBy(i => i.IP)
                                .ToList();

                            if (historyItemsToInsert.Any())
                            {
                                double oldOffset = _scrollViewer.VerticalOffset;
                                double oldHeight = _scrollViewer.ExtentHeight;

                                for (int i = 0; i < historyItemsToInsert.Count; i++)
                                {
                                    _internalSource.Insert(i, historyItemsToInsert[i]);
                                }

                                // 插入新项目后，内容高度增加，需要调整滚动偏移以保持视图稳定
                                _scrollViewer.UpdateLayout();
                                double newHeight = _scrollViewer.ExtentHeight;
                                _scrollViewer.ScrollToVerticalOffset(oldOffset + (newHeight - oldHeight));
                            }
                            break;
                    }
                    // 数据更新后，重新估算行高
                    // _estimatedLineHeight = EstimateLineHeight();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading instructions: {ex.Message}");
                // 出错时回退到旧的事件模式
                RequestData?.Invoke(this, new DataRequestEventArgs(address, requestType));
            }
            finally
            {
                _isPrefetching = false;
                ShowLoading(false);
            }
        }

        private void OnLineUpButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // 防止默认行为
            MoveSelectionUp();
        }
        
        private void OnLineDownButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // 防止默认行为
            MoveSelectionDown();
        }
        
        private void MoveSelectionUp()
        {
            if (!_internalSource.Any() || _isLoading) return;
            
            // 获取当前可见的第一个项的索引
            int firstVisibleIndex = GetFirstVisibleItemIndex();
            if (firstVisibleIndex <= 0)
            {
                // 如果已经是第一项，需要加载更多历史数据
                RequestMoreData(DataRequestEventArgs.RequestType.RequestHistory);
                return;
            }
            
            // 向上滚动一行，但不改变选中项
            if (_scrollViewer != null)
            {
                try
                {
                    _isProgrammaticScrolling = true; // 设置标志，表示这是程序触发的滚动
                    
                    // 获取一行的高度（近似值）
                    double lineHeight = EstimateLineHeight();
                    
                    // 向上滚动一行的距离
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - lineHeight);
                    
                    // 确保滑块位置不变
                    ScrollBar.Value = 0;
                }
                finally
                {
                    _isProgrammaticScrolling = false; // 重置标志
                }
            }
        }
        
        private void MoveSelectionDown()
        {
            if (!_internalSource.Any() || _isLoading) return;
            
            // 获取最后一个可见项的索引
            int lastVisibleIndex = GetLastVisibleItemIndex();
            if (lastVisibleIndex >= _internalSource.Count - 1)
            {
                // 如果已经是最后一项，需要加载更多未来数据
                RequestMoreData(DataRequestEventArgs.RequestType.RequestFuture);
                return;
            }
            
            // 向下滚动一行，但不改变选中项
            if (_scrollViewer != null)
            {
                try
                {
                    _isProgrammaticScrolling = true; // 设置标志，表示这是程序触发的滚动
                    
                    // 获取一行的高度（近似值）
                    double lineHeight = EstimateLineHeight();
                    
                    // 向下滚动一行的距离
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + lineHeight);
                    
                    // 确保滑块位置不变
                    ScrollBar.Value = 0;
                }
                finally
                {
                    _isProgrammaticScrolling = false; // 重置标志
                }
            }
        }
        
        // 估算一行的高度
        private double EstimateLineHeight()
        {
            if (_internalSource.Count == 0 || _scrollViewer == null)
                return 20; // 默认高度
                
            // 如果有足够的项，可以通过总高度除以项数来估算
            if (_scrollViewer.ExtentHeight > 0 && _internalSource.Count > 0)
            {
                return _scrollViewer.ExtentHeight / _internalSource.Count;
            }
            
            return 20; // 默认高度
        }
        
        // 获取第一个可见项的索引
        private int GetFirstVisibleItemIndex()
        {
            if (_scrollViewer == null || _internalSource.Count == 0)
                return 0;
                
            double verticalOffset = _scrollViewer.VerticalOffset;
            double lineHeight = EstimateLineHeight();
            
            if (lineHeight <= 0)
                return 0;
                
            int estimatedIndex = (int)(verticalOffset / lineHeight);
            return Math.Max(0, Math.Min(estimatedIndex, _internalSource.Count - 1));
        }
        
        // 获取最后一个可见项的索引
        private int GetLastVisibleItemIndex()
        {
            if (_scrollViewer == null || _internalSource.Count == 0)
                return 0;
                
            double verticalOffset = _scrollViewer.VerticalOffset;
            double viewportHeight = _scrollViewer.ViewportHeight;
            double lineHeight = EstimateLineHeight();
            
            if (lineHeight <= 0)
                return 0;
                
            int estimatedIndex = (int)((verticalOffset + viewportHeight) / lineHeight);
            return Math.Max(0, Math.Min(estimatedIndex, _internalSource.Count - 1));
        }

        // 向上翻页
        private void PageUp()
        {
            if (!_internalSource.Any() || _isLoading) return;
            
            if (_scrollViewer != null)
            {
                try
                {
                    _isProgrammaticScrolling = true;
                    
                    // 计算一页的高度，减去一点以便有重叠，提高用户体验
                    double pageHeight = _scrollViewer.ViewportHeight * 0.9;
                    
                    // 如果滚动到顶部附近，加载更多历史数据
                    if (_scrollViewer.VerticalOffset < pageHeight)
                    {
                        RequestMoreData(DataRequestEventArgs.RequestType.RequestHistory);
                    }
                    
                    // 向上滚动一页
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - pageHeight);
                    
                    // 确保滑块位置不变
                    ScrollBar.Value = 0;
                }
                finally
                {
                    _isProgrammaticScrolling = false;
                }
            }
        }
        
        // 向下翻页
        private void PageDown()
        {
            if (!_internalSource.Any() || _isLoading) return;
            
            if (_scrollViewer != null)
            {
                try
                {
                    _isProgrammaticScrolling = true;
                    
                    // 计算一页的高度，减去一点以便有重叠，提高用户体验
                    double pageHeight = _scrollViewer.ViewportHeight * 0.9;
                    
                    // 如果滚动到底部附近，加载更多未来数据
                    if (_scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight + pageHeight > _scrollViewer.ExtentHeight)
                    {
                        RequestMoreData(DataRequestEventArgs.RequestType.RequestFuture);
                    }
                    
                    // 向下滚动一页
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + pageHeight);
                    
                    // 确保滑块位置不变
                    ScrollBar.Value = 0;
                }
                finally
                {
                    _isProgrammaticScrolling = false;
                }
            }
        }
    }

    public class DataRequestEventArgs : EventArgs
    {
        public ulong Address { get; }
        public RequestType Type { get; }

        public DataRequestEventArgs(ulong address, RequestType type)
        {
            Address = address;
            Type = type;
        }

        public enum RequestType
        {
            Reload,
            RequestHistory,
            RequestFuture
        }
    }
}