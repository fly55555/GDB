using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Symbol
{
    /// <summary>
    /// PDB文件中的符号信息
    /// </summary>
    public class PdbSymbol
    {
        /// <summary>
        /// 符号名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 符号相对于模块基址的偏移
        /// </summary>
        public ulong Offset { get; set; }
        
        /// <summary>
        /// 符号大小（对于函数有效）
        /// </summary>
        public int Size { get; set; }
        
        /// <summary>
        /// 符号类型
        /// </summary>
        public PdbSymbolType Type { get; set; }
    }
    
    /// <summary>
    /// PDB符号类型
    /// </summary>
    public enum PdbSymbolType
    {
        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown,
        
        /// <summary>
        /// 函数
        /// </summary>
        Function,
        
        /// <summary>
        /// 数据
        /// </summary>
        Data,
        
        /// <summary>
        /// 标签（代码中的标记点）
        /// </summary>
        Label
    }

    /// <summary>
    /// PDB文件读取器，负责解析PDB文件并提取符号信息
    /// </summary>
    public class PdbReader : IDisposable
    {
        // PDB文件路径
        private readonly string _pdbFilePath;
        
        // 日志记录回调
        private Action<string> _logCallback;
        private bool _initialized = false;
        private string _pdbType = "Unknown";
        
        // 缓存所有符号的地址排序列表，用于估算函数大小
        private List<PdbSymbol> _sortedSymbols = null;

        public PdbReader(string pdbFilePath, Action<string> logCallback = null)
        {
            _pdbFilePath = pdbFilePath;
            _logCallback = logCallback ?? (msg => System.Diagnostics.Debug.WriteLine(msg));
            Initialize();
        }

        /// <summary>
        /// 初始化PDB读取器
        /// </summary>
        private void Initialize()
        {
            try
            {
                if (!File.Exists(_pdbFilePath))
                {
                    Log($"PDB文件不存在: {_pdbFilePath}");
                    return;
                }
                
                // 尝试加载PDB文件签名
                using (var stream = new FileStream(_pdbFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] signature = new byte[4];
                    if (stream.Read(signature, 0, signature.Length) < signature.Length)
                    {
                        Log("无法读取PDB文件头部");
                        return;
                    }
                    
                    string signatureText = Encoding.ASCII.GetString(signature);
                    
                    // 根据签名判断PDB类型
                    if (signatureText == "Micr") // 微软格式PDB
                    {
                        _pdbType = "Microsoft";
                        Log($"PDB文件签名: {signatureText} (微软格式PDB)");
                    }
                    else if (signatureText == "BSJB") // 便携式PDB
                    {
                        _pdbType = "Portable";
                        Log($"PDB文件签名: {signatureText} (便携式PDB)");
                    }
                    else
                    {
                        _pdbType = "Unknown";
                        Log($"PDB文件签名: {signatureText} (未知格式)");
                    }
                }
                
                _initialized = true;
                Log("PDB读取器初始化成功");
            }
            catch (Exception ex)
            {
                Log($"初始化PDB读取器失败: {ex.Message}");
                Log(ex.StackTrace);
            }
        }

        /// <summary>
        /// 读取PDB文件中的公共符号
        /// </summary>
        public List<PdbSymbol> ReadPublicSymbols()
        {
            var result = new List<PdbSymbol>();
            var reader = new SharpPdb.Native.PdbFileReader(_pdbFilePath);
            var symbols = reader.PublicSymbols;
            foreach (var item in symbols)
            {
                var symbol = new PdbSymbol();
                symbol.Name = item.Name;
                symbol.Offset = item.RelativeVirtualAddress;
                symbol.Type = item.IsFunction ? PdbSymbolType.Function : 
                             (item.Name.EndsWith("Label") ? PdbSymbolType.Label : PdbSymbolType.Data);
                result.Add(symbol);
            }
            
            // 对符号按地址排序，用于后续估算函数大小
            _sortedSymbols = result.OrderBy(s => s.Offset).ToList();
            
            // 估算函数大小
            EstimateFunctionSizes();
            
            return result;
        }

        /// <summary>
        /// 读取PDB文件中的函数符号
        /// </summary>
        public List<PdbSymbol> ReadFunctionSymbols()
        {
            var result = new List<PdbSymbol>();
            var reader = new SharpPdb.Native.PdbFileReader(_pdbFilePath);
            var symbols = reader.PublicSymbols;
            foreach (var item in symbols)
            {
                if (item.IsFunction)
                {
                    var symbol = new PdbSymbol();
                    symbol.Name = item.Name;
                    symbol.Offset = item.RelativeVirtualAddress;
                    symbol.Type = PdbSymbolType.Function;
                    symbol.Size = 0;
                    result.Add(symbol);
                }
            }
            
            // 如果没有从ReadPublicSymbols初始化过排序符号列表，则在这里初始化
            if (_sortedSymbols == null)
            {
                _sortedSymbols = result.OrderBy(s => s.Offset).ToList();
                // 估算函数大小
                EstimateFunctionSizes();
            }
            
            return result;
        }
        
        /// <summary>
        /// 估算函数大小
        /// </summary>
        private void EstimateFunctionSizes()
        {
            if (_sortedSymbols == null || _sortedSymbols.Count == 0)
                return;
                
            // 对于每个函数符号，如果其大小为0，则估算其大小
            for (int i = 0; i < _sortedSymbols.Count; i++)
            {
                var symbol = _sortedSymbols[i];
                
                // 如果不是函数或已有大小，则跳过
                if (symbol.Type != PdbSymbolType.Function || symbol.Size > 0)
                    continue;
                    
                // 查找下一个符号
                PdbSymbol nextSymbol = null;
                for (int j = i + 1; j < _sortedSymbols.Count; j++)
                {
                    // 只考虑函数或标签作为边界
                    if (_sortedSymbols[j].Type == PdbSymbolType.Function || 
                        _sortedSymbols[j].Type == PdbSymbolType.Label)
                    {
                        nextSymbol = _sortedSymbols[j];
                        break;
                    }
                }
                
                if (nextSymbol != null)
                {
                    // 计算估算大小，但限制最大值为64KB（防止过大的估算值）
                    ulong estimatedSize = nextSymbol.Offset - symbol.Offset;
                    symbol.Size = (int)Math.Min(estimatedSize, 65536);
                }
                else
                {
                    // 如果没有下一个符号，设置一个默认值
                    symbol.Size = 128; // 默认函数大小为128字节
                }
            }
        }
        
        /// <summary>
        /// 尝试从PDB文件提取基本信息
        /// </summary>
        private List<PdbSymbol> TryExtractBasicPdbInfo()
        {
            var symbols = new List<PdbSymbol>();
            
            try
            {
                using (var stream = new FileStream(_pdbFilePath, FileMode.Open, FileAccess.Read))
                {
                    // 读取PDB头部信息
                    byte[] headerBuffer = new byte[1024]; // 读取前1KB数据
                    int bytesRead = stream.Read(headerBuffer, 0, headerBuffer.Length);
                    
                    if (_pdbType == "Microsoft")
                    {
                        // 微软PDB格式解析 (非常基础的实现)
                        // 实际的PDB格式非常复杂，这里仅作示例
                        
                        // 读取PDB页面大小 (通常在头部的某个位置)
                        int pageSize = BitConverter.ToInt32(headerBuffer, 32);
                        Log($"PDB页面大小: {pageSize}字节");
                        
                        // 读取PDB总页数 (这是演示性质的代码)
                        int totalPages = BitConverter.ToInt32(headerBuffer, 36);
                        Log($"PDB总页数: {totalPages}");
                        
                        // 在实际实现中，应该解析PDB的目录结构并找到符号流
                        // 由于PDB格式复杂性，这里仅作示例
                        Log("Microsoft PDB格式需要DIA SDK或类似库才能完全解析");
                    }
                    else if (_pdbType == "Portable")
                    {
                        // 便携式PDB解析 (非常基础的实现)
                        Log("便携式PDB格式基于Metadata，需要相应的解析库");
                        
                        // 读取一些基本的文件信息
                        byte[] guidBytes = new byte[16];
                        stream.Seek(16, SeekOrigin.Begin);
                        stream.Read(guidBytes, 0, guidBytes.Length);
                        
                        Guid pdbGuid = new Guid(guidBytes);
                        Log($"PDB GUID: {pdbGuid}");
                    }
                }
                
                // 在实际应用中，此函数应该返回实际从PDB文件中解析出的符号
                // 但是由于解析真实PDB需要复杂的实现，这里返回空列表
                return symbols;
            }
            catch (Exception ex)
            {
                Log($"尝试提取PDB基本信息失败: {ex.Message}");
                return symbols;
            }
        }
        

        /// <summary>
        /// 记录日志信息
        /// </summary>
        private void Log(string message)
        {
            _logCallback?.Invoke($"[PdbReader] {message}");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 没有特殊资源需要释放
        }
    }
} 