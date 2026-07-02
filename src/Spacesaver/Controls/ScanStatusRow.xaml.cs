using Microsoft.UI.Xaml.Controls;

namespace Spacesaver.Controls;

public sealed partial class ScanStatusRow : UserControl
{
    public ScanStatusRow()
    {
        InitializeComponent();
    }

    public ProgressRing ScanRing => ScanProgressRing;

    public TextBlock StatusText => ScanStatusText;

    public Button CancelButton => CancelScanButton;

    public Button RescanButtonControl => RescanButton;
}
