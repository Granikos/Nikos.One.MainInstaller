using System;
using System.Web;
using System.Windows.Forms;

namespace WpiWrapper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(params string[] args)
        {
            string[] contextualEntryProducts;
            string contextualEntryLanguage;
            bool useIisExpress;

            if (!ParseCommandLine(args, out contextualEntryProducts, out contextualEntryLanguage, out useIisExpress))
            {
                return -1;
            }

            Environment.SetEnvironmentVariable("WEBPI_REFERRER", "webpi");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new Form1(useIisExpress, contextualEntryProducts, contextualEntryLanguage);
            Application.Run(form);

            //form.InstallerService.Print();

            return 0;
        }

        //private static readonly Regex RegexParams = new Regex(@"\w+", RegexOptions.Compiled);

        //public static Dictionary<string, string> GetParams(string[] args)
        //{
        //    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        //    for (var i = 0; i < args.Length; i += 2)
        //    {
        //        var key = RegexParams.Match(args[i]).Value;
        //        if (string.IsNullOrWhiteSpace(key))
        //        {
        //            i--;
        //            continue;
        //        }

        //        var value = args.Length > i + 1 ? args[i + 1] : null;

        //        result.Add(key, value);
        //    }

        //    return result;
        //}
        private static bool ParseCommandLine(string[] args, out string[] contextualEntryProducts, out string contextualEntryLanguage, out bool useIisExpress)
        {
            contextualEntryProducts = null;
            contextualEntryLanguage = string.Empty;
            useIisExpress = false;

            if (args == null)
            {
                return true;
            }

            var flag = false;
            var text = string.Empty;

            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals("/id", args[i], StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    if (i >= args.Length)
                    {
                        flag = true;
                        break;
                    }
                    text = args[i];
                }
                else if (string.Equals("/language", args[i], StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    if (i >= args.Length)
                    {
                        flag = true;
                        break;
                    }
                    contextualEntryLanguage = args[i];
                }
                else if (string.Equals("/iisexpress", args[i], StringComparison.OrdinalIgnoreCase))
                {
                    useIisExpress = true;
                }
            }

            if (flag)
            {
                throw new Exception("Invalid command line");
            }

            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            var flag3 = false;
            if (text.StartsWith("wpi://", StringComparison.OrdinalIgnoreCase))
            {
                flag3 = true;
                text = text.Substring(6);
                text = text.Trim(new[] { '/' });
            }

            text = HttpUtility.UrlDecode(text);
            var array = text.Split(new[] { '?' });

            if (array.Length < 1 || string.IsNullOrEmpty(array[0].Trim()))
            {
                return true;
            }

            contextualEntryProducts = array[0].Split(new[] { '&' });

            if (!flag3)
            {
                return true;
            }

            var text2 = contextualEntryProducts[contextualEntryProducts.Length - 1].Trim(new[] { '/' });
            contextualEntryProducts[contextualEntryProducts.Length - 1] = text2;

            return true;
        }
    }
}
