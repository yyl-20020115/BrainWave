using System.IO.Ports;
using System.Windows;
using System.Windows.Threading;
using ScottPlot.Plottables;

namespace BrainWave;


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    protected SerialPort? port = null;
    protected DispatcherTimer timer = new();

    private readonly DataStreamer PoorSignalStreamer;

    private readonly DataStreamer BrainWaveStreamer;
    private readonly DataStreamer AttentionStreamer;
    private readonly DataStreamer HeartRateStreamer;
    private readonly DataStreamer MediationStreamer;

    private readonly DataStreamer DeltaStreamer;
    private readonly DataStreamer ThetaStreamer;
    private readonly DataStreamer LowAlphaStreamer;
    private readonly DataStreamer HighAlphaStreamer;
    private readonly DataStreamer LowBetaStreamer;
    private readonly DataStreamer HighBetaStreamer;
    private readonly DataStreamer LowGammaStreamer;
    private readonly DataStreamer MiddleGammaStreamer;


    private const int StreamLength = 800;
    private const int SlowStreamLength = 20;

    public class ListItem(int ComPort = -1)
    {
        public static ListItem Parse(string? name)
            => new(!string.IsNullOrEmpty(name)
                && name.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(name[3..], out var n) ? n : -1);
        public readonly int ComPort = ComPort;
        public override string ToString()
            => this.ComPort >= 0 ? $"COM{this.ComPort}" : string.Empty;
    }

    public MainWindow()
    {
        InitializeComponent();


        this.BrainWaveStreamer = this.BrainWavePlot.Plot.Add.DataStreamer(StreamLength);
        this.BrainWaveStreamer.LegendText = "Wave";
        this.BrainWaveStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.BrainWaveStreamer, true);


        this.PoorSignalStreamer = this.DataPlot.Plot.Add.DataStreamer(StreamLength);
        this.PoorSignalStreamer.LegendText = "PoorSignal";

        this.PoorSignalStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.PoorSignalStreamer, true);


        this.AttentionStreamer = this.DataPlot.Plot.Add.DataStreamer(StreamLength);
        this.AttentionStreamer.LegendText = "Attention";
        this.AttentionStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.AttentionStreamer, true);

        this.HeartRateStreamer = this.DataPlot.Plot.Add.DataStreamer(StreamLength);
        this.HeartRateStreamer.LegendText = "Heart Rate";
        this.HeartRateStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.HeartRateStreamer, true);

        this.MediationStreamer = this.DataPlot.Plot.Add.DataStreamer(StreamLength);
        this.MediationStreamer.LegendText = "Mediation";
        this.MediationStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.MediationStreamer, true);



        this.DeltaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.DeltaStreamer.LegendText = "Delta";
        this.DeltaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.DeltaStreamer, true);

        this.ThetaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.ThetaStreamer.LegendText = "Theta";
        this.ThetaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.ThetaStreamer, true);

        this.LowAlphaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.LowAlphaStreamer.LegendText = "LowAlpha";
        this.LowAlphaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.LowAlphaStreamer, true);

        this.HighAlphaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.HighAlphaStreamer.LegendText = "HighAlpha";
        this.HighAlphaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.HighAlphaStreamer, true);

        this.LowBetaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.LowBetaStreamer.LegendText = "LowBeta";
        this.LowBetaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.LowBetaStreamer, true);

        this.HighBetaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.HighBetaStreamer.LegendText = "HighBeta";
        this.HighBetaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.HighBetaStreamer, true);

        this.LowGammaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.LowGammaStreamer.LegendText = "LowGamma";
        this.LowGammaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.LowGammaStreamer, true);

        this.MiddleGammaStreamer = this.ParametersPlot.Plot.Add.DataStreamer(SlowStreamLength);
        this.MiddleGammaStreamer.LegendText = "MiddleGamma";
        this.MiddleGammaStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.MiddleGammaStreamer, true);


    }


    protected override void OnInitialized(EventArgs e)
    {

        this.OnUpdateList();
        this.timer.Interval = TimeSpan.FromMilliseconds(500);
        this.timer.Tick += Timer_Tick;
        this.timer.Start();
        base.OnInitialized(e);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        this.OnUpdateList();
    }
    protected virtual void OnUpdateList()
    {
        if (!this.StartButton.IsChecked.GetValueOrDefault())
        {
            var knowns = new List<ListItem>();
            foreach (var item in this.ComPortsList.Items)
                knowns.Add(ListItem.Parse(item.ToString()) ?? new());

            var founds = SerialPort
                .GetPortNames()
                .Select(name => ListItem.Parse(name))
                .ToList()
                ;

            if (!Enumerable.SequenceEqual(
                founds.OrderBy(i => i.ComPort),
                knowns.OrderBy(i => i.ComPort)))
            {
                var selection =
                    (this.ComPortsList.SelectedItem as ListItem)?.ComPort ?? -1;
                this.ComPortsList.Items.Clear();
                if (founds.Count > 0)
                {
                    foreach (var item in founds)
                        this.ComPortsList.Items.Add(item);
                    this.ComPortsList.SelectedItem
                        = selection == -1
                        ? founds[0]
                        : (object?)founds.FirstOrDefault(i => i.ComPort == selection)
                        ;
                }
            }
        }
    }
    //protected readonly 
    private readonly PacketParser parser = new();
    private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (this.port != null && e.EventType == SerialData.Chars)
        {
            int b;
            while (this.port.IsOpen && (b = this.port.ReadByte()) != -1)
            {
                switch (parser.ParseByte((byte)b))
                {
                    case ParseResult.InProgress:
                        break;
                    case ParseResult.Complete:
                        {
                            BrainWaveStreamer.Add(parser.RawWaveData);
                            this.BrainWavePlot.Refresh();
                        }

                        {
                            PoorSignalStreamer.Add(parser.PoorSignal);
                            AttentionStreamer.Add(parser.Attention);
                            HeartRateStreamer.Add(parser.HeartRate);
                            MediationStreamer.Add(parser.Mediation);
                            this.DataPlot.Refresh();
                            this.BrainWavePlot.Refresh();
                        }
                        if (parser.IsStatPacket)
                        {
                            DeltaStreamer.Add(parser.Delta);
                            ThetaStreamer.Add(parser.Theta);
                            LowAlphaStreamer.Add(parser.LowAlpha);
                            HighAlphaStreamer.Add(parser.HighAlpha);
                            LowBetaStreamer.Add(parser.LowBeta);
                            HighBetaStreamer.Add(parser.HighBeta);
                            LowGammaStreamer.Add(parser.LowGamma);
                            MiddleGammaStreamer.Add(parser.MiddleGamma);

                            this.ParametersPlot.Refresh();
                        }

                        return;
                    case ParseResult.CheckSumError:
                        //Debug.WriteLine("BAD");
                        return;
                    default:
                        break;
                }
            }
        }
    }

    private void StartButton_Checked(object sender, RoutedEventArgs e)
    {
        if (this.port != null)
        {
            this.port.Close();
            this.port.Dispose();
            this.port = null;
        }
        try
        {
            this.port = new SerialPort(ComPortsList.Text, 57600, Parity.None, 8, StopBits.One)
            {
                ReadBufferSize = 8 * 512 + 0
            };
            this.port.DataReceived += Port_DataReceived;

            this.port.Open();
        }
        catch (Exception ex)
        {
            this.port = null;
            MessageBox.Show(ex.Message, "ERROR");
            this.StartButton.IsChecked = false;
        }
    }

    private void StartButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (this.port != null)
        {
            this.port.Close();
            this.port.Dispose();
            this.port = null;
        }
    }
}
