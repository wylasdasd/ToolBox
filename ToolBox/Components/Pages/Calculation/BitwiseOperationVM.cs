using Blazing.Mvvm.ComponentModel;
using System.Text;

namespace ToolBox.Components.Pages.Calculation;

public partial class BitwiseOperationVM : ViewModelBase
{
    private int _bitWidth = 32;
    private bool _isSigned = true;
    private long _operandA = 0;
    private long _operandB = 0;
    private long _result = 0;
    private string _selectedOperation = "AND";

    public int BitWidth
    {
        get => _bitWidth;
        set
        {
            if (SetProperty(ref _bitWidth, value))
            {
                MaskValues();
                Calculate();
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
                Calculate();
                OnPropertyChanged(nameof(OperandADisplay));
                OnPropertyChanged(nameof(OperandBDisplay));
                OnPropertyChanged(nameof(ResultDisplay));
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
                Calculate();
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

    public string SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (SetProperty(ref _selectedOperation, value))
            {
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
            // Negative number, extend sign
            long mask = GetMask();
            return value | ~mask;
        }
        else
        {
            // Positive number, mask to ensure upper bits are 0
            return value & GetMask();
        }
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
        return "0x" + maskedValue.ToString("X");
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

    private void Calculate()
    {
        long a = OperandA;
        long b = OperandB;
        
        // Mask inputs for calculation just in case
        long mask = GetMask();
        
        // For shift operations, B is usually small (0-63)
        int shiftAmount = (int)(b & 0x3F); // Limit shift to 63
        
        long res = 0;
        
        switch (SelectedOperation)
        {
            case "AND":
                res = a & b;
                break;
            case "OR":
                res = a | b;
                break;
            case "XOR":
                res = a ^ b;
                break;
            case "NOT": // Unary on A
                res = ~Result;
                break;
            case "SHL": // <<
                res = Result << shiftAmount;
             
                break;
            case "SHR_A": // >> Arithmetic (Signed)
                // If we want arithmetic shift, we must cast to correct signed type
                // But long is signed 64-bit.
                // If BitWidth < 64, we need to ensure sign bit is respected.
                // Our SignExtend(a) ensures 'a' has correct sign bits set up to 64-bit.
                // So (a >> shiftAmount) works for arithmetic shift if 'a' is sign-extended correctly.
                res = SignExtend(Result) >> shiftAmount;
                break;
            case "SHR_L": // >>> Logical (Unsigned)
                // Treat as unsigned, shift, then mask result
                ulong ua = (ulong)(Result & mask);
                res = (long)(ua >> shiftAmount);
                break;
        }
        
        // Mask result to fit bit width
        Result = res & mask;
        
        // If operation is NOT, we usually just show result. 
        // If Signed, result might need sign extension for display consistency, 
        // but 'Result' property stores raw bits (masked).
    }
    
    // UI Commands
    public void SetOperation(string op) => SelectedOperation = op;

    
    public void SetOperation2(string op) 
     {
        if(SelectedOperation!=op)
            SelectedOperation = op;
        else 
            Calculate();
     }
}
