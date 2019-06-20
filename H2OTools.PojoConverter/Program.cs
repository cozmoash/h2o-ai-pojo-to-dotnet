using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace H2OTools.PojoConverter
{
  
    class Program
    {
        static void Main(string[] args)
        {
            //a simple tool for converting a h2o pojo java file into a .net assembly.

            var fileName = "inputH2omodel.java";
            var name = "outputname_{id}";
            
            var id = GetModelUuId(fileName);

            if (id == 0)
            {
                throw new Exception("could not find id");
            }

            name = name.Replace("{id}", id.ToString());

            var newModelName = name;

            string newPojoPath = Path.Combine(Environment.CurrentDirectory,
                newModelName + ".cs");

            if (File.Exists(newPojoPath))
            {
                throw new Exception("existing model already exists, possibly from a previous run of this tool?");
            }

            var path = Path.Combine(Environment.CurrentDirectory, fileName);

            var pojoLines = File.ReadAllLines(path);

            StringBuilder writebuffer = new StringBuilder();

            for (int index = 0; index < pojoLines.Length; index++)
            {
                StringBuilder source = new StringBuilder();

                var line = pojoLines[index];

                var newLine = line.Replace("final ", "")
                    .Replace("public class err_gbm", "public class " + name)
                    .Replace("public err_gbm()", "public " + name + "()")
                    .Replace("public class gbm", "public class " + name)
                    .Replace("public gbm()", "public " + name + "()")
                    .Replace("implements java.io.Serializable", "")
                    .Replace("import java.util.Map;", "using System; using System.Collections.Generic; public static class H2oArrayExtensions { public static void Fill<T>(this T[] originalArray, T with) { for(int i = 0; i < originalArray.Length; i++){ originalArray[i] = with; } }  }")
                    .Replace("1./(1. + Math.min(1e19, Math.exp", "1.0/(1.0 + Math.Min(1e19, Math.Exp")
                    .Replace("import hex.genmodel.GenModel;", "public static class BitSetContains_Helper { public static bool BitSetContains(byte[] bits, int nbits, int bitoff, double dnum) { int idx = (int)dnum; idx -= bitoff; return (bits[idx >> 3] & ((byte)1 << (idx & 7))) != 0; } } public static class BitSetIsInRange_Helper { public static bool BitSetIsInRange(int nbits, int bitoff, double dnum) { int idx = (int)dnum; idx -= bitoff; return (idx >= 0 && idx < nbits); } }")
                    .Replace("import hex.genmodel.annotations.ModelPojo;", "public class GetPrediction_Helper {  public static int GetPrediction(double[] preds, double[] priorClassDist, double[] data, double threshold) { if (preds.Length == 3) {return (preds[2] >= threshold) ? 1 : 0; }List<int> ties = new List<int>();ties.Add(0);int best = 1, tieCnt = 0;   for (int c = 2; c < preds.Length; c++){if (preds[best] < preds[c]){best = c;           tieCnt = 0;         }else if (preds[best] == preds[c]){tieCnt++;           ties.Add(c - 1);}}if (tieCnt == 0) return best - 1; long hash = 0;if (data != null)foreach (double d in data) hash ^= BitConverter.DoubleToInt64Bits(d) >> 6; if (priorClassDist != null){double sum = 0;foreach (var i in ties){ sum += priorClassDist[i]; }Random rng = new Random((int)hash);double tie = rng.NextDouble(); double partialSum = 0;foreach (var i in ties){partialSum += priorClassDist[i] / sum; if (tie <= partialSum)return i;}}double res = preds[best];    int idx = (int)hash % (tieCnt + 1);  for (best = 1; best < preds.Length; best++)if (res == preds[best] && --idx < 0)return best - 1;          throw new ApplicationException(\"Should Not Reach Here\");}}")
                    .Replace("@ModelPojo(name=\"err_gbm\", algorithm=\"gbm\")", "")
                    .Replace("@ModelPojo(name=\"gbm\", algorithm=\"gbm\")", "")
                    .Replace("boolean ", "bool ")
                    .Replace("java.util.Arrays.fill(preds,0);", "")
                    .Replace("extends GenModel", "")
                    .Replace("java.util.Arrays.fill(", "H2oArrayExtensions.Fill(")
                    .Replace("public hex.ModelCategory getModelCategory() { return hex.ModelCategory.Regression; }", "")
                    //.Replace("() { super(NAMES,DOMAINS); }", "() {  }")
                    .Replace("static void fill(String[] sa) {", "public static void fill(String[] sa) {")
                    .Replace("static double score0(double[] data) {", "public static double score0(double[] data) {")
                    .Replace("double[] fdata = hex.genmodel.GenModel.SharedTree_clean(data);","double[] fdata = SharedTree_clean(data);")
                    .Replace("isNaN", "IsNaN")
                    .Replace(".length", ".Length")
                    .Replace("IllegalArgumentException", "ArgumentException")
                    .Replace("RuntimeException", "ArgumentException")
                    .Replace("static void fill", "public static void fill").Replace("public public static void fill", "public static void fill")
                    .Replace("public hex.ModelCategory getModelCategory() { return hex.ModelCategory.Binomial; }", "")
                    .Replace("hex.genmodel.GenModel.GLM_logitInv", "GenModelHelper.GLM_logitInv")
                    .Replace("hex.genmodel.GenModel.GLM_identityInv", "GenModelHelper.GLM_identityInv")
                    .Replace("GenModel.bitSetContains", "BitSetContains_Helper.BitSetContains")
                    .Replace("GenModel.bitSetIsInRange", "BitSetIsInRange_Helper.BitSetIsInRange")
                    .Replace("byte", "sbyte")

                    .Replace("Math.max(", "Math.Max(")
                    .Replace("Math.exp(", "Math.Exp(")

                    .Replace("hex.genmodel.GenModel.getPrediction", "GetPrediction_Helper.GetPrediction");



                ;


                if (line.Contains("() { super(NAMES,DOMAINS"))
                {
                    newLine = string.Empty;
                }

                if (line.Contains("@ModelPojo"))
                {
                    newLine = string.Empty;
                }

                if (line.Contains("Long.toString"))
                {
                    source.AppendLine(
                            "public static double[] SharedTree_clean(double[] data)   {      double[] fs = new double[data.Length];      for (int i = 0; i < data.Length; i++)          fs[i] = Double.IsNaN(data[i]) ? -Double.MaxValue : data[i];      return fs;  }");

                    source.AppendLine();

                    source.AppendLine("public static bool BitSetContains(byte[] bits, int bitoff, double dnum ) { if (Double.IsNaN(dnum)) return true; int num = (int)dnum; if (num < 0) { throw new ArgumentException(\"bitSet can only contain integer factor levels >= 0, but got \" + num); } num -= bitoff; return (num >= 0) && (num < (bits.Length<<3)) && (bits[num >> 3] & ((byte)1 << (num & 7))) != 0; }");

                    source.AppendLine(
                        "public static partial class GenModelHelper { public static double GLM_identityInv(double x) { return x; } public static double GLM_logitInv(double x) { return 1.0 / (Math.Exp(-x) + 1.0); } public static double GLM_logInv(double x) { return Math.Exp(x); } public static double GLM_inverseInv(double x) { double xx = (x < 0) ? Math.Min(-1e-5, x) : Math.Max(1e-5, x); return 1.0 / xx; } public static double GLM_tweedieInv(double x, double tweedie_link_power) { return tweedie_link_power == 0 ? Math.Max(2e-16, Math.Exp(x)) : Math.Pow(x, 1.0 / tweedie_link_power); }}");

                    source.AppendLine();
                }

                if (line.Contains("Long.toString"))
                {
                    var versionNumber = line.Replace("()", "").Split('(', ')')[1].Replace("-","");
                    newLine = "public String getUUID() { return " + versionNumber + ".ToString(); }";

                }

                if (line.Contains(" static {")) 
                {
                    var classDeclaration = pojoLines[index - 2];
                    string[] strings = classDeclaration.Split(' ');

                    var className = "";
                    for (var i = 0; i < strings.Length; i++)
                    {
                        if(strings[i] == "class")
                        {
                            className = strings[i + 1];
                        }
                    }

                    newLine = "static " + className+"() {";
                }

                source.AppendLine(newLine);

                writebuffer.Append(source.ToString());

                if (index % 1000 == 0)
                {
                    File.AppendAllText(newPojoPath, writebuffer.ToString());
                    writebuffer.Clear();
                }
            }

            File.AppendAllText(newPojoPath, writebuffer.ToString());
           

            CompileModel(newPojoPath);
        }

        private static long GetModelUuId(string fileName)
        {
            long id = 0;
            foreach (var line in File.ReadLines(fileName))
            {
                if (line.Contains("public String getUUID() { return Long.toString("))
                {
                    var uuid = line.Replace("public String getUUID() { return Long.toString(", "")
                        .Replace("-", "")
                        .Replace("); }", "")
                        .Replace("L", "");

                    id = long.Parse(uuid);
                    break;
                }
            }
            return id;
        }

        private static void CompileModel(string newPojoPath)
        {
            Console.WriteLine("compiling...");

            var csc = Process.Start(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                string.Format("/t:library {0}", newPojoPath));

            csc.WaitForExit();

            Console.WriteLine("compiled");
        }



    }
}
