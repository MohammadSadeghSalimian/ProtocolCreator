using ProtocolCreator.Core;
using ProtocolCreator.Infrastructures;

namespace ProtocolCreator.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Drift protocol builder");
            Console.WriteLine("This program calculates drift segments and saves results to an Excel file.");
            Console.WriteLine("Make sure you have 'DriftSegments.xlsx' in the current directory.");
            Console.WriteLine("The results will be saved to 'Results.xlsx' in the current directory.");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();
            Console.WriteLine("Started!");
            var inputPath = Path.Combine(Environment.CurrentDirectory, "DriftSegments.xlsx");
            var aa = new ExcelInputLoader();
            aa.Open(new FileInfo(inputPath));
            var driftSegments = aa.LoadDriftSegments();
            var info = aa.LoadAnalysis();
            aa.Close();
            var engine = new Engine(driftSegments, info);
            engine.Calculate();
            
            var excelSaver = new ExcelResultSaver();
            var outputPath = Path.Combine(Environment.CurrentDirectory, "Results.xlsx");
            excelSaver.Save(new FileInfo(outputPath),engine.Deltas);
            Console.WriteLine("Results saved to 'Results.xlsx'.");
            Console.WriteLine("Press any key to exit...");
           

        }
    }
}
