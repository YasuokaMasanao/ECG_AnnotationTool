using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace annotationTool;

public class ModelAdministrater {
    public PageModels page0;
    public PageModels page1;
    public PageModels page2;

    public ushort[]? signal;
    public int[] annotations;

    public ObservableCollection<AnnotationLabel> annotationLabels;

    public int ecg_ymin;
    public int ecg_ymax;

    public int currentPage;
    public int pageSize;

    private bool labelFlag = false;

    public ModelAdministrater(ushort[]? signal, int[] annotations, ObservableCollection<AnnotationLabel> annotationLabels, int pageSize) {
        this.signal = signal;
        this.annotations = annotations;
        this.annotationLabels = annotationLabels;

        ecg_ymin = 1500;
        ecg_ymax = 2200;

        currentPage = 0;
        this.pageSize = pageSize;

        page0 = CreatePageModels(0);
        page1 = CreatePageModels(1);
        page2 = CreatePageModels(2);
    }


    public PlotModel CreatePlotModel(int startBlock, int endBlock) {
        var Model = new PlotModel()
        {
            PlotAreaBorderThickness = new OxyThickness(0),
            //PlotMargins = new OxyThickness(-7, -10, -7, -10),
            PlotMargins = new OxyThickness(10, -10, 10, -10),
            //Background = OxyColors.Transparent
        };

        var xAxis = new LinearAxis()
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            //Maximum = 75000,
            Maximum = 15000,
            //MajorStep = 15000,
            MajorStep = 3000,
            //MinorStep = 2500,
            MinorStep = 500,
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
        for (int i = startIdx; i < endIdx; i = i + 5) {
            if (i >= signal.Length) {
                break;
            }
            lineSeries.Points.Add(new DataPoint(k, signal[i]));
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
                MinimumX = j * 500,
                MaximumX = (j + 1) * 500,
                MinimumY = ecg_ymin,
                MaximumY = ecg_ymax,
                Fill = OxyColor.Parse(matchedLabel.color),
                Layer = AnnotationLayer.BelowAxes,
            });
            j += 1;
        }

        return Model;
    }


    public PageModels CreatePageModels(int page) {
        PageModels pageModels = new PageModels();
        pageModels.pageIndex = page;

        int startBlock = page * 180;
        pageModels.ecg0 = CreatePlotModel(startBlock, startBlock + 30);
        pageModels.ecg1 = CreatePlotModel(startBlock + 30, startBlock + 60);
        pageModels.ecg2 = CreatePlotModel(startBlock + 60, startBlock + 90);
        pageModels.ecg3 = CreatePlotModel(startBlock + 90, startBlock + 120);
        pageModels.ecg4 = CreatePlotModel(startBlock + 120, startBlock + 150);
        pageModels.ecg5 = CreatePlotModel(startBlock + 150, startBlock + 180);
        return pageModels;
    }


    public PageModels GetPageModels(int page) {
        if (labelFlag && page < currentPage) {
            currentPage = page;
            labelFlag = false;
            if (page0.pageIndex == page) {
                page0 = CreatePageModels(page);
                return page0;
            } else if (page1.pageIndex == page) {
                page1 = CreatePageModels(page);
                return page1;
            } else if (page2.pageIndex == page) {
                page2 = CreatePageModels(page);
                return page2;
            } else {
                page0 = CreatePageModels(page);
                return page0;
            }
        } else {
            if (page != currentPage) {
                labelFlag = false;
            }
            currentPage = page;
            if (page0.pageIndex == page) {
                return page0;
            } else if (page1.pageIndex == page) {
                return page1;
            } else if (page2.pageIndex == page) {
                return page2;
            } else {
                page0 = CreatePageModels(page);
                return page0;
            }
        }
    }

    public void Update_PreviousPage() {
        if (currentPage != 0) {
            if (page0.pageIndex == currentPage + 2) {
                page0 = CreatePageModels(currentPage - 1);
            } else if (page1.pageIndex == currentPage + 2) {
                page1 = CreatePageModels(currentPage - 1);
            } else if (page2.pageIndex == currentPage + 2) {
                page2 = CreatePageModels(currentPage - 1);
            }
        }
    }

    public void Update_NextPage() {
        if (currentPage != pageSize) {
            if (page0.pageIndex == currentPage-2) {
                page0 = CreatePageModels(currentPage + 1);
            } else if (page1.pageIndex == currentPage-2) {
                page1 = CreatePageModels(currentPage + 1);
            } else if (page2.pageIndex == currentPage-2){
                page2 = CreatePageModels(currentPage + 1);
            }
        }
    }

    public void Update_Jump() {
        if (currentPage == 0) {
            page1 = CreatePageModels(currentPage + 1);
            page2 = CreatePageModels(currentPage + 2);
        } else if (currentPage == pageSize) {
            page1 = CreatePageModels(currentPage - 1);
            page2 = CreatePageModels(currentPage - 2);
        } else {
            page1 = CreatePageModels(currentPage - 1);
            page2 = CreatePageModels(currentPage + 1);
        }
    }

    public PageModels UpdateGet_Scale(int ymin, int ymax) {
        this.ecg_ymin = ymin;
        this.ecg_ymax = ymax;
        page0.ecg0.Axes[1].Maximum = ymax;
        page0.ecg0.Axes[1].Minimum = ymin;
        page0.ecg1.Axes[1].Maximum = ymax;
        page0.ecg1.Axes[1].Minimum = ymin;
        page0.ecg2.Axes[1].Maximum = ymax;
        page0.ecg2.Axes[1].Minimum = ymin;
        page0.ecg3.Axes[1].Maximum = ymax;
        page0.ecg3.Axes[1].Minimum = ymin;
        page0.ecg4.Axes[1].Maximum = ymax;
        page0.ecg4.Axes[1].Minimum = ymin;
        page0.ecg5.Axes[1].Maximum = ymax;
        page0.ecg5.Axes[1].Minimum = ymin;
        page1.ecg0.Axes[1].Maximum = ymax;
        page1.ecg0.Axes[1].Minimum = ymin;
        page1.ecg1.Axes[1].Maximum = ymax;
        page1.ecg1.Axes[1].Minimum = ymin;
        page1.ecg2.Axes[1].Maximum = ymax;
        page1.ecg2.Axes[1].Minimum = ymin;
        page1.ecg3.Axes[1].Maximum = ymax;
        page1.ecg3.Axes[1].Minimum = ymin;
        page1.ecg4.Axes[1].Maximum = ymax;
        page1.ecg4.Axes[1].Minimum = ymin;
        page1.ecg5.Axes[1].Maximum = ymax;
        page1.ecg5.Axes[1].Minimum = ymin;
        page2.ecg0.Axes[1].Maximum = ymax;
        page2.ecg0.Axes[1].Minimum = ymin;
        page2.ecg1.Axes[1].Maximum = ymax;
        page2.ecg1.Axes[1].Minimum = ymin;
        page2.ecg2.Axes[1].Maximum = ymax;
        page2.ecg2.Axes[1].Minimum = ymin;
        page2.ecg3.Axes[1].Maximum = ymax;
        page2.ecg3.Axes[1].Minimum = ymin;
        page2.ecg4.Axes[1].Maximum = ymax;
        page2.ecg4.Axes[1].Minimum = ymin;
        page2.ecg5.Axes[1].Maximum = ymax;
        page2.ecg5.Axes[1].Minimum = ymin;
        if (page0.pageIndex == currentPage) {
            return page0;
        }else if (page1.pageIndex == currentPage) {
            return page1;
        } else {
            return page2;
        }
    }


    public PageModels UpdateGet_Label() {
        PageModels pageModels = new PageModels();

        int startBlock = currentPage * 180;
        pageModels.ecg0 = CreatePlotModel(startBlock, startBlock + 30);
        pageModels.ecg1 = CreatePlotModel(startBlock + 30, startBlock + 60);
        pageModels.ecg2 = CreatePlotModel(startBlock + 60, startBlock + 90);
        pageModels.ecg3 = CreatePlotModel(startBlock + 90, startBlock + 120);
        pageModels.ecg4 = CreatePlotModel(startBlock + 120, startBlock + 150);
        pageModels.ecg5 = CreatePlotModel(startBlock + 150, startBlock + 180);
        if (page0.pageIndex == currentPage) {
            page0 = pageModels;
        } else if (page1.pageIndex == currentPage) {
            page1 = pageModels;
        } else {
            page2 = pageModels;
        }
        labelFlag = true;
        return pageModels;
    }
}
