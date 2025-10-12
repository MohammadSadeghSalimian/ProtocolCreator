using ProtocolCreator.Core;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using SkiaSharp;

namespace ProtocolCreator.Infrastructures;

public class Plotter
{
    public void PlotDriftElongationVsCycle(DirectoryInfo outputFolder, string name, IReadOnlyList<Delta> deltas)
    {
        ArgumentNullException.ThrowIfNull(outputFolder);
        Directory.CreateDirectory(outputFolder.FullName);

        var p1 = new Plot();

        var gg = deltas.GroupBy(x => x.Segment.CycleState).ToDictionary(x => x.Key, x => x.ToList());

        var s1 = p1.Add.ScatterLine(
            (new[] { new Coordinates(0, 0) })
            .Concat(deltas.Select(x => new Coordinates(x.Cycle, x.Drift.End)))
            .ToArray()
);

        s1.Axes.YAxis = p1.Axes.Right;
        s1.LineWidth = 1;
        s1.MarkerStyle.Shape = MarkerShape.FilledCircle;
        s1.MarkerSize = 6;
        s1.Color = Colors.Gray;
        s1.MarkerFillColor = Colors.Black;
        s1.LineWidth = 2;
        s1.LinePattern = LinePattern.Solid;
        s1.LegendText="Plastic hinge rotation";
        p1.Grid.IsVisible = true;
        

        p1.Axes.Title.Label.FontName = "Times New Roman";

        p1.Axes.Right.Label.Text = "Plastic hinge rotation (%)";
        p1.Axes.Bottom.Label.Text = "Cycle";
        p1.Axes.Left.Label.Text = "Elongation (mm)";

        p1.Axes.Bottom.Label.FontName = "Times New Roman";
        p1.Axes.Left.Label.FontName = "Times New Roman";


   
        //foreach (var pair in gg)
        //{
        //    var sx = p1.Add.ScatterPoints(pair.Value.Select(x => new Coordinates(x.Cycle, x.Drift.End)).ToArray());
        //    sx.Axes.YAxis = p1.Axes.Left;
        //    sx.LineWidth = 0;
        //    sx.MarkerStyle.MarkerColor = colors[k % colors.Length];
        //    sx.MarkerSize = 4;
        //    sx.MarkerShape = MarkerShape.FilledSquare;
        //    k++;
        //}


        var s2 = p1.Add.ScatterLine(
            (new[] { new Coordinates(0, 0) })
            .Concat(
            deltas.Select(x => new Coordinates(x.Cycle, x.Elongation.End))).ToArray());
        s2.Axes.YAxis = p1.Axes.Left;
        s2.Color = Colors.C0;
        s2.MarkerFillColor = Colors.C3;
        s2.MarkerSize = 8;
        s2.LineWidth = 2;
        s2.MarkerStyle.Shape = MarkerShape.FilledCircle;
        s2.LegendText="Elongation";

        p1.ShowLegend();


        //var k = 0;
        //foreach (var pair in gg)
        //{
        //    var sx = p1.Add.ScatterPoints(pair.Value.Select(x => new Coordinates(x.Cycle, x.Elongation.End)).ToArray());
        //    sx.Axes.YAxis = p1.Axes.Right;
        //    sx.LineWidth = 0;
        //    sx.MarkerStyle.MarkerColor = colors[k % colors.Length];
        //    sx.MarkerSize = 6;
        //    sx.MarkerShape = MarkerShape.FilledCircle;
        //    k++;
        //}
        p1.ScaleFactor = 4;
        //p2.ScaleFactor = 4;

        p1.SavePng(Path.Combine(outputFolder.FullName, name + "Cycle-Drift.png"), 4 * 1600, 4 * 900);



    }

    public void DriftElongationPlotter(FileInfo file, IReadOnlyList<Delta> deltas)
    {
        var pp = new Plot();
        pp.ShowGrid();
        pp.Title("Drift vs Elongation");
        pp.XLabel("Drift (%)");
        pp.YLabel("Elongation (mm)");
        var ss = pp.Add.Scatter(
            new [] {new Coordinates(0,0)}.Concat(deltas.Select(x => new Coordinates(x.Drift.End,x.Elongation.End)))
            .ToArray());
        ss.MarkerSize = 10;
        ss.LineWidth = 2;
        ss.Color = Colors.C1;
        ss.MarkerFillColor= Colors.C3;
        pp.ScaleFactor = 4;
        pp.SavePng(file.FullName, 4 * 900, 4 * 900);

        pp.SaveSvg(Path.ChangeExtension(file.FullName, ".svg"), 4 * 900, 4 * 900);

     
        

    }

}