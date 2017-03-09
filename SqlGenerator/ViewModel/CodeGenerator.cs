using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlGenerator.Model;

namespace SqlGenerator.ViewModel
{
    public class CodeGenerator
    {
        DataTable SchemaTable { get; set; }
        private List<Column> Columns { get; set; }
        public string ConnectionString { get; set; }
        public string Query { get; set; }

        public string ObjectName { get; set; }
        public string SchemaName { get; set; }
        public string FullName { get { return string.Format("[{0}].[{1}]", SchemaName, ObjectName); } }

        public CodeGenerator(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private async Task<DataTable> GetSchemaTable(string sqlObject)
        {
            if (sqlObject == null) throw new ArgumentNullException("sqlObject");

            //using (ImpersonatedUser.GetImpersonationContext(true, ConnectionString))
            //{
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    command.CommandText = sqlObject;

                    command.CommandTimeout = 960;
                    command.CommandType = CommandType.Text;
                    command.Connection = connection;

                    connection.Open();

                    SqlDataReader reader = await command.ExecuteReaderAsync();

                    DataTable schemaTable = reader.GetSchemaTable();

                    connection.Close();

                    return schemaTable;
                }
            }
            //}

        }

        public async Task<string> GetPocoScript()
        {
            string script = await GetPocoCreationScript() + "\n\n";

            return script;
        }

        private async Task<string> GetPocoUpdateScript()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("\n\tpublic void Update(SqlLink link)\n\t{");

            sb.Append(string.Format("\n\t\tlink.ExecuteNonQueryStoredProcedure(\"{0}.upd{1}\", new \n\t\t{{\n", SchemaName, ObjectName));

            foreach (Column column in Columns)
            {
                sb.Append("\t\t\t" + column.ColumnName + ",\n");
            }

            string result = sb.ToString().TrimEnd(',', '\n') + "\n\t\t});\n\t}";

            return result;
        }

        private async Task<string> GetPocoCreationScript()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format("public class {0}{1}{{{2}", ObjectName, Environment.NewLine, Environment.NewLine));

            foreach (Column column in Columns)
            {
                sb.Append(column.PocoPropertyScript());
            }

            sb.Append(await GetPocoUpdateScript());

            sb.Append("\n}");

            return sb.ToString();
        }

        public async Task GenerateQuerySchema(string query)
        {
            SchemaTable = await GetSchemaTable(query);
            Columns = await GenerateColumnObjects(SchemaTable);

            string schema = string.Empty;

            if (query.Contains('.'))
            {
                schema = query.Remove(query.LastIndexOf('.'));
                schema = schema.Substring(schema.LastIndexOf('.') + 1);
                query = query.Substring(query.LastIndexOf('.') + 1);
            }

            if (query.Contains('('))
                query = query.Remove(query.IndexOf('('));

            if (query.Contains(' '))
                query = query.Remove(query.IndexOf(' '));

            if (query.StartsWith("sel"))
                query = query.TrimStart('s', 'e', 'l');

            if (query.StartsWith("tfn"))
                query = query.TrimStart('t', 'f', 'n');

            if (query.EndsWith("s"))
                query = query.TrimEnd('s');

            SchemaName = schema;
            ObjectName = query;
            Query = query;
        }

        public async Task<string> GetSqlScript()
        {
            string script = await GetSqlCreateScript() + "\n\n";

            script += await GetSqlUpdateScript();

            return script;
        }

        public async Task<string> GetSqlCreateScript()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("/*** Table Declaration ***/\n");

            sb.Append(string.Format("CREATE TABLE #tmp{0}{1}({2}", ObjectName, Environment.NewLine, Environment.NewLine));

            foreach (Column column in Columns)
            {
                sb.Append(column.SqlField);
            }

            string result = sb.ToString().TrimEnd(',', '\n');

            result += "\n)";

            return result;
        }

        private static async Task<List<Column>> GenerateColumnObjects(DataTable schemaTable)
        {
            return await Task.Run(() =>
            {
                List<Column> columns = (from DataRow row in schemaTable.Rows select new Column(row)).ToList();

                return columns;
            });
        }

        public async Task<string> GetSqlUpdateScript()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("/*** Update Procedure Declaration ***/\n");

            sb.Append(string.Format("CREATE PROCEDURE {0}.upd{1}{2}({3}", SchemaName, ObjectName, Environment.NewLine, Environment.NewLine));

            foreach (Column column in Columns)
            {
                sb.Append(column.SqlParameter);
            }

            string result = sb.ToString().TrimEnd(',', '\n');

            string ifNewExpression;

            if (Columns.Count(c => c.IsIdentity) == 1)
            {
                var identity = Columns.Single(c => c.IsIdentity);

                ifNewExpression = string.Format("IF [{0}] IS NULL --NEW RECORD", identity.ColumnName);
            }
            else
            {
                ifNewExpression = "--IF NEW EXPRESSION";
            }

            result += string.Format("\n)\nAS\n\nBEGIN\n\n{0}\nBEGIN\n\n\tINSERT INTO {1}\n\t(\n", ifNewExpression, FullName);

            foreach (Column column in Columns)
            {
                if (column.IsIdentity) continue;
                result += "\t\t[" + column.ColumnName + "],\n";
            }

            result = result.TrimEnd(',', '\n');

            result += "\n\t)\n\tVALUES\n\t(\n";

            foreach (Column column in Columns)
            {
                if (column.IsIdentity) continue;
                result += column.SqlInsertField;
            }

            result = result.TrimEnd(',', '\n');

            result += "\n\t)\n\nEND\nELSE -- UPDATE EXISTING\nBEGIN\n\n";

            result += string.Format("\tUPDATE {0}\n\tSET\n", FullName);

            foreach (Column column in Columns)
            {
                if (column.IsIdentity) continue;
                result += column.SqlUpdateField;
            }

            result = result.TrimEnd(',', '\n');

            if (Columns.Count(c => c.IsIdentity) == 1)
            {
                var identity = Columns.Single(c => c.IsIdentity);

                result += string.Format("\n\tWHERE [{0}] = @{0}\n\nEND\n", identity.ColumnName);
            }
            else
            {
                result += "\n\tWHERE -- EXISTING RECORD FILTER HERE\n\nEND\n";
            }

            result += "\nEND";

            return result;
        }
    }
}
