using System.ComponentModel;
using System.Configuration;

namespace MedPlot
{
    [RunInstaller(true)]
    public partial class MedPlotInstallerClass : System.Configuration.Install.Installer
    {
        public MedPlotInstallerClass()
        {
            InitializeComponent();
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            //get Configuration section 
            //name from custom action parameter
            string sectionName = this.Context.Parameters["sectionName"];

            //get Protected Configuration Provider 
            //name from custom action parameter
            string provName = this.Context.Parameters["provName"];

            // get the exe path from the default context parameters
            string exeFilePath = this.Context.Parameters["assemblypath"];

            //encrypt the configuration section
            ProtectSection(sectionName, provName, exeFilePath);
        }

        private void ProtectSection(string sectionName,
                     string provName, string exeFilePath)
        {
            Configuration config =
              ConfigurationManager.OpenExeConfiguration(exeFilePath);
            ConfigurationSection section = config.GetSection(sectionName);

            if (!section.SectionInformation.IsProtected)
            {
                //Protecting the specified section with the specified provider
                section.SectionInformation.ProtectSection(provName);
            }
            section.SectionInformation.ForceSave = true;
            config.Save(ConfigurationSaveMode.Modified);
        }
    }
}
