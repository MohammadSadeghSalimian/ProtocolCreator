using ProtocolCreator.Core;
using ProtocolCreator.Infrastructures;

namespace ProtocolCreator.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Console.WriteLine("Select the modules:");
            Console.WriteLine("1. Create drift segments file");
            Console.WriteLine("2. Create elongation file");
            Console.WriteLine("3. Exit");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Invalid input. Exiting...");
                return;
            }
            switch (input.Trim())
            {
                case "1":
                    CreateSegmentFile();
                    break;
                case "2":
                    CreateElongationFile();
                    break;
                case "3":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid input. Exiting...");
                    return;
            }
        }

        private static void CreateElongationFile()
        {
            Console.WriteLine("Drift protocol builder");
            Console.WriteLine("This program calculates drift segments and saves results to an Excel file.");
            Console.WriteLine("Make sure you have 'DriftSegments.xlsx' in the current directory.");
            Console.WriteLine("The results will be saved to 'Results.xlsx' in the current directory.");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();
            Console.WriteLine("Started!");
            try
            {
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
                var downSampledData = engine.Deltas.Downsample(x => x.Id, 1e-6,1,
                    x => x.Drift.End, x => x.Elongation.End);
                excelSaver.Save(new FileInfo(outputPath), downSampledData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
            Console.WriteLine("Results saved to 'Results.xlsx'.");
            Console.WriteLine("Press any key to exit...");
        }

        public static void CreateSegmentFile()
        {
            Console.WriteLine("Drift segment file builder");
            Console.WriteLine("This program creates drift segments and saves them to an Excel file.");
            Console.WriteLine("The results will be saved to 'DriftSegments.xlsx' in the current directory.");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();
            Console.WriteLine("Enter the number of the repats in cycles:");
            var repeatText= Console.ReadLine();
            if (string.IsNullOrEmpty(repeatText))
            {
                Console.WriteLine("Repeat should be a positive integer number");
                return;
            }

            var res = int.TryParse(repeatText, out var n);
            if (!res || n <= 0)
            {
                Console.WriteLine("Repeat should be a positive number");
                return;
            }
            Console.WriteLine("Enter all the drift level and separate them by comma:");
            var text=Console.ReadLine();
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("No drift levels provided. Exiting...");
                return;
            }
            Console.WriteLine("Started!");
            var path = Path.Combine(Environment.CurrentDirectory, "DriftSegments.xlsx");
            var driftLevels = text.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(double.Parse)
                .ToList();
          
            var segmentBuilder = new DriftSegmentCreator();
            try
            {
                segmentBuilder.Create(new FileInfo(path),n,driftLevels);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
               return;
            }
            Console.WriteLine($"Drift segments saved to '{path}'.");




        }
    }
}
