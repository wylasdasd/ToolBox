using Blazing.Mvvm.ComponentModel;
using System.Text;

namespace ToolBox.Components.Pages.Calculation;

public partial class BitwiseOperationVM : ViewModelBase
{
    private int _bitWidth = 8;
    private bool _isSigned = true;
    private long _operandA = 0;
    private long _operandB = 0;
    private int _operandC = 1;
    private long _result = 0;
    private string _selectedOperation = "AND";
    private bool _shiftChainActive;
    private long _shiftBaseValue;
    private bool _shiftBaseCaptured;

    private static bool IsShiftOp(string op) => op is "SHL" or "SHR_A" or "SHR_L";

    public int BitWidth
    {
        get => _bitWidth;
        set
        {
            if (SetProperty(ref _bitWidth, value))
            {
                if (_operandC > value) OperandC = value;
                MaskValues();
                OnShiftInputsChanged();
            }
        }
    }

    public bool IsSigned
    {
        get => _isSigned;
        set
        {
            if (SetProperty(ref _isSigned, value))
            {
                MaskValues();
                if (IsShiftOp(SelectedOperation))
                    OnShiftInputsChanged();
                else
                    Calculate();
                NotifyAllDisplayProperties();
            }
        }
    }

    public long OperandA
    {
        get => _operandA;
        set
        {
            if (SetProperty(ref _operandA, value))
            {
                MaskValues();
                OnShiftInputsChanged();
            }
        }
    }
    
    // For manual input/binding from UI
    public string OperandADisplay
    {
        get => FormatValue(OperandA);
        set
        {
            if (long.TryParse(value, out long val))
            {
                OperandA = val;
            }
        }
    }

    public string OperandBDisplay
    {
        get => FormatValue(OperandB);
        set
        {
            if (long.TryParse(value, out long val))
            {
                OperandB = val;
            }
        }
    }

    public string OperandAHex
    {
        get => FormatHex(OperandA);
        set
        {
            try
            {
                string cleanHex = value.Replace("0x", "").Trim();
                if (string.IsNullOrEmpty(cleanHex)) return;
                long val = Convert.ToInt64(cleanHex, 16);
                OperandA = val;
            }
            catch { }
        }
    }

    public string OperandBHex
    {
        get => FormatHex(OperandB);
        set
        {
            try
            {
                string cleanHex = value.Replace("0x", "").Trim();
                if (string.IsNullOrEmpty(cleanHex)) return;
                long val = Convert.ToInt64(cleanHex, 16);
                OperandB = val;
            }
            catch { }
        }
    }

    public string ResultDisplay => FormatValue(Result);
    
    public string OperandABinary => FormatBinary(OperandA);
    public string OperandBBinary => FormatBinary(OperandB);
    public string ResultBinary => FormatBinary(Result);
    
    public string ResultHex => FormatHex(Result);

    public long OperandB
    {
        get => _operandB;
        set
        {
            if (SetProperty(ref _operandB, value))
            {
                MaskValues();
                Calculate();
            }
        }
    }

    /// <summary>Shift amount for SHL / SHR_A / SHR_L (manual input, not bit-masked).</summary>
    public int OperandC
    {
        get => _operandC;
        set
        {
            int clamped = Math.Clamp(value, 0, BitWidth);
            if (SetProperty(ref _operandC, clamped))
            {
                OnPropertyChanged(nameof(OperandCDisplay));
                OnShiftInputsChanged();
            }
        }
    }

    public string OperandCDisplay
    {
        get => _operandC.ToString();
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (int.TryParse(value.Trim(), out int val))
                OperandC = val;
        }
    }

    public string SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (!SetProperty(ref _selectedOperation, value))
                return;

            if (IsShiftOp(value))
                ApplyShift(fromResult: _shiftChainActive);
            else
            {
                ResetShiftChain();
                Calculate();
            }
        }
    }
    
    public long Result
    {
        get => _result;
        private set
        {
            if (SetProperty(ref _result, value))
            {
                OnPropertyChanged(nameof(ResultDisplay));
                OnPropertyChanged(nameof(ResultBinary));
                OnPropertyChanged(nameof(ResultHex));
            }
        }
    }

    private void MaskValues()
    {
        // Mask based on bit width
        long mask = GetMask();
        _operandA &= mask;
        _operandB &= mask;
        
        // Ensure proper sign extension if signed
        if (IsSigned)
        {
            _operandA = SignExtend(_operandA);
            _operandB = SignExtend(_operandB);
        }
        
        OnPropertyChanged(nameof(OperandA));
        OnPropertyChanged(nameof(OperandB));
        OnPropertyChanged(nameof(OperandADisplay));
        OnPropertyChanged(nameof(OperandBDisplay));
        OnPropertyChanged(nameof(OperandABinary));
        OnPropertyChanged(nameof(OperandBBinary));
        OnPropertyChanged(nameof(OperandAHex));
        OnPropertyChanged(nameof(OperandBHex));
    }
    
    private long GetMask()
    {
        if (BitWidth == 64) return -1L;
        return (1L << BitWidth) - 1;
    }

    private long SignExtend(long value)
    {
        if (BitWidth == 64) return value;
        
        long signBit = 1L << (BitWidth - 1);
        if ((value & signBit) != 0)
        {
            long mask = GetMask();
            return value | ~mask;
        }

        return value & GetMask();
    }

    private long NormalizeResult(long res, bool signExtendWhenSigned = true)
    {
        res &= GetMask();
        if (IsSigned && signExtendWhenSigned)
            res = SignExtend(res);
        return res;
    }

    private void NotifyAllDisplayProperties()
    {
        OnPropertyChanged(nameof(OperandADisplay));
        OnPropertyChanged(nameof(OperandBDisplay));
        OnPropertyChanged(nameof(OperandABinary));
        OnPropertyChanged(nameof(OperandBBinary));
        OnPropertyChanged(nameof(OperandAHex));
        OnPropertyChanged(nameof(OperandBHex));
        OnPropertyChanged(nameof(ResultDisplay));
        OnPropertyChanged(nameof(ResultBinary));
        OnPropertyChanged(nameof(ResultHex));
    }

    private string FormatValue(long value)
    {
        long maskedValue = value & GetMask();
        
        if (IsSigned)
        {
             // Sign extend to display as signed long correctly if needed, 
             // but if we keep internal logic consistent, value should already be correct for current BitWidth context
             // except when BitWidth < 64, the upper bits of 'long' might be 1s for negative numbers.
             // Our SignExtend method handles this.
             return SignExtend(maskedValue).ToString();
        }
        else
        {
            // Treat as unsigned
            ulong uValue = (ulong)maskedValue;
            return uValue.ToString();
        }
    }

    private string FormatBinary(long value)
    {
        long maskedValue = value & GetMask();
        string binary = Convert.ToString(maskedValue, 2).PadLeft(BitWidth, '0');
        
        if (binary.Length > BitWidth)
        {
            binary = binary.Substring(binary.Length - BitWidth);
        }
        
        // Add spaces for readability (every 4 bits)
        var sb = new StringBuilder();
        for (int i = 0; i < binary.Length; i++)
        {
            if (i > 0 && (binary.Length - i) % 4 == 0)
                sb.Append(" ");
            sb.Append(binary[i]);
        }
        return sb.ToString();
    }
    
    private string FormatHex(long value)
    {
        long maskedValue = value & GetMask();
        return maskedValue.ToString("X");
    }

    public void ToggleBitA(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex >= BitWidth) return;
        OperandA ^= (1L << bitIndex);
    }
    
    public void ToggleBitB(int bitIndex)
    {
         if (bitIndex < 0 || bitIndex >= BitWidth) return;
        OperandB ^= (1L << bitIndex);
    }
    
    // Helper for UI binding of bits
    public bool GetBitA(int index) => (OperandA & (1L << index)) != 0;
    public bool GetBitB(int index) => (OperandB & (1L << index)) != 0;
    public bool GetBitResult(int index) => (Result & (1L << index)) != 0;

    private void ResetShiftChain() => _shiftChainActive = false;

    private void CaptureShiftBase()
    {
        _shiftBaseValue = OperandA & GetMask();
        _shiftBaseCaptured = true;
    }

    private void RestoreShiftBaseToResult()
    {
        Result = NormalizeResult(_shiftBaseValue);
    }

    /// <summary>Restore Result to the value of A when the current shift chain started.</summary>
    public void ResetShiftFromA()
    {
        if (!IsShiftOp(SelectedOperation)) return;
        if (!_shiftBaseCaptured)
            CaptureShiftBase();
        ResetShiftChain();
        RestoreShiftBaseToResult();
    }

    private void OnShiftInputsChanged()
    {
        if (IsShiftOp(SelectedOperation))
        {
            CaptureShiftBase();
            ResetShiftChain();
            RestoreShiftBaseToResult();
        }
        else
            Calculate();
    }

    private void ApplyShift(bool fromResult)
    {
        if (!fromResult)
            CaptureShiftBase();

        long mask = GetMask();
        int shiftAmount = Math.Clamp(OperandC, 0, BitWidth);
        long input = fromResult ? Result & mask : OperandA & mask;

        long res = SelectedOperation switch
        {
            "SHL" => ShiftLeft(input, shiftAmount, mask),
            "SHR_A" => IsSigned
                ? ShiftRightArithmetic(input, shiftAmount, mask)
                : ShiftRightLogical(input, shiftAmount, mask),
            "SHR_L" => ShiftRightLogical(input, shiftAmount, mask),
            _ => input,
        };

        if (IsSigned && SelectedOperation is "SHR_A" or "SHL")
            Result = NormalizeResult(res);
        else
            Result = NormalizeResult(res, signExtendWhenSigned: false);

        _shiftChainActive = true;
    }

    private void Calculate()
    {
        if (IsShiftOp(SelectedOperation))
        {
            if (!_shiftChainActive)
                ApplyShift(fromResult: false);
            return;
        }

        long mask = GetMask();
        long am = OperandA & mask;
        long bm = OperandB & mask;
        if (IsSigned)
        {
            am = SignExtend(am);
            bm = SignExtend(bm);
        }

        long res = SelectedOperation switch
        {
            "AND" => am & bm,
            "OR" => am | bm,
            "XOR" => am ^ bm,
            "NOT" => ~(am & mask),
            _ => 0,
        };

        Result = NormalizeResult(res);
    }

    private long ShiftLeft(long value, int shift, long mask)
    {
        if (shift >= BitWidth) return 0;
        return (value & mask) << shift & mask;
    }

    private long ShiftRightLogical(long value, int shift, long mask)
    {
        if (shift >= BitWidth) return 0;
        return (long)((ulong)(value & mask) >> shift);
    }

    private long ShiftRightArithmetic(long value, int shift, long mask)
    {
        if (shift >= BitWidth)
        {
            long msb = 1L << (BitWidth - 1);
            return (value & msb) != 0 ? mask : 0;
        }

        long bits = value & mask;
        long msbBit = 1L << (BitWidth - 1);
        ulong u = (ulong)bits >> shift;
        if ((bits & msbBit) != 0)
        {
            ulong uMask = BitWidth == 64 ? ulong.MaxValue : (ulong)mask;
            u |= uMask ^ (uMask >> shift);
        }

        return BitWidth == 64 ? (long)u : (long)(u & (ulong)mask);
    }
    
    // UI Commands
    public void SetOperation(string op) => SelectedOperation = op;

    
    public void SetOperation2(string op)
    {
        if (SelectedOperation != op)
            SelectedOperation = op;
        else
            ApplyShift(fromResult: _shiftChainActive);
    }
}
