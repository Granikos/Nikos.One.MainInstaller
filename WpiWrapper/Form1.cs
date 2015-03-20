using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeploymentTools;
using Installer;
using Microsoft.Web.PlatformInstaller;
using Microsoft.Web.PlatformInstaller.UI;
using Product = DeploymentTools.InstalledProduct;

namespace WpiWrapper
{
    public partial class Form1 : Form
    {
        protected readonly static string MainXml = "http://www.microsoft.com/web/webpi/4.6/webproductlist.xml";
        protected static string CustomXml;

        internal InstallerServiceProxy InstallerServiceProxy { get; private set; }

        static Form1()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            CustomXml = Path.Combine(Path.GetDirectoryName(typeof(Form1).Assembly.Location), "Installers\\wpi\\GranikosFeed.xml");
        }

        public Form1(bool useIisExpress, string[] contextualEntryProducts, string contextualEntryLanguage)
        {
            InitializeComponent();

            var applicationOverrideFeed = MainXml;
            string[] contextualEntryFeeds = { MainXml, CustomXml };
            var contextualEntryModes = useIisExpress ? ContextualEntryModes.TargetIisExpress : ContextualEntryModes.TargetIis;// | ContextualEntryModes.Sqm | ContextualEntryModes.AcceptEula;

            new WebPiPreferences { SelectedFeeds = CustomXml }.Save();

            var hostService = new HostService
            {
                AllowIisExpressAppInstall = useIisExpress,
                UseIisExpressForAppInstall = useIisExpress
            };

            var serviceProvider = new UIHost(hostService, applicationOverrideFeed);
            serviceProvider.UseIisExpressForAppInstall(useIisExpress);
            var managementFrame = new ManagementFrameFull(serviceProvider, contextualEntryProducts, contextualEntryFeeds, contextualEntryLanguage, contextualEntryModes);
            var isInstallClicked = false;

            Shown += delegate
            {
                if (isInstallClicked) return;
                //
                // Automatically click 'Install'
                //
                var cartService = serviceProvider.GetService("CartService");
                var installProduct = cartService.GetProperty("InstallProductList") as List<Product>;
                if (installProduct != null && installProduct.Any())
                {
                    ClickInstallButton(managementFrame);
                }
                else
                {
                    cartService.AddEventHandler<EventArgs>("ProductAdded", delegate
                    {
                        ClickInstallButton(managementFrame);
                    }, true);
                }

                isInstallClicked = true;
            };

            InstallerServiceProxy = new InstallerServiceProxy(serviceProvider, serviceProvider.GetService("InstallerService"))
            {
                UseIisExpress = useIisExpress
            };

            //
            // Disable update notification
            //
            serviceProvider.GetService("ProductService").SetField("_upgradeOptionOffered", true);

            FormClosing += (s, e) =>
            {
                //InstallerService.Print();
                hostService.RaiseShellFormClosing(s, e);
            };

            SuspendLayout();
            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            //this._managementFrame = new ManagementFrameFull(this._webPIServiceProvider, this.contextualEntryProducts, this.contextualEntryFeeds, this.contextualEntryLanguage, this.contextualEntryModes);
            var num = Size.Width - ClientSize.Width;
            var num2 = Size.Height - ClientSize.Height;
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            MinimumSize = new Size(managementFrame.MinimumSize.Width + num, managementFrame.MinimumSize.Height + num2);
            FormBorderStyle = FormBorderStyle.Sizable;
            Size = new Size(780, 540);
            CenterToScreen();
            managementFrame.Dock = DockStyle.Fill;
            Controls.Clear();
            Controls.Add(managementFrame);
            //this._lastWindowState = base.WindowState;
            managementFrame.LoadUI();
            ResumeLayout(false);
            PerformLayout();


            //managementFrame.LoadUI();

        }

        private void ClickInstallButton(ManagementFrameFull managementFrame)
        {
            var mainPage = managementFrame.GetProperty("MainPage");
            if (mainPage == null) return;

            var m = mainPage.GetMethod<EventHandler>("ButtonBar_InstallButtonClicked");
            if (m == null) return;

            DisableLaunchWebMatrix();

            try
            {
                m(mainPage, EventArgs.Empty);

                Close();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {

            }
        }

        private static void DisableLaunchWebMatrix()
        {
            Task.Factory.StartNew(delegate
            {
                Form wizard = null;

                for (var i = 0; i < 100; i++)
                {
                    wizard = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "InstallWizard");

                    if (wizard != null)
                    {
                        break;
                    }

                    Thread.CurrentThread.Join(100);
                }

                if (wizard == null)
                {
                    return;
                }

                var finishPage = wizard.GetProperty("FinishPage");

                if (finishPage != null)
                {
                    finishPage.SetField("_webMatrixLaunched", true);
                }
            });
        }
    }
}