using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;
using System.Windows.Documents;

namespace annotationTool;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// 

/*
public class HeaderInfo {
    public string? Preamble { get; private set; }
    public string? Manufacturer { get; private set; }
    public byte[]? Data { get; private set; }

    public void SetValues(string preamble, string manufacturer, byte[] data) {
        Preamble = preamble;
        Manufacturer = manufacturer;
        Data = data;
    }
}
*/

public partial class MainWindow : Window {
    public const int BLOCK_SIZE = 250 * 10;

    private int pageSize;
    private int currentPage;
    private int ecg_ymin;
    private int ecg_ymax;
    private int ecg_yrate;
    private int zoom_ecg_ymin;
    private int zoom_ecg_ymax;
    private int zoom_ecg_yrate;
    private int? zoom_block = null;

    //private int[] annotations;
    public ObservableCollection<AnnotationLabel> annotationLabels { get; set; } = new ObservableCollection<AnnotationLabel>();
    private int currentId = 1;
    private int currentIdx = 1;

    private MwfReader? ECGData = null;
    private CsvReader? AnnotationData = null;
    private CsvReader? RRIData = null;

    private bool isDragging = false;
    private int nowBlock = 0;
    private bool openFile = false;

    private PlotModel RRImodel;
    private int RRIymin = 0;
    private int RRIymax = 150;

    private ModelAdministrater modelAdministrater = null;

    List<int> rri_x;
    List<int> rri_y;



    public MainWindow() {
        InitializeComponent();
        //メインウィン増のサイズを固定
        this.MinWidth = 1400;
        this.MinHeight = 950;
        pageSize = 0;
        currentPage = 0;
        InitECGGraph();
        InitZoomECGGraph();
        InitRRIGraph();
        var controller = new PlotController();
        controller.UnbindMouseWheel();
        controller.UnbindMouseDown(OxyMouseButton.Left);
        controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
        controller.UnbindKeyDown(OxyKey.Left);
        controller.UnbindKeyDown(OxyKey.Right);
        plt_ECG0.Controller = controller;
        plt_ECG1.Controller = controller;
        plt_ECG2.Controller = controller;
        plt_ECG3.Controller = controller;
        plt_ECG4.Controller = controller;
        plt_ECG5.Controller = controller;
        ecg_yrate = 100;
        zoom_ecg_yrate = 100;

        annotationLabels.Add(new AnnotationLabel { isSelected=false, labelName="Normal", color="#FFFFFF", id=0});
        annotationLabels.Add(new AnnotationLabel { isSelected=true, labelName="AF", color="#FFC0CB", id=1 });
        annotationLabels.Add(new AnnotationLabel { isSelected=false, labelName="Noise", color="#B0B0B0", id=2 });
        annotationLabels.Add(new AnnotationLabel { isSelected = false, labelName = "marginal", color = "#B5BAFF", id = 3 });
        annotationLabels.Add(new AnnotationLabel { isSelected = false, labelName = "AT", color = "#D094F9", id = 4 });
        annotationLabels.Add(new AnnotationLabel { isSelected = false, labelName = "PVC", color = "#D5F994", id = 5 });
        annotationLabels.Add(new AnnotationLabel { isSelected = false, labelName = "PAC", color = "#F9F794", id = 6 });
        dgd_labels.ItemsSource = annotationLabels;

    }





    private void OpenFileButton_Click(object sender, RoutedEventArgs e) {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Text files (*.mwf)|*.mwf|All files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true) {
            string filePath = openFileDialog.FileName;
            try {
                ECGData = new MwfReader(filePath);
                //filePath + "_rpeaks.csv"が存在するか確認
                if (System.IO.File.Exists(filePath + "_rpeaks.csv")) {
                    RRIData = new CsvReader(filePath + "_rpeaks.csv");
                }
                AnnotationData = new CsvReader(filePath+".csv");
                pageSize = (ECGData.Signal.Length / 450000);
                currentPage = 0;
                lbl_page.Content = "1 / " + (pageSize + 1).ToString();
                btn_nextPage.IsEnabled = true;
                txb_page.IsEnabled = true;
                txb_page.Text = "1";
                btn_pageJump.IsEnabled = true;
                txb_ymin.IsEnabled = true;
                ecg_ymin = 1500;
                zoom_ecg_ymin = 1500;
                txb_ymax.IsEnabled = true;
                ecg_ymax = 2200;
                zoom_ecg_ymax = 2200;
                txb_date.IsEnabled = true;
                btn_ymaxup.IsEnabled = true;
                btn_ymaxdown.IsEnabled = true;
                btn_yminup.IsEnabled = true;
                btn_ymindown.IsEnabled = true;
                txb_rriymax.IsEnabled = true;
                txb_rriymin.IsEnabled = true;

                int[] annotations = new int[(ECGData.Signal.Length / 2500)+1];
                if (AnnotationData.isExist) {
                    foreach (DataRow row in AnnotationData.dataTable.Rows) {
                        //2列目の値から3列目の値までの範囲を添え字とするannotationsに1列目の値を代入
                        int start = int.Parse(row[1].ToString());
                        int end = int.Parse(row[2].ToString());
                        for (int i = start; i <= end; i++) {
                            if (int.Parse(row[0].ToString()) != -1) {
                                annotations[i] = int.Parse(row[0].ToString());
                            }
                        }
                    }
                } else {
                    for (int i = 0; i < annotations.Length; i++) {
                        annotations[i] = 0;
                    }
                }
                
                dgd_labels.IsEnabled = true;

                plt_ECG0.IsEnabled = true;
                plt_ECG1.IsEnabled = true;
                plt_ECG2.IsEnabled = true;
                plt_ECG3.IsEnabled = true;
                plt_ECG4.IsEnabled = true;
                plt_ECG5.IsEnabled = true;
                modelAdministrater = new ModelAdministrater(ECGData.Signal, annotations, annotationLabels, pageSize);
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                PlotECGGraph(pm);
                PlotECGTime();
                if (openFile) {
                    InitRRIGraph();
                }
                //CreateRRImodel();
                //RRIData.dataTableの1列目と3列目をint型のリストに変換
                if (RRIData != null) {
                    rri_x = RRIData.dataTable.AsEnumerable()
                                       .Select(row => row[0]?.ToString()) // 1列目を文字列として取得
                                       .Where(value => int.TryParse(value, out _)) // 数値に変換可能な値だけをフィルタリング
                                       .Select(value => int.Parse(value)) // 安全に int に変換
                                       .ToList();
                    rri_y = RRIData.dataTable.AsEnumerable()
                                       .Select(row => row[2]?.ToString()) // 3列目を文字列として取得
                                       .Where(value => int.TryParse(value, out _)) // 数値に変換可能な値だけをフィルタリング
                                       .Select(value => int.Parse(value)) // 安全に int に変換
                                       .ToList();
                }
                PlotRRIGraph(0, 180);
                mni_savefile.IsEnabled = true;
                mni_savefile2.IsEnabled = true;
                openFile = true;

            } catch (Exception ex) {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
            }
        }
    }



    /*
    private PlotModel CreatePlotModel(int startBlock, int endBlock) {
        var Model = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            //PlotMargins = new OxyThickness(-7, -10, -7, -10),
            PlotMargins = new OxyThickness(0, -10, 0, -10),
            Background = OxyColors.Transparent
        };

        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            //Maximum = 75000,
            Maximum = 3750,
            //MajorStep = 15000,
            MajorStep = 750,
            //MinorStep = 2500,
            MinorStep = 125,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MajorGridlineThickness = 1.5,
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MinorGridlineThickness = 0.5,
            TickStyle = TickStyle.None,
            LabelFormatter = (x) => string.Empty,
            IsZoomEnabled = false,
            IsPanEnabled = false,
        };
        Model.Axes.Add(xAxis);

        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Minimum = ecg_ymin,
            Maximum = ecg_ymax,
            IsAxisVisible = false,
        };
        Model.Axes.Add(yAxis);

        var lineSeries = new LineSeries()
        {
            Color = OxyColor.Parse("#1E90FF"), //折線グラフの色
            StrokeThickness = 0.75, //折線グラフの太さ
        };
        int k = 0;
        int startIdx = startBlock * 2500;
        int endIdx = endBlock * 2500;
        for (int i = startIdx; i < endIdx; i = i +20) {
            if (i >= ECGData.Signal.Length) {
                break;
            }
            lineSeries.Points.Add(new DataPoint(k, ECGData.Signal[i]));
            k += 1;
            
            
        }
        Model.Series.Add(lineSeries);

        
        int j = 0;
        for (int i = startBlock; i < endBlock; i++) {
            if (i >= annotations.Length) {
                break;
            }
            var matchedLabel = annotationLabels.FirstOrDefault(label => label.id == annotations[i]);
            Model.Annotations.Add(new RectangleAnnotation()
            {
                //MinimumX = j * 2500,
                //MaximumX = (j + 1) * 2500,
                MinimumX = j * 250,
                MaximumX = (j + 1) * 250,
                MinimumY = ecg_ymin,
                MaximumY = ecg_ymax,
                Fill = OxyColor.Parse(matchedLabel.color),
                Layer = AnnotationLayer.BelowAxes,
            });
            j += 1;
        }
        
        return Model;
    }



    private void PlotECGGraph() {
        int startBlock = currentPage * 180;
        plt_ECG0.Model = CreatePlotModel(startBlock, startBlock + 30);
        plt_ECG0.InvalidatePlot(true);
        plt_ECG1.Model = CreatePlotModel(startBlock + 30, startBlock + 60);
        plt_ECG1.InvalidatePlot(true);
        plt_ECG2.Model = CreatePlotModel(startBlock + 60, startBlock + 90);
        plt_ECG2.InvalidatePlot(true);
        plt_ECG3.Model = CreatePlotModel(startBlock + 90, startBlock + 120);
        plt_ECG3.InvalidatePlot(true);
        plt_ECG4.Model = CreatePlotModel(startBlock + 120, startBlock + 150);
        plt_ECG4.InvalidatePlot(true);
        plt_ECG5.Model = CreatePlotModel(startBlock + 150, startBlock + 180);
        plt_ECG5.InvalidatePlot(true);
    }
    */
    private void PlotECGGraph(PageModels pm) {
        plt_ECG0.Model = pm.ecg0;
        plt_ECG0.InvalidatePlot(true);
        plt_ECG1.Model = pm.ecg1;
        plt_ECG1.InvalidatePlot(true);
        plt_ECG2.Model = pm.ecg2;
        plt_ECG2.InvalidatePlot(true);
        plt_ECG3.Model = pm.ecg3;
        plt_ECG3.InvalidatePlot(true);
        plt_ECG4.Model = pm.ecg4;
        plt_ECG4.InvalidatePlot(true);
        plt_ECG5.Model = pm.ecg5;
        plt_ECG5.InvalidatePlot(true);
    }



    private void PlotECGTime() {
        DateTime recordingTime = (DateTime)ECGData.MetaData["Recording time"];
        int startTime = currentPage * 30;
        txb_date.Text = recordingTime.AddMinutes(startTime).ToString("yyyy/MM/dd");
        lbl_time0.Content = recordingTime.AddMinutes(startTime).ToString("HH:mm:ss") + "\n      |\n" + recordingTime.AddMinutes(startTime+5).ToString("HH:mm:ss");
        lbl_time1.Content = recordingTime.AddMinutes(startTime+5).ToString("HH:mm:ss") + "\n      |\n" + recordingTime.AddMinutes(startTime+10).ToString("HH:mm:ss");
        lbl_time2.Content = recordingTime.AddMinutes(startTime+10).ToString("HH:mm:ss") + "\n      |\n" + recordingTime.AddMinutes(startTime+15).ToString("HH:mm:ss");
        lbl_time3.Content = recordingTime.AddMinutes(startTime+15).ToString("HH:mm:ss") + "\n      |\n" + recordingTime.AddMinutes(startTime+20).ToString("HH:mm:ss");
        lbl_time4.Content = recordingTime.AddMinutes(startTime+20).ToString("HH:mm:ss") + "\n      |\n" + recordingTime.AddMinutes(startTime+25).ToString("HH:mm:ss");
        lbl_time5.Content = recordingTime.AddMinutes(startTime+25).ToString("HH:mm:ss") + "\n      |\n" + recordingTime.AddMinutes(startTime+30).ToString("HH:mm:ss");
    }


    /*
    private void CreateRRImodel() {
        var scatterSeries = new ScatterSeries()
        {
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.Parse("#1E90FF"),
        };

        foreach (DataRow row in RRIData.dataTable.Rows) {
            int x = int.Parse(row[0].ToString());
            int y = int.Parse(row[2].ToString());
            scatterSeries.Points.Add(new ScatterPoint(x, y));
        }
        RRImodel.Series.Add(scatterSeries);
    }




    private void PlotRRIGraph() {
        int startBlock = currentPage * 180;
        int endBlock = startBlock + 180;
        // 既存のX軸を取得して変更
        var existingXAxis = RRImodel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
        if (existingXAxis != null) {
            existingXAxis.Minimum = startBlock*2500; // 新しい最小値
            existingXAxis.Maximum = (endBlock+1)*2500; // 新しい最大値
        }

        // 既存のY軸を取得して変更
        var existingYAxis = RRImodel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
        if (existingYAxis != null) {
            existingYAxis.Minimum = RRIymin; // 新しい最小値
            existingYAxis.Maximum = RRIymax; // 新しい最大値
        }
        plt_RRI.InvalidatePlot(true);
    }
    */
    private void PlotRRIGraph(int startBlock, int endBlock) {
        int startIdx = startBlock * 2500;
        int endIdx = (endBlock + 1) * 2500;

        if (RRIData == null) {
            return;
        }

        RRImodel = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            PlotMargins = new OxyThickness(5, -10, 15, -10),
        };
        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 4500,
            MajorStep = 750,
            MinorStep = 150,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MajorGridlineThickness = 1.5,
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MinorGridlineThickness = 0.5,
            TickStyle = TickStyle.None,
            LabelFormatter = (x) => string.Empty,
            IsZoomEnabled = false,
            IsPanEnabled = false,
        };
        RRImodel.Axes.Add(xAxis);

        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Minimum = RRIymin,
            Maximum = RRIymax,
            IsAxisVisible = false,
        };
        RRImodel.Axes.Add(yAxis);

        var scatterSeries = new ScatterSeries()
        {
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.Parse("#1E90FF"),
        };


        //DataRow[] filtered = RRIData.dataTable.Select($"Convert(Rpeaks, 'System.Int32') >= {startIdx} AND Convert(Rpeaks, 'System.Int32') < {endIdx}");
        List<int> indices = rri_x
            .Select((value, index) => new { value, index }) // 値とインデックスをペアにする
            .Where(pair => pair.value >= startIdx && pair.value < endIdx) // 条件を満たすペアをフィルタリング
            .Select(pair => pair.index) // インデックスだけを取得
            .ToList();


        int i = 0;
        /*
        foreach (DataRow row in filtered) {
            int y = int.Parse(row[2].ToString());
            if (i%3 != 0 || (55 <= y && y <= 65)) {
                int x = (int.Parse(row[0].ToString()) - startIdx) / 100;
                scatterSeries.Points.Add(new ScatterPoint(x, y));
            }   
            i = i + 1;
        }
        */
        foreach (int index in indices) {
            int y = rri_y[index];
            if (i % 10 != 0 || (50 <= y && y <= 70)) {
                int x = (rri_x[index] - startIdx) / 100;
                scatterSeries.Points.Add(new ScatterPoint(x, y));
            }
            i = i + 1;
        }
        RRImodel.Series.Add(scatterSeries);
        plt_RRI.Model = RRImodel;
    }



    private void InitRRIGraph() {
        RRImodel = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            PlotMargins = new OxyThickness(5, -10, 15, -10),
        };
        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 450,
            MajorStep = 75,
            MinorStep = 15,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MajorGridlineThickness = 1.5,
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MinorGridlineThickness = 0.5,
            TickStyle = TickStyle.None,
            LabelFormatter = (x) => string.Empty,
            IsZoomEnabled = false,
            IsPanEnabled = false,
        };
        RRImodel.Axes.Add(xAxis);

        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Minimum = RRIymin,
            Maximum = RRIymax,
            IsAxisVisible = false,
        };
        RRImodel.Axes.Add(yAxis);
        plt_RRI.Model = RRImodel;
        //plt_RRI.InvalidatePlot(true);
    }



    private PlotModel InitPlotModel() {
        var Model = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            PlotMargins = new OxyThickness(10, -10, 10, -10),
        };

        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 3750,
            MajorStep = 750,
            MinorStep = 125,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MajorGridlineThickness = 1.5,
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MinorGridlineThickness = 0.5,
            TickStyle = TickStyle.None,
            LabelFormatter = (x) => string.Empty,
            IsZoomEnabled = false,
            IsPanEnabled = false,
        };
        Model.Axes.Add(xAxis);

        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = 100,
            IsAxisVisible = false,
        };
        Model.Axes.Add(yAxis);
        return Model;
    }



    private void InitECGGraph() {
        plt_ECG0.Model = InitPlotModel();
        plt_ECG0.InvalidatePlot(true);
        plt_ECG1.Model = InitPlotModel();
        plt_ECG1.InvalidatePlot(true);
        plt_ECG2.Model = InitPlotModel();
        plt_ECG2.InvalidatePlot(true);
        plt_ECG3.Model = InitPlotModel();
        plt_ECG3.InvalidatePlot(true);
        plt_ECG4.Model = InitPlotModel();
        plt_ECG4.InvalidatePlot(true);
        plt_ECG5.Model = InitPlotModel();
        plt_ECG5.InvalidatePlot(true);
    }



    private void InitZoomECGGraph() {
        var Model = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            PlotMargins = new OxyThickness(10, -10, 10, -10),
        };

        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 2500,
            MajorStep = 2500,
            MinorStep = 250,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MajorGridlineThickness = 1.5,
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MinorGridlineThickness = 0.5,
            TickStyle = TickStyle.None,
            LabelFormatter = (x) => string.Empty,
            IsZoomEnabled = false,
            IsPanEnabled = false,
        };
        Model.Axes.Add(xAxis);

        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = 100,
            IsAxisVisible = false,
        };
        Model.Axes.Add(yAxis);
        plt_zoomECG.Model = Model;
        plt_zoomECG.InvalidatePlot(true);
    }



    private void btn_previousPage_Click(object sender, RoutedEventArgs e) {
        currentPage -= 1;
        if (currentPage == 0) {
            btn_previousPage.IsEnabled = false;
        }
        if (currentPage < pageSize) {
            btn_nextPage.IsEnabled = true;
        }
        lbl_page.Content = (currentPage + 1).ToString() + " / " + (pageSize + 1).ToString();
        PageModels pm = modelAdministrater.GetPageModels(currentPage);
        PlotECGGraph(pm);
        PlotECGTime();
        int startBlock = currentPage * 180;
        PlotRRIGraph(startBlock, startBlock+180);
        modelAdministrater.Update_PreviousPage();
    }



    private void btn_nextPage_Click(object sender, RoutedEventArgs e) {
        currentPage += 1;
        if (currentPage == pageSize) {
            btn_nextPage.IsEnabled = false;
        }
        if (currentPage > 0) {
            btn_previousPage.IsEnabled = true;
        }
        lbl_page.Content = (currentPage + 1).ToString() + " / " + (pageSize + 1).ToString();
        PageModels pm = modelAdministrater.GetPageModels(currentPage);
        PlotECGGraph(pm);
        PlotECGTime();
        int startBlock = currentPage * 180;
        PlotRRIGraph(startBlock, startBlock + 180);
        modelAdministrater.Update_NextPage();
    }



    private void txb_page_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        //入力された文字が数字でないなら打たれた文字のみ削除
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_page.Text, "^[0-9]+$")) {
            txb_page.Text = "";
        }
    }



    private void btn_pageJump_Click(object sender, RoutedEventArgs e) {
        //txb_pageに入力されたページ数にジャンプ
        int page = int.Parse(txb_page.Text);
        if (page > 0 && page <= pageSize + 1) {
            currentPage = page - 1;
            if (currentPage == 0) {
                btn_previousPage.IsEnabled = false;
            } else {
                btn_previousPage.IsEnabled = true;
            }
            if (currentPage == pageSize) {
                btn_nextPage.IsEnabled = false;
            } else {
                btn_nextPage.IsEnabled = true;
            }
            lbl_page.Content = (currentPage + 1).ToString() + " / " + (pageSize + 1).ToString();
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            PlotECGGraph(pm);
            PlotECGTime();
            int startBlock = currentPage * 180;
            PlotRRIGraph(startBlock, startBlock + 180);
            modelAdministrater.Update_Jump();
        }
    }


    private void txb_ymin_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_ymin.Text, "^[0-9]+$")) {
            txb_ymin.Text = "";
        }
    }


    private void txb_ymax_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_ymax.Text, "^[0-9]+$")) {
            txb_ymax.Text = "";
        }
    }


    private void txb_ymax_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            int ymin = int.Parse(txb_ymin.Text);
            int ymax = int.Parse(txb_ymax.Text);
            if (ymin < ymax) {
                ecg_ymin = ymin;
                ecg_ymax = ymax;
                PageModels pm = modelAdministrater.UpdateGet_Scale(ecg_ymin, ecg_ymax);
                PlotECGGraph(pm);
            }else {
                txb_ymin.Text = ecg_ymin.ToString();
                txb_ymax.Text = ecg_ymax.ToString();
            }
        }
    }

    private void txb_ymin_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            int ymin = int.Parse(txb_ymin.Text);
            int ymax = int.Parse(txb_ymax.Text);
            if (ymin < ymax) {
                ecg_ymin = ymin;
                ecg_ymax = ymax;
                PageModels pm = modelAdministrater.UpdateGet_Scale(ecg_ymin, ecg_ymax);
                PlotECGGraph(pm);
            }else {
                txb_ymin.Text = ecg_ymin.ToString();
                txb_ymax.Text = ecg_ymax.ToString();
            }
        }
    }

    private void txb_ymax_LostFocus(object sender, RoutedEventArgs e) {
        txb_ymax.Text = ecg_ymax.ToString();
    }

    private void txb_ymin_LostFocus(object sender, RoutedEventArgs e) {
        txb_ymin.Text = ecg_ymin.ToString();
    }



    private void txb_rriymin_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_rriymin.Text, "^[0-9]+$")) {
            txb_ymin.Text = "";
        }
    }

    private void txb_rriymax_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_rriymax.Text, "^[0-9]+$")) {
            txb_ymax.Text = "";
        }
    }

    private void txb_rriymax_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            int ymin = int.Parse(txb_rriymin.Text);
            int ymax = int.Parse(txb_rriymax.Text);
            if (ymin < ymax) {
                RRIymin = ymin;
                RRIymax = ymax;
                int startBlock = currentPage * 180;
                PlotRRIGraph(startBlock, startBlock + 180);
            } else {
                txb_rriymin.Text = RRIymin.ToString();
                txb_rriymax.Text = RRIymax.ToString();
            }
        }
    }

    private void txb_rriymin_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            int ymin = int.Parse(txb_rriymin.Text);
            int ymax = int.Parse(txb_rriymax.Text);
            if (ymin < ymax) {
                RRIymin = ymin;
                RRIymax = ymax;
                int startBlock = currentPage * 180;
                PlotRRIGraph(startBlock, startBlock + 180);
            } else {
                txb_rriymin.Text = RRIymin.ToString();
                txb_rriymax.Text = RRIymax.ToString();
            }
        }
    }

    private void txb_rriymax_LostFocus(object sender, RoutedEventArgs e) {
        txb_rriymax.Text = RRIymax.ToString();
    }

    private void txb_rriymin_LostFocus(object sender, RoutedEventArgs e) {
        txb_rriymin.Text = RRIymin.ToString();
    }


    private void CreateZoomBlock(int block) {
        int k = 0;
        int margin = 50;
        int min = zoom_ecg_ymin;
        int max = zoom_ecg_ymax;
        for (int i = 2500 * block; i < 2500 * (block + 1); i++) {
            if (i >= ECGData.Signal.Length) {
                break;
            }
            if (k == 0) {
                min = ECGData.Signal[i] - margin;
                max = ECGData.Signal[i] + margin;
            } else if (min > ECGData.Signal[i]-margin) {
                min = ECGData.Signal[i] - margin;
            } else if (max < ECGData.Signal[i]+margin) {
                max = ECGData.Signal[i] + margin;
            }
            k += 1;
        }
        zoom_block = block;
        zoom_ecg_ymin = min;
        zoom_ecg_ymax = max;
        txb_zoomymin.IsEnabled = true;
        txb_zoomymax.IsEnabled = true;
        txb_zoomymin.Text = zoom_ecg_ymin.ToString();
        txb_zoomymax.Text = zoom_ecg_ymax.ToString();
        btn_zoomymaxup.IsEnabled = true;
        btn_zoomymaxdown.IsEnabled = true;
        btn_zoomyminup.IsEnabled = true;
        btn_zoomymindown.IsEnabled = true;
        DateTime recordingTime = (DateTime)ECGData.MetaData["Recording time"];
        lbl_zoomtime.Content = "StartTime\n" + recordingTime.AddSeconds((block * 10)).ToString("yyyy/MM/dd\nHH:mm:ss");
    }

    private void PlotZoomECGGraph() {
        var Model = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            PlotMargins = new OxyThickness(10, -10, 10, -10),
        };

        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 2500,
            MajorStep = 2500,
            MinorStep = 250,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MajorGridlineThickness = 1.5,
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(128, 128, 128),
            MinorGridlineThickness = 0.5,
            TickStyle = TickStyle.None,
            LabelFormatter = (x) => string.Empty,
            IsZoomEnabled = false,
            IsPanEnabled = false,
        };
        Model.Axes.Add(xAxis);
        
        var yAxis = new LinearAxis()
        {
            Position = AxisPosition.Left,
            Minimum = zoom_ecg_ymin,
            Maximum = zoom_ecg_ymax,
            IsAxisVisible = false,
        };
        Model.Axes.Add(yAxis);

        var lineSeries = new LineSeries()
        {
            Color = OxyColor.Parse("#1E90FF"), //折線グラフの色
            StrokeThickness = 1, //折線グラフの太さ
        };
        int k = 0;
        for (int i = (int)(2500 * zoom_block); i < 2500 * (zoom_block + 1); i++) {
            if (i >= ECGData.Signal.Length) {
                break;
            }
            lineSeries.Points.Add(new DataPoint(k, ECGData.Signal[i]));
            k += 1;
        }
        Model.Series.Add(lineSeries);

        if (RRIData != null) {
            var scatterSeries = new ScatterSeries()
            {
                MarkerType = MarkerType.Diamond,
                MarkerSize = 2,
                MarkerFill = OxyColor.Parse("#FA0602"),
            };
            /*
            foreach (DataRow row in RRIData.dataTable.Rows) {
                if (int.Parse(row[0].ToString()) >= 2500 * zoom_block && int.Parse(row[0].ToString()) < 2500 * (zoom_block + 1)) {
                    int i = int.Parse(row[0].ToString());
                    int x = i - 2500 * (int)zoom_block;
                    scatterSeries.Points.Add(new ScatterPoint(x, ECGData.Signal[i]));
                } else if (int.Parse(row[0].ToString()) >= 2500 * (zoom_block + 1)) {
                    break;
                }
            }
            */
            List<int> indices = rri_x
                .Select((value, index) => new { value, index }) // 値とインデックスをペアにする
                .Where(pair => pair.value >= 2500 * zoom_block && pair.value < 2500 * (zoom_block + 1)) // 条件を満たすペアをフィルタリング
                .Select(pair => pair.index) // インデックスだけを取得
                .ToList();
            foreach (int index in indices) {
                int i = rri_x[index];
                int x = i - 2500 * (int)zoom_block;
                scatterSeries.Points.Add(new ScatterPoint(x, ECGData.Signal[i]));
            }

            Model.Series.Add(scatterSeries);
        }

        plt_zoomECG.Model = Model;
        plt_zoomECG.InvalidatePlot(true);
    }

    private void plt_ECG0_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG0);
        var plotModel = plt_ECG0.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + (x / 2500));
        var block = (int)((currentPage * 180) + (x / 500));
        CreateZoomBlock(block);
        PlotZoomECGGraph();
    }

    private void plt_ECG1_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG1);
        var plotModel = plt_ECG1.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 30 + (x / 2500));
        var block = (int)((currentPage * 180) + 30 + (x / 500));
        CreateZoomBlock(block);
        PlotZoomECGGraph();
    }

    private void plt_ECG2_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG2);
        var plotModel = plt_ECG2.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 60 + (x / 2500));
        var block = (int)((currentPage * 180) + 60 + (x / 500));
        CreateZoomBlock(block);
        PlotZoomECGGraph();
    }

    private void plt_ECG3_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG3);
        var plotModel = plt_ECG3.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 90 + (x / 2500));
        var block = (int)((currentPage * 180) + 90 + (x / 500));
        CreateZoomBlock(block);
        PlotZoomECGGraph();
    }

    private void plt_ECG4_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG4);
        var plotModel = plt_ECG4.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 120 + (x / 2500));
        var block = (int)((currentPage * 180) + 120 + (x / 500));
        CreateZoomBlock(block);
        PlotZoomECGGraph();
    }

    private void plt_ECG5_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG5);
        var plotModel = plt_ECG5.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 150 + (x / 2500));
        var block = (int)((currentPage * 180) + 150 + (x / 500));
        CreateZoomBlock(block);
        PlotZoomECGGraph();
    }

    private void txb_zoomymax_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_zoomymax.Text, "^[0-9]+$")) {
            txb_zoomymax.Text = "";
        }
    }

    private void txb_zoomymin_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
        if (!System.Text.RegularExpressions.Regex.IsMatch(txb_zoomymin.Text, "^[0-9]+$")) {
            txb_zoomymin.Text = "";
        }
    }

    private void txb_zoomymin_LostFocus(object sender, RoutedEventArgs e) {
        txb_zoomymin.Text = zoom_ecg_ymin.ToString();
    }

    private void txb_zoomymax_LostFocus(object sender, RoutedEventArgs e) {
        txb_zoomymax.Text = zoom_ecg_ymax.ToString();
    }

    private void txb_zoomymax_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            int ymin = int.Parse(txb_zoomymin.Text);
            int ymax = int.Parse(txb_zoomymax.Text);
            if (ymin < ymax) {
                zoom_ecg_ymin = ymin;
                zoom_ecg_ymax = ymax;
                PlotZoomECGGraph();
            } else {
                txb_zoomymin.Text = zoom_ecg_ymin.ToString();
                txb_zoomymax.Text = zoom_ecg_ymax.ToString();
            }
        }
    }

    private void txb_zoomymin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Enter) {
            int ymin = int.Parse(txb_zoomymin.Text);
            int ymax = int.Parse(txb_zoomymax.Text);
            if (ymin < ymax) {
                zoom_ecg_ymin = ymin;
                zoom_ecg_ymax = ymax;
                PlotZoomECGGraph();
            }else {
                txb_zoomymin.Text = zoom_ecg_ymin.ToString();
                txb_zoomymax.Text = zoom_ecg_ymax.ToString();
            }
        }
    }
    private void RadioButton_Checked(object sender, RoutedEventArgs e) {
        // Get the selected RadioButton
        var radioButton = (RadioButton)sender;

        // Get the selected AnnotationLabel
        var selectedLabel = (AnnotationLabel)radioButton.DataContext;

        // Update the selection
        foreach (var label in annotationLabels) {
            if (label == selectedLabel) {
                label.isSelected = true;
                currentId = label.id;
                currentIdx = annotationLabels.IndexOf(label);
            } else {
                label.isSelected = false;
            }
        }
    }

    private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        var grid = (DataGrid)sender;
        var pos = e.GetPosition(grid);
        var result = VisualTreeHelper.HitTest(grid, pos);
        if (result != null) {
            var cell = FindVisualParent<DataGridCell>(result.VisualHit);
            if (cell != null) {
                var row = FindVisualParent<DataGridRow>(cell);
                if (row != null) {
                    var label = (AnnotationLabel)row.Item;
                    label.isSelected = true;
                }
            }
        }
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject {
        var parent = VisualTreeHelper.GetParent(child);
        if (parent == null) return null;
        if (parent is T t) return t;
        return FindVisualParent<T>(parent);
    }


    private void plt_ECG0_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG0);
        var plotModel = plt_ECG0.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + (x / 2500));
        var block = (int)((currentPage * 180) + (x / 500));
        nowBlock = block;
        isDragging = true;
        if(block < modelAdministrater.annotations.Length) {
            modelAdministrater.annotations[block] = currentId;
            int startBlock = currentPage * 180;
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            pm.ecg0 = modelAdministrater.CreatePlotModel(startBlock, startBlock + 30);
            plt_ECG0.Model = pm.ecg0;
            plt_ECG0.InvalidatePlot(true);
            
        }
        
    }

    private void plt_ECG0_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        isDragging = false;
    }

    private void plt_ECG0_MouseMove(object sender, MouseEventArgs e) {
        if (isDragging) {
            var position = e.GetPosition(plt_ECG0);
            var plotModel = plt_ECG0.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + (x / 2500));
            var block = (int)((currentPage * 180) + (x / 500));
            if (block < modelAdministrater.annotations.Length && block != nowBlock) {
                modelAdministrater.annotations[block] = currentId;
                int startBlock = currentPage * 180;
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                pm.ecg0 = modelAdministrater.CreatePlotModel(startBlock, startBlock + 30);
                plt_ECG0.Model = pm.ecg0;
                plt_ECG0.InvalidatePlot(true);
                nowBlock = block;
            }
        }   
    }

    private void plt_ECG0_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
            var position = e.GetPosition(plt_ECG0);
            var plotModel = plt_ECG0.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + (x / 2500));
            var block = (int)((currentPage * 180) + (x / 500));
            if (block < modelAdministrater.annotations.Length) {
                do {
                    modelAdministrater.annotations[block] = currentId;
                    block -= 1;
                } while (block >= 0 && modelAdministrater.annotations[block] != currentId);
                PageModels pm = modelAdministrater.UpdateGet_Label();
                PlotECGGraph(pm);
            }
            
        }
    }

    private void plt_ECG1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG1);
        var plotModel = plt_ECG1.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 30 + (x / 2500));
        var block = (int)((currentPage * 180) + 30 + (x / 500));
        nowBlock = block;
        isDragging = true;
        if (block < modelAdministrater.annotations.Length) {
            modelAdministrater.annotations[block] = currentId;
            int startBlock = currentPage * 180;
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            pm.ecg1 = modelAdministrater.CreatePlotModel(startBlock + 30, startBlock + 60);
            plt_ECG1.Model = pm.ecg1;
            plt_ECG1.InvalidatePlot(true);
        }
    }

    private void plt_ECG1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
            var position = e.GetPosition(plt_ECG1);
            var plotModel = plt_ECG1.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 30 + (x / 2500));
            var block = (int)((currentPage * 180) + 30 + (x / 500));
            if (block < modelAdministrater.annotations.Length) {
                do {
                    modelAdministrater.annotations[block] = currentId;
                    block -= 1;
                } while (block >= 0 && modelAdministrater.annotations[block] != currentId);
                PageModels pm = modelAdministrater.UpdateGet_Label();
                PlotECGGraph(pm);
            }
        }
    }

    private void plt_ECG1_MouseMove(object sender, MouseEventArgs e) {
        if (isDragging) {
            var position = e.GetPosition(plt_ECG1);
            var plotModel = plt_ECG1.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 30 + (x / 2500));
            var block = (int)((currentPage * 180) + 30 + (x / 500));
            if (block < modelAdministrater.annotations.Length && block != nowBlock) {
                modelAdministrater.annotations[block] = currentId;
                int startBlock = currentPage * 180;
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                pm.ecg1 = modelAdministrater.CreatePlotModel(startBlock + 30, startBlock + 60);
                plt_ECG1.Model = pm.ecg1;
                plt_ECG1.InvalidatePlot(true);
                nowBlock = block;
            }
        }
    }

    private void plt_ECG2_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG2);
        var plotModel = plt_ECG2.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 60 + (x / 2500));
        var block = (int)((currentPage * 180) + 60 + (x / 500));
        nowBlock = block;
        isDragging = true;
        if (block < modelAdministrater.annotations.Length) {
            modelAdministrater.annotations[block] = currentId;
            int startBlock = currentPage * 180;
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            pm.ecg2 = modelAdministrater.CreatePlotModel(startBlock + 60, startBlock + 90);
            plt_ECG2.Model = pm.ecg2;
            plt_ECG2.InvalidatePlot(true);
        }
    }

    private void plt_ECG2_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
            var position = e.GetPosition(plt_ECG2);
            var plotModel = plt_ECG2.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 60 + (x / 2500));
            var block = (int)((currentPage * 180) + 60 + (x / 500));
            if (block < modelAdministrater.annotations.Length) {
                do {
                    modelAdministrater.annotations[block] = currentId;
                    block -= 1;
                } while (block >= 0 && modelAdministrater.annotations[block] != currentId);
                PageModels pm = modelAdministrater.UpdateGet_Label();
                PlotECGGraph(pm);
            }
        }
    }

    private void plt_ECG2_MouseMove(object sender, MouseEventArgs e) {
        if (isDragging) {
            var position = e.GetPosition(plt_ECG2);
            var plotModel = plt_ECG2.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 60 + (x / 2500));
            var block = (int)((currentPage * 180) + 60 + (x / 500));
            if (block < modelAdministrater.annotations.Length && block != nowBlock) {
                modelAdministrater.annotations[block] = currentId;
                int startBlock = currentPage * 180;
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                pm.ecg2 = modelAdministrater.CreatePlotModel(startBlock + 60, startBlock + 90);
                plt_ECG2.Model = pm.ecg2;
                plt_ECG2.InvalidatePlot(true);
                nowBlock = block;
            }
        }
    }

    private void plt_ECG3_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG3);
        var plotModel = plt_ECG3.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 90 + (x / 2500));
        var block = (int)((currentPage * 180) + 90 + (x / 500));
        nowBlock = block;
        isDragging = true;
        if (block < modelAdministrater.annotations.Length) {
            modelAdministrater.annotations[block] = currentId;
            int startBlock = currentPage * 180;
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            pm.ecg3 = modelAdministrater.CreatePlotModel(startBlock + 90, startBlock + 120);
            plt_ECG3.Model = pm.ecg3;
            plt_ECG3.InvalidatePlot(true);
        }
    }

    private void plt_ECG3_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
            var position = e.GetPosition(plt_ECG3);
            var plotModel = plt_ECG3.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 90 + (x / 2500));
            var block = (int)((currentPage * 180) + 90 + (x / 500));
            if (block < modelAdministrater.annotations.Length) {
                do {
                    modelAdministrater.annotations[block] = currentId;
                    block -= 1;
                } while (block >= 0 && modelAdministrater.annotations[block] != currentId);
                PageModels pm = modelAdministrater.UpdateGet_Label();
                PlotECGGraph(pm);
            }
        }
    }

    private void plt_ECG3_MouseMove(object sender, MouseEventArgs e) {
        if (isDragging) {
            var position = e.GetPosition(plt_ECG3);
            var plotModel = plt_ECG3.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 90 + (x / 2500));
            var block = (int)((currentPage * 180) + 90 + (x / 500));
            if (block < modelAdministrater.annotations.Length && block != nowBlock) {
                modelAdministrater.annotations[block] = currentId;
                int startBlock = currentPage * 180;
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                pm.ecg3 = modelAdministrater.CreatePlotModel(startBlock + 90, startBlock + 120);
                plt_ECG3.Model = pm.ecg3;
                plt_ECG3.InvalidatePlot(true);
                nowBlock = block;
            }
        }
    }

    private void plt_ECG4_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG4);
        var plotModel = plt_ECG4.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 120 + (x / 2500));
        var block = (int)((currentPage * 180) + 120 + (x / 500));
        nowBlock = block;
        isDragging = true;
        if (block < modelAdministrater.annotations.Length) {
            modelAdministrater.annotations[block] = currentId;
            int startBlock = currentPage * 180;
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            pm.ecg4 = modelAdministrater.CreatePlotModel(startBlock + 120, startBlock + 150);
            plt_ECG4.Model = pm.ecg4;
            //plt_ECG4.Model = CreatePlotModel(startBlock + 120, startBlock + 150);
            plt_ECG4.InvalidatePlot(true);
        }
    }

    private void plt_ECG4_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
            var position = e.GetPosition(plt_ECG4);
            var plotModel = plt_ECG4.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 120 + (x / 2500));
            var block = (int)((currentPage * 180) + 120 + (x / 500));
            if (block < modelAdministrater.annotations.Length) {
                do {
                    modelAdministrater.annotations[block] = currentId;
                    block -= 1;
                } while (block >= 0 && modelAdministrater.annotations[block] != currentId);
                PageModels pm = modelAdministrater.UpdateGet_Label();
                PlotECGGraph(pm);
            }
        }
    }

    private void plt_ECG4_MouseMove(object sender, MouseEventArgs e) {
        if (isDragging) {
            var position = e.GetPosition(plt_ECG4);
            var plotModel = plt_ECG4.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 120 + (x / 2500));
            var block = (int)((currentPage * 180) + 120 + (x / 500));
            if (block < modelAdministrater.annotations.Length && block != nowBlock) {
                modelAdministrater.annotations[block] = currentId;
                int startBlock = currentPage * 180;
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                pm.ecg4 = modelAdministrater.CreatePlotModel(startBlock + 120, startBlock + 150);
                plt_ECG4.Model = pm.ecg4;
                //plt_ECG4.Model = CreatePlotModel(startBlock + 120, startBlock + 150);
                plt_ECG4.InvalidatePlot(true);
                nowBlock = block;
            }
        }
    }

    private void plt_ECG5_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        var position = e.GetPosition(plt_ECG5);
        var plotModel = plt_ECG5.Model;
        var xAxis = plotModel.Axes[0];
        var x = xAxis.InverseTransform(position.X);
        //var block = (int)((currentPage * 180) + 150 + (x / 2500));
        var block = (int)((currentPage * 180) + 150 + (x / 500));
        nowBlock = block;
        isDragging = true;
        if (block < modelAdministrater.annotations.Length) {
            modelAdministrater.annotations[block] = currentId;
            int startBlock = currentPage * 180;
            PageModels pm = modelAdministrater.GetPageModels(currentPage);
            pm.ecg5 = modelAdministrater.CreatePlotModel(startBlock + 150, startBlock + 180);
            plt_ECG5.Model = pm.ecg5;
            //plt_ECG5.Model = CreatePlotModel(startBlock + 150, startBlock + 180);
            plt_ECG5.InvalidatePlot(true);
        }
    }

    private void plt_ECG5_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) {
            var position = e.GetPosition(plt_ECG5);
            var plotModel = plt_ECG5.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 150 + (x / 2500));
            var block = (int)((currentPage * 180) + 150 + (x / 500));
            if (block < modelAdministrater.annotations.Length) {
                do {
                    modelAdministrater.annotations[block] = currentId;
                    block -= 1;
                } while (block >= 0 && modelAdministrater.annotations[block] != currentId);
                PageModels pm = modelAdministrater.UpdateGet_Label();
                PlotECGGraph(pm);
            }
        }
    }

    private void plt_ECG5_MouseMove(object sender, MouseEventArgs e) {
        if (isDragging) {
            var position = e.GetPosition(plt_ECG5);
            var plotModel = plt_ECG5.Model;
            var xAxis = plotModel.Axes[0];
            var x = xAxis.InverseTransform(position.X);
            //var block = (int)((currentPage * 180) + 150 + (x / 2500));
            var block = (int)((currentPage * 180) + 150 + (x / 500));
            if (block < modelAdministrater.annotations.Length && block != nowBlock) {
                modelAdministrater.annotations[block] = currentId;
                int startBlock = currentPage * 180;
                PageModels pm = modelAdministrater.GetPageModels(currentPage);
                pm.ecg5 = modelAdministrater.CreatePlotModel(startBlock + 150, startBlock + 180);
                plt_ECG5.Model = pm.ecg5;
                //plt_ECG5.Model = CreatePlotModel(startBlock + 150, startBlock + 180);
                plt_ECG5.InvalidatePlot(true);
                nowBlock = block;
            }
        }
    }

    private void mni_savefile_Click(object sender, RoutedEventArgs e) {
        DataTable dt = new DataTable();
        string[] columnNames = { "ID", "Start", "End", "Label", "StartTime", "EndTime" };
        foreach (var columnName in columnNames) {
            dt.Columns.Add(columnName);
        }
        DataRow dr = dt.NewRow();
        dr[0] = -1;
        dr[1] = 0;
        dr[2] = modelAdministrater.annotations.Length - 1;
        dr[3] = "Start-End";
        dr[4] = ((DateTime)ECGData.MetaData["Recording time"]).ToString("yyyy/MM/dd HH:mm:ss");
        dr[5] = ((DateTime)ECGData.MetaData["Recording time"]).AddSeconds(modelAdministrater.annotations.Length * 10).ToString("yyyy/MM/dd HH:mm:ss");
        dt.Rows.Add(dr);
        dr = dt.NewRow();
        int id = 0;
        DateTime recordingTime = (DateTime)ECGData.MetaData["Recording time"];
        for (int i = 0; i < modelAdministrater.annotations.Length; i++) {
            if (modelAdministrater.annotations[i] != id) {
                if(id == 0) {
                    dr = dt.NewRow();
                    id = modelAdministrater.annotations[i];
                    dr[0] = id;
                    dr[1] = i;
                    foreach (var label in annotationLabels) {
                        if (label.id == id) {
                            dr[3] = label.labelName;
                        }
                    }
                    dr[4] = recordingTime.AddSeconds(i * 10).ToString("yyyy/MM/dd HH:mm:ss");
                } else {
                    dr[2] = i-1;
                    dr[5] = recordingTime.AddSeconds((i+1) * 10).ToString("yyyy/MM/dd HH:mm:ss");
                    dt.Rows.Add(dr);
                    id = modelAdministrater.annotations[i];
                    if(id != 0) {
                        dr = dt.NewRow();
                        dr[0] = id;
                        dr[1] = i;
                        foreach (var label in annotationLabels) {
                            if (label.id == id) {
                                dr[3] = label.labelName;
                            }
                        }
                        dr[4] = recordingTime.AddSeconds(i * 10).ToString("yyyy/MM/dd HH:mm:ss");
                    }                    
                }    
            }else if (i == modelAdministrater.annotations.Length - 1 && id != 0) {
                dr[2] = i;
                dr[5] = recordingTime.AddSeconds((i + 1) * 10).ToString("yyyy/MM/dd HH:mm:ss");
                dt.Rows.Add(dr);
            }
        }
        AnnotationData.SaveDataTableToCsv(dt);
    }


    private void mni_savefile2_Click(object sender, RoutedEventArgs e) {
        DataTable dt = new DataTable();
        string[] columnNames = { "ID", "Start", "End", "Label", "StartTime", "EndTime" };
        foreach (var columnName in columnNames) {
            dt.Columns.Add(columnName);
        }
        DataRow dr = dt.NewRow();
        int id = 0;
        DateTime recordingTime = (DateTime)ECGData.MetaData["Recording time"];
        for (int i = 0; i < modelAdministrater.annotations.Length; i++) {
            if (modelAdministrater.annotations[i] != id) {
                if (id == 0) {
                    dr = dt.NewRow();
                    id = modelAdministrater.annotations[i];
                    dr[0] = id;
                    dr[1] = i;
                    foreach (var label in annotationLabels) {
                        if (label.id == id) {
                            dr[3] = label.labelName;
                        }
                    }
                    dr[4] = recordingTime.AddSeconds(i * 10).ToString("yyyy/MM/dd HH:mm:ss");
                } else {
                    dr[2] = i - 1;
                    dr[5] = recordingTime.AddSeconds((i + 1) * 10).ToString("yyyy/MM/dd HH:mm:ss");
                    dt.Rows.Add(dr);
                    id = modelAdministrater.annotations[i];
                    if (id != 0) {
                        dr = dt.NewRow();
                        dr[0] = id;
                        dr[1] = i;
                        foreach (var label in annotationLabels) {
                            if (label.id == id) {
                                dr[3] = label.labelName;
                            }
                        }
                        dr[4] = recordingTime.AddSeconds(i * 10).ToString("yyyy/MM/dd HH:mm:ss");
                    }
                }
            } else if (i == modelAdministrater.annotations.Length - 1 && id != 0) {
                dr[2] = i;
                dr[5] = recordingTime.AddSeconds((i + 1) * 10).ToString("yyyy/MM/dd HH:mm:ss");
                dt.Rows.Add(dr);
            }
        }
        AnnotationData.SaveDataTableToCsv(dt);
    }



    private void wid_main_KeyDown(object sender, KeyEventArgs e) {
        if (openFile) {
            if (e.Key >= Key.D1 && e.Key <= Key.D9){
                int id = e.Key - Key.D1;
                foreach (var label in annotationLabels) {
                    if (label.id == id) {
                        label.isSelected = true;
                        currentId = label.id;
                        currentIdx = annotationLabels.IndexOf(label);
                    } else {
                        label.isSelected = false;
                    }
                }
            }

            if (e.Key == Key.Left) {
                if (currentPage != 0) {
                    btn_previousPage_Click(sender, e);
                }
            }else if (e.Key == Key.Right) {
                if (currentPage != pageSize) {
                    btn_nextPage_Click(sender, e);
                }
            }
  
            
            switch (e.Key) {
                case Key.F1:
                    if (currentPage != 0) {
                        btn_previousPage_Click(sender, e);
                    }
                    break;
                case Key.F2:
                    if (currentPage != pageSize) {
                        btn_nextPage_Click(sender, e);
                    }
                    break;
                default:
                    break;
            }
            
            
        }
    }

    private void btn_ymaxup_Click(object sender, RoutedEventArgs e) {
        if(ecg_ymax+ecg_yrate > ecg_ymin) {
            ecg_ymax += ecg_yrate;
            txb_ymax.Text = ecg_ymax.ToString();
            PageModels pm = modelAdministrater.UpdateGet_Scale(ecg_ymin, ecg_ymax);
            PlotECGGraph(pm);
        }
    }

    private void btn_ymaxdown_Click(object sender, RoutedEventArgs e) {
        if (ecg_ymax - ecg_yrate > ecg_ymin) {
            ecg_ymax -= ecg_yrate;
            txb_ymax.Text = ecg_ymax.ToString();
            PageModels pm = modelAdministrater.UpdateGet_Scale(ecg_ymin, ecg_ymax);
            PlotECGGraph(pm);
        }
    }

    private void btn_yminup_Click(object sender, RoutedEventArgs e) {
        if (ecg_ymin + ecg_yrate < ecg_ymax) {
            ecg_ymin += ecg_yrate;
            txb_ymin.Text = ecg_ymin.ToString();
            PageModels pm = modelAdministrater.UpdateGet_Scale(ecg_ymin, ecg_ymax);
            PlotECGGraph(pm);
        }
    }

    private void btn_ymindown_Click(object sender, RoutedEventArgs e) {
        if (ecg_ymin - ecg_yrate < ecg_ymax) {
            ecg_ymin -= ecg_yrate;
            txb_ymin.Text = ecg_ymin.ToString();
            PageModels pm = modelAdministrater.UpdateGet_Scale(ecg_ymin, ecg_ymax);
            PlotECGGraph(pm);
        }
    }

    private void btn_zoomymaxup_Click(object sender, RoutedEventArgs e) {
        if (zoom_ecg_ymax + zoom_ecg_yrate > zoom_ecg_ymin) {
            zoom_ecg_ymax += zoom_ecg_yrate;
            txb_zoomymax.Text = zoom_ecg_ymax.ToString();
            PlotZoomECGGraph();
        }
    }

    private void btn_zoomymaxdown_Click(object sender, RoutedEventArgs e) {
        if (zoom_ecg_ymax - zoom_ecg_yrate > zoom_ecg_ymin) {
            zoom_ecg_ymax -= zoom_ecg_yrate;
            txb_zoomymax.Text = zoom_ecg_ymax.ToString();
            PlotZoomECGGraph();
        }
    }

    private void btn_zoomyminup_Click(object sender, RoutedEventArgs e) {
        if (zoom_ecg_ymin + zoom_ecg_yrate < zoom_ecg_ymax) {
            zoom_ecg_ymin += zoom_ecg_yrate;
            txb_zoomymin.Text = zoom_ecg_ymin.ToString();
            PlotZoomECGGraph();
        }
    }

    private void btn_zoomymindown_Click(object sender, RoutedEventArgs e) {
        if (zoom_ecg_ymin - zoom_ecg_yrate < zoom_ecg_ymax) {
            zoom_ecg_ymin -= zoom_ecg_yrate;
            txb_zoomymin.Text = zoom_ecg_ymin.ToString();
            PlotZoomECGGraph();
        }
    }

}

