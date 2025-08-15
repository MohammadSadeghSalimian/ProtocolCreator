
using Microsoft.Extensions.Logging;
using ProtocolCreator.Core;
using ScottPlot;

namespace ProtocolCreator.Infrastructures
{
    public class ElongationPlotter(ILogger<ElongationPlotter> logger) : IElongationPlotter
    {
        public void DriftElongationPlotter(FileInfo file, IReadOnlyList<double> drift, IReadOnlyList<double> elongation, int with = 1920, int height = 1080)
        {
            if (drift.Count != elongation.Count)
            {
                throw new ArgumentException("Drift and elongation lists must have the same length.");

            }

            var plotModel = new Plot()
            {
                ScaleFactor = 2.0, // Adjust scale factor for better visibility
            };


            var coordinates = drift.Zip(elongation, (d, e) => new Coordinates(d, e)).ToArray();
            var sL = plotModel.Add.ScatterLine(coordinates);
            sL.LegendText = "Drift vs Elongation";
            sL.LineWidth = 2; // Set line width for better visibility
            sL.MarkerSize = 5; // Set marker size for better visibility
            sL.Color = Colors.Blue;
            plotModel.XLabel("Drift (%)");
            plotModel.YLabel("Elongation (mm)");
            plotModel.Grid.LineColor = Colors.LightGray;
            plotModel.Grid.LineWidth = 1;
            plotModel.Legend.IsVisible = true;
            plotModel.Grid.LinePattern= LinePattern.Dashed;


            var savedImage = plotModel.SavePng(file.FullName, with, height);
            logger.LogInformation("Png file is saved. File size: {SavedImageFileSize}", savedImage.FileSize);
            savedImage.LaunchFile();
            
        }
        public void CycleElongationPlotter(FileInfo file, IReadOnlyList<double> cycle, IReadOnlyList<double> elongation, int with = 1920, int height = 1080)
        {
            if (cycle.Count != elongation.Count)
            {
                throw new ArgumentException("Drift and elongation lists must have the same length.");

            }

            var plotModel = new Plot()
            {
                ScaleFactor = 2.0, // Adjust scale factor for better visibility
            };


            var coordinates = cycle.Zip(elongation, (d, e) => new Coordinates(d, e)).ToArray();
            var sL = plotModel.Add.ScatterLine(coordinates);
            sL.LegendText = "Cycle vs Elongation";
            sL.LineWidth = 2; // Set line width for better visibility
            sL.MarkerSize = 5; // Set marker size for better visibility
            sL.Color = Colors.IndianRed;
            plotModel.XLabel("Cycle");
            plotModel.YLabel("Elongation (mm)");
            plotModel.Grid.LineColor = Colors.LightGray;
            plotModel.Grid.LineWidth = 1;
            plotModel.Legend.IsVisible = true;
            plotModel.Grid.LinePattern = LinePattern.Dashed;


            var savedImage = plotModel.SavePng(file.FullName, with, height);
            logger.LogInformation("Png file is saved. File size: {SavedImageFileSize}", savedImage.FileSize);
            savedImage.LaunchFile();

        }
    }
}
