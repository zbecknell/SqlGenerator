using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SqlGenerator.Model;
using SqlGenerator.ViewModel;

namespace SqlGenerator
{
    public partial class Generator
    {
        public ScriptResultList ScriptResults
        {
            get { return SqlGenerator.Properties.Settings.Default.ScriptResultList; }
            set { SqlGenerator.Properties.Settings.Default.ScriptResultList = value; }
        }

        public string Server
        {
            get { return (string)ServerCombo.SelectedItem; }
        }

        public string ConnectionString
        {
            get
            {
                return string.Format(@"Data Source={0};Integrated Security=True", Server);
            }
        }

        private CodeGenerator _CodeGenerator;

        private string Query { get { return SqlEditor.Text; } }

        public Generator()
        {
            InitializeComponent();

            PriorQueryCombo.DropDownClosed += PriorQueryComboOnDropDownClosed;

            if (ScriptResults != null && ScriptResults.Any())
            {
                PriorQueryCombo.ItemsSource = ScriptResults.OrderByDescending(r => r.ScriptTime);

                var result = ScriptResults.OrderByDescending(r => r.ScriptTime).FirstOrDefault();

                if (result != null)
                {
                    PriorQueryCombo.SelectedItem = result;
                    SqlEditor.Text = result.InputScript;

                    SqlOutput.Text = result.OutputSql;
                    CSharpOutput.Text = result.OutputCSharp;
                    ServerCombo.Text = result.Server;
                }
            }
            else
            {
                SqlEditor.Text = "SELECT TOP 100 * FROM Nutshell.dbo.Customer -- Example Script";
            }


        }

        private void PriorQueryComboOnDropDownClosed(object sender, EventArgs eventArgs)
        {
            var selectedScript = PriorQueryCombo.SelectedItem as ScriptResult;

            if (selectedScript == null) return;

            SqlEditor.Text = selectedScript.InputScript;
            SqlOutput.Text = selectedScript.OutputSql;
            CSharpOutput.Text = selectedScript.OutputCSharp;
            ServerCombo.Text = selectedScript.Server;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlOutput.Text = "Generating Script...";
                CSharpOutput.Text = "Generating Script...";

                _CodeGenerator = new CodeGenerator(ConnectionString);

                await _CodeGenerator.GenerateQuerySchema(Query);

                var sqlResult = await _CodeGenerator.GetSqlScript();
                var cSharpResult = await _CodeGenerator.GetPocoScript();

                SqlOutput.Text = sqlResult;
                CSharpOutput.Text = cSharpResult;

                if (ScriptResults == null)
                    ScriptResults = new ScriptResultList();

                var lastResult = ScriptResults.FirstOrDefault(r => r.InputScript == Query);
                ScriptResult newResult = new ScriptResult();

                // If there already is a matching result, update the run time
                if (lastResult != null)
                {
                    lastResult.ScriptTime = DateTime.Now;
                }
                else
                {
                    newResult.Server = Server;
                    newResult.InputScript = Query;
                    newResult.OutputCSharp = cSharpResult;
                    newResult.OutputSql = sqlResult;
                    newResult.ScriptTime = DateTime.Now;

                    ScriptResults.CullList(newResult);

                    ScriptResults.Add(newResult);
                }

                PriorQueryCombo.ItemsSource = ScriptResults.OrderByDescending(r => r.ScriptTime);
                PriorQueryCombo.Text = (lastResult == null) ? newResult.ToString() : lastResult.ToString();

                SqlGenerator.Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                SqlOutput.Text = string.Format("/*{0}*/", ex.Message);
                CSharpOutput.Text = string.Format("/*{0}*/", ex.Message);
            }
        }

        private void ServerCombo_OnInitialized(object sender, EventArgs e)
        {
            List<string> serverList = new List<string> { @"(localdb)\MSSQLLocalDB" };

            ServerCombo.ItemsSource = serverList;

            ServerCombo.SelectedItem = SqlGenerator.Properties.Settings.Default.Server ?? @"(localdb)\MSSQLLocalDB";
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            ScriptResults.Clear();
            SqlGenerator.Properties.Settings.Default.Save();

            PriorQueryCombo.ItemsSource = null;
        }
    }
}



