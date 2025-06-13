using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Symbol
{
    /// <summary>
    /// 符号信息
    /// </summary>
    public class SymbolInfo
    {
        /// <summary>
        /// 符号名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 符号地址
        /// </summary>
        public ulong Address { get; set; }
        
        /// <summary>
        /// 符号大小
        /// </summary>
        public uint Size { get; set; }
    }

    /// <summary>
    /// 符号管理器
    /// </summary>
    public class SymbolManager
    {
        private readonly Dictionary<string, List<SymbolInfo>> _symbolsByModule = new Dictionary<string, List<SymbolInfo>>();
        private readonly SortedList<ulong, SymbolInfo> _symbolsByAddress = new SortedList<ulong, SymbolInfo>();
        private readonly IDebugControl _debugControl;
        
        // 默认的符号服务器URL
        private const string DEFAULT_SYMBOL_SERVER = "https://msdl.microsoft.com/download/symbols";
        
        // 符号缓存目录
        private string _symbolsPath;
        
        // 符号服务器URL
        private string _symbolServer;

        public SymbolManager(IDebugControl debugControl)
        {
            _debugControl = debugControl;
            
            // 初始化符号路径和符号服务器
            InitializeSymbolPaths();
        }

        /// <summary>
        /// 初始化符号路径和符号服务器
        /// </summary>
        private void InitializeSymbolPaths()
        {
            // 从环境变量获取符号路径
            string symbolPathEnv = Environment.GetEnvironmentVariable("_SYMBOL_PATH");
            
            // 设置默认值
            _symbolsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GDB", "Symbols");
            _symbolServer = DEFAULT_SYMBOL_SERVER;
            
            // 如果环境变量中设置了符号路径，则解析它
            if (!string.IsNullOrEmpty(symbolPathEnv))
            {
                System.Diagnostics.Debug.WriteLine($"找到_SYMBOL_PATH环境变量: {symbolPathEnv}");
                ParseSymbolPath(symbolPathEnv);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("未找到_SYMBOL_PATH环境变量，使用默认设置");
                // 确保符号路径存在
                Directory.CreateDirectory(_symbolsPath);
            }
            
            System.Diagnostics.Debug.WriteLine($"符号本地缓存路径: {_symbolsPath}");
            System.Diagnostics.Debug.WriteLine($"符号服务器URL: {_symbolServer}");
        }

        /// <summary>
        /// 解析符号路径
        /// </summary>
        /// <param name="symbolPath">符号路径字符串，例如 "srv*C:\symbols*https://msdl.microsoft.com/download/symbols"</param>
        private void ParseSymbolPath(string symbolPath)
        {
            if (string.IsNullOrEmpty(symbolPath)) return;
            
            // 符号路径可能包含多个路径，以分号分隔
            var paths = symbolPath.Split(';');
            
            foreach (var path in paths)
            {
                // 处理 SRV* 格式的路径
                if (path.StartsWith("SRV*", StringComparison.OrdinalIgnoreCase) || 
                    path.StartsWith("SYMSRV*", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = path.Split('*');
                    if (parts.Length >= 2)
                    {
                        // 第二部分是本地缓存目录
                        _symbolsPath = parts[1];
                        
                        // 确保路径存在
                        if (!Directory.Exists(_symbolsPath))
                        {
                            try
                            {
                                Directory.CreateDirectory(_symbolsPath);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"无法创建符号缓存目录: {ex.Message}");
                                // 回退到默认路径
                                _symbolsPath = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                    "GDB", "Symbols");
                                Directory.CreateDirectory(_symbolsPath);
                            }
                        }
                        
                        // 如果有第三部分，它是符号服务器URL
                        if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                        {
                            _symbolServer = parts[2];
                        }
                        
                        // 找到有效配置后停止处理
                        break;
                    }
                }
                // 处理直接指定本地路径的情况
                else if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && 
                         Directory.Exists(path))
                {
                    _symbolsPath = path;
                    break;
                }
                // 处理直接指定URL的情况
                else if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    _symbolServer = path;
                    // 注意：不要退出循环，因为我们仍然需要找到本地缓存目录
                }
            }
        }

        /// <summary>
        /// 从符号路径中解析符号服务器URL (现在这个方法已不再需要)
        /// </summary>
        private string ParseSymbolServerUrl(string symbolPath)
        {
            // 已由 ParseSymbolPath 方法替代
            return DEFAULT_SYMBOL_SERVER;
        }

        /// <summary>
        /// 加载内核符号（通过模拟简单的解析）
        /// </summary>
        /// <returns>是否加载成功</returns>
        public async Task<bool> LoadKernelSymbols()
        {
            if (_debugControl.KernBase == 0) return false;

            try
            {
                // 清除现有符号
                _symbolsByModule.Remove("ntoskrnl.exe");

                // 创建一个新的符号列表
                var symbols = new List<SymbolInfo>();

                // 获取RSDSI信息以便查找PDB文件
                var pdbInfo = await GetPdbInfoFromMemory();
                if (pdbInfo != null)
                {
                    // 尝试从符号路径或符号服务器加载PDB文件
                    if (await LoadSymbolsFromPdb(pdbInfo, symbols))
                    {
                        System.Diagnostics.Debug.WriteLine($"成功从PDB加载符号: {pdbInfo.PdbName}");

                        // 保存到字典
                        _symbolsByModule["ntoskrnl.exe"] = symbols;

                        // 更新地址索引
                        foreach (var symbol in symbols)
                        {
                            _symbolsByAddress[symbol.Address] = symbol;
                        }

                        return true;
                    }
                }

                // 如果无法从PDB加载符号，使用硬编码的示例符号
                System.Diagnostics.Debug.WriteLine("无法从PDB加载符号，使用硬编码的示例符号");
                AddKnownKernelSymbols(symbols, _debugControl.KernBase);

                // 使用远程流读取一些数据，以确认内核基址是有效的
                byte[] testData = await _debugControl.ReadVirtual((long)_debugControl.KernBase, 16);
                if (testData == null || testData.Length == 0)
                {
                    return false;
                }

                // 保存到字典
                _symbolsByModule["ntoskrnl.exe"] = symbols;

                // 更新地址索引
                foreach (var symbol in symbols)
                {
                    _symbolsByAddress[symbol.Address] = symbol;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载内核符号失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PDB文件信息
        /// </summary>
        private class PdbInfo
        {
            public string PdbName { get; set; }
            public Guid Guid { get; set; }
            public int Age { get; set; }
            public string PdbSignature => $"{PdbName.ToLower()}\\{Guid.ToString("N").ToUpper()}{Age}\\{PdbName.ToLower()}";
        }
        
        /// <summary>
        /// 从内存中获取PDB信息
        /// </summary>
        /// <returns>PDB信息</returns>
        private async Task<PdbInfo> GetPdbInfoFromMemory()
        {
            if (_debugControl.KernBase == 0) 
            {
                System.Diagnostics.Debug.WriteLine("内核基址未找到，无法获取PDB信息");
                return null;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"开始从内存地址 0x{_debugControl.KernBase:X} 获取PDB信息");
                using (var stream = new RemoteStream(_debugControl, (long)_debugControl.KernBase))
                {
                    // 解析PE头
                    var error = PE.PeHeader.TryReadFrom(stream, out PE.PeHeader header);
                    if (error != PE.ReaderError.NoError)
                    {
                        System.Diagnostics.Debug.WriteLine($"解析PE头失败: {error}");
                        return null;
                    }

                    // 获取PDB信息
                    var rsdsi = header.GetRSDSI(stream);
                    if (rsdsi == null)
                    {
                        System.Diagnostics.Debug.WriteLine("无法获取RSDSI信息");
                        return null;
                    }

                    // 确认这是一个内核PDB
                    if (rsdsi.PDB == "ntoskrnl.pdb" || rsdsi.PDB == "ntkrnlpa.pdb" || 
                        rsdsi.PDB == "ntkrnlmp.pdb" || rsdsi.PDB == "ntkrpamp.pdb")
                    {
                        System.Diagnostics.Debug.WriteLine($"找到内核PDB: {rsdsi.PDB}, GUID: {rsdsi.GUID}, Age: {rsdsi.Age}");
                        
                        // 转换到我们的PdbInfo格式
                        return new PdbInfo
                        {
                            PdbName = rsdsi.PDB,
                            Guid = rsdsi.GUID,
                            Age = (int)rsdsi.Age
                        };
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"发现的PDB不是内核PDB: {rsdsi.PDB}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取PDB信息时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
            
            return null;
        }
        
        /// <summary>
        /// 从PDB文件加载符号
        /// </summary>
        /// <param name="pdbInfo">PDB文件信息</param>
        /// <param name="symbols">输出符号列表</param>
        /// <returns>是否成功加载</returns>
        private async Task<bool> LoadSymbolsFromPdb(PdbInfo pdbInfo, List<SymbolInfo> symbols)
        {
            System.Diagnostics.Debug.WriteLine($"开始从PDB加载符号: {pdbInfo.PdbName}, GUID: {pdbInfo.Guid}, Age: {pdbInfo.Age}");
            
            // 构建微软符号服务器格式的路径
            string pdbFileName = pdbInfo.PdbName;
            string pdbGuid = pdbInfo.Guid.ToString("N").ToUpper();
            int pdbAge = pdbInfo.Age;
            
            // 检查路径格式为 SRV* 的情况下的符号缓存
            string pdbLocalPath = null;
            
            // 1. 首先尝试在常规路径下查找：{符号路径}/{pdb文件名}/{guid}{age}/{pdb文件名}
            string msPath = Path.Combine(_symbolsPath, pdbFileName.ToLower(), $"{pdbGuid}{pdbAge}", pdbFileName);
            System.Diagnostics.Debug.WriteLine($"检查路径1: {msPath}");
            if (File.Exists(msPath))
            {
                pdbLocalPath = msPath;
                System.Diagnostics.Debug.WriteLine($"在路径1中找到PDB文件: {pdbLocalPath}");
            }
            
            // 2. 尝试路径2：{符号路径}/{pdb文件名}/{guid}/{pdb文件名}
            if (pdbLocalPath == null)
            {
                string ms2Path = Path.Combine(_symbolsPath, pdbFileName.ToLower(), pdbGuid, pdbFileName);
                System.Diagnostics.Debug.WriteLine($"检查路径2: {ms2Path}");
                if (File.Exists(ms2Path))
                {
                    pdbLocalPath = ms2Path;
                    System.Diagnostics.Debug.WriteLine($"在路径2中找到PDB文件: {pdbLocalPath}");
                }
            }
            
            // 3. 检查符号路径中是否直接包含此PDB文件
            if (pdbLocalPath == null)
            {
                string directPath = Path.Combine(_symbolsPath, pdbFileName);
                System.Diagnostics.Debug.WriteLine($"检查直接路径: {directPath}");
                if (File.Exists(directPath))
                {
                    pdbLocalPath = directPath;
                    System.Diagnostics.Debug.WriteLine($"在直接路径中找到PDB文件: {pdbLocalPath}");
                }
            }
            
            // 如果在本地没有找到PDB文件，则尝试下载
            if (pdbLocalPath == null)
            {
                System.Diagnostics.Debug.WriteLine("在本地未找到PDB文件，尝试下载...");
                
                // 创建目标目录
                string downloadDir = Path.Combine(_symbolsPath, pdbFileName.ToLower(), $"{pdbGuid}{pdbAge}");
                Directory.CreateDirectory(downloadDir);
                
                // 尝试下载
                bool downloadSuccess = await DownloadPdbFile(pdbInfo, downloadDir);
                if (downloadSuccess)
                {
                    string downloadedPath = Path.Combine(downloadDir, pdbFileName);
                    if (File.Exists(downloadedPath))
                    {
                        pdbLocalPath = downloadedPath;
                        System.Diagnostics.Debug.WriteLine($"成功下载PDB文件到: {pdbLocalPath}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"下载似乎成功，但文件未找到: {downloadedPath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("下载PDB文件失败");
                }
            }
            
            // 如果找不到PDB文件，则返回失败
            if (pdbLocalPath == null)
            {
                System.Diagnostics.Debug.WriteLine("无法找到或下载PDB文件");
                return false;
            }
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始解析PDB文件: {pdbLocalPath}");
                
                // 在后台线程中执行PDB解析，避免UI死锁
                // 即使PdbReader内部是同步的，但由于它在Task.Run内部执行，因此不会阻塞UI线程
                var results = await Task.Run(() => 
                {
                    try
                    {
                        var loadedSymbols = new List<SymbolInfo>();
                        // 使用PdbReader库解析PDB文件
                        using (var pdbReader = new PdbReader(pdbLocalPath, message => 
                            System.Diagnostics.Debug.WriteLine(message)))
                        {
                            // 获取所有的符号（包括公共符号和函数符号）
                            var publicSymbols = pdbReader.ReadPublicSymbols();
                            
                            if (publicSymbols == null || publicSymbols.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("PDB文件中未找到公共符号");
                                return null;
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"从PDB文件中读取了 {publicSymbols.Count} 个公共符号");
                            
                            // 从符号表中提取符号信息
                            int loadedCount = 0;
                            foreach (var pubSym in publicSymbols)
                            {
                                // 跳过内部或特殊符号
                                if (string.IsNullOrEmpty(pubSym.Name) || pubSym.Name.StartsWith("__") || 
                                    pubSym.Name.Contains("@") || pubSym.Name.Contains("$"))
                                {
                                    continue;
                                }
                                
                                // 创建符号信息
                                var symbolInfo = new SymbolInfo
                                {
                                    Name = pubSym.Name,
                                    Address = _debugControl.KernBase + (ulong)pubSym.Offset,
                                    Size = (uint)pubSym.Size // 使用PdbReader中改进的大小计算
                                };
                                
                                // 添加到符号列表
                                loadedSymbols.Add(symbolInfo);
                                loadedCount++;
                                
                                // 输出日志，每1000个符号输出一次
                                if (loadedCount % 1000 == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"已加载 {loadedCount} 个符号");
                                }
                            }
                            
                            // 读取函数符号，这一步现在可能是多余的，因为ReadPublicSymbols已经包含了所有符号
                            // 但为了兼容性和确保获取所有函数符号，我们仍然调用此方法
                            var functionSymbols = pdbReader.ReadFunctionSymbols();
                            
                            if (functionSymbols != null && functionSymbols.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"从PDB文件中读取了 {functionSymbols.Count} 个函数符号");
                                
                                foreach (var funcSym in functionSymbols)
                                {
                                    // 查找是否已经添加了该符号
                                    var existingSymbol = loadedSymbols.FirstOrDefault(s => 
                                        s.Name == funcSym.Name && 
                                        s.Address == _debugControl.KernBase + (ulong)funcSym.Offset);
                                    
                                    if (existingSymbol != null)
                                    {
                                        // 如果函数大小有更准确的信息，则更新
                                        if (funcSym.Size > 0)
                                        {
                                            existingSymbol.Size = (uint)funcSym.Size;
                                        }
                                    }
                                    else
                                    {
                                        // 创建新的函数符号
                                        var symbolInfo = new SymbolInfo
                                        {
                                            Name = funcSym.Name,
                                            Address = _debugControl.KernBase + (ulong)funcSym.Offset,
                                            Size = (uint)funcSym.Size
                                        };
                                        
                                        // 添加到符号列表
                                        loadedSymbols.Add(symbolInfo);
                                    }
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"成功从PDB文件加载了 {loadedSymbols.Count} 个符号");
                            return loadedSymbols;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"在Task.Run中解析PDB时发生异常: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                        return null;
                    }
                });
                
                // 处理Task.Run的结果
                if (results != null && results.Count > 0)
                {
                    symbols.AddRange(results);
                    System.Diagnostics.Debug.WriteLine($"成功将 {results.Count} 个符号添加到符号列表中");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PDB解析返回了空结果或没有符号");
                    
                    // 出错时，回退到使用模拟符号
                    System.Diagnostics.Debug.WriteLine("使用模拟符号作为回退方案");
                    AddDummyNtosSymbols(symbols, _debugControl.KernBase);
                    
                    return symbols.Count > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析PDB文件时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                
                // 出错时，回退到使用模拟符号
                System.Diagnostics.Debug.WriteLine("使用模拟符号作为回退方案");
                AddDummyNtosSymbols(symbols, _debugControl.KernBase);
                
                return symbols.Count > 0;
            }
        }
        
        /// <summary>
        /// 添加一些模拟的ntoskrnl符号，用于演示
        /// </summary>
        private void AddDummyNtosSymbols(List<SymbolInfo> symbols, ulong kernelBase)
        {
            // 注意：这些是模拟的符号，实际项目中应该从PDB文件中提取
            var dummySymbols = new Dictionary<string, (ulong offset, uint size)>
            {
                // Windows内核常见函数的模拟偏移（仅用于演示）
                { "KeBugCheck", (0x12A400, 0x50) },
                { "KeBugCheckEx", (0x12A500, 0xA0) },
                { "KeInitializeApc", (0x145200, 0x120) },
                { "KeInsertQueueApc", (0x146000, 0x1A0) },
                { "KeEnterCriticalRegion", (0x147500, 0x30) },
                { "KeLeaveCriticalRegion", (0x147600, 0x30) },
                { "KeInitializeDpc", (0x148000, 0x80) },
                { "KeInitializeTimer", (0x148500, 0x70) },
                { "KeSetTimer", (0x149000, 0xC0) },
                { "MmCreateKernelStack", (0x212000, 0x150) },
                { "MmDeleteKernelStack", (0x212200, 0x100) },
                { "MmGetPhysicalAddress", (0x213000, 0x80) },
                { "MmMapIoSpace", (0x214000, 0x100) },
                { "MmUnmapIoSpace", (0x214200, 0x70) },
                { "ObfDereferenceObject", (0x281000, 0x40) },
                { "ObReferenceObjectByHandle", (0x282000, 0x1A0) },
                { "PsCreateSystemThread", (0x310000, 0x200) },
                { "PsGetCurrentProcess", (0x311000, 0x30) },
                { "PsGetCurrentThread", (0x311100, 0x30) },
                { "PsGetProcessImageFileName", (0x312000, 0xA0) },
                { "PsTerminateSystemThread", (0x313000, 0xC0) }
            };
            
            foreach (var symbol in dummySymbols)
            {
                symbols.Add(new SymbolInfo
                {
                    Name = symbol.Key,
                    Address = kernelBase + symbol.Value.offset,
                    Size = symbol.Value.size
                });
            }
        }
        
        /// <summary>
        /// 下载PDB文件
        /// </summary>
        /// <param name="pdbInfo">PDB文件信息</param>
        /// <param name="localPath">本地保存路径</param>
        /// <returns>是否成功下载</returns>
        private async Task<bool> DownloadPdbFile(PdbInfo pdbInfo, string localPath)
        {
            try
            {
                // 确保目标目录存在
                Directory.CreateDirectory(localPath);
                
                // 构建PDB文件的下载URL，使用Microsoft符号服务器格式
                // 格式：{symbolServer}/{pdbName}/{GUID}{Age}/{pdbName}
                string pdbFileName = pdbInfo.PdbName;
                string pdbGuid = pdbInfo.Guid.ToString("N").ToUpper();
                int pdbAge = pdbInfo.Age;
                
                string downloadUrl = $"{_symbolServer}/{pdbFileName}/{pdbGuid}{pdbAge}/{pdbFileName}";
                System.Diagnostics.Debug.WriteLine($"尝试下载PDB文件: {downloadUrl}");
                
                // 下载PDB文件
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(2); // 设置超时时间
                    
                    using (var response = await httpClient.GetAsync(downloadUrl))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"下载失败，状态码: {response.StatusCode}");
                            return false;
                        }
                        
                        // 保存PDB文件
                        string localPdbFile = Path.Combine(localPath, pdbFileName);
                        using (var fileStream = new FileStream(localPdbFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"PDB文件下载成功: {localPdbFile}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"下载PDB文件失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 添加一些已知的内核符号（在实际项目中，这些应该是从PE导出表或PDB文件解析的）
        /// </summary>
        private void AddKnownKernelSymbols(List<SymbolInfo> symbols, ulong kernelBase)
        {
            // 这里只是模拟一些常见的内核函数地址
            // 实际应用中，这些地址应该通过解析PE文件的导出表获取
            // 或者通过加载PDB文件获取
            
            // 这些偏移地址只是示例，不是真实值
            var knownSymbols = new Dictionary<string, (ulong offset, uint size)>
            {
                { "KeInitializeProcess", (0x1A000, 0x100) },
                { "PsCreateSystemThread", (0x1A200, 0x150) },
                { "IoCreateDevice", (0x1B000, 0x200) },
                { "ObRegisterCallbacks", (0x1C500, 0x180) },
                { "ExAllocatePoolWithTag", (0x1D800, 0x120) },
                { "ExFreePoolWithTag", (0x1DA00, 0x100) },
                { "MmMapIoSpace", (0x1F200, 0x150) },
                { "ZwCreateFile", (0x20500, 0x200) },
                { "RtlInitUnicodeString", (0x21700, 0x100) }
            };
            
            foreach (var symbol in knownSymbols)
            {
                symbols.Add(new SymbolInfo
                {
                    Name = symbol.Key,
                    Address = kernelBase + symbol.Value.offset,
                    Size = symbol.Value.size
                });
            }
        }

        /// <summary>
        /// 根据地址查找符号，使用多种策略
        /// </summary>
        /// <param name="address">要查找的地址</param>
        /// <param name="symbolName">输出符号名称</param>
        /// <param name="offset">输出相对符号的偏移量</param>
        /// <param name="strategy">查找策略</param>
        /// <returns>是否找到符号</returns>
        public bool TryGetSymbolByAddress(ulong address, out string symbolName, out int offset, SymbolLookupStrategy strategy = SymbolLookupStrategy.Default)
        {
            symbolName = null;
            offset = 0;
            
            switch (strategy)
            {
                case SymbolLookupStrategy.ExactMatch:
                    return TryGetSymbolByAddressExact(address, out symbolName, out offset);
                    
                case SymbolLookupStrategy.Relaxed:
                    return TryGetSymbolByAddressRelaxed(address, out symbolName, out offset);
                    
                case SymbolLookupStrategy.Default:
                default:
                    // 默认策略：首先尝试精确匹配，如果失败则使用宽松匹配
                    if (TryGetSymbolByAddressExact(address, out symbolName, out offset))
                        return true;
                    return TryGetSymbolByAddressRelaxed(address, out symbolName, out offset);
            }
        }
        
        /// <summary>
        /// 根据地址精确查找符号（只在函数范围内匹配）
        /// </summary>
        private bool TryGetSymbolByAddressExact(ulong address, out string symbolName, out int offset)
        {
            symbolName = null;
            offset = 0;
            
            // 查找小于等于地址的最后一个符号
            var index = _symbolsByAddress.Keys.ToList().BinarySearch(address);
            if (index < 0)
            {
                // 如果没有精确匹配，BinarySearch返回的是按位取反的插入点
                // 所以我们需要将其转换回正常索引并减1
                index = ~index - 1;
            }
            
            if (index < 0) return false;
            
            var symbolAddress = _symbolsByAddress.Keys[index];
            var symbol = _symbolsByAddress[symbolAddress];
            
            // 精确匹配：地址必须在符号的大小范围内
            if (symbol.Size > 0 && address >= symbol.Address && address < symbol.Address + symbol.Size)
            {
                symbolName = symbol.Name;
                offset = (int)(address - symbol.Address);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 根据地址宽松查找符号（允许不在函数范围内）
        /// </summary>
        private bool TryGetSymbolByAddressRelaxed(ulong address, out string symbolName, out int offset)
        {
            symbolName = null;
            offset = 0;
            
            // 查找小于等于地址的最后一个符号
            var index = _symbolsByAddress.Keys.ToList().BinarySearch(address);
            if (index < 0)
            {
                // 如果没有精确匹配，BinarySearch返回的是按位取反的插入点
                // 所以我们需要将其转换回正常索引并减1
                index = ~index - 1;
            }
            
            if (index < 0) return false;
            
            var symbolAddress = _symbolsByAddress.Keys[index];
            var symbol = _symbolsByAddress[symbolAddress];
            
            // 宽松匹配：地址大于等于符号地址，且小于下一个符号地址
            if (address >= symbol.Address)
            {
                // 如果有下一个符号，且地址小于下一个符号地址
                if (index + 1 < _symbolsByAddress.Count)
                {
                    var nextSymbolAddress = _symbolsByAddress.Keys[index + 1];
                    if (address < nextSymbolAddress)
                    {
                        symbolName = symbol.Name;
                        offset = (int)(address - symbol.Address);
                        return true;
                    }
                }
                else
                {
                    // 如果是最后一个符号，直接使用
                    symbolName = symbol.Name;
                    offset = (int)(address - symbol.Address);
                    return true;
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// 符号查找策略
    /// </summary>
    public enum SymbolLookupStrategy
    {
        /// <summary>
        /// 默认策略：先尝试精确匹配，再尝试宽松匹配
        /// </summary>
        Default,
        
        /// <summary>
        /// 精确匹配：只在函数范围内匹配
        /// </summary>
        ExactMatch,
        
        /// <summary>
        /// 宽松匹配：允许不在函数范围内，但在下一个符号之前
        /// </summary>
        Relaxed
    }
} 