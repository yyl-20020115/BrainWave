using ScottPlot.Plottables;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Xml.Linq;
namespace BrainWave;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    protected SerialPort? port = null;
    protected DispatcherTimer timer = new();

    private readonly DataStreamer SignalStreamer;

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


    private const int StreamLength = 1000;
    private const int SlowStreamLength = 20;
    public MainWindow()
    {
        InitializeComponent();
        
        this.BrainWavePlot.Plot.Axes.SetLimitsY(-1.5, 1.5);

        this.BrainWaveStreamer = this.BrainWavePlot.Plot.Add.DataStreamer(StreamLength);
        this.BrainWaveStreamer.LegendText = "Wave";
        this.BrainWaveStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.BrainWaveStreamer, true);


        this.SignalStreamer = this.DataPlot.Plot.Add.DataStreamer(StreamLength);
        this.SignalStreamer.LegendText = "Signal";
        
        this.SignalStreamer.Renderer = new ScottPlot.DataViews.Scroll(this.SignalStreamer,true);


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
    protected class ListItem
    {
        public static ListItem Parse(string name)
            => !string.IsNullOrEmpty(name) && name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[3..], out var n)
                ? new ListItem { ComPort = n }
                : new ListItem();

        public int ComPort = -1;
        public override string ToString()
            => this.ComPort >= 0 ? $"COM{this.ComPort}" : string.Empty;
    }
    protected virtual void OnUpdateList()
    {
        if (!this.StartButton.IsChecked.GetValueOrDefault())
        {
            var items = SerialPort.GetPortNames()
                .Select(name=>ListItem.Parse(name)).ToList();
            var olds = new List<ListItem>();
            foreach (var item in this.ComPortsList.Items)
                olds.Add(ListItem.Parse(item.ToString() ?? string.Empty)??new ListItem());
            
            var do_update = !Enumerable.SequenceEqual(
                items.OrderBy(i=>i.ComPort), 
                olds.OrderBy(i=>i.ComPort));

            if (do_update)
            {
                var selection = 
                    (this.ComPortsList.SelectedItem as ListItem)?.ComPort??-1;

                this.ComPortsList.Items.Clear();
                if (items.Count > 0) {
                    foreach (var item in items)
                        this.ComPortsList.Items.Add(item);

                    if (selection == -1)
                    {
                        this.ComPortsList.SelectedIndex = 0;
                    }
                    else
                    {
                        this.ComPortsList.SelectedItem 
                            = items.FirstOrDefault(i => i.ComPort == selection);
                    }
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
                            SignalStreamer.Add(parser.Signal);
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

        this.port = new SerialPort(ComPortsList.Text, 57600, Parity.None, 8, StopBits.One);
        this.port.ReadBufferSize = 8 * 512 + 0;
        this.port.DataReceived += Port_DataReceived;
        this.port.Open();
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
