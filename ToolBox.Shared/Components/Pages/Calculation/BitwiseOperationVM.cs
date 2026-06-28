using Blazing.Mvvm.ComponentModel;
using ToolBox.Tools.Calculation;

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
                if (BitwiseService.IsShiftOp(SelectedOperation))
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

    public string OperandADisplay
    {
        get => FormatValue(OperandA);
        set
        {
            if (long.TryParse(value, out long val))
                OperandA = val;
        }
    }

    public string OperandBDisplay
    {
        get => FormatValue(OperandB);
        set
        {
            if (long.TryParse(value, out long val))
                OperandB = val;
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

            if (BitwiseService.IsShiftOp(value))
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
        var mask = BitwiseService.GetMask(BitWidth);
        _operandA &= mask;
        _operandB &= mask;

        if (IsSigned)
        {
            _operandA = BitwiseService.SignExtend(_operandA, BitWidth);
            _operandB = BitwiseService.SignExtend(_operandB, BitWidth);
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

    private string FormatValue(long value) =>
        BitwiseService.FormatValue(value, BitWidth, IsSigned);

    private string FormatBinary(long value) =>
        BitwiseService.FormatBinary(value, BitWidth);

    private string FormatHex(long value) =>
        BitwiseService.FormatHex(value, BitWidth);

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

    public bool GetBitA(int index) => (OperandA & (1L << index)) != 0;
    public bool GetBitB(int index) => (OperandB & (1L << index)) != 0;
    public bool GetBitResult(int index) => (Result & (1L << index)) != 0;

    private void ResetShiftChain() => _shiftChainActive = false;

    private void CaptureShiftBase()
    {
        _shiftBaseValue = OperandA & BitwiseService.GetMask(BitWidth);
        _shiftBaseCaptured = true;
    }

    private void RestoreShiftBaseToResult()
    {
        Result = BitwiseService.NormalizeResult(_shiftBaseValue, BitWidth, IsSigned);
    }

    public void ResetShiftFromA()
    {
        if (!BitwiseService.IsShiftOp(SelectedOperation)) return;
        if (!_shiftBaseCaptured)
            CaptureShiftBase();
        ResetShiftChain();
        RestoreShiftBaseToResult();
    }

    private void OnShiftInputsChanged()
    {
        if (BitwiseService.IsShiftOp(SelectedOperation))
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

        Result = BitwiseService.ApplyShift(
            SelectedOperation,
            OperandA,
            Result,
            OperandC,
            BitWidth,
            IsSigned,
            fromResult);

        _shiftChainActive = true;
    }

    private void Calculate()
    {
        if (BitwiseService.IsShiftOp(SelectedOperation))
        {
            if (!_shiftChainActive)
                ApplyShift(fromResult: false);
            return;
        }

        var computed = BitwiseService.Compute(
            SelectedOperation,
            OperandA,
            OperandB,
            OperandC,
            BitWidth,
            IsSigned,
            Result,
            _shiftChainActive);

        if (computed.Success)
            Result = computed.Value!.Result;
    }

    public void SetOperation(string op) => SelectedOperation = op;

    public void SetOperation2(string op)
    {
        if (SelectedOperation != op)
            SelectedOperation = op;
        else
            ApplyShift(fromResult: _shiftChainActive);
    }
}
